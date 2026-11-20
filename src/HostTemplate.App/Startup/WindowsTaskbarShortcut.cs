using System;
using System.IO;
using HostTemplate.Core.Runtime.Startup;

namespace HostTemplate.App.Startup
{
    /// <summary>
    /// 启动时补齐任务栏和开始菜单快捷方式。图标用程序旁边的 HostTemplate.ico（也嵌在 exe 里）。
    /// 目标路径和开机自启相同：已安装时指向 {安装根}\current\HostTemplate.exe。
    /// 快捷方式已经指向这个路径就跳过。钉不住时只记日志，不让启动失败。
    /// </summary>
    internal static class WindowsTaskbarShortcut
    {
        public const string IconFileName = "HostTemplate.ico";

        public const string PinLimitation =
            "快捷方式已写好，但当前 Windows 不接受程序自己静默钉住任务栏。"
            + "任务栏的 taskbarpin 命令已被系统去掉；Windows 11 24H2 及以后，钉住接口会返回成功却并不出现在任务栏上。"
            + "开始菜单里的 HostTemplate 可以右键钉一次。程序会继续启动。";

        public static ShortcutEnsureResult Apply()
        {
            var executable = WindowsAutoStart.ResolveExecutable();
            var icon = TaskbarShortcut.ResolveIconPath(executable, IconFileName, File.Exists);
            return TaskbarShortcut.Ensure(
                executable,
                icon,
                new ShellShortcutCatalog(TaskbarDirectory()),
                new ShellShortcutCatalog(StartMenuDirectory()));
        }

        public static bool TryPin(string shortcutPath)
        {
            return TaskbarPin.TryPin(shortcutPath);
        }

        public static string TaskbarDirectory()
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(root, "Microsoft", "Internet Explorer", "Quick Launch", "User Pinned", "TaskBar");
        }

        public static string StartMenuDirectory()
        {
            var programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            if (!string.IsNullOrWhiteSpace(programs))
            {
                return programs;
            }

            var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(root, "Microsoft", "Windows", "Start Menu", "Programs");
        }
    }
}
