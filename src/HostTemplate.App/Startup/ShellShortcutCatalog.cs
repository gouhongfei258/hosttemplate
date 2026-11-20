using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using HostTemplate.Core.Runtime.Startup;

namespace HostTemplate.App.Startup
{
    /// <summary>
    /// 用 IShellLink 读写一个目录里的 .lnk。读取目标时用原始路径，避免 current 目录被解析成某个版本目录后每次启动都被当成过期快捷方式。
    /// </summary>
    internal sealed class ShellShortcutCatalog : IShortcutCatalog
    {
        private const uint SlgpRawPath = 0x00000004;

        private readonly string _directory;

        public ShellShortcutCatalog(string directory)
        {
            _directory = directory ?? string.Empty;
        }

        public IReadOnlyList<ShortcutLink> List()
        {
            if (string.IsNullOrWhiteSpace(_directory) || !Directory.Exists(_directory))
            {
                return Array.Empty<ShortcutLink>();
            }

            var links = new List<ShortcutLink>();
            foreach (var file in Directory.EnumerateFiles(_directory, "*.lnk"))
            {
                if (TryRead(file, out var link))
                {
                    links.Add(link);
                }
            }

            return links;
        }

        public void Write(ShortcutLink link)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            if (string.IsNullOrWhiteSpace(link.FileName))
            {
                throw new InvalidOperationException("快捷方式文件名为空。");
            }

            Directory.CreateDirectory(_directory);
            var path = PathOf(link.FileName);
            var shell = (IShellLinkW)new ShellLink();
            try
            {
                shell.SetPath(link.TargetPath ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(link.WorkingDirectory))
                {
                    shell.SetWorkingDirectory(link.WorkingDirectory);
                }

                if (!string.IsNullOrWhiteSpace(link.Description))
                {
                    shell.SetDescription(link.Description);
                }

                var icon = string.IsNullOrWhiteSpace(link.IconPath) ? link.TargetPath : link.IconPath;
                if (!string.IsNullOrWhiteSpace(icon))
                {
                    shell.SetIconLocation(icon, 0);
                }

                shell.SetShowCmd(1);
                var file = (IPersistFile)shell;
                file.Save(path, true);
            }
            finally
            {
                Marshal.ReleaseComObject(shell);
            }
        }

        public string PathOf(string fileName)
        {
            return Path.Combine(_directory, fileName ?? string.Empty);
        }

        private static bool TryRead(string path, out ShortcutLink link)
        {
            link = null;
            object shell = null;

            try
            {
                shell = new ShellLink();
                var file = (IPersistFile)shell;
                file.Load(path, 0);
                var native = (IShellLinkW)shell;
                var target = new StringBuilder(32768);
                native.GetPath(target, target.Capacity, IntPtr.Zero, SlgpRawPath);
                link = new ShortcutLink
                {
                    FileName = Path.GetFileName(path),
                    TargetPath = target.ToString(),
                };
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (shell != null)
                {
                    Marshal.ReleaseComObject(shell);
                }
            }
        }

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLink
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);

            void GetIDList(out IntPtr ppidl);

            void SetIDList(IntPtr pidl);

            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);

            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);

            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);

            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

            void GetHotkey(out short hotkey);

            void SetHotkey(short hotkey);

            void GetShowCmd(out int showCmd);

            void SetShowCmd(int showCmd);

            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int iconIndex);

            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iconIndex);

            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint reserved);

            void Resolve(IntPtr hwnd, uint flags);

            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("0000010B-0000-0000-C000-000000000046")]
        private interface IPersistFile
        {
            void GetClassID(out Guid classId);

            [PreserveSig]
            int IsDirty();

            void Load([MarshalAs(UnmanagedType.LPWStr)] string fileName, uint mode);

            void Save([MarshalAs(UnmanagedType.LPWStr)] string fileName, bool remember);

            void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string fileName);

            void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string fileName);
        }
    }
}
