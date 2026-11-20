using System;
using System.IO;
using HostTemplate.Contracts;

namespace HostTemplate.Core.Configuration.Store
{
    /// <summary>
    /// 把随程序发布的模板写进配置目录。只补缺失文件，不覆盖现场已经有的配置。
    /// </summary>
    public static class ConfigTemplates
    {
        public static void WriteMissing(string templateDirectory, string configDirectory, string stationId, out string message)
        {
            message = string.Empty;

            if (string.IsNullOrWhiteSpace(configDirectory))
            {
                throw new ArgumentException("配置目录为空。", nameof(configDirectory));
            }

            Directory.CreateDirectory(configDirectory);
            var wroteApp = false;

            foreach (var name in StationFiles.Required)
            {
                var destination = Path.Combine(configDirectory, name);
                if (File.Exists(destination) && new FileInfo(destination).Length > 0)
                {
                    continue;
                }

                var shipped = string.IsNullOrWhiteSpace(templateDirectory)
                    ? null
                    : Path.Combine(templateDirectory, name);

                if (!string.IsNullOrEmpty(shipped) && File.Exists(shipped))
                {
                    File.Copy(shipped, destination, overwrite: true);
                    message += $"已从模板复制 {name}。\r\n";
                }
                else
                {
                    WriteCodeDefault(name, destination);
                    message += $"模板中没有 {name}，已写入程序内置默认值。\r\n";
                }

                if (string.Equals(name, StationFiles.App, StringComparison.OrdinalIgnoreCase))
                {
                    wroteApp = true;
                }
            }

            if (wroteApp && !string.IsNullOrWhiteSpace(stationId))
            {
                var appPath = Path.Combine(configDirectory, StationFiles.App);
                if (XmlConfigStore.Load(appPath, out AppConfig app, out _) && app != null)
                {
                    if (!string.Equals(app.EquId, stationId, StringComparison.Ordinal))
                    {
                        app.EquId = stationId;
                        XmlConfigStore.Save(appPath, app, out _);
                        message += $"已把工站号写成引导文件中的 {stationId}。\r\n";
                    }
                }
            }
        }

        public static void ExportDefaults(string directory)
        {
            Directory.CreateDirectory(directory);
            WriteCodeDefault(StationFiles.App, Path.Combine(directory, StationFiles.App));
            WriteCodeDefault(StationFiles.Plc, Path.Combine(directory, StationFiles.Plc));
            WriteCodeDefault(StationFiles.Api, Path.Combine(directory, StationFiles.Api));
        }

        private static void WriteCodeDefault(string name, string destination)
        {
            if (string.Equals(name, StationFiles.App, StringComparison.OrdinalIgnoreCase))
            {
                XmlConfigStore.Save(destination, ConfigManager.CreateDefaultApp(), out _);
                return;
            }

            if (string.Equals(name, StationFiles.Plc, StringComparison.OrdinalIgnoreCase))
            {
                XmlConfigStore.Save(destination, ConfigManager.CreateDefaultPlc(), out _);
                return;
            }

            if (string.Equals(name, StationFiles.Api, StringComparison.OrdinalIgnoreCase))
            {
                XmlConfigStore.Save(destination, ConfigManager.CreateDefaultApi(), out _);
            }
        }
    }
}
