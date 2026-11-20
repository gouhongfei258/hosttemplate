using System;
using System.IO;
using HostTemplate.Core.Abstractions;
using HostTemplate.Core.Configuration;
using HostTemplate.Core.Diagnostics;
using HostTemplate.Core.Runtime.State;
using Xunit;

namespace HostTemplate.Tests
{
    /// <summary>
    /// 配置热加载测试。配置页保存后走的就是 ReloadConfig，
    /// 这条路径错了，现场改完地址表会「看着生效其实没生效」。
    /// </summary>
    public class StationContextTests : IDisposable
    {
        private readonly string _tempDirectory;

        public StationContextTests()
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
                // 清理失败不影响测试结论。
            }
        }

        private StationContext CreateContext(out ConfigManager manager)
        {
            manager = new ConfigManager(_tempDirectory);
            manager.Load(out _);

            return new StationContext(manager, new NullLogger(), _tempDirectory);
        }

        [Fact]
        public void ReloadConfig_ShouldRefreshAddressIndex()
        {
            var context = CreateContext(out var manager);

            Assert.Equal("MW200", context.GetAddress(AddressKeys.Command));

            foreach (var item in manager.Plc.Addresses)
            {
                if (item.Name == AddressKeys.Command)
                {
                    item.Address = "MW300";
                }
            }

            Assert.True(manager.SavePlc(out var error), error);

            Assert.True(context.ReloadConfig(out _));
            Assert.Equal("MW300", context.GetAddress(AddressKeys.Command));
        }

        [Fact]
        public void ReloadConfig_ShouldRefreshAppConfig()
        {
            var context = CreateContext(out var manager);

            manager.App.WorkStationName = "热加载后的工站";
            Assert.True(manager.SaveApp(out var error), error);

            context.ReloadConfig(out _);

            Assert.Equal("热加载后的工站", context.App.WorkStationName);
        }

        [Fact]
        public void DatabasePath_ShouldStayBesideConfigDirectory()
        {
            var context = CreateContext(out var manager);

            Assert.Equal(Path.Combine(manager.ConfigDirectory, manager.App.DatabasePath), context.DatabasePath);
            Assert.StartsWith(manager.ConfigDirectory, context.DatabasePath);
        }

        [Fact]
        public void LoadingConfig_ShouldNotCopyAddressTableIntoStationState()
        {
            var context = CreateContext(out var manager);

            Assert.True(manager.Plc.Addresses.Count > 1);
            Assert.Equal((ushort)0, context.State.ProcessWord);
            Assert.Equal(string.Empty, context.State.SerialNumber);
            Assert.Equal(string.Empty, context.State.RunningStep);
            Assert.False(context.State.Testing);
            Assert.NotNull(context.GetAddress(AddressKeys.Command));
        }

        [Fact]
        public void FindMissingAddressKeys_ShouldReportOnlyMissing()
        {
            var manager = new ConfigManager(_tempDirectory);
            manager.Load(out _);
            manager.Plc.Addresses.RemoveAll(a => a.Name == AddressKeys.TrayNumber);

            var context = new StationContext(manager, new NullLogger(), _tempDirectory);
            var missing = context.FindMissingAddressKeys(AddressKeys.Command, AddressKeys.TrayNumber);

            Assert.Single(missing);
            Assert.Equal(AddressKeys.TrayNumber, missing[0]);
        }

        [Fact]
        public void Session_ShouldDefaultToAnonymousAdmin()
        {
            var context = CreateContext(out _);

            Assert.False(context.Session.IsAuthenticated);
            Assert.True(context.Session.HasRole(Core.Security.UserRole.管理员));
        }

        private sealed class NullLogger : IAppLogger
        {
            public event EventHandler<LogEventArgs> Written
            {
                add { }
                remove { }
            }

            public void Debug(string message)
            {
            }

            public void Info(string message)
            {
            }

            public void Warn(string message)
            {
            }

            public void Error(string message)
            {
            }

            public void Error(string message, Exception exception)
            {
            }
        }
    }
}
