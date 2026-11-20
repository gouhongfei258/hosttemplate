using HslCommunication.ModBus;
using HostTemplate.Devices.Hsl;

namespace HostTemplate.Devices.Plc.Modbus
{
    /// <summary>
    /// Modbus 家族基类。通用 Modbus TCP、汇川、台达都继承自 HSL 的
    /// <see cref="ModbusTcpNet"/>，只是 series 不同，所以把这部分抽出来。
    /// </summary>
    public abstract class ModbusClientBase : HslDeviceBase
    {
        private readonly ModbusTcpNet _modbus;

        protected ModbusClientBase(string name, ModbusTcpNet device)
        {
            _modbus = device;

            Name = name;
            Address = $"{device.IpAddress}:{device.Port}";
            Device = device;
            ConnectAction = () => device.ConnectServer();
            CloseAction = () => device.ConnectClose();
        }

        /// <summary>底层 Modbus 对象，需要调本类未封装的方法时用。</summary>
        public ModbusTcpNet Modbus => _modbus;

        /// <summary>读线圈（0x 区），比通用的 ReadBool 语义更明确。</summary>
        public bool ReadCoil(string address, out bool value, out string error)
        {
            return TryRead(address, () => _modbus.ReadCoil(address), out value, out error);
        }

        /// <summary>读离散输入（1x 区）。</summary>
        public bool ReadDiscrete(string address, out bool value, out string error)
        {
            return TryRead(address, () => _modbus.ReadDiscrete(address), out value, out error);
        }

        /// <summary>写单个保持寄存器（功能码 06）。</summary>
        public bool WriteOneRegister(string address, ushort value, out string error)
        {
            return TryWrite(address, () => _modbus.WriteOneRegister(address, value), out error);
        }

        /// <summary>写掩码（功能码 22），改寄存器里的某几位而不影响其它位。</summary>
        public bool WriteMask(string address, ushort andMask, ushort orMask, out string error)
        {
            return TryWrite(address, () => _modbus.WriteMask(address, andMask, orMask), out error);
        }
    }
}
