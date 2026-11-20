using System;
using System.Threading;
using System.Threading.Tasks;
using HostTemplate.Contracts;
using HostTemplate.Core.Configuration.Loading;
using HostTemplate.Core.Runtime.State;

namespace HostTemplate.Core.Updates
{
    public interface IUpdateInstaller
    {
        bool IsInstalled { get; }

        Task DownloadAsync(TargetVersion version, CancellationToken cancellationToken);

        void ApplyAndRestart(TargetVersion version);
    }

    public enum UpdateStatus
    {
        Noop = 0,
        Cancelled = 1,
        UpToDate = 2,
        Deferred = 3,
        Applied = 4,
        Failed = 5,
        Ready = 6,
    }

    public sealed class UpdateActionResult
    {
        public UpdateStatus Status { get; set; }

        public string Message { get; set; } = string.Empty;

        public string Version { get; set; } = string.Empty;

        public static UpdateActionResult Of(UpdateStatus status, string message, string version = null)
        {
            return new UpdateActionResult
            {
                Status = status,
                Message = message ?? string.Empty,
                Version = version ?? string.Empty,
            };
        }
    }

    /// <summary>
    /// 手动选版本、自动更新、以及「只在空闲时安装」都在这里。界面只负责确认和展示。
    /// </summary>
    public sealed class StationUpdateController
    {
        private readonly Func<string> _stationId;
        private readonly Func<bool> _autoUpdateEnabled;
        private readonly Func<string> _currentVersion;
        private readonly IStationServiceClient _client;
        private readonly IStationIdle _idle;
        private readonly IUpdateInstaller _installer;
        private readonly string _serviceBaseUrl;
        private readonly object _gate = new object();
        private readonly TimeSpan _autoInterval;

        private TargetVersion _pending;
        private bool _deferredAnnounced;
        private bool _busy;
        private bool _autoInstallNoticeSent;
        private DateTime _lastAutoUtc = DateTime.MinValue;

        public StationUpdateController(
            string serviceBaseUrl,
            Func<string> stationId,
            Func<bool> autoUpdateEnabled,
            Func<string> currentVersion,
            IStationServiceClient client,
            IStationIdle idle,
            IUpdateInstaller installer,
            TimeSpan? autoInterval = null)
        {
            _serviceBaseUrl = (serviceBaseUrl ?? string.Empty).Trim().TrimEnd('/');
            _stationId = stationId ?? throw new ArgumentNullException(nameof(stationId));
            _autoUpdateEnabled = autoUpdateEnabled ?? throw new ArgumentNullException(nameof(autoUpdateEnabled));
            _currentVersion = currentVersion ?? throw new ArgumentNullException(nameof(currentVersion));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _idle = idle ?? throw new ArgumentNullException(nameof(idle));
            _installer = installer ?? throw new ArgumentNullException(nameof(installer));
            _autoInterval = autoInterval ?? TimeSpan.FromSeconds(60);
        }

        public string ServiceBaseUrl => _serviceBaseUrl;

