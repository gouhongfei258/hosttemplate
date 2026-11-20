using System;
using System.Collections.Generic;
using System.IO;
using HostTemplate.Core.Runtime.Startup;
using Xunit;

namespace HostTemplate.Tests
{
    public class TaskbarShortcutTests
    {
        private const string CurrentExe = @"C:\Users\me\AppData\Local\HostTemplate\current\HostTemplate.exe";

        private const string CurrentIcon = @"C:\Users\me\AppData\Local\HostTemplate\current\HostTemplate.ico";

        private const string TaskbarDir = @"C:\Users\me\AppData\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar";

        private const string StartMenuDir = @"C:\Users\me\AppData\Microsoft\Windows\Start Menu\Programs";

        [Fact]
        public void Ensure_Missing_WritesTaskbarAndStartMenu()
        {
            var taskbar = new MemoryShortcutCatalog(TaskbarDir);
            var startMenu = new MemoryShortcutCatalog(StartMenuDir);

            var result = TaskbarShortcut.Ensure(CurrentExe, CurrentIcon, taskbar, startMenu);

            Assert.True(result.Success, result.Error);
            Assert.False(result.Skipped);
            Assert.True(result.ShouldPin);
            Assert.Equal(taskbar.PathOf(TaskbarShortcut.LinkFileName), result.PinPath);
            Assert.Equal(CurrentExe, result.Target);

            var link = Assert.Single(taskbar.Items);
            Assert.Equal(TaskbarShortcut.LinkFileName, link.FileName);
            Assert.Equal(CurrentExe, link.TargetPath);
            Assert.Equal(CurrentIcon, link.IconPath);
            Assert.Equal(@"C:\Users\me\AppData\Local\HostTemplate\current", link.WorkingDirectory);
            Assert.Equal(TaskbarShortcut.Description, link.Description);
            Assert.Equal(CurrentExe, Assert.Single(startMenu.Items).TargetPath);
        }

        [Fact]
        public void Ensure_AlreadyPresent_SkipsWithoutRewrite()
        {
            var taskbar = new MemoryShortcutCatalog(TaskbarDir);
            taskbar.Items.Add(new ShortcutLink
            {
                FileName = "数采系统.lnk",
                TargetPath = "\"" + CurrentExe.ToLowerInvariant() + "\"",
            });
            var startMenu = new MemoryShortcutCatalog(StartMenuDir);
            startMenu.Items.Add(new ShortcutLink
            {
                FileName = TaskbarShortcut.LinkFileName,
                TargetPath = CurrentExe.Replace('\\', '/'),
            });

            var result = TaskbarShortcut.Ensure(CurrentExe, CurrentIcon, taskbar, startMenu);

            Assert.True(result.Success, result.Error);
            Assert.True(result.Skipped);
            Assert.False(result.ShouldPin);
            Assert.Equal(string.Empty, result.PinPath);
            Assert.Equal(0, taskbar.Writes);
            Assert.Equal(0, startMenu.Writes);
            Assert.Equal("数采系统.lnk", Assert.Single(taskbar.Items).FileName);
        }

        [Fact]
        public void Ensure_SecondCall_Skips()
        {
            var taskbar = new MemoryShortcutCatalog(TaskbarDir);
            var startMenu = new MemoryShortcutCatalog(StartMenuDir);

            var first = TaskbarShortcut.Ensure(CurrentExe, CurrentIcon, taskbar, startMenu);
            var taskbarWrites = taskbar.Writes;
            var startWrites = startMenu.Writes;
            var second = TaskbarShortcut.Ensure(CurrentExe, CurrentIcon, taskbar, startMenu);

            Assert.True(first.ShouldPin);
            Assert.True(second.Success, second.Error);
            Assert.True(second.Skipped);
            Assert.False(second.ShouldPin);
            Assert.Equal(taskbarWrites, taskbar.Writes);
            Assert.Equal(startWrites, startMenu.Writes);
        }

        [Fact]
        public void Ensure_StaleInstalledPath_RewritesThatShortcutAndDoesNotPin()
        {
            var taskbar = new MemoryShortcutCatalog(TaskbarDir);
            taskbar.Items.Add(new ShortcutLink
            {
                FileName = "数采系统.lnk",
                TargetPath = @"C:\Users\me\AppData\Local\HostTemplate\app-1.0.1\HostTemplate.exe",
            });
            var startMenu = new MemoryShortcutCatalog(StartMenuDir);
            startMenu.Items.Add(new ShortcutLink
            {
                FileName = "旧版本.lnk",
                TargetPath = @"D:\HostTemplate\app-1.0.1\HostTemplate.exe",
            });

            var result = TaskbarShortcut.Ensure(CurrentExe, CurrentIcon, taskbar, startMenu);

            Assert.True(result.Success, result.Error);
            Assert.False(result.Skipped);
            Assert.False(result.ShouldPin);
            Assert.Contains("当前程序路径", result.Detail);

            var taskbarLink = Assert.Single(taskbar.Items);
            Assert.Equal("数采系统.lnk", taskbarLink.FileName);
            Assert.Equal(CurrentExe, taskbarLink.TargetPath);
            Assert.Equal(CurrentIcon, taskbarLink.IconPath);

            var startLink = Assert.Single(startMenu.Items);
            Assert.Equal("旧版本.lnk", startLink.FileName);
            Assert.Equal(CurrentExe, startLink.TargetPath);
        }

