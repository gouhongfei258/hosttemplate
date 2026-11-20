using System;
using HslCommunication.Profinet.Omron;

namespace HostTemplate.Devices.Hsl
{
    /// <summary>
    /// 欧姆龙 FINS 协议（NJ / NX / CJ / CS 系列，走以太网 FINS/UDP-TCP）。
    ///
    /// FINS 需要节点号：DA1 是 PLC 节点，SA1 是上位机节点。
    /// HSL 默认 DA1=0，直连时通常要等于 PLC 的 IP 末位，所以这里：
    ///   node &gt; 0 用配置值；node = 0 自动取 IP 最后一段。
    /// </summary>
    public sealed class OmronFinsClient : HslDeviceBase
    {
        private readonly OmronFinsNet _fins;

        public OmronFinsClient(
            string ipAddress,
            int port = 9600,
            int node = 0,
            int connectTimeoutMs = 2000,
            int receiveTimeoutMs = 2000)
        {
            _fins = new OmronFinsNet(ipAddress, port <= 0 ? 9600 : port)
            {
                ConnectTimeOut = connectTimeoutMs,
                ReceiveTimeOut = receiveTimeoutMs,
            };

            _fins.SA1 = 1;
            _fins.DA1 = (byte)(node > 0 ? Math.Min(node, 254) : LastOctet(ipAddress));

            Name = "OmronFins";
            Address = $"{ipAddress}:{_fins.Port}";
            Device = _fins;
            ConnectAction = () => _fins.ConnectServer();
            CloseAction = () => _fins.ConnectClose();
        }

        /// <summary>底层 FINS 对象。DNA/DA2 等高级参数可直接改。</summary>
        public OmronFinsNet Fins => _fins;

        /// <summary>实际使用的 PLC 节点号。</summary>
        public byte Node => _fins.DA1;

        private static int LastOctet(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                return 1;
            }

            var parts = ipAddress.Trim().Split('.');
            if (parts.Length == 4 && byte.TryParse(parts[3], out var last))
            {
                return last;
            }

            return 1;
        }
    }
}
