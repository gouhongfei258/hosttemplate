using System;
using System.Threading;
using System.Threading.Tasks;
using HostTemplate.Contracts;
using HostTemplate.Core.Updates;

namespace HostTemplate.Core.Configuration
{
    public enum ConfigLoadSource
    {
        Local = 0,
        Remote = 1,
        Template = 2,
    }

    public sealed class ConfigLoadResult
    {
        public ConfigManager Manager { get; set; }

        public ConfigLoadSource Source { get; set; }

        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 启动时的配置加载顺序：本地完整配置 → 远端工站包 → 随程序发布的模板。
    /// 本地已经齐全时不会向服务要配置，也不会覆盖。
    /// </summary>
    public static class StationConfigLoader
    {
        public static async Task<ConfigLoadResult> ResolveAsync(
            BootstrapConfig bootstrap,
            string templateDirectory,
            IStationServiceClient client,
            CancellationToken cancellationToken)
        {
            if (bootstrap == null)
            {
                throw new ArgumentNullException(nameof(bootstrap));
            }

            bootstrap.ApplyDefaults();
            var directory = bootstrap.ConfigDirectory;
            var manager = new ConfigManager(directory);
            var message = string.Empty;

            if (manager.HasRequiredFiles())
            {
                manager.Load(out var loadMessage);
                return new ConfigLoadResult
                {
                    Manager = manager,
                    Source = ConfigLoadSource.Local,
                    Message = loadMessage ?? string.Empty,
                };
            }

            message += "本地配置不完整。\r\n";
            var remote = await TryRemoteAsync(bootstrap, directory, client, cancellationToken).ConfigureAwait(false);
            if (remote.Ok)
            {
                manager.Load(out var loadMessage);
                return new ConfigLoadResult
                {
                    Manager = manager,
                    Source = ConfigLoadSource.Remote,
                    Message = message + remote.Message + (loadMessage ?? string.Empty),
                };
            }

            message += remote.Message;
            ConfigTemplates.WriteMissing(templateDirectory, directory, bootstrap.StationId, out var templateMessage);
            manager.Load(out var templateLoadMessage);

            return new ConfigLoadResult
            {
                Manager = manager,
                Source = ConfigLoadSource.Template,
                Message = message + templateMessage + (templateLoadMessage ?? string.Empty),
            };
        }

        private static async Task<RemoteAttempt> TryRemoteAsync(
            BootstrapConfig bootstrap,
            string directory,
            IStationServiceClient client,
            CancellationToken cancellationToken)
        {
            if (client == null || string.IsNullOrWhiteSpace(bootstrap.ServiceBaseUrl))
            {
                return RemoteAttempt.Fail("未配置更新服务地址，跳过远端配置。\r\n");
            }

            var stationId = bootstrap.ResolveStationId(null);
            if (string.IsNullOrWhiteSpace(stationId))
            {
                return RemoteAttempt.Fail("引导文件未填写工站编号，跳过远端配置。\r\n");
            }

            try
            {
                var package = await client.GetConfigAsync(stationId, cancellationToken).ConfigureAwait(false);
                var applied = RemoteConfigSync.Apply(confirmed: true, package, directory);
                if (!applied.Applied)
                {
                    return RemoteAttempt.Fail(applied.Message + "\r\n");
                }

                return RemoteAttempt.Success("已从服务写入工站配置。\r\n");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return RemoteAttempt.Fail("拉取远端配置失败：" + ex.Message + "\r\n");
            }
        }

        private readonly struct RemoteAttempt
        {
            public RemoteAttempt(bool ok, string message)
            {
                Ok = ok;
                Message = message ?? string.Empty;
            }

            public bool Ok { get; }

            public string Message { get; }

            public static RemoteAttempt Success(string message)
            {
                return new RemoteAttempt(true, message);
            }

            public static RemoteAttempt Fail(string message)
            {
                return new RemoteAttempt(false, message);
            }
        }
    }
}
