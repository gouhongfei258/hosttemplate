using System;
using System.IO;
using System.Xml.Serialization;

namespace HostTemplate.Core.Configuration
{
    /// <summary>
    /// 放在程序旁边的引导文件。真正的工站配置不在这里，只记录去哪读、向哪台服务要。
    /// </summary>
    [XmlRoot("Bootstrap")]
    public sealed class BootstrapConfig
    {
        /// <summary>工站配置目录。app.xml / plc.xml / api.xml / users.xml 都在这里。</summary>
        public string ConfigDirectory { get; set; } = string.Empty;

        /// <summary>更新与配置服务的基地址，例如 http://10.0.0.8:5080。留空则只使用本地配置。</summary>
        public string ServiceBaseUrl { get; set; } = string.Empty;

        /// <summary>向服务要配置和版本时使用的工站编号。留空则回落到 app.xml 的工站号。</summary>
        public string StationId { get; set; } = string.Empty;

        /// <summary>可选。服务开启令牌校验时放到请求头 X-Host-Token。本地不设令牌就留空。</summary>
        public string ServiceToken { get; set; } = string.Empty;

        /// <summary>日志目录。不要放进会被版本更新替换掉的程序目录。</summary>
        public string LogDirectory { get; set; } = string.Empty;

        public static string DefaultConfigDirectory()
        {
            return Path.Combine(StableRoot(), "config");
        }

        public static string DefaultLogDirectory()
        {
            return Path.Combine(StableRoot(), "Logs");
        }

        public static string StableRoot()
        {
            var common = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (string.IsNullOrWhiteSpace(common))
            {
                common = Path.GetTempPath();
            }

            return Path.Combine(common, "HostTemplate");
        }

        public string ResolveStationId(string equId)
        {
            if (!string.IsNullOrWhiteSpace(StationId))
            {
                return StationId.Trim();
            }

            return string.IsNullOrWhiteSpace(equId) ? string.Empty : equId.Trim();
        }

        /// <summary>补齐空字段。返回 true 表示改过对象，调用方可以落盘。</summary>
        public bool ApplyDefaults()
        {
            var changed = false;

            if (string.IsNullOrWhiteSpace(ConfigDirectory))
            {
                ConfigDirectory = DefaultConfigDirectory();
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(LogDirectory))
            {
                LogDirectory = DefaultLogDirectory();
                changed = true;
            }

            var url = (ServiceBaseUrl ?? string.Empty).Trim().TrimEnd('/');
            if (!string.Equals(url, ServiceBaseUrl, StringComparison.Ordinal))
            {
                ServiceBaseUrl = url;
                changed = true;
            }

            var station = (StationId ?? string.Empty).Trim();
            if (!string.Equals(station, StationId, StringComparison.Ordinal))
            {
                StationId = station;
                changed = true;
            }

            if (ServiceToken == null)
            {
                ServiceToken = string.Empty;
                changed = true;
            }

            if (!Path.IsPathRooted(ConfigDirectory))
            {
                ConfigDirectory = Path.GetFullPath(ConfigDirectory);
                changed = true;
            }

            if (!Path.IsPathRooted(LogDirectory))
            {
                LogDirectory = Path.GetFullPath(LogDirectory);
                changed = true;
            }

            return changed;
        }
    }
}
