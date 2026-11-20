using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using HslCommunication;
using HslCommunication.Core.Device;
using HostTemplate.Core.Abstractions;

namespace HostTemplate.Devices.Hsl
{
    /// <summary>
    /// HslCommunication 设备统一基类。
    ///
    /// 为什么要这一层：HSL 的 S7 / Modbus / 三菱 / 欧姆龙 / 汇川等协议，
    /// 公开方法全部落在 <see cref="DeviceCommunication"/> 上（ReadBoolAsync / ReadInt32Async /
    /// ReadFloatAsync / WriteAsync / WaitAsync …），差异只在「怎么连」。所以把连接和读写拆开：
    ///
    ///   基类：连接守卫 + 串行门闩 + 全类型读写（异步），返回 <see cref="DeviceResult"/>，不抛异常；
    ///   子类：只负责 new 出具体协议对象、给出 ConnectAction / CloseAction。
    ///
    /// 新增一个品牌的 PLC 只要改连接动作（见 <see cref="HostTemplate.Devices.Plc.Modbus.ModbusTcpClient"/>）。
    ///
    /// 设计取舍：
    ///   1. 全部返回 <see cref="DeviceResult"/>，和 <see cref="IPlcClient"/> 一致，主循环不需要 try/catch；
    ///   2. HSL 对象不是线程安全的。异步路径用 <see cref="SemaphoreSlim"/>，不在 await 两侧持有阻塞锁；
    ///   3. 未连接时直接返回失败，不隐式重连——重连节奏由引擎控制，避免现场抖动时反复建连；
    ///   4. 读写调用的是 HSL 自己的 *Async 方法（ConnectServerAsync / Read*Async / Write*Async / WaitAsync）。
    ///      12.8.3 里不少 *Async 仍是 Task.Run 包同步实现（管道基类就是这样，串口因此也是）。
    ///      公开 API 仍然走这些异步方法，不在模板里再包一层同步调用。
    ///      HSL 的 Async 不接收 CancellationToken，令牌只取消排队，并在发起调用前检查一次。
    /// </summary>
    public abstract class HslDeviceBase : IPlcClient
    {
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private int _disposeStarted;

        /// <summary>底层 HSL 设备对象。子类在构造里赋值。</summary>
        protected DeviceCommunication Device { get; set; }

        /// <summary>建连动作，由子类按协议提供。应调用 HSL 的 ConnectServerAsync。</summary>
        protected Func<Task<OperateResult>> ConnectAction { get; set; }

        /// <summary>断开动作，由子类按协议提供。应调用 HSL 的 ConnectCloseAsync。</summary>
        protected Func<Task<OperateResult>> CloseAction { get; set; }

        /// <summary>设备名，用于日志。</summary>
        public string Name { get; protected set; } = "HslDevice";

        public string Address { get; protected set; } = string.Empty;

        public bool IsConnected { get; protected set; }

        /// <summary>最近一次失败的原因，便于界面直接展示。</summary>
        public string LastError { get; private set; } = string.Empty;

        /// <summary>原始 HSL 对象。要调本类没包到的方法时用它。</summary>
        public DeviceCommunication RawDevice => Device;

