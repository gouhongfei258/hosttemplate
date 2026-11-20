using System;

namespace HostTemplate.Core.Abstractions
{
    /// <summary>
    /// 通用外设抽象（扫码枪、打印机、仪器）。具体协议在实现里解析帧。
    /// </summary>
    public interface IDevice : IDisposable
    {
        string Name { get; }

        bool IsConnected { get; }

        bool Open(out string error);

        void Close();

        bool Send(byte[] data, out string error);

        bool Receive(out byte[] data, out string error);
    }
}
