using System;
using System.Threading;
using System.Threading.Tasks;

namespace HostTemplate.Core.Abstractions
{
    /// <summary>
    /// 字节流外设（扫码枪、打印机、网口仪器、串口）。具体协议在实现里解析帧。
    /// </summary>
    public interface IDevice : IDisposable, IAsyncDisposable
    {
        string Name { get; }

        bool IsConnected { get; }

        Task<DeviceResult> OpenAsync(CancellationToken cancellationToken = default);

        Task CloseAsync(CancellationToken cancellationToken = default);

        Task<DeviceResult> SendAsync(byte[] data, CancellationToken cancellationToken = default);

        /// <summary>
        /// 读当前已经到达的一截数据，最多 <paramref name="maxLength"/> 字节。
        /// 不等待凑满长度；凑满定长帧用各实现上的 ReceiveExactAsync。
        /// </summary>
        Task<DeviceResult<byte[]>> ReceiveAsync(int maxLength, CancellationToken cancellationToken = default);
    }
}