        public async Task<UpdateActionResult> PullConfigAsync(
            bool confirmed,
            string configDirectory,
            Func<Task> reloadAsync,
            CancellationToken cancellationToken)
        {
            if (!confirmed)
            {
                return UpdateActionResult.Of(UpdateStatus.Cancelled, "已取消。本地配置未改动。");
            }

            var configured = EnsureConfigured();
            if (configured != null)
            {
                return configured;
            }

            ConfigPackage package;
            try
            {
                package = await _client.GetConfigAsync(RequireStationId(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return UpdateActionResult.Of(UpdateStatus.Failed, "下载远端配置失败：" + ex.Message);
            }

            Configuration.Loading.RemoteConfigSyncResult applied;
            try
            {
                applied = RemoteConfigSyncApply(package, configDirectory);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return UpdateActionResult.Of(UpdateStatus.Failed, "写入本地配置失败：" + ex.Message);
            }

            if (!applied.Applied)
            {
                return UpdateActionResult.Of(
                    applied.Cancelled ? UpdateStatus.Cancelled : UpdateStatus.Failed,
                    applied.Message);
            }

            if (reloadAsync != null)
            {
                await reloadAsync().ConfigureAwait(false);
            }

            return UpdateActionResult.Of(UpdateStatus.Ready, "已用远端配置覆盖本地配置并重新加载。");
        }

        public async Task<VersionListResponse> ListVersionsAsync(CancellationToken cancellationToken)
        {
            var configured = EnsureConfigured();
            if (configured != null)
            {
                throw new InvalidOperationException(configured.Message);
            }

            return await _client.GetVersionsAsync(RequireStationId(), cancellationToken).ConfigureAwait(false);
        }

        public async Task<UpdateActionResult> InstallAsync(string version, bool confirmed, CancellationToken cancellationToken)
        {
            if (!confirmed)
            {
                return UpdateActionResult.Of(UpdateStatus.Cancelled, "已取消。未下载、未安装。");
            }

            var configured = EnsureConfigured();
            if (configured != null)
            {
                return configured;
            }

            if (string.IsNullOrWhiteSpace(version))
            {
                return UpdateActionResult.Of(UpdateStatus.Failed, "未选择版本。");
            }

            VersionListResponse catalog;
            try
            {
                catalog = await _client.GetVersionsAsync(RequireStationId(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return UpdateActionResult.Of(UpdateStatus.Failed, "读取版本列表失败：" + ex.Message);
            }

            if (!VersionSelection.TryPick(catalog, version, _currentVersion(), out var picked, out var error))
            {
                var status = error != null && error.IndexOf("已经是", StringComparison.Ordinal) >= 0
                    ? UpdateStatus.UpToDate
                    : UpdateStatus.Failed;
                return UpdateActionResult.Of(status, error);
            }

            return await DownloadAndStageAsync(picked, cancellationToken).ConfigureAwait(false);
        }

        public async Task<UpdateActionResult> TickAsync(CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                if (_busy)
                {
                    return UpdateActionResult.Of(UpdateStatus.Noop, string.Empty);
                }

                _busy = true;
            }

            try
            {
                var pending = ApplyPending(announce: false);
                if (pending.Status == UpdateStatus.Applied
                    || pending.Status == UpdateStatus.Deferred
                    || pending.Status == UpdateStatus.Failed)
                {
                    return pending;
                }

                if (_pending != null || !_autoUpdateEnabled() || string.IsNullOrWhiteSpace(_serviceBaseUrl))
                {
                    return UpdateActionResult.Of(UpdateStatus.Noop, string.Empty);
                }

                var stationId = _stationId();
                if (string.IsNullOrWhiteSpace(stationId))
                {
                    return UpdateActionResult.Of(UpdateStatus.Noop, string.Empty);
                }

                if (DateTime.UtcNow - _lastAutoUtc < _autoInterval)
                {
                    return UpdateActionResult.Of(UpdateStatus.Noop, string.Empty);
                }

                _lastAutoUtc = DateTime.UtcNow;

                TargetVersion latest;
                try
                {
                    latest = await _client.GetLatestAsync(stationId, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    return UpdateActionResult.Of(UpdateStatus.Failed, "查询目标版本失败：" + ex.Message);
                }

                if (!VersionSelection.ShouldAutoDownload(latest, _currentVersion()))
                {
                    return UpdateActionResult.Of(UpdateStatus.Noop, string.Empty);
                }

                if (!_installer.IsInstalled)
                {
                    if (_autoInstallNoticeSent)
                    {
                        return UpdateActionResult.Of(UpdateStatus.Noop, string.Empty);
                    }

                    _autoInstallNoticeSent = true;
                    return UpdateActionResult.Of(
                        UpdateStatus.Failed,
                        "当前程序不是 Velopack 安装包，不能替换程序文件。请用 vpk 安装后再更新。",
                        latest.Version);
                }

                return await DownloadAndStageAsync(latest, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                lock (_gate)
                {
                    _busy = false;
                }
            }
        }

        private async Task<UpdateActionResult> DownloadAndStageAsync(TargetVersion version, CancellationToken cancellationToken)
        {
            if (!_installer.IsInstalled)
            {
                return UpdateActionResult.Of(
                    UpdateStatus.Failed,
                    "当前程序不是 Velopack 安装包，不能替换程序文件。请用 vpk 安装后再更新。",
                    version.Version);
            }

            try
            {
                await _installer.DownloadAsync(version, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return UpdateActionResult.Of(UpdateStatus.Failed, "下载版本 " + version.Version + " 失败：" + ex.Message, version.Version);
            }

            lock (_gate)
            {
                _pending = version;
                _deferredAnnounced = false;
            }

            return ApplyPending(announce: true);
        }

        private UpdateActionResult ApplyPending(bool announce)
        {
            TargetVersion pending;
            lock (_gate)
            {
                pending = _pending;
            }

            if (pending == null)
            {
                return UpdateActionResult.Of(UpdateStatus.Noop, string.Empty);
            }

            if (!_idle.IsIdle)
            {
                if (!announce)
                {
                    lock (_gate)
                    {
                        if (_deferredAnnounced)
                        {
                            return UpdateActionResult.Of(UpdateStatus.Noop, string.Empty);
                        }

                        _deferredAnnounced = true;
                    }
                }

                return UpdateActionResult.Of(
                    UpdateStatus.Deferred,
                    "工站正在生产，版本 " + pending.Version + " 已下载，将在空闲后安装并重启。",
                    pending.Version);
            }

            lock (_gate)
            {
                if (!ReferenceEquals(_pending, pending))
                {
                    return UpdateActionResult.Of(UpdateStatus.Noop, string.Empty);
                }

                _pending = null;
            }

            try
            {
                _installer.ApplyAndRestart(pending);
            }
            catch (Exception ex)
            {
                lock (_gate)
                {
                    _pending = pending;
                    _deferredAnnounced = false;
                }

                return UpdateActionResult.Of(UpdateStatus.Failed, "安装版本 " + pending.Version + " 失败：" + ex.Message, pending.Version);
            }

            return UpdateActionResult.Of(UpdateStatus.Applied, "正在安装版本 " + pending.Version + " 并重启。", pending.Version);
        }

        private static Configuration.Loading.RemoteConfigSyncResult RemoteConfigSyncApply(ConfigPackage package, string configDirectory)
        {
            return Configuration.Loading.RemoteConfigSync.Apply(true, package, configDirectory);
        }

        private UpdateActionResult EnsureConfigured()
        {
            if (string.IsNullOrWhiteSpace(_serviceBaseUrl))
            {
                return UpdateActionResult.Of(UpdateStatus.Failed, "未配置更新服务地址。请编辑程序目录下的 bootstrap.xml。");
            }

            if (string.IsNullOrWhiteSpace(_stationId()))
            {
                return UpdateActionResult.Of(UpdateStatus.Failed, "未配置工站编号。请在 bootstrap.xml 填写 StationId。");
            }

            return null;
        }

        private string RequireStationId()
        {
            var stationId = _stationId();
            if (string.IsNullOrWhiteSpace(stationId))
            {
                throw new InvalidOperationException("未配置工站编号。");
            }

            return stationId.Trim();
        }
    }

    /// <summary>
    /// 版本选择。自动更新只看服务返回的目标版本，不自己在列表里取最大号。
    /// </summary>
    public static class VersionSelection
    {
        public static bool TryPick(
            VersionListResponse catalog,
            string version,
            string currentVersion,
            out TargetVersion picked,
            out string error)
        {
            picked = null;
            error = null;

            if (catalog == null || catalog.Versions == null)
            {
                error = "服务端没有可用版本。";
                return false;
            }

            foreach (var item in catalog.Versions)
            {
                if (item != null && string.Equals(item.Version, version, StringComparison.OrdinalIgnoreCase))
                {
                    picked = item;
                    break;
                }
            }

            if (picked == null)
            {
                error = "服务端没有版本 " + version + "。";
                return false;
            }

            if (AppVersion.Compare(picked.Version, currentVersion) == 0)
            {
                error = "当前已经是版本 " + currentVersion + "。";
                picked = null;
                return false;
            }

            return true;
        }

        public static bool ShouldAutoDownload(TargetVersion latest, string currentVersion)
        {
            return latest != null
                && !string.IsNullOrWhiteSpace(latest.Version)
                && AppVersion.IsNewer(latest.Version, currentVersion);
        }
    }
}
