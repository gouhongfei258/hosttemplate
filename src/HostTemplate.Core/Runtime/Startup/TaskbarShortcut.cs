using System;
using System.Collections.Generic;

namespace HostTemplate.Core.Runtime.Startup
{
    /// <summary>
    /// 一个已经存在或即将写入的快捷方式。测试用内存目录，Windows 上对应一个 .lnk。
    /// </summary>
    public sealed class ShortcutLink
    {
        public string FileName { get; set; }

        public string TargetPath { get; set; }

        public string IconPath { get; set; }

        public string WorkingDirectory { get; set; }

        public string Description { get; set; }
    }

    /// <summary>
    /// 快捷方式所在的目录。Windows 实现读写 .lnk，测试用内存实现，不需要资源管理器任务栏。
    /// </summary>
    public interface IShortcutCatalog
    {
        IReadOnlyList<ShortcutLink> List();

        void Write(ShortcutLink link);

        string PathOf(string fileName);
    }

    /// <summary>
    /// 一次确保任务栏快捷方式存在的结果。
    /// </summary>
    public readonly struct ShortcutEnsureResult
    {
        public ShortcutEnsureResult(
            bool success,
            bool skipped,
            bool shouldPin,
            string pinPath,
            string target,
            string error,
            string detail)
        {
            Success = success;
            Skipped = skipped;
            ShouldPin = shouldPin;
            PinPath = pinPath ?? string.Empty;
            Target = target ?? string.Empty;
            Error = error ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public bool Success { get; }

        /// <summary>任务栏里已经有指向当前程序的快捷方式，这次没有重写它。</summary>
        public bool Skipped { get; }

        /// <summary>这次新写了一个任务栏快捷方式，调用方可以再请求系统把它钉住。</summary>
        public bool ShouldPin { get; }

        public string PinPath { get; }

        public string Target { get; }

        public string Error { get; }

        public string Detail { get; }

        public static ShortcutEnsureResult Fail(string error)
        {
            return new ShortcutEnsureResult(false, false, false, string.Empty, string.Empty, error, string.Empty);
        }
    }

    /// <summary>
    /// 决定任务栏快捷方式是跳过、新建还是把旧路径改到当前程序。
    /// 不调用外壳钉住接口，方便在没有 Windows 任务栏会话时测试。
    /// 已安装的程序使用和开机自启相同的 current 目录，更新后快捷方式不用改。
    /// </summary>
    public static class TaskbarShortcut
    {
        public const string LinkFileName = "HostTemplate.lnk";

        public const string Description = "十四事业部装备中心数采系统";

        public static string ResolveIconPath(string executablePath, string iconFileName, Func<string, bool> exists)
        {
            var executable = Clean(executablePath);
            if (executable.Length == 0 || string.IsNullOrWhiteSpace(iconFileName))
            {
                return executable;
            }

            var directory = DirectoryOf(executable);
            if (directory.Length == 0)
            {
                return executable;
            }

            var separator = executable.IndexOf('\\') >= 0
                ? '\\'
                : executable.IndexOf('/') >= 0 ? '/' : System.IO.Path.DirectorySeparatorChar;
            var icon = directory + separator + iconFileName.Trim().Trim('"');
            if (exists != null && exists(icon))
            {
                return icon;
            }

            return executable;
        }

        public static ShortcutEnsureResult Ensure(
            string executablePath,
            string iconPath,
            IShortcutCatalog taskbar,
            IShortcutCatalog startMenu)
        {
            if (taskbar == null)
            {
                return ShortcutEnsureResult.Fail("任务栏快捷方式目录未提供");
            }

            var target = Clean(executablePath);
            if (target.Length == 0)
            {
                return ShortcutEnsureResult.Fail("启动程序路径为空");
            }

            var icon = Clean(iconPath);
            if (icon.Length == 0)
            {
                icon = target;
            }

            IReadOnlyList<ShortcutLink> existing;
            try
            {
                existing = taskbar.List();
            }
            catch (Exception ex)
            {
                return ShortcutEnsureResult.Fail(ex.Message);
            }

            var ours = Find(existing, target);
            var menuNote = TryEnsure(startMenu, target, icon, out var menuError);

            if (ours != null && SamePath(ours.TargetPath, target))
            {
                return new ShortcutEnsureResult(true, true, false, string.Empty, target, string.Empty, Join(menuNote, menuError));
            }

            var fileName = ours != null && !string.IsNullOrWhiteSpace(ours.FileName)
                ? ours.FileName.Trim()
                : LinkFileName;
            var link = CreateLink(fileName, target, icon);

            try
            {
                taskbar.Write(link);
            }
            catch (Exception ex)
            {
                return ShortcutEnsureResult.Fail(ex.Message);
            }

            var created = ours == null;
            var detail = created
                ? "已写入任务栏快捷方式。"
                : "已把任务栏快捷方式改到当前程序路径。";
            var pinPath = created ? taskbar.PathOf(fileName) : string.Empty;

            return new ShortcutEnsureResult(
                true,
                false,
                created,
                pinPath,
                target,
                string.Empty,
                Join(detail, Join(menuNote, menuError)));
        }

        public static bool SamePath(string left, string right)
        {
            return string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);
        }

        private static string TryEnsure(IShortcutCatalog catalog, string target, string icon, out string error)
        {
            error = string.Empty;
            if (catalog == null)
            {
                return string.Empty;
            }

            try
            {
                var existing = Find(catalog.List(), target);
                if (existing != null && SamePath(existing.TargetPath, target))
                {
                    return string.Empty;
                }

                var fileName = existing != null && !string.IsNullOrWhiteSpace(existing.FileName)
                    ? existing.FileName.Trim()
                    : LinkFileName;
                catalog.Write(CreateLink(fileName, target, icon));
                return "已写入开始菜单快捷方式。";
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return string.Empty;
            }
        }

        private static ShortcutLink CreateLink(string fileName, string target, string icon)
        {
            return new ShortcutLink
            {
                FileName = fileName,
                TargetPath = target,
                IconPath = icon,
                WorkingDirectory = DirectoryOf(target),
                Description = Description,
            };
        }

        private static ShortcutLink Find(IReadOnlyList<ShortcutLink> links, string target)
        {
            if (links == null)
            {
                return null;
            }

            var exeName = FileNameOf(target);
            ShortcutLink sameProgram = null;
            ShortcutLink reserved = null;

            foreach (var link in links)
            {
                if (link == null)
                {
                    continue;
                }

                if (SamePath(link.TargetPath, target))
                {
                    return link;
                }

                if (sameProgram == null
                    && exeName.Length > 0
                    && string.Equals(FileNameOf(link.TargetPath), exeName, StringComparison.OrdinalIgnoreCase))
                {
                    sameProgram = link;
                }

                if (reserved == null
                    && string.Equals(link.FileName, LinkFileName, StringComparison.OrdinalIgnoreCase))
                {
                    reserved = link;
                }
            }

            return sameProgram ?? reserved;
        }

        private static string Join(string left, string right)
        {
            if (string.IsNullOrEmpty(left))
            {
                return right ?? string.Empty;
            }

            if (string.IsNullOrEmpty(right))
            {
                return left;
            }

            return left + right;
        }

        private static string Clean(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return path.Trim().Trim('"');
        }

        private static string Normalize(string path)
        {
            var text = Clean(path).Replace('/', '\\');
            while (text.Length > 3 && text.EndsWith("\\", StringComparison.Ordinal))
            {
                text = text.Substring(0, text.Length - 1);
            }

            return text;
        }

        private static string DirectoryOf(string path)
        {
            var slash = Math.Max(path.LastIndexOf('\\'), path.LastIndexOf('/'));
            return slash > 0 ? path.Substring(0, slash) : string.Empty;
        }

        private static string FileNameOf(string path)
        {
            var text = Clean(path);
            if (text.Length == 0)
            {
                return string.Empty;
            }

            var slash = Math.Max(text.LastIndexOf('\\'), text.LastIndexOf('/'));
            return slash >= 0 ? text.Substring(slash + 1) : text;
        }
    }
}
