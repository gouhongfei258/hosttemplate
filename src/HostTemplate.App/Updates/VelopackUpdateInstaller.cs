using System;
using System.Threading;
using System.Threading.Tasks;
using HostTemplate.Contracts;
using HostTemplate.Core.Updates;
using Velopack;
using Velopack.Sources;

namespace HostTemplate.App.Updates
{
    /// <summary>
    /// 用 Velopack 下载并替换程序。服务只负责指出该装哪一个版本。
    /// </summary>
    public sealed class VelopackUpdateInstaller : IUpdateInstaller
    {
        private readonly string _token;
        private UpdateManager _manager;
        private VelopackAsset _asset;

        public VelopackUpdateInstaller(string token)
        {
            _token = token ?? string.Empty;
        }

        public bool IsInstalled
        {
            get
            {
                try
                {
                    var manager = new UpdateManager(new SimpleWebSource("http://127.0.0.1"), new UpdateOptions
                    {
                        ExplicitChannel = "win",
                    });
                    return manager.IsInstalled;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public async Task DownloadAsync(TargetVersion version, CancellationToken cancellationToken)
        {
            if (version == null || string.IsNullOrWhiteSpace(version.FeedUrl))
            {
                throw new InvalidOperationException("版本没有 feed 地址。");
            }

            _manager = new UpdateManager(
                new SimpleWebSource(version.FeedUrl, new TokenFileDownloader(_token)),
                new UpdateOptions
                {
                    AllowVersionDowngrade = true,
                    ExplicitChannel = "win",
                });

            if (!_manager.IsInstalled)
            {
                throw new InvalidOperationException("当前不是 Velopack 安装。");
            }

            var info = await _manager.CheckForUpdatesAsync().ConfigureAwait(false);
            if (info == null || info.TargetFullRelease == null)
            {
                throw new InvalidOperationException("feed 中没有可安装的版本。");
            }

            await _manager.DownloadUpdatesAsync(info, null, cancellationToken).ConfigureAwait(false);
            _asset = info.TargetFullRelease;
        }

        public void ApplyAndRestart(TargetVersion version)
        {
            if (_manager == null || _asset == null)
            {
                throw new InvalidOperationException("还没有下载安装包。");
            }

            _manager.ApplyUpdatesAndRestart(_asset);
        }
    }
}
