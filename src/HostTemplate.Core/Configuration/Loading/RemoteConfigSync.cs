using System;
using HostTemplate.Contracts;

namespace HostTemplate.Core.Configuration.Loading
{
    public sealed class RemoteConfigSyncResult
    {
        public bool Applied { get; set; }

        public bool Cancelled { get; set; }

        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 远端配置覆盖本地。未确认时直接返回，不读也不写本地文件。
    /// </summary>
    public static class RemoteConfigSync
    {
        public static RemoteConfigSyncResult Apply(bool confirmed, ConfigPackage package, string configDirectory)
        {
            if (!confirmed)
            {
                return new RemoteConfigSyncResult
                {
                    Applied = false,
                    Cancelled = true,
                    Message = "已取消。本地配置未改动。",
                };
            }

            if (package == null || !package.HasRequiredFiles())
            {
                return new RemoteConfigSyncResult
                {
                    Applied = false,
                    Message = "远端没有完整的工站配置，本地配置未改动。",
                };
            }

            if (string.IsNullOrWhiteSpace(configDirectory))
            {
                return new RemoteConfigSyncResult
                {
                    Applied = false,
                    Message = "本地配置目录为空，已取消写入。",
                };
            }

            package.WriteToDirectory(configDirectory, overwrite: true);
            return new RemoteConfigSyncResult
            {
                Applied = true,
                Message = "已用远端配置覆盖本地配置。",
            };
        }
    }
}
