using System.IO.Ports;
using HostTemplate.Devices.Hsl.Http;
using HostTemplate.Devices.Hsl.OpenProtocol;
using HostTemplate.Devices.Hsl.Serial;
using HostTemplate.Devices.Hsl.Udp;
using HostTemplate.Devices.Tcp;

namespace HostTemplate.Devices.Hsl
{
    /// <summary>
    /// 工站侧创建 HSL 传输对象的入口。PLC 仍走 <see cref="HslDeviceFactory"/>。
    /// 这些传输不进工站配置加载顺序，调用方自己决定什么时候 new。
    /// </summary>
    public static class HslTransportFactory
    {
        public static HslTcpClient CreateTcp(string ipAddress, int port, int timeoutMs = 2000)
        {
            return new HslTcpClient(ipAddress, port, timeoutMs);
        }

        public static HslUdpClient CreateUdp(string ipAddress, int port, int timeoutMs = 2000)
        {
            return new HslUdpClient(ipAddress, port, timeoutMs);
        }

        public static HslSerialClient CreateSerial(
            string portName,
            int baudRate = 9600,
            int dataBits = 8,
            StopBits stopBits = StopBits.One,
            Parity parity = Parity.None,
            int timeoutMs = 2000)
        {
            return new HslSerialClient(portName, baudRate, dataBits, stopBits, parity, timeoutMs);
        }

        public static HslHttpClient CreateHttp(string host, int port = 80, int timeoutMs = 5000)
        {
            return new HslHttpClient(host, port, timeoutMs);
        }

        public static HslHttpServer CreateHttpServer()
        {
            return new HslHttpServer();
        }

        public static HslOpenProtocolClient CreateOpenProtocol(
            string ipAddress,
            int port = 4545,
            int timeoutMs = 2000,
            int revisionOnConnected = 1)
        {
            return new HslOpenProtocolClient(ipAddress, port, timeoutMs, timeoutMs, revisionOnConnected);
        }

        public static HslOpenProtocolServer CreateOpenProtocolServer()
        {
            return new HslOpenProtocolServer();
        }
    }
}
