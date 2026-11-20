using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using HostTemplate.Devices.Hsl;
using HostTemplate.Devices.Hsl.Http;
using HostTemplate.Devices.Hsl.OpenProtocol;
using HostTemplate.Devices.Hsl.Serial;
using HostTemplate.Devices.Hsl.Udp;
using HostTemplate.Devices.Tcp;
using Xunit;

namespace HostTemplate.Tests
{
    /// <summary>
    /// 传输层封装。TCP / UDP / HTTP / Open Protocol 用本机回环，不连现场设备。
    /// 串口没有硬件，只验证打不开时失败，并且不会抛到测试外面。
    /// </summary>
    public class HslTransportTests
    {
        [Fact]
        public void Factory_ShouldCreateEachTransport()
        {
            using (var tcp = HslTransportFactory.CreateTcp("127.0.0.1", 1, 200))
            using (var udp = HslTransportFactory.CreateUdp("127.0.0.1", 1, 200))
            using (var serial = HslTransportFactory.CreateSerial("COM99", timeoutMs: 200))
            using (var http = HslTransportFactory.CreateHttp("127.0.0.1", 80, 200))
            using (var httpServer = HslTransportFactory.CreateHttpServer())
            using (var open = HslTransportFactory.CreateOpenProtocol("127.0.0.1", 4545, 200))
            using (var openServer = HslTransportFactory.CreateOpenProtocolServer())
            {
                Assert.IsType<HslTcpClient>(tcp);
                Assert.NotNull(tcp.RawTcp);
                Assert.IsType<HslUdpClient>(udp);
                Assert.NotNull(udp.RawUdp);
                Assert.IsType<HslSerialClient>(serial);
                Assert.NotNull(serial.RawSerial);
                Assert.IsType<HslHttpClient>(http);
                Assert.NotNull(http.RawClient);
                Assert.IsType<HslHttpServer>(httpServer);
                Assert.NotNull(httpServer.RawServer);
                Assert.IsType<HslOpenProtocolClient>(open);
                Assert.NotNull(open.RawDevice);
                Assert.IsType<HslOpenProtocolServer>(openServer);
                Assert.NotNull(openServer.RawServer);
            }
        }

        [Fact]
        public async Task Tcp_WhenNotConnected_ShouldFailWithoutThrowing()
        {
            await using (var client = HslTransportFactory.CreateTcp("127.0.0.1", 1, 200))
            {
                var missing = await client.SendAsync(null);
                Assert.Equal("发送数据为空", missing.Error);

                var send = await client.SendAsync(new byte[] { 1 });
                Assert.Equal("设备未连接", send.Error);

                var receive = await client.ReceiveAsync(8);
                Assert.Equal("设备未连接", receive.Error);
            }
        }

