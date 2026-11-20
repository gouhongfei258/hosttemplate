using HslCommunication.Profinet.Delta;

namespace HostTemplate.Devices.Plc.Modbus
{
    /// <summary>
    /// 台达 PLC（DVP/AS 系列，Modbus TCP 兼容）。
    /// 地址按 Modbus 写法：D0 写 0，M0 用线圈读写。
    /// </summary>
    public sealed class DeltaTcpClient : ModbusClientBase
    {
        public DeltaTcpClient(
            string ipAddress,
            int port = 502,
            byte station = 1,
            int connectTimeoutMs = 2000,
            int receiveTimeoutMs = 2000)
            : base("DeltaTcp", new DeltaTcpNet(ipAddress, port <= 0 ? 502 : port, station)
            {
                ConnectTimeOut = connectTimeoutMs,
                ReceiveTimeOut = receiveTimeoutMs,
            })
        {
        }
    }
}
