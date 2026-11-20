using HslCommunication.Profinet.Inovance;

namespace HostTemplate.Devices.Hsl
{
    /// <summary>
    /// 汇川 PLC（AM/H5U 等，Modbus TCP 兼容）。
    /// 地址按 Modbus 写法：D0 写 0，M0 用线圈读写。
    /// </summary>
    public sealed class InovanceTcpClient : ModbusClientBase
    {
        public InovanceTcpClient(
            string ipAddress,
            int port = 502,
            byte station = 1,
            int connectTimeoutMs = 2000,
            int receiveTimeoutMs = 2000)
            : base("InovanceTcp", new InovanceTcpNet(ipAddress, port <= 0 ? 502 : port, station)
            {
                ConnectTimeOut = connectTimeoutMs,
                ReceiveTimeOut = receiveTimeoutMs,
            })
        {
        }
    }
}