        public async Task<DeviceResult> OpenAsync(CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (Device == null)
                {
                    LastError = "设备未初始化";
                    return DeviceResult.Fail(LastError);
                }

                try
                {
                    var result = ConnectAction == null ? null : await ConnectAction().ConfigureAwait(false);
                    if (result != null && !result.IsSuccess)
                    {
                        LastError = result.Message ?? string.Empty;
                        IsConnected = false;
                        return DeviceResult.Fail(LastError);
                    }

                    IsConnected = true;
                    LastError = string.Empty;
                    return DeviceResult.Ok();
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LastError = ex.Message;
                    IsConnected = false;
                    return DeviceResult.Fail(ex.Message);
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task CloseAsync(CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                IsConnected = false;

                try
                {
                    if (CloseAction != null)
                    {
                        await CloseAction().ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    // 关闭失败不影响下次重连。
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>Ping 一下设备 IP，用来区分「网线没插」和「PLC 没开」。HSL 这里只有同步的 IpAddressPing。</summary>
        public bool Ping()
        {
            try
            {
                return Device is DeviceTcpNet tcp && tcp.IpAddressPing() == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>长连接模式。部分交换机/网关会主动断空闲连接，开这个可减少重连。HSL 此方法仍是同步的。</summary>
        public void EnablePersistentConnection()
        {
            try
            {
#pragma warning disable CS0612 // HSL 标记为过时，但现场仍在用，保留这个开关。
                (Device as DeviceTcpNet)?.SetPersistentConnection();
#pragma warning restore CS0612
            }
            catch
            {
                // 个别协议不支持，忽略。
            }
        }

        // ---------------- 读 ----------------

        public Task<DeviceResult<bool>> ReadBoolAsync(string address, CancellationToken cancellationToken = default)
        {
            return TryReadAsync(address, () => Device.ReadBoolAsync(address), cancellationToken);
        }

        public Task<DeviceResult<bool[]>> ReadBoolArrayAsync(string address, int count, CancellationToken cancellationToken = default)
        {
            return TryReadAsync(address, () => Device.ReadBoolAsync(address, ToUShort(count)), cancellationToken);
        }

        /// <summary>
        /// 读单个字节。HSL 的 <see cref="DeviceCommunication"/> 没有统一的 ReadByte，
        /// 这里用 ReadAsync 读 1 字节，所有协议行为一致。
        /// </summary>
        public async Task<DeviceResult<byte>> ReadByteAsync(string address, CancellationToken cancellationToken = default)
        {
            var read = await ReadBytesAsync(address, 1, cancellationToken).ConfigureAwait(false);
            if (!read.Success || read.Value == null || read.Value.Length == 0)
            {
                return DeviceResult<byte>.Fail(read.Error);
            }

            return DeviceResult<byte>.Ok(read.Value[0]);
        }

        public Task<DeviceResult<byte[]>> ReadBytesAsync(string address, int length, CancellationToken cancellationToken = default)
        {
            return TryReadAsync(address, () => Device.ReadAsync(address, ToUShort(length)), cancellationToken);
        }

        public Task<DeviceResult<short>> ReadInt16Async(string address, CancellationToken cancellationToken = default)
        {
            return TryReadAsync(address, () => Device.ReadInt16Async(address), cancellationToken);
        }

        public Task<DeviceResult<short[]>> ReadInt16ArrayAsync(string address, int count, CancellationToken cancellationToken = default)
        {
            return TryReadAsync(address, () => Device.ReadInt16Async(address, ToUShort(count)), cancellationToken);
        }

        public Task<DeviceResult<ushort>> ReadWordAsync(string address, CancellationToken cancellationToken = default)
        {
            return ReadUInt16Async(address, cancellationToken);
        }

        public Task<DeviceResult<ushort>> ReadUInt16Async(string address, CancellationToken cancellationToken = default)
        {
            return TryReadAsync(address, () => Device.ReadUInt16Async(address), cancellationToken);
        }

        public Task<DeviceResult<ushort[]>> ReadWordArrayAsync(string address, int count, CancellationToken cancellationToken = default)
        {
            return ReadUInt16ArrayAsync(address, count, cancellationToken);
        }

        public Task<DeviceResult<ushort[]>> ReadUInt16ArrayAsync(string address, int count, CancellationToken cancellationToken = default)
        {
            return TryReadAsync(address, () => Device.ReadUInt16Async(address, ToUShort(count)), cancellationToken);
        }

        public Task<DeviceResult<int>> ReadInt32Async(string address, CancellationToken cancellationToken = default)
        {
            return TryReadAsync(address, () => Device.ReadInt32Async(address), cancellationToken);
        }

        public Task<DeviceResult<int[]>> ReadInt32ArrayAsync(string address, int count, CancellationToken cancellationToken = default)
        {
            return TryReadAsync(address, () => Device.ReadInt32Async(address, ToUShort(count)), cancellationToken);
        }

        public Task<DeviceResult<uint>> ReadUInt32Async(string address, CancellationToken cancellationToken = default)
        {
            return TryReadAsync(address, () => Device.ReadUInt32Async(address), cancellationToken);
        }

        public Task<DeviceResult<uint[]>> ReadUInt32ArrayAsync(string address, int count, CancellationToken cancellationToken = default)
        {
            return TryReadAsync(address, () => Device.ReadUInt32Async(address, ToUShort(count)), cancellationToken);
        }

        public Task<DeviceResult<long>> ReadInt64Async(string address, CancellationToken cancellationToken = default)
        {
            return TryReadAsync(address, () => Device.ReadInt64Async(address), cancellationToken);
        }

        public Task<DeviceResult<ulong>> ReadUInt64Async(string address, CancellationToken cancellationToken = default)
        {
            return TryReadAsync(address, () => Device.ReadUInt64Async(address), cancellationToken);
        }

        public Task<DeviceResult<float>> ReadRealAsync(string address, CancellationToken cancellationToken = default)
        {
            return ReadFloatAsync(address, cancellationToken);
        }

        public Task<DeviceResult<float>> ReadFloatAsync(string address, CancellationToken cancellationToken = default)
        {
            return TryReadAsync(address, () => Device.ReadFloatAsync(address), cancellationToken);
        }

        public Task<DeviceResult<float[]>> ReadFloatArrayAsync(string address, int count, CancellationToken cancellationToken = default)
        {
            return TryReadAsync(address, () => Device.ReadFloatAsync(address, ToUShort(count)), cancellationToken);
        }

        public Task<DeviceResult<double>> ReadDoubleAsync(string address, CancellationToken cancellationToken = default)
        {
            return TryReadAsync(address, () => Device.ReadDoubleAsync(address), cancellationToken);
        }

        public Task<DeviceResult<double[]>> ReadDoubleArrayAsync(string address, int count, CancellationToken cancellationToken = default)
        {
            return TryReadAsync(address, () => Device.ReadDoubleAsync(address, ToUShort(count)), cancellationToken);
        }

        public async Task<DeviceResult<string>> ReadStringAsync(string address, int length, CancellationToken cancellationToken = default)
        {
            var read = await TryReadAsync(address, () => Device.ReadStringAsync(address, ToUShort(length)), cancellationToken)
                .ConfigureAwait(false);
            if (!read.Success)
            {
                return read;
            }

            return DeviceResult<string>.Ok(read.Value ?? string.Empty);
        }

        /// <summary>按结构体读取（HSL 的 ReadStructAsync）。T 为自定义类，字段顺序与 PLC 内存布局一致。</summary>
        public Task<DeviceResult<T>> ReadStructAsync<T>(string address, int length, CancellationToken cancellationToken = default)
            where T : class, new()
        {
            return TryReadAsync(address, () => Device.ReadStructAsync<T>(address, ToUShort(length)), cancellationToken);
        }

        // ---------------- 写 ----------------

        public Task<DeviceResult> WriteBoolAsync(string address, bool value, CancellationToken cancellationToken = default)
        {
            return TryWriteAsync(address, () => Device.WriteAsync(address, value), cancellationToken);
        }

        public Task<DeviceResult> WriteBoolArrayAsync(string address, bool[] values, CancellationToken cancellationToken = default)
        {
            return TryWriteAsync(address, () => Device.WriteAsync(address, values), cancellationToken);
        }

        /// <summary>
        /// 写单个字节。<see cref="DeviceCommunication"/> 没有 Write(string, byte) / WriteAsync(string, byte)。
        /// 原先同步封装经重载决议落到 Write(string, short)，这里显式调用 WriteAsync(string, short)，行为保持不变。
        /// </summary>
        public Task<DeviceResult> WriteByteAsync(string address, byte value, CancellationToken cancellationToken = default)
        {
            return TryWriteAsync(address, () => Device.WriteAsync(address, (short)value), cancellationToken);
        }

        public Task<DeviceResult> WriteBytesAsync(string address, byte[] values, CancellationToken cancellationToken = default)
        {
            return TryWriteAsync(address, () => Device.WriteAsync(address, values), cancellationToken);
        }

        public Task<DeviceResult> WriteInt16Async(string address, short value, CancellationToken cancellationToken = default)
        {
            return TryWriteAsync(address, () => Device.WriteAsync(address, value), cancellationToken);
        }

        public Task<DeviceResult> WriteWordAsync(string address, ushort value, CancellationToken cancellationToken = default)
        {
            return WriteUInt16Async(address, value, cancellationToken);
        }

        public Task<DeviceResult> WriteUInt16Async(string address, ushort value, CancellationToken cancellationToken = default)
        {
            return TryWriteAsync(address, () => Device.WriteAsync(address, value), cancellationToken);
        }

        public Task<DeviceResult> WriteInt32Async(string address, int value, CancellationToken cancellationToken = default)
        {
            return TryWriteAsync(address, () => Device.WriteAsync(address, value), cancellationToken);
        }

        public Task<DeviceResult> WriteUInt32Async(string address, uint value, CancellationToken cancellationToken = default)
        {
            return TryWriteAsync(address, () => Device.WriteAsync(address, value), cancellationToken);
        }

        public Task<DeviceResult> WriteInt64Async(string address, long value, CancellationToken cancellationToken = default)
        {
            return TryWriteAsync(address, () => Device.WriteAsync(address, value), cancellationToken);
        }

        public Task<DeviceResult> WriteUInt64Async(string address, ulong value, CancellationToken cancellationToken = default)
        {
            return TryWriteAsync(address, () => Device.WriteAsync(address, value), cancellationToken);
        }

        public Task<DeviceResult> WriteRealAsync(string address, float value, CancellationToken cancellationToken = default)
        {
            return WriteFloatAsync(address, value, cancellationToken);
        }

        public Task<DeviceResult> WriteFloatAsync(string address, float value, CancellationToken cancellationToken = default)
        {
            return TryWriteAsync(address, () => Device.WriteAsync(address, value), cancellationToken);
        }

        public Task<DeviceResult> WriteDoubleAsync(string address, double value, CancellationToken cancellationToken = default)
        {
            return TryWriteAsync(address, () => Device.WriteAsync(address, value), cancellationToken);
        }

        public Task<DeviceResult> WriteStringAsync(string address, string value, CancellationToken cancellationToken = default)
        {
            return TryWriteAsync(address, () => Device.WriteAsync(address, value ?? string.Empty), cancellationToken);
        }

        /// <summary>定长写字符串。PLC 侧是 String[n] 时必须用这个，避免长度不匹配。</summary>
        public Task<DeviceResult> WriteStringAsync(string address, string value, int length, CancellationToken cancellationToken = default)
        {
            return TryWriteAsync(address, () => Device.WriteAsync(address, value ?? string.Empty, length), cancellationToken);
        }

        // ---------------- 等待 ----------------

        /// <summary>
        /// 等某个位变成期望值。成功表示在超时前等到，Value 是实际耗时。
        /// 气缸到位、夹具夹紧这类握手用它比手写轮询清楚。
        /// </summary>
        public Task<DeviceResult<TimeSpan>> WaitBoolAsync(string address, bool expect, int timeoutMs, CancellationToken cancellationToken = default)
        {
            return TryReadAsync(address, () => Device.WaitAsync(address, expect, 20, timeoutMs), cancellationToken);
        }

        public Task<DeviceResult<TimeSpan>> WaitWordAsync(string address, ushort expect, int timeoutMs, CancellationToken cancellationToken = default)
        {
            return TryReadAsync(address, () => Device.WaitAsync(address, expect, 20, timeoutMs), cancellationToken);
        }

        public Task<DeviceResult<TimeSpan>> WaitInt32Async(string address, int expect, int timeoutMs, CancellationToken cancellationToken = default)
        {
            return TryReadAsync(address, () => Device.WaitAsync(address, expect, 20, timeoutMs), cancellationToken);
        }

        // ---------------- 内部 ----------------

        /// <summary>带连接守卫与串行门闩的读。子类需要自定义读取时复用它。</summary>
        protected Task<DeviceResult<T>> TryReadAsync<T>(string address, Func<Task<OperateResult<T>>> action, CancellationToken cancellationToken)
        {
            return InvokeAsync(address, action, cancellationToken);
        }

        /// <summary>不需要地址的调用（如读订货号）。</summary>
        protected Task<DeviceResult<T>> TryCallAsync<T>(Func<Task<OperateResult<T>>> action, CancellationToken cancellationToken)
        {
            return InvokeAsync(null, action, cancellationToken);
        }

        /// <summary>带连接守卫与串行门闩的写。</summary>
        protected async Task<DeviceResult> TryWriteAsync(string address, Func<Task<OperateResult>> action, CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var ready = EnsureReady(address);
                if (!ready.Success)
                {
                    return ready;
                }

                try
                {
                    var result = await action().ConfigureAwait(false);
                    var mapped = HslResults.From(result);
                    if (!mapped.Success)
                    {
                        LastError = mapped.Error;
                    }

                    return mapped;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LastError = ex.Message;
                    return DeviceResult.Fail(ex.Message);
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task<DeviceResult<T>> InvokeAsync<T>(string address, Func<Task<OperateResult<T>>> action, CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var ready = EnsureReady(address);
                if (!ready.Success)
                {
                    return DeviceResult<T>.Fail(ready.Error);
                }

                try
                {
                    var result = await action().ConfigureAwait(false);
                    var mapped = HslResults.From(result);
                    if (!mapped.Success)
                    {
                        LastError = mapped.Error;
                    }

                    return mapped;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LastError = ex.Message;
                    return DeviceResult<T>.Fail(ex.Message);
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        private DeviceResult EnsureReady(string address)
        {
            if (!IsConnected || Device == null)
            {
                LastError = "设备未连接";
                return DeviceResult.Fail(LastError);
            }

            if (address != null && string.IsNullOrWhiteSpace(address))
            {
                LastError = "地址为空";
                return DeviceResult.Fail(LastError);
            }

            return DeviceResult.Ok();
        }

        private static ushort ToUShort(int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            return count > ushort.MaxValue ? ushort.MaxValue : (ushort)count;
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
            {
                return;
            }

            try
            {
                await CloseAsync().ConfigureAwait(false);
            }
            catch
            {
                // 释放阶段不再把关闭失败抛出去。
            }

            _gate.Dispose();
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
