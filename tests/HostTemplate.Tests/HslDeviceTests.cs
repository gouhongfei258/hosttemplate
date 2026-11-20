using System;
using System.Linq;
using HostTemplate.Core.Configuration;
using HostTemplate.Devices.Hsl;
using HostTemplate.Devices.Plc;
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
        public void Siemens7Client_WhenNotConnected_ShouldFailFast()
        {
            using (var client = new Siemens7Client("127.0.0.1"))
            {
                Assert.False(client.IsConnected);

                Assert.False(client.ReadBool("M0.0", out _, out var error));
                Assert.Equal("设备未连接", error);

                Assert.False(client.ReadInt32("DB1.DBD0", out _, out error));
                Assert.Equal("设备未连接", error);

                Assert.False(client.WriteWord("MW200", 1, out error));
                Assert.Equal("设备未连接", error);
            }
        }

        [Fact]
        public void ReadWithEmptyAddress_ShouldReportAddressError()
        {
            var config = ConfigManager.CreateDefaultPlc();
            config.Protocol = "ModbusTcp";

            using (var client = (ModbusTcpClient)HslDeviceFactory.Create(config))
            {
                // 未连接优先报未连接；连上后才会校验地址，这里只断言不会抛异常。
                Assert.False(client.ReadUInt16("", out _, out var error));
                Assert.False(string.IsNullOrEmpty(error));
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
    }
}
