using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using HostTemplate.Core.Runtime.Startup;
using Velopack.Locators;

namespace HostTemplate.App.Startup
{
    /// <summary>
    /// 把开机自启写进当前用户的 Run 项：
    /// HKCU\Software\Microsoft\Windows\CurrentVersion\Run\HostTemplate。
    /// 不需要管理员权限，和 Velopack 的按用户安装一致。
    /// </summary>
    internal static class WindowsAutoStart
    {
        public const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public static AutoStartResult Apply(bool enabled)
        {
            return AutoStartRegistration.Apply(enabled, ResolveExecutable(), new RegistryRunKeyStore());
        }

        public static string ResolveExecutable()
        {
            var process = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(process))
            {
                try
                {
                    process = Process.GetCurrentProcess().MainModule?.FileName;
                }
                catch
                {
                    process = null;
                }
            }

            string root = null;
            string version = null;
            var portable = false;

            try
            {
                if (VelopackLocator.IsCurrentSet)
                {
                    var locator = VelopackLocator.Current;
                    root = locator.RootAppDir;
                    version = locator.CurrentlyInstalledVersion?.ToString();
                    portable = locator.IsPortable;
                }
            }
            catch
            {
                // 调试目录里没有 Velopack 清单时，按正在运行的程序登记。
            }

            return AutoStartRegistration.ResolveExecutable(process, root, version, portable, File.Exists);
        }

        private sealed class RegistryRunKeyStore : IAutoStartStore
        {
            public string Get(string name)
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false))
                {
                    return key?.GetValue(name) as string;
                }
            }

            public void Set(string name, string command)
            {
                using (var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true))
                {
                    if (key == null)
                    {
                        throw new InvalidOperationException("无法打开当前用户的启动项。");
                    }

                    key.SetValue(name, command, RegistryValueKind.String);
                }
            }

            public bool Remove(string name)
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true))
                {
                    if (key == null || key.GetValue(name) == null)
                    {
                        return false;
                    }

                    key.DeleteValue(name, throwOnMissingValue: false);
                    return true;
                }
            }
        }
    }
}