        [Fact]
        public void Ensure_OtherShortcut_IsLeftAlone()
        {
            var taskbar = new MemoryShortcutCatalog(TaskbarDir);
            taskbar.Items.Add(new ShortcutLink
            {
                FileName = "Notepad.lnk",
                TargetPath = @"C:\Windows\System32\notepad.exe",
            });

            var result = TaskbarShortcut.Ensure(CurrentExe, CurrentIcon, taskbar, new MemoryShortcutCatalog(StartMenuDir));

            Assert.True(result.ShouldPin);
            Assert.Equal(2, taskbar.Items.Count);
            Assert.Contains(taskbar.Items, link => link.TargetPath == @"C:\Windows\System32\notepad.exe");
            Assert.Contains(taskbar.Items, link => link.FileName == TaskbarShortcut.LinkFileName && link.TargetPath == CurrentExe);
        }

        [Fact]
        public void Ensure_TaskbarAlreadyPresent_StillFillsEmptyStartMenu()
        {
            var taskbar = new MemoryShortcutCatalog(TaskbarDir);
            taskbar.Items.Add(new ShortcutLink
            {
                FileName = TaskbarShortcut.LinkFileName,
                TargetPath = CurrentExe,
            });
            var startMenu = new MemoryShortcutCatalog(StartMenuDir);

            var result = TaskbarShortcut.Ensure(CurrentExe, CurrentIcon, taskbar, startMenu);

            Assert.True(result.Skipped);
            Assert.False(result.ShouldPin);
            Assert.Equal(0, taskbar.Writes);
            Assert.Equal(CurrentExe, Assert.Single(startMenu.Items).TargetPath);
            Assert.Contains("开始菜单", result.Detail);
        }

        [Fact]
        public void Ensure_StartMenuWriteFailure_StillWritesTaskbar()
        {
            var taskbar = new MemoryShortcutCatalog(TaskbarDir);

            var result = TaskbarShortcut.Ensure(CurrentExe, CurrentIcon, taskbar, new ThrowingCatalog());

            Assert.True(result.Success, result.Error);
            Assert.True(result.ShouldPin);
            Assert.Equal(CurrentExe, Assert.Single(taskbar.Items).TargetPath);
            Assert.Contains("开始菜单被占用", result.Detail);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Ensure_BlankExecutable_DoesNotWrite(string executable)
        {
            var taskbar = new MemoryShortcutCatalog(TaskbarDir);

            var result = TaskbarShortcut.Ensure(executable, CurrentIcon, taskbar, null);

            Assert.False(result.Success);
            Assert.False(result.ShouldPin);
            Assert.Empty(taskbar.Items);
        }

        [Fact]
        public void Ensure_NullTaskbar_Fails()
        {
            var result = TaskbarShortcut.Ensure(CurrentExe, CurrentIcon, null, new MemoryShortcutCatalog(StartMenuDir));

            Assert.False(result.Success);
            Assert.False(string.IsNullOrEmpty(result.Error));
        }

        [Fact]
        public void ResolveIconPath_PrefersTheIconBesideTheExecutable()
        {
            var executable = @"C:\Users\me\AppData\Local\HostTemplate\current\HostTemplate.exe";

            var found = TaskbarShortcut.ResolveIconPath(
                executable,
                "HostTemplate.ico",
                path => path == @"C:\Users\me\AppData\Local\HostTemplate\current\HostTemplate.ico");
            var missing = TaskbarShortcut.ResolveIconPath(executable, "HostTemplate.ico", _ => false);

            Assert.Equal(@"C:\Users\me\AppData\Local\HostTemplate\current\HostTemplate.ico", found);
            Assert.Equal(executable, missing);
            Assert.Equal(string.Empty, TaskbarShortcut.ResolveIconPath("  ", "HostTemplate.ico", _ => true));
        }

        private sealed class MemoryShortcutCatalog : IShortcutCatalog
        {
            public MemoryShortcutCatalog(string directory)
            {
                Directory = directory;
            }

            public string Directory { get; }

            public List<ShortcutLink> Items { get; } = new List<ShortcutLink>();

            public int Writes { get; private set; }

            public IReadOnlyList<ShortcutLink> List()
            {
                var copy = new List<ShortcutLink>(Items.Count);
                foreach (var item in Items)
                {
                    copy.Add(Clone(item));
                }

                return copy;
            }

            public void Write(ShortcutLink link)
            {
                Writes++;
                var stored = Clone(link);
                var index = Items.FindIndex(item => string.Equals(item.FileName, stored.FileName, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    Items[index] = stored;
                    return;
                }

                Items.Add(stored);
            }

            public string PathOf(string fileName)
            {
                var separator = Directory.IndexOf('\\') >= 0 ? '\\' : '/';
                return Directory.TrimEnd('\\', '/') + separator + fileName;
            }

            private static ShortcutLink Clone(ShortcutLink link)
            {
                return new ShortcutLink
                {
                    FileName = link.FileName,
                    TargetPath = link.TargetPath,
                    IconPath = link.IconPath,
                    WorkingDirectory = link.WorkingDirectory,
                    Description = link.Description,
                };
            }
        }

        private sealed class ThrowingCatalog : IShortcutCatalog
        {
            public IReadOnlyList<ShortcutLink> List()
            {
                return Array.Empty<ShortcutLink>();
            }

            public void Write(ShortcutLink link)
            {
                throw new IOException("开始菜单被占用");
            }

            public string PathOf(string fileName)
            {
                return fileName;
            }
        }
    }
}
