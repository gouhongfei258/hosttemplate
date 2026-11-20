namespace HostTemplate.Devices.Tcp
{
    /// <summary>
    /// TCP 外设基类（打印机、扫码枪、仪器等）。
    /// 连接与收发已经落到 <see cref="HslTcpClient"/>（HSL 的 PipeTcpNet），不再另维护一套 Socket。
    /// 子类只需关心「发什么帧、怎么解析回包」。
    /// </summary>
    public abstract class TcpDeviceBase : HslTcpClient
    {
        protected TcpDeviceBase(string name, string ipAddress, int port, int timeoutMs = 2000)
            : base(name, ipAddress, port, timeoutMs)
        {
        }
    }
}
