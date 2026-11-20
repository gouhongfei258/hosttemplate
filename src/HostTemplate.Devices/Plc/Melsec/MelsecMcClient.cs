using HslCommunication.Profinet.Melsec;
using HostTemplate.Devices.Hsl;

namespace HostTemplate.Devices.Plc.Melsec
{
    /// <summary>
    /// 三菱 MC 协议（Q / L / FX5U / iQ-R，走以太网 MC 3E 帧）。
    /// 地址按三菱写法：D100、M100、X0、Y0、W100。
    /// </summary>
    public sealed class MelsecMcClient : HslDeviceBase
    {
        private readonly MelsecMcNet _mc;

        public MelsecMcClient(
            string ipAddress,
            int port = 6000,
            int connectTimeoutMs = 2000,
            int receiveTimeoutMs = 2000)
        {
            _mc = new MelsecMcNet(ipAddress, port <= 0 ? 6000 : port)
            {
                ConnectTimeOut = connectTimeoutMs,
                ReceiveTimeOut = receiveTimeoutMs,
            };

            Name = "MelsecMc";
            Address = $"{ipAddress}:{_mc.Port}";
            Device = _mc;
            ConnectAction = () => _mc.ConnectServer();
            CloseAction = () => _mc.ConnectClose();
        }

        /// <summary>底层 MC 对象。网络号/PLC 号等参数可直接改。</summary>
        public MelsecMcNet Mc => _mc;
    }
}
