using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using HslCommunication.ModBus;
using HostTemplate.Core.Configuration;
using HostTemplate.Devices.Hsl;
using HostTemplate.Devices.Plc.Melsec;
using HostTemplate.Devices.Plc.Modbus;
using HostTemplate.Devices.Plc.Omron;
using HostTemplate.Devices.Plc.Siemens;
using Xunit;

namespace HostTemplate.Tests
{
    /// <summary>
    /// HSL 封装层测试。不连硬件，只验证「协议 → 实现」映射和未连接时的行为，
    /// 这两处错了现场最难查。
    /// </summary>
    public class HslDeviceTests
    {
        [Fact]
        public void Factory_DefaultConfig_ShouldCreateSiemens()
        {
            var config = ConfigManager.CreateDefaultPlc();

            var client = HslDeviceFactory.Create(config);

            Assert.IsType<Siemens7Client>(client);
            Assert.Equal("192.168.0.1", client.Address);
            client.Dispose();
        }

        [Fact]
        public void Factory_ModbusProtocol_ShouldCreateModbusClient()
        {
            var config = ConfigManager.CreateDefaultPlc();
            config.Protocol = "ModbusTcp";
            config.Port = 502;
            config.StationNo = 3;

            var client = HslDeviceFactory.Create(config);

            var modbus = Assert.IsType<ModbusTcpClient>(client);
            Assert.Equal("192.168.0.1:502", modbus.Address);
            Assert.Equal(3, modbus.Modbus.Station);
            client.Dispose();
        }

        [Fact]
        public void Factory_ProtocolName_ShouldBeCaseInsensitive()
        {
            var config = ConfigManager.CreateDefaultPlc();
            config.Protocol = "modbustcp";

            var client = HslDeviceFactory.Create(config);

            Assert.IsType<ModbusTcpClient>(client);
            client.Dispose();
        }

        [Fact]
        public void Factory_UnknownProtocol_ShouldThrow()
        {
            var config = ConfigManager.CreateDefaultPlc();
            config.Protocol = "Profinet";

            Assert.Throws<NotSupportedException>(() => HslDeviceFactory.Create(config));
        }

        [Fact]
        public void TryCreate_UnknownProtocol_ShouldReturnFalseWithMessage()
        {
            var config = ConfigManager.CreateDefaultPlc();
            config.Protocol = "Profinet";

            Assert.False(HslDeviceFactory.TryCreate(config, out var client, out var error));
            Assert.Null(client);
            Assert.Contains("不支持", error);
        }

        [Fact]
        public async Task Siemens7Client_WhenNotConnected_ShouldFailFast()
        {
            await using (var client = new Siemens7Client("127.0.0.1"))
            {
                Assert.False(client.IsConnected);

                var bit = await client.ReadBoolAsync("M0.0");
                Assert.False(bit.Success);
                Assert.Equal("设备未连接", bit.Error);

                var number = await client.ReadInt32Async("DB1.DBD0");
                Assert.False(number.Success);
                Assert.Equal("设备未连接", number.Error);

                var write = await client.WriteWordAsync("MW200", 1);
                Assert.False(write.Success);
                Assert.Equal("设备未连接", write.Error);
            }
        }

        [Fact]
        public async Task ReadWithEmptyAddress_WhenDisconnected_ShouldNotThrow()
        {
            var config = ConfigManager.CreateDefaultPlc();
            config.Protocol = "ModbusTcp";

            await using (var client = (ModbusTcpClient)HslDeviceFactory.Create(config))
            {
                // 未连接优先报未连接；连上后才会校验地址。
                var read = await client.ReadUInt16Async("");
                Assert.False(read.Success);
                Assert.False(string.IsNullOrEmpty(read.Error));
            }
        }

        [Fact]
        public async Task OpenAsync_WhenPortClosed_ShouldStayDisconnected()
        {
            await using (var client = new Siemens7Client("127.0.0.1", 1, "S1200", 300, 300))
            {
                var opened = await client.OpenAsync();

                Assert.False(opened.Success);
                Assert.False(string.IsNullOrEmpty(opened.Error));
                Assert.False(client.IsConnected);

                var read = await client.ReadBoolAsync("M0.0");
                Assert.Equal("设备未连接", read.Error);
            }
        }

