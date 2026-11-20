using System;
using System.Threading;
using System.Threading.Tasks;

namespace HostTemplate.Core.Abstractions
{
    /// <summary>
    /// PLC 通信抽象。引擎只依赖此接口，换 PLC 品牌只换实现。
    /// 读写返回 <see cref="DeviceResult"/>，不抛异常——主循环不能被单次通信失败打断。
    /// HSL 12.8.3 的 Async 方法本身不接收 <see cref="CancellationToken"/>，令牌只用于取消排队，并在真正发起调用前检查一次。
    /// </summary>
    public interface IPlcClient : IDisposable, IAsyncDisposable
    {
        string Address { get; }

        bool IsConnected { get; }

        Task<DeviceResult> OpenAsync(CancellationToken cancellationToken = default);

        Task CloseAsync(CancellationToken cancellationToken = default);

        Task<DeviceResult<bool>> ReadBoolAsync(string address, CancellationToken cancellationToken = default);

        Task<DeviceResult<bool[]>> ReadBoolArrayAsync(string address, int count, CancellationToken cancellationToken = default);

        Task<DeviceResult<ushort>> ReadWordAsync(string address, CancellationToken cancellationToken = default);

        Task<DeviceResult<ushort[]>> ReadWordArrayAsync(string address, int count, CancellationToken cancellationToken = default);

        Task<DeviceResult<int>> ReadInt32Async(string address, CancellationToken cancellationToken = default);

        Task<DeviceResult<float>> ReadRealAsync(string address, CancellationToken cancellationToken = default);

        Task<DeviceResult<string>> ReadStringAsync(string address, int length, CancellationToken cancellationToken = default);

        Task<DeviceResult> WriteBoolAsync(string address, bool value, CancellationToken cancellationToken = default);

        Task<DeviceResult> WriteWordAsync(string address, ushort value, CancellationToken cancellationToken = default);

        Task<DeviceResult> WriteInt32Async(string address, int value, CancellationToken cancellationToken = default);

        Task<DeviceResult> WriteRealAsync(string address, float value, CancellationToken cancellationToken = default);

        Task<DeviceResult> WriteStringAsync(string address, string value, CancellationToken cancellationToken = default);
    }
}
