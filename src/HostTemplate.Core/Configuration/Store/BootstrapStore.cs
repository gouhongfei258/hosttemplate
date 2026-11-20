using System;
using System.IO;

namespace HostTemplate.Core.Configuration.Store
{
    /// <summary>
    /// 读取程序目录下的 bootstrap.xml。
    /// 更新会换掉程序目录，所以每次成功读取后都镜像到稳定目录；程序目录里的文件被换掉时再从镜像恢复。
    /// </summary>
    public static class BootstrapStore
    {
        public const string FileName = "bootstrap.xml";

        public static string DefaultStablePath()
        {
            return Path.Combine(BootstrapConfig.StableRoot(), FileName);
        }

        public static BootstrapConfig LoadOrCreate(string exeDirectory, out string message)
        {
            return LoadOrCreate(exeDirectory, DefaultStablePath(), out message);
        }

        public static BootstrapConfig LoadOrCreate(string exeDirectory, string stablePath, out string message)
        {
            message = string.Empty;

            if (string.IsNullOrWhiteSpace(exeDirectory))
            {
                throw new ArgumentException("程序目录为空。", nameof(exeDirectory));
            }

            Directory.CreateDirectory(exeDirectory);
            var exePath = Path.Combine(exeDirectory, FileName);

            if (!File.Exists(exePath) && !string.IsNullOrWhiteSpace(stablePath) && File.Exists(stablePath))
            {
                File.Copy(stablePath, exePath, overwrite: false);
                message = "程序目录缺少 bootstrap.xml，已从稳定目录恢复。";
            }

            BootstrapConfig config;
            if (!File.Exists(exePath))
            {
                config = CreateDefault();
                config.ApplyDefaults();
                XmlConfigStore.Save(exePath, config, out _);
                message = Append(message, "已生成 bootstrap.xml。配置目录：" + config.ConfigDirectory);
            }
            else if (!XmlConfigStore.Load(exePath, out config, out var error) || config == null)
            {
                config = CreateDefault();
                config.ApplyDefaults();
                XmlConfigStore.Save(exePath, config, out _);
                message = Append(message, "bootstrap.xml 无法读取，已重建：" + error);
            }
            else if (config.ApplyDefaults())
            {
                XmlConfigStore.Save(exePath, config, out _);
            }

            Mirror(exePath, stablePath);
            return config;
        }

        public static BootstrapConfig CreateDefault()
        {
            return new BootstrapConfig
            {
                ConfigDirectory = BootstrapConfig.DefaultConfigDirectory(),
                LogDirectory = BootstrapConfig.DefaultLogDirectory(),
                ServiceBaseUrl = string.Empty,
                StationId = "OP010",
                ServiceToken = string.Empty,
            };
        }

        private static void Mirror(string exePath, string stablePath)
        {
            if (string.IsNullOrWhiteSpace(stablePath) || !File.Exists(exePath))
            {
                return;
            }

            try
            {
                var directory = Path.GetDirectoryName(stablePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Copy(exePath, stablePath, overwrite: true);
            }
            catch (IOException)
            {
                // 镜像失败不阻断启动，程序目录里的引导文件仍然可用。
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static string Append(string left, string right)
        {
            if (string.IsNullOrEmpty(left))
            {
                return right;
            }

            return left + "\r\n" + right;
        }
    }
}