        [Fact]
        public async Task ModbusTcpClient_Loopback_ShouldReadAndWriteThroughAsyncApi()
        {
            var port = FreeTcpPort();
            var server = new ModbusTcpServer();
            server.ServerStart(port);

            try
            {
                await using (var client = new ModbusTcpClient("127.0.0.1", port, 1, 2000, 2000))
                {
                    var opened = await client.OpenAsync();
                    Assert.True(opened.Success, opened.Error);

                    var empty = await client.ReadUInt16Async(" ");
                    Assert.False(empty.Success);
                    Assert.Equal("地址为空", empty.Error);

                    var written = await client.WriteWordAsync("100", 42);
                    Assert.True(written.Success, written.Error);

                    var read = await client.ReadWordAsync("100");
                    Assert.True(read.Success, read.Error);
                    Assert.Equal((ushort)42, read.Value);

                    var register = await client.WriteOneRegisterAsync("101", 7);
                    Assert.True(register.Success, register.Error);
                    var registerRead = await client.ReadWordAsync("101");
                    Assert.True(registerRead.Success, registerRead.Error);
                    Assert.Equal((ushort)7, registerRead.Value);

                    var coil = await client.WriteBoolAsync("0", true);
                    Assert.True(coil.Success, coil.Error);
                    var coilRead = await client.ReadCoilAsync("0");
                    Assert.True(coilRead.Success, coilRead.Error);
                    Assert.True(coilRead.Value);

                    var parallel = await Task.WhenAll(
                        client.ReadWordAsync("100"),
                        client.ReadWordAsync("100"),
                        client.ReadWordAsync("100"));
                    Assert.All(parallel, item => Assert.Equal((ushort)42, item.Value));

                    await client.CloseAsync();
                    Assert.False(client.IsConnected);

                    var afterClose = await client.ReadWordAsync("100");
                    Assert.Equal("设备未连接", afterClose.Error);
                }
            }
            finally
            {
                server.ServerClose();
            }
        }

        [Fact]
        public void Siemens7Client_ShouldExposeS7Object()
        {
            using (var client = new Siemens7Client("127.0.0.1", 102, "S1500"))
            {
                Assert.NotNull(client.S7);
                Assert.NotNull(client.RawDevice);
            }
        }

        [Fact]
        public void Catalog_Keys_ShouldBeUnique()
        {
            var keys = PlcDeviceCatalog.All.Select(d => d.Key).ToList();

            Assert.Equal(keys.Count, keys.Distinct().Count());
        }

        [Fact]
        public void Catalog_EveryDescriptor_ShouldCreateClient()
        {
            // 清单里每一台设备都必须能被工厂创建，否则配置页选了会崩。
            foreach (var device in PlcDeviceCatalog.All)
            {
                var config = ConfigManager.CreateDefaultPlc();
                config.Protocol = device.Protocol;
                config.CpuType = device.CpuType;
                config.Port = device.DefaultPort;
                config.StationNo = device.DefaultStation;

                using (var client = HslDeviceFactory.Create(config))
                {
                    Assert.NotNull(client);
                    Assert.False(client.IsConnected);
                }
            }
        }

        [Fact]
        public void Catalog_Find_ShouldMatchCpuVariant()
        {
            Assert.Equal("S7-1500", PlcDeviceCatalog.Find("SiemensS7", "S1500").Key);
            Assert.Equal("S7-300", PlcDeviceCatalog.Find("SiemensS7", "S300").Key);
            Assert.Equal("ModbusTcp", PlcDeviceCatalog.Find("ModbusTcp", string.Empty).Key);
        }

        [Fact]
        public void Catalog_Find_UnknownProtocol_ShouldFallBackToSiemens()
        {
            var device = PlcDeviceCatalog.Find("Profinet", string.Empty);

            Assert.Equal(HslDeviceFactory.ProtocolSiemensS7, device.Protocol);
        }

        [Fact]
        public void Factory_Melsec_ShouldUseDefaultPort6000()
        {
            var config = ConfigManager.CreateDefaultPlc();
            config.Protocol = "MelsecMc";
            config.Port = 0;

            using (var client = HslDeviceFactory.Create(config))
            {
                Assert.IsType<MelsecMcClient>(client);
                Assert.Equal("192.168.0.1:6000", client.Address);
            }
        }

        [Fact]
        public void Factory_Omron_ShouldDeriveNodeFromIpWhenZero()
        {
            var config = ConfigManager.CreateDefaultPlc();
            config.Protocol = "OmronFins";
            config.Address = "192.168.1.77";
            config.StationNo = 0;
            config.Port = 0;   // 0 表示用协议默认端口

            using (var client = HslDeviceFactory.Create(config))
            {
                var fins = Assert.IsType<OmronFinsClient>(client);
                Assert.Equal(77, fins.Node);
                Assert.Equal("192.168.1.77:9600", client.Address);
            }
        }

        [Fact]
        public void Factory_Omron_ShouldUseConfiguredNode()
        {
            var config = ConfigManager.CreateDefaultPlc();
            config.Protocol = "OmronFins";
            config.StationNo = 12;

            using (var client = HslDeviceFactory.Create(config))
            {
                Assert.Equal(12, Assert.IsType<OmronFinsClient>(client).Node);
            }
        }

        [Fact]
        public void Factory_InovanceAndDelta_ShouldCreateModbusFamily()
        {
            var config = ConfigManager.CreateDefaultPlc();
            config.Protocol = "InovanceTcp";

            using (var inovance = HslDeviceFactory.Create(config))
            {
                Assert.IsType<InovanceTcpClient>(inovance);
            }

            config.Protocol = "DeltaTcp";

            using (var delta = HslDeviceFactory.Create(config))
            {
                Assert.IsType<DeltaTcpClient>(delta);
            }
        }

        private static int FreeTcpPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
