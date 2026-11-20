using System;
using System.Collections.Generic;
using System.IO;
using HostTemplate.Core.Configuration;
using HostTemplate.Core.Configuration.Store;
using HostTemplate.Core.Runtime.Live;
using HostTemplate.Core.Runtime.Startup;
using Xunit;

namespace HostTemplate.Tests
{
    public class LiveViewAndStartupTests : IDisposable
    {
        private readonly string _tempDirectory;

        public LiveViewAndStartupTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "HostTemplateTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_tempDirectory, true);
            }
            catch
            {
                // 临时目录清理失败不影响测试结论。
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("默认")]
        [InlineData("default")]
        [InlineData("DEFAULT")]
        [InlineData("没登记过")]
        public void Resolve_UnknownOrDefault_UsesBuiltInView(string testType)
        {
            var registry = new LiveViewRegistry<string, string>();
            registry.Register("扭矩", ctx => "torque:" + ctx);

            Assert.Equal(LiveViewRegistry<string, string>.DefaultType, registry.Resolve(testType));
            Assert.Equal("demo", registry.Create(testType, "ctx", _ => "demo"));
        }

        [Fact]
        public void Create_RegisteredType_UsesThatView()
        {
            var registry = new LiveViewRegistry<string, string>();
            registry.Register(" 扭矩 ", ctx => "torque:" + ctx);
            registry.Register("视觉", ctx => "vision:" + ctx);

            Assert.Equal("扭矩", registry.Resolve("扭矩"));
            Assert.Equal("torque:station", registry.Create("扭矩", "station", _ => "demo"));
            Assert.Equal("vision:station", registry.Create("视觉", "station", _ => "demo"));
        }

        [Fact]
        public void Register_DefaultName_IsRejected()
        {
            var registry = new LiveViewRegistry<string, string>();

            Assert.Throws<ArgumentException>(() => registry.Register("默认", _ => "other"));
            Assert.Throws<ArgumentException>(() => registry.Register("default", _ => "other"));
        }

        [Fact]
        public void Register_ReplacesAnExistingType()
        {
            var registry = new LiveViewRegistry<string, string>();
            registry.Register("扭矩", _ => "first");
            registry.Register("扭矩", _ => "second");

            Assert.Equal("second", registry.Create("扭矩", "ctx", _ => "demo"));
        }

        [Fact]
        public void Apply_Enable_WritesQuotedCommand()
        {
            var store = new MemoryAutoStartStore();
            var path = @"C:\Users\Operator\AppData\Local\HostTemplate\current\HostTemplate.exe";

            var result = AutoStartRegistration.Apply(true, path, store);

            Assert.True(result.Success, result.Error);
            Assert.True(result.Enabled);
            Assert.Equal("\"" + path + "\"", store.Get(AutoStartRegistration.RunValueName));
        }

        [Fact]
        public void Apply_Disable_RemovesTheRunValue()
        {
            var store = new MemoryAutoStartStore();
            AutoStartRegistration.Apply(true, @"D:\debug\HostTemplate.exe", store);

            var result = AutoStartRegistration.Apply(false, @"D:\debug\HostTemplate.exe", store);

            Assert.True(result.Success, result.Error);
            Assert.False(result.Enabled);
            Assert.Null(store.Get(AutoStartRegistration.RunValueName));
        }

        [Fact]
        public void Apply_EnableAgain_ReplacesTheCommand()
        {
            var store = new MemoryAutoStartStore();
            AutoStartRegistration.Apply(true, @"D:\old\HostTemplate.exe", store);

            var updated = @"C:\Users\me\AppData\Local\HostTemplate\current\HostTemplate.exe";
            var result = AutoStartRegistration.Apply(true, updated, store);

            Assert.True(result.Success, result.Error);
            Assert.Equal("\"" + updated + "\"", result.Command);
            Assert.Equal(result.Command, store.Get(AutoStartRegistration.RunValueName));
        }

        [Fact]
        public void Apply_DisableWhenAbsent_Succeeds()
        {
            var store = new MemoryAutoStartStore();

            var result = AutoStartRegistration.Apply(false, null, store);

            Assert.True(result.Success, result.Error);
            Assert.False(store.RemoveWasCalled && store.Values.Count > 0);
            Assert.Null(store.Get(AutoStartRegistration.RunValueName));
        }

