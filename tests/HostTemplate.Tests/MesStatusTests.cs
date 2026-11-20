using System;
using System.Threading;
using System.Threading.Tasks;
using HostTemplate.Core.Configuration;
using HostTemplate.Core.Diagnostics;
using HostTemplate.Core.Runtime.Live;
using HostTemplate.Devices.Api;
using Xunit;

namespace HostTemplate.Tests
{
    public class MesStatusTests
    {
        [Fact]
        public void MesLamp_Disabled_ShouldBeAlarmEvenWhenPingWouldSucceed()
        {
            Assert.Equal(MesLamp.Alarm, MesLampState.Resolve(hasClient: true, enabled: false, pingSucceeded: true));
            Assert.Equal(MesLamp.Alarm, MesLampState.Resolve(hasClient: true, enabled: false, pingSucceeded: false));
        }

        [Fact]
        public void MesLamp_Enabled_ShouldFollowPing()
        {
            Assert.Equal(MesLamp.On, MesLampState.Resolve(hasClient: true, enabled: true, pingSucceeded: true));
            Assert.Equal(MesLamp.Off, MesLampState.Resolve(hasClient: true, enabled: true, pingSucceeded: false));
        }

        [Fact]
        public void MesLamp_WithoutClient_ShouldStayOff()
        {
            Assert.Equal(MesLamp.Off, MesLampState.Resolve(hasClient: false, enabled: false, pingSucceeded: true));
            Assert.Equal(MesLamp.Off, MesLampState.Resolve(hasClient: false, enabled: true, pingSucceeded: true));
        }

        [Fact]
        public async Task PingAsync_WhenDisabled_ShouldReturnFalseWithoutCallingTheServer()
        {
            var config = new ApiConfig
            {
                Enabled = false,
                BaseUrl = "http://127.0.0.1:1",
                TimeoutMs = 500,
            };

            using (var client = new HttpApiClient(config, new SilentLogger()))
            {
                Assert.False(client.IsAvailable);

                var ping = await client.PingAsync(CancellationToken.None);

                Assert.False(ping);

                var response = await client.PostAsync("ApplySn", null, CancellationToken.None);
                Assert.True(response.Success);
            }
        }

        [Fact]
        public async Task PingAsync_WhenEnabled_ShouldReturnFalseIfTheServerIsUnreachable()
        {
            var config = new ApiConfig
            {
                Enabled = true,
                BaseUrl = "http://127.0.0.1:1",
                TimeoutMs = 800,
            };

            using (var client = new HttpApiClient(config, new SilentLogger()))
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
            {
                Assert.True(client.IsAvailable);
                Assert.False(await client.PingAsync(cts.Token));
            }
        }

        private sealed class SilentLogger : IAppLogger
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
