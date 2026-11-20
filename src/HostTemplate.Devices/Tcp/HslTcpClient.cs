using HslCommunication.Core.Pipe;
using HostTemplate.Devices.Hsl;

namespace HostTemplate.Devices.Tcp
{
    /// <summary>
    /// 原始 TCP 客户端，包在 HSL <see cref="PipeTcpNet"/> 上。
    /// 连接、发送、接收走 OpenCommunicationAsync / SendAsync / ReceiveAsync，不使用 HSL 自己的应用层组帧。
    /// 需要按协议组帧时继承 <see cref="TcpDeviceBase"/>。
    /// 带报文头的问答式交换是另一个类型 <c>HslCommunication.Core.Net.TcpNetCommunication.ReadFromCoreServerAsync</c>，这里不混进去。
    /// </summary>
    public class HslTcpClient : HslPipeChannel
    {
        public HslTcpClient(string ipAddress, int port, int timeoutMs = 2000)
            : this("Tcp", ipAddress, port, timeoutMs)
        {
        }

        public HslTcpClient(string name, string ipAddress, int port, int timeoutMs = 2000)
            : base(name, CreatePipe(ipAddress, port, timeoutMs), timeoutMs)
        {
            IpAddress = ipAddress ?? string.Empty;
            Port = port;
        }

        public string IpAddress { get; }

        public int Port { get; }

        /// <summary>原始 TCP 管道。</summary>
        public PipeTcpNet RawTcp => RawPipe as PipeTcpNet;

        private static PipeTcpNet CreatePipe(string ipAddress, int port, int timeoutMs)
        {
            var timeout = timeoutMs > 0 ? timeoutMs : 2000;
            return new PipeTcpNet(ipAddress, port)
            {
                ConnectTimeOut = timeout,
                ReceiveTimeOut = timeout,
            };
        }
    }
}