        [Fact]
        public void Apply_EnableWithoutPath_DoesNotWrite()
        {
            var store = new MemoryAutoStartStore();

            var result = AutoStartRegistration.Apply(true, "  ", store);

            Assert.False(result.Success);
            Assert.Null(store.Get(AutoStartRegistration.RunValueName));
        }

        [Fact]
        public void ResolveExecutable_DebugRun_UsesTheRunningExe()
        {
            var running = @"D:\src\HostTemplate\bin\HostTemplate.exe";

            var path = AutoStartRegistration.ResolveExecutable(running, null, null, false, _ => true);

            Assert.Equal(running, path);
        }

        [Fact]
        public void ResolveExecutable_Installed_UsesCurrentFolder()
        {
            var running = @"C:\Users\me\AppData\Local\HostTemplate\app-1.0.2\HostTemplate.exe";
            var root = @"C:\Users\me\AppData\Local\HostTemplate";

            var path = AutoStartRegistration.ResolveExecutable(running, root, "1.0.2", false, _ => true);

            Assert.Equal(root + @"\current\HostTemplate.exe", path);
        }

        [Fact]
        public void ResolveExecutable_InstalledButCurrentMissing_FallsBackToRunningExe()
        {
            var running = @"C:\Users\me\AppData\Local\HostTemplate\app-1.0.2\HostTemplate.exe";

            var path = AutoStartRegistration.ResolveExecutable(
                running,
                @"C:\Users\me\AppData\Local\HostTemplate",
                "1.0.2",
                false,
                _ => false);

            Assert.Equal(running, path);
        }

        [Fact]
        public void ResolveExecutable_Portable_UsesTheRunningExe()
        {
            var running = @"E:\HostTemplate\HostTemplate.exe";

            var path = AutoStartRegistration.ResolveExecutable(running, @"E:\HostTemplate", "1.0.0", true, _ => true);

            Assert.Equal(running, path);
        }

        [Fact]
        public void Keyboard_PrefersOsk_ThenTabTip()
        {
            var system = @"C:\Windows\System32";
            var common = @"C:\Program Files\Common Files";
            var osk = Path.Combine(system, "osk.exe");
            var tabTip = Path.Combine(common, "microsoft shared", "ink", "TabTip.exe");

            Assert.Equal(osk, OnScreenKeyboard.Select(system, common, path => path == osk || path == tabTip));
            Assert.Equal(tabTip, OnScreenKeyboard.Select(system, common, path => path == tabTip));
            Assert.Equal(OnScreenKeyboard.OskFileName, OnScreenKeyboard.Select(system, common, _ => false));
        }

        [Fact]
        public void Keyboard_TryOpen_StartsTheSelectedProgram()
        {
            string started = null;
            var opened = OnScreenKeyboard.TryOpen(
                @"C:\Windows\System32",
                @"C:\Program Files\Common Files",
                path => path.EndsWith("osk.exe", StringComparison.OrdinalIgnoreCase),
                path => started = path,
                out var error);

            Assert.True(opened, error);
            Assert.EndsWith("osk.exe", started, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void AppConfig_NewFields_RoundTripAndDefaultWhenMissing()
        {
            var manager = new ConfigManager(_tempDirectory);
            manager.Load(out _);
            manager.App.TestType = "扭矩";
            manager.App.StartWithWindows = true;
            Assert.True(manager.SaveApp(out var error), error);

            var reloaded = new ConfigManager(_tempDirectory);
            reloaded.Load(out _);
            Assert.Equal("扭矩", reloaded.App.TestType);
            Assert.True(reloaded.App.StartWithWindows);

            File.WriteAllText(Path.Combine(_tempDirectory, "legacy.xml"), "<AppConfig><WorkStationName>旧工站</WorkStationName></AppConfig>");
            Assert.True(XmlConfigStore.Load(Path.Combine(_tempDirectory, "legacy.xml"), out AppConfig legacy, out error), error);
            Assert.Equal("旧工站", legacy.WorkStationName);
            Assert.Equal("默认", legacy.TestType);
            Assert.False(legacy.StartWithWindows);
            Assert.False(legacy.AutoUpdate);
        }

        private sealed class MemoryAutoStartStore : IAutoStartStore
        {
            public Dictionary<string, string> Values { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public bool RemoveWasCalled { get; private set; }

            public string Get(string name)
            {
                return Values.TryGetValue(name, out var value) ? value : null;
            }

            public void Set(string name, string command)
            {
                Values[name] = command;
            }

            public bool Remove(string name)
            {
                RemoveWasCalled = true;
                return Values.Remove(name);
            }
        }
    }
}