        [Fact]
        public async Task Tcp_Loopback_ShouldEchoAndStayDownAfterClose()
        {
            var port = FreeTcpPort();
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();

            try
            {
                var echo = EchoOnceAsync(listener);

                await using (var client = new SampleTcpDevice("127.0.0.1", port))
                {
                    var opened = await client.OpenAsync();
                    Assert.True(opened.Success, opened.Error);
                    Assert.True(client.IsConnected);

                    var payload = Encoding.ASCII.GetBytes("ping");
                    var sent = await client.SendAsync(payload);
                    Assert.True(sent.Success, sent.Error);

                    var received = await client.ReceiveExactAsync(payload.Length);
                    Assert.True(received.Success, received.Error);
                    Assert.Equal(payload, received.Value);

                    await client.CloseAsync();
                    Assert.False(client.IsConnected);

                    var after = await client.SendAsync(payload);
                    Assert.Equal("设备未连接", after.Error);
                }

                await echo;
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task Udp_Loopback_ShouldEchoOneDatagram()
        {
            var port = FreeUdpPort();
            using (var server = new UdpClient(new IPEndPoint(IPAddress.Loopback, port)))
            {
                var echo = EchoUdpOnceAsync(server);

                await using (var client = HslTransportFactory.CreateUdp("127.0.0.1", port, 2000))
                {
                    var opened = await client.OpenAsync();
                    Assert.True(opened.Success, opened.Error);

                    var payload = Encoding.ASCII.GetBytes("udp");
                    var sent = await client.SendAsync(payload);
                    Assert.True(sent.Success, sent.Error);

                    var received = await client.ReceiveAsync(32);
                    Assert.True(received.Success, received.Error);
                    Assert.Equal(payload, received.Value);
                }

                await echo;
            }
        }

        [Fact]
        public async Task Http_Loopback_ShouldGetAndPost()
        {
            var port = FreeTcpPort();
            using (var server = HslTransportFactory.CreateHttpServer())
            {
                server.HandleRequest = (request, response, body) =>
                    string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase)
                        ? "echo:" + (body ?? string.Empty)
                        : "pong";

                var started = await server.StartAsync(port);
                if (!started.Success)
                {
                    started = await server.StartAsync("http://127.0.0.1:" + port + "/");
                }

                Assert.True(started.Success, started.Error);
                Assert.Equal(port, server.Port);

                using (var client = HslTransportFactory.CreateHttp("127.0.0.1", port, 3000))
                {
                    var get = await client.GetAsync("/ping");
                    Assert.True(get.Success, get.Error);
                    Assert.Equal("pong", get.Value);

                    var post = await client.PostAsync("/echo", "hello");
                    Assert.True(post.Success, post.Error);
                    Assert.Equal("echo:hello", post.Value);

                    var put = await client.RequestAsync(HttpMethod.Put, "/echo", "x", "text/plain");
                    Assert.True(put.Success, put.Error);
                    Assert.Equal("pong", put.Value);
                }

                await server.StopAsync();
                Assert.False(server.IsStarted);
            }

            var prefixPort = FreeTcpPort();
            using (var server = HslTransportFactory.CreateHttpServer())
            {
                server.HandleRequest = (request, response, body) => "prefix";
                var started = await server.StartAsync("http://127.0.0.1:" + prefixPort + "/");
                Assert.True(started.Success, started.Error);
                Assert.Equal(prefixPort, server.Port);

                using (var client = HslTransportFactory.CreateHttp("127.0.0.1", prefixPort, 3000))
                {
                    var get = await client.GetAsync("/ping");
                    Assert.True(get.Success, get.Error);
                    Assert.Equal("prefix", get.Value);
                }
            }
        }

        [Fact]
        public async Task OpenProtocol_Loopback_ShouldConnectAndReadCustomer()
        {
            var port = FreeTcpPort();
            using (var server = HslTransportFactory.CreateOpenProtocolServer())
            {
                var started = await server.StartAsync(port);
                Assert.True(started.Success, started.Error);
                Assert.Equal(port, server.Port);
                server.SetVinNumber("VIN123");
                server.Publish(9999, 1, 0, 0, new[] { "a" });

                await using (var client = HslTransportFactory.CreateOpenProtocol("127.0.0.1", port, 3000))
                {
                    var closed = await client.ReadCustomerAsync(1, 1, 0, 0, null);
                    Assert.Equal("设备未连接", closed.Error);

                    var opened = await client.OpenAsync();
                    Assert.True(opened.Success, opened.Error);

                    var read = await client.ReadCustomerAsync(1, 1, 0, 0, null);
                    Assert.True(read.Success, read.Error);
                    Assert.False(string.IsNullOrEmpty(read.Value));

                    await client.CloseAsync();
                    var after = await client.ReadCustomerAsync(1, 1, 0, 0, null);
                    Assert.Equal("设备未连接", after.Error);
                }

                await server.StopAsync();
            }
        }

        [Fact]
        public async Task Serial_MissingPort_ShouldFail()
        {
            await using (var client = HslTransportFactory.CreateSerial("/dev/ttyHOSTTEMPLATE_MISSING", timeoutMs: 300))
            {
                var opened = await client.OpenAsync();
                Assert.False(opened.Success);
                Assert.False(string.IsNullOrEmpty(opened.Error));
                Assert.False(client.IsConnected);

                var send = await client.SendAsync(new byte[] { 1 });
                Assert.Equal("设备未连接", send.Error);
            }
        }

        private static async Task EchoOnceAsync(TcpListener listener)
        {
            using (var tcp = await listener.AcceptTcpClientAsync())
            using (var stream = tcp.GetStream())
            {
                var buffer = new byte[16];
                var count = await stream.ReadAsync(buffer, 0, buffer.Length);
                await stream.WriteAsync(buffer, 0, count);
            }
        }

        private static async Task EchoUdpOnceAsync(UdpClient server)
        {
            var result = await server.ReceiveAsync();
            await server.SendAsync(result.Buffer, result.Buffer.Length, result.RemoteEndPoint);
        }

        private static int FreeTcpPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static int FreeUdpPort()
        {
            using (var client = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)))
            {
                return ((IPEndPoint)client.Client.LocalEndPoint).Port;
            }
        }

        /// <summary>旧的 TCP 外设基类现在应该和 HslTcpClient 是同一条管道。</summary>
        private sealed class SampleTcpDevice : TcpDeviceBase
        {
            public SampleTcpDevice(string ipAddress, int port)
                : base("sample", ipAddress, port, 2000)
            {
            }
        }
    }
}
