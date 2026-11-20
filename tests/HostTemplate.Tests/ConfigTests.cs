using System;
using System.IO;
using HostTemplate.Core.Configuration;
using Xunit;

namespace HostTemplate.Tests
{
    public class ConfigTests : IDisposable
    {
        private readonly string _tempDirectory;

        public ConfigTests()
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

        [Fact]
        public void Load_ShouldCreateDefaultFilesWhenMissing()
        {
            var manager = new ConfigManager(_tempDirectory);

            manager.Load(out var message);

            Assert.True(File.Exists(manager.AppPath));
            Assert.True(File.Exists(manager.PlcPath));
            Assert.True(File.Exists(manager.ApiPath));
            Assert.Contains("默认值", message);
        }

        [Fact]
        public void SaveAndLoad_ShouldRoundTrip()
        {
            var manager = new ConfigManager(_tempDirectory);
            manager.Load(out _);

            manager.App.WorkStationName = "测试工站";
            manager.App.EquId = "OP999";
            manager.App.Simulate = true;
            Assert.True(manager.SaveApp(out var error), error);

            var reloaded = new ConfigManager(_tempDirectory);
            reloaded.Load(out _);

            Assert.Equal("测试工站", reloaded.App.WorkStationName);
            Assert.Equal("OP999", reloaded.App.EquId);
            Assert.True(reloaded.App.Simulate);
        }

        [Fact]
        public void BuildIndex_ShouldDetectDuplicateNames()
        {
            var plc = ConfigManager.CreateDefaultPlc();
            plc.Addresses.Add(new AddrOption { Name = "PLC_指令", Address = "MW900", DataType = "Word" });

            var index = plc.BuildIndex(out var warning);

            Assert.Equal(7, index.Count);
            Assert.Contains("重复", warning);
            Assert.Equal("MW200", index["PLC_指令"].Address);
        }

        [Fact]
        public void ApiConfig_GetPath_ShouldBeCaseInsensitive()
        {
            var api = ConfigManager.CreateDefaultApi();

            Assert.Equal("/api/ProcessStation/RequestSnNumber", api.GetPath("applysn"));
            Assert.Equal(string.Empty, api.GetPath("not-exists"));
            Assert.Equal("/fallback", api.GetPath("not-exists", "/fallback"));
        }

        [Fact]
        public void DefaultPlc_ShouldContainAllCoreAddressKeys()
        {
            var plc = ConfigManager.CreateDefaultPlc();
            var index = plc.BuildIndex(out _);

            Assert.True(index.ContainsKey(Core.Abstractions.AddressKeys.Heartbeat));
            Assert.True(index.ContainsKey(Core.Abstractions.AddressKeys.Command));
            Assert.True(index.ContainsKey(Core.Abstractions.AddressKeys.ReviewResult));
            Assert.Equal(2, index[Core.Abstractions.AddressKeys.Command].Length);
        }

        [Fact]
        public void Clone_ShouldBeIndependentCopy()
        {
            var app = ConfigManager.CreateDefaultApp();
            app.WorkStationName = "原始";

            var copy = XmlConfigStore.Clone(app);
            copy.WorkStationName = "副本";
            copy.RequiredSteps.Add(99);

            Assert.Equal("原始", app.WorkStationName);
            Assert.DoesNotContain((ushort)99, app.RequiredSteps);
            Assert.Contains((ushort)99, copy.RequiredSteps);
        }

        [Fact]
        public void Clone_Plc_ShouldKeepAddressTable()
        {
            var plc = ConfigManager.CreateDefaultPlc();

            var copy = XmlConfigStore.Clone(plc);
            copy.Addresses[0].Address = "M999.9";

            Assert.Equal(plc.Addresses.Count, copy.Addresses.Count);
            Assert.NotEqual("M999.9", plc.Addresses[0].Address);
        }

        [Fact]
        public void SaveAll_ShouldPersistThreeFiles()
        {
            var manager = new ConfigManager(_tempDirectory);
            manager.Load(out _);

            var app = ConfigManager.CreateDefaultApp();
            app.WorkStationName = "整体保存";

            var plc = ConfigManager.CreateDefaultPlc();
            plc.Address = "10.0.0.9";

            var api = ConfigManager.CreateDefaultApi();
            api.BaseUrl = "http://10.0.0.5:9000";

            Assert.True(manager.SaveAll(app, plc, api, out var error), error);

            var reloaded = new ConfigManager(_tempDirectory);
            reloaded.Load(out _);

            Assert.Equal("整体保存", reloaded.App.WorkStationName);
            Assert.Equal("10.0.0.9", reloaded.Plc.Address);
            Assert.Equal("http://10.0.0.5:9000", reloaded.Api.BaseUrl);
        }

        [Fact]
        public void SaveAll_ShouldRejectNullConfig()
        {
            var manager = new ConfigManager(_tempDirectory);
            manager.Load(out _);

            Assert.False(manager.SaveAll(null, manager.Plc, manager.Api, out var error));
            Assert.Contains("为空", error);
        }
    }
}
