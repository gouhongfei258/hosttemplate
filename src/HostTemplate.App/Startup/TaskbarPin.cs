using System;
using System.IO;
using System.Runtime.InteropServices;

namespace HostTemplate.App.Startup
{
    /// <summary>
    /// 请求外壳把一个已存在的 .lnk 钉到任务栏。
    /// Windows 10 1809 到 Windows 11 24H2 之前，IPinnedList3.Modify 会真正钉住，而且不询问。
    /// taskbarpin 动词更早被资源管理器拿掉了，只有快捷方式菜单里还看得到这项时才调用。
    /// Windows 11 24H2（内部版本 26100）起，IPinnedList3 会返回成功但并不钉住，所以这里直接不再调用它。
    /// </summary>
    internal static class TaskbarPin
    {
        private const int Windows11Version24H2 = 26100;

        public static bool TryPin(string shortcutPath)
        {
            if (string.IsNullOrWhiteSpace(shortcutPath) || !File.Exists(shortcutPath))
            {
                return false;
            }

            if (!SilentPinIsBlocked() && PinnedList.TryModify(shortcutPath))
            {
                return true;
            }

            return LegacyVerb.TryInvoke(shortcutPath);
        }

        private static bool SilentPinIsBlocked()
        {
            try
            {
                return Environment.OSVersion.Version.Build >= Windows11Version24H2;
            }
            catch
            {
                return false;
            }
        }

        private static class PinnedList
        {
            public static bool TryModify(string shortcutPath)
            {
                var pidl = IntPtr.Zero;
                object pin = null;

                try
                {
                    var parsed = SHParseDisplayName(shortcutPath, IntPtr.Zero, out pidl, 0, out _);
                    if (parsed < 0 || pidl == IntPtr.Zero)
                    {
                        return false;
                    }

                    pin = new TaskbandPinClass();
                    var list = (IPinnedList3)pin;
                    return list.Modify(IntPtr.Zero, pidl, int.MaxValue) == 0;
                }
                catch
                {
                    return false;
                }
                finally
                {
                    if (pidl != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(pidl);
                    }

                    if (pin != null)
                    {
                        try
                        {
                            Marshal.ReleaseComObject(pin);
                        }
                        catch
                        {
                            // 钉住对象释放失败不影响快捷方式文件。
                        }
                    }
                }
            }

            [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
            private static extern int SHParseDisplayName(
                string name,
                IntPtr bindContext,
                out IntPtr pidl,
                uint attributesIn,
                out uint attributesOut);

            [ComImport]
            [Guid("90AA3A4E-1CBA-4233-B8BB-535773D48449")]
            private class TaskbandPinClass
            {
            }

            /// <summary>
            /// 前 13 个方法只用来占住虚表位置，不能调用。Modify 是第 14 个方法（含 IUnknown 后的槽位 16）。
            /// </summary>
            [ComImport]
            [Guid("0DD79AE2-D156-45D4-9EEB-3B549769E940")]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            private interface IPinnedList3
            {
                void Reserved0();

                void Reserved1();

                void Reserved2();

                void Reserved3();

                void Reserved4();

                void Reserved5();

                void Reserved6();

                void Reserved7();

                void Reserved8();

                void Reserved9();

                void Reserved10();

                void Reserved11();

                void Reserved12();

                [PreserveSig]
                int Modify(IntPtr unpin, IntPtr pin, int caller);
            }
        }

        private static class LegacyVerb
        {
            public static bool TryInvoke(string shortcutPath)
            {
                try
                {
                    var shellType = Type.GetTypeFromProgID("Shell.Application");
                    if (shellType == null)
                    {
                        return false;
                    }

                    dynamic shell = Activator.CreateInstance(shellType);
                    dynamic folder = shell.NameSpace(Path.GetDirectoryName(shortcutPath));
                    if (folder == null)
                    {
                        return false;
                    }

                    dynamic item = folder.ParseName(Path.GetFileName(shortcutPath));
                    if (item == null)
                    {
                        return false;
                    }

                    foreach (dynamic verb in item.Verbs())
                    {
                        if (!IsPinVerb(verb.Name as string))
                        {
                            continue;
                        }

                        verb.DoIt();
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }

            private static bool IsPinVerb(string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return false;
                }

                var text = name.Replace("&", string.Empty).Trim();
                if (text.IndexOf("unpin", StringComparison.OrdinalIgnoreCase) >= 0
                    || text.Contains("取消固定")
                    || text.Contains("从任务栏脱离"))
                {
                    return false;
                }

                return text.Equals("taskbarpin", StringComparison.OrdinalIgnoreCase)
                    || text.IndexOf("Pin to taskbar", StringComparison.OrdinalIgnoreCase) >= 0
                    || text.Contains("锁定到任务栏")
                    || text.Contains("固定到任务栏");
            }
        }
    }
}
