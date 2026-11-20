using HslCommunication.ModBus;

namespace HostTemplate.Devices.Plc.Modbus
{
    /// <summary>
    /// 通用 Modbus TCP 客户端。温控表、远程 IO、传感器大多走这个协议。
    ///
    /// 地址写法沿用 HSL 的规则：
    ///   保持寄存器 "0" / "100"（4x 区，读写）
    ///   线圈       "0" 配合 ReadCoil，或 "coil=0"
    ///   离散输入   "dis=0"
    ///   输入寄存器 "x=3;0"
    /// </summary>
    public sealed class ModbusTcpClient : ModbusClientBase
    {
        public ModbusTcpClient(
            string ipAddress,
            int port = 502,
            byte station = 1,
            int connectTimeoutMs = 2000,
            int receiveTimeoutMs = 2000)
            : base("ModbusTcp", new ModbusTcpNet(ipAddress, port <= 0 ? 502 : port, station)
            {
                ConnectTimeOut = connectTimeoutMs,
                ReceiveTimeOut = receiveTimeoutMs,
            })
        {
        }
    }
}
