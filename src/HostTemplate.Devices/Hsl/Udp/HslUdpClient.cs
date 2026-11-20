using HslCommunication.Core.Pipe;
using HostTemplate.Devices.Hsl;

namespace HostTemplate.Devices.Hsl.Udp
{
    /// <summary>
    /// 原始 UDP 客户端，包在 HSL <see cref="PipeUdpNet"/> 上。
    /// 打开、发送、接收调用 OpenCommunicationAsync / SendAsync / ReceiveAsync。
    /// <c>HslCommunication.Enthernet.NetUdpClient</c> 是 HSL 自己的带 NetHandle 的协议，而且 12.8.3 没有对应的 Async 读方法，这里不包它。
    /// </summary>
    public sealed class HslUdpClient : HslPipeChannel
    {
        public HslUdpClient(string ipAddress, int port, int timeoutMs = 2000)
            : base("Udp", CreatePipe(ipAddress, port, timeoutMs), timeoutMs)
        {
            IpAddress = ipAddress ?? string.Empty;
            Port = port;
        }

        public string IpAddress { get; }

        public int Port { get; }

        /// <summary>原始 UDP 管道。</summary>
        public PipeUdpNet RawUdp => RawPipe as PipeUdpNet;

        private static PipeUdpNet CreatePipe(string ipAddress, int port, int timeoutMs)
        {
            var timeout = timeoutMs > 0 ? timeoutMs : 2000;
            return new PipeUdpNet(ipAddress, port)
            {
                ConnectTimeOut = timeout,
                ReceiveTimeOut = timeout,
            };
        }
    }
}
