using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace HostTemplate.App.Startup
{
    /// <summary>
    /// 窗口图标用输出目录里的 HostTemplate.ico。打不开时继续用 exe 里嵌的同一枚图标。
    /// </summary>
    internal static class WindowIcon
    {
        public static void TryApply(Form form)
        {
            if (form == null)
            {
                return;
            }

            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, WindowsTaskbarShortcut.IconFileName);
                if (!File.Exists(path))
                {
                    return;
                }

                form.Icon = new Icon(path);
            }
            catch
            {
                // 图标文件打不开时，窗口继续用可执行文件自带的图标。
            }
        }
    }
}
