using System.Threading;
using System.Threading.Tasks;
using HslCommunication.ModBus;
using HostTemplate.Core.Abstractions;
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
            ConnectAction = () => device.ConnectServerAsync();
            CloseAction = () => device.ConnectCloseAsync();
        }

        /// <summary>底层 Modbus 对象，需要调本类未封装的方法时用。</summary>
        public ModbusTcpNet Modbus => _modbus;

        /// <summary>读线圈（0x 区），比通用的 ReadBool 语义更明确。调用 HSL 的 ReadCoilAsync。</summary>
        public Task<DeviceResult<bool>> ReadCoilAsync(string address, CancellationToken cancellationToken = default)
        {
            return TryReadAsync(address, () => _modbus.ReadCoilAsync(address), cancellationToken);
        }

        /// <summary>读离散输入（1x 区）。调用 HSL 的 ReadDiscreteAsync。</summary>
        public Task<DeviceResult<bool>> ReadDiscreteAsync(string address, CancellationToken cancellationToken = default)
        {
            return TryReadAsync(address, () => _modbus.ReadDiscreteAsync(address), cancellationToken);
        }

        /// <summary>写单个保持寄存器（功能码 06）。调用 HSL 的 WriteOneRegisterAsync。</summary>
        public Task<DeviceResult> WriteOneRegisterAsync(string address, ushort value, CancellationToken cancellationToken = default)
        {
            return TryWriteAsync(address, () => _modbus.WriteOneRegisterAsync(address, value), cancellationToken);
        }

        /// <summary>写掩码（功能码 22），改寄存器里的某几位而不影响其它位。调用 HSL 的 WriteMaskAsync。</summary>
        public Task<DeviceResult> WriteMaskAsync(string address, ushort andMask, ushort orMask, CancellationToken cancellationToken = default)
        {
            return TryWriteAsync(address, () => _modbus.WriteMaskAsync(address, andMask, orMask), cancellationToken);
        }
    }
}
