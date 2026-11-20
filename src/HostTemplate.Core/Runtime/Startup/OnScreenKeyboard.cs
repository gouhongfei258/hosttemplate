using System;
using System.IO;

namespace HostTemplate.Core.Runtime.Startup
{
    /// <summary>
    /// 选择并打开 Windows 屏幕键盘。优先 osk.exe，没有时用触摸键盘 TabTip.exe，
    /// 两个文件都找不到时仍尝试让系统按名字启动 osk.exe。
    /// </summary>
    public static class OnScreenKeyboard
    {
        public const string OskFileName = "osk.exe";

        public const string TabTipFileName = "TabTip.exe";

        public static string Select(string systemDirectory, string commonProgramFiles, Func<string, bool> exists)
        {
            if (exists == null)
            {
                exists = _ => false;
            }

            var osk = string.IsNullOrWhiteSpace(systemDirectory)
                ? null
                : Path.Combine(systemDirectory, OskFileName);
            if (!string.IsNullOrEmpty(osk) && exists(osk))
            {
                return osk;
            }

            var tabTip = string.IsNullOrWhiteSpace(commonProgramFiles)
                ? null
                : Path.Combine(commonProgramFiles, "microsoft shared", "ink", TabTipFileName);
            if (!string.IsNullOrEmpty(tabTip) && exists(tabTip))
            {
                return tabTip;
            }

            return OskFileName;
        }

        public static bool TryOpen(
            string systemDirectory,
            string commonProgramFiles,
            Func<string, bool> exists,
            Action<string> start,
            out string error)
        {
            error = string.Empty;

            if (start == null)
            {
                error = "没有启动程序的方法";
                return false;
            }

            var target = Select(systemDirectory, commonProgramFiles, exists);

            try
            {
                start(target);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
