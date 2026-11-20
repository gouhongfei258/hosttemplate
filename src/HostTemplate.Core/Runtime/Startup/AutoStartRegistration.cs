using System;

namespace HostTemplate.Core.Runtime.Startup
{
    /// <summary>
    /// 当前用户的「登录后启动」项。Windows 实现写注册表，测试用内存实现。
    /// </summary>
    public interface IAutoStartStore
    {
        string Get(string name);

        void Set(string name, string command);

        bool Remove(string name);
    }

    /// <summary>
    /// 一次登记或取消开机自启的结果。
    /// </summary>
    public readonly struct AutoStartResult
    {
        public AutoStartResult(bool success, bool enabled, string command, string error, bool changed)
        {
            Success = success;
            Enabled = enabled;
            Command = command ?? string.Empty;
            Error = error ?? string.Empty;
            Changed = changed;
        }

        public bool Success { get; }

        public bool Enabled { get; }

        public string Command { get; }

        public string Error { get; }

        /// <summary>启动项这次有没有被写上或删掉。</summary>
        public bool Changed { get; }

        public static AutoStartResult Ok(bool enabled, string command, bool changed)
        {
            return new AutoStartResult(true, enabled, command, string.Empty, changed);
        }

        public static AutoStartResult Fail(string error)
        {
            return new AutoStartResult(false, false, string.Empty, error, false);
        }
    }

    /// <summary>
    /// 开机自启的路径选择和启动项写入。不直接碰注册表，方便在没有 Windows 登录会话时测试。
    /// 已安装的程序优先登记 Velopack 的 current 目录，更新后这个路径不变；调试运行登记正在跑的程序。
    /// </summary>
    public static class AutoStartRegistration
    {
        public const string RunValueName = "HostTemplate";

        public const string CurrentFolderName = "current";

        public static string ResolveExecutable(
            string processPath,
            string rootAppDir,
            string installedVersion,
            bool isPortable,
            Func<string, bool> fileExists)
        {
            if (string.IsNullOrWhiteSpace(processPath))
            {
                return null;
            }

            var running = processPath.Trim().Trim('"');
            var installed = !isPortable
                && !string.IsNullOrWhiteSpace(installedVersion)
                && !string.IsNullOrWhiteSpace(rootAppDir);

            if (!installed)
            {
                return running;
            }

            var stable = CombineCurrent(rootAppDir, running);
            if (fileExists == null || fileExists(stable))
            {
                return stable;
            }

            return running;
        }

        public static string QuoteCommand(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                return string.Empty;
            }

            var path = executablePath.Trim().Trim('"');
            return "\"" + path + "\"";
        }

        public static AutoStartResult Apply(bool enabled, string executablePath, IAutoStartStore store)
        {
            if (store == null)
            {
                return AutoStartResult.Fail("启动项存储未提供");
            }

            try
            {
                if (!enabled)
                {
                    var existed = !string.IsNullOrEmpty(store.Get(RunValueName));
                    if (existed)
                    {
                        store.Remove(RunValueName);
                    }

                    return AutoStartResult.Ok(false, string.Empty, existed);
                }

                if (string.IsNullOrWhiteSpace(executablePath))
                {
                    return AutoStartResult.Fail("启动程序路径为空");
                }

                var command = QuoteCommand(executablePath);
                var previous = store.Get(RunValueName);
                if (!string.Equals(previous, command, StringComparison.Ordinal))
                {
                    store.Set(RunValueName, command);
                }

                return AutoStartResult.Ok(true, command, !string.Equals(previous, command, StringComparison.Ordinal));
            }
            catch (Exception ex)
            {
                return AutoStartResult.Fail(ex.Message);
            }
        }

        public static string CombineCurrent(string rootAppDir, string processPath)
        {
            var exe = FileNameOf(processPath);
            var root = rootAppDir.Trim().TrimEnd('\\', '/');
            var separator = root.IndexOf('\\') >= 0 ? '\\' : System.IO.Path.DirectorySeparatorChar;
            return root + separator + CurrentFolderName + separator + exe;
        }

        private static string FileNameOf(string path)
        {
            var trimmed = path.Trim().Trim('"');
            var slash = Math.Max(trimmed.LastIndexOf('\\'), trimmed.LastIndexOf('/'));
            return slash >= 0 ? trimmed.Substring(slash + 1) : trimmed;
        }
    }
}
