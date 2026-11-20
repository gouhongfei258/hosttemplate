using System;
using System.Threading;
using System.Threading.Tasks;
using HslCommunication.Core.Pipe;
using HostTemplate.Core.Abstractions;

namespace HostTemplate.Devices.Hsl
{
    /// <summary>
    /// 字节流通道。TCP / UDP / 串口都落到 HSL 的 <see cref="CommunicationPipe"/>，
    /// 打开、发送、接收、关闭分别调用 OpenCommunicationAsync / SendAsync / ReceiveAsync / CloseCommunicationAsync。
    /// 未连接时直接失败，不在读写里隐式重连。
    /// </summary>
    public class HslPipeChannel : IDevice
    {
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private readonly CommunicationPipe _pipe;
        private int _disposeStarted;

        protected HslPipeChannel(string name, CommunicationPipe pipe, int timeoutMs)
        {
            if (pipe == null)
            {
                throw new ArgumentNullException(nameof(pipe));
            }

            Name = string.IsNullOrWhiteSpace(name) ? "Pipe" : name;
            _pipe = pipe;
            TimeoutMs = timeoutMs > 0 ? timeoutMs : 2000;
            _pipe.ReceiveTimeOut = TimeoutMs;
        }

        public string Name { get; }

        public int TimeoutMs { get; }

        public bool IsConnected { get; private set; }

        public string LastError { get; private set; } = string.Empty;

        /// <summary>原始 HSL 管道。模板没包到的收发方式从这里走。</summary>
        public CommunicationPipe RawPipe => _pipe;

        public virtual async Task<DeviceResult> OpenAsync(CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    // OpenCommunicationAsync 返回 OperateResult<bool>。这里只要成败，不要那个布尔值。
                    var result = await _pipe.OpenCommunicationAsync().ConfigureAwait(false);
                    var mapped = HslResults.From((HslCommunication.OperateResult)result);
                    IsConnected = mapped.Success;
                    LastError = mapped.Success ? string.Empty : mapped.Error;
                    return mapped;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    IsConnected = false;
                    LastError = ex.Message;
                    return DeviceResult.Fail(ex.Message);
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        public virtual async Task CloseAsync(CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                IsConnected = false;
                try
                {
                    await _pipe.CloseCommunicationAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    // 关闭失败不影响下次打开。
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        public virtual Task<DeviceResult> SendAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (data == null)
            {
                return Task.FromResult(DeviceResult.Fail("发送数据为空"));
            }

            return ExchangeAsync(
                async () =>
                {
                    var result = await _pipe.SendAsync(data).ConfigureAwait(false);
                    return HslResults.From(result);
                },
                cancellationToken);
        }

        public virtual Task<DeviceResult<byte[]>> ReceiveAsync(int maxLength, CancellationToken cancellationToken = default)
        {
            if (maxLength <= 0)
            {
                return Task.FromResult(DeviceResult<byte[]>.Fail("长度无效"));
            }

            // 长度传负数时，HSL 的 ReceiveAsync 只取当前这一包，不等待凑满缓冲区。
            return ReceiveCoreAsync(maxLength, exact: false, cancellationToken);
        }

        /// <summary>一直读到凑满 <paramref name="length"/> 字节，或超时。对应 HSL ReceiveAsync 的正长度重载。</summary>
        public virtual Task<DeviceResult<byte[]>> ReceiveExactAsync(int length, CancellationToken cancellationToken = default)
        {
            if (length <= 0)
            {
                return Task.FromResult(DeviceResult<byte[]>.Fail("长度无效"));
            }

            return ReceiveCoreAsync(length, exact: true, cancellationToken);
        }

        private Task<DeviceResult<byte[]>> ReceiveCoreAsync(int length, bool exact, CancellationToken cancellationToken)
        {
            return ExchangeAsync(
                async () =>
                {
                    var buffer = new byte[length];
                    var expect = exact ? length : -1;
                    var result = await _pipe.ReceiveAsync(buffer, 0, expect, TimeoutMs, null).ConfigureAwait(false);
                    var mapped = HslResults.From(result);
                    if (!mapped.Success)
                    {
                        return DeviceResult<byte[]>.Fail(mapped.Error);
                    }

                    var count = mapped.Value;
                    if (count < 0 || count > buffer.Length)
                    {
                        return DeviceResult<byte[]>.Fail("接收长度异常");
                    }

                    if (count == buffer.Length)
                    {
                        return DeviceResult<byte[]>.Ok(buffer);
                    }

                    var payload = new byte[count];
                    Buffer.BlockCopy(buffer, 0, payload, 0, count);
                    return DeviceResult<byte[]>.Ok(payload);
                },
                cancellationToken);
        }

        private async Task<T> ExchangeAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
            where T : struct
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!IsConnected)
                {
                    LastError = "设备未连接";
                    return Fail<T>(LastError);
                }

                try
                {
                    var result = await action().ConfigureAwait(false);
                    if (!IsSuccess(result))
                    {
                        LastError = ErrorOf(result);
                        IsConnected = false;
                        try
                        {
                            await _pipe.CloseCommunicationAsync().ConfigureAwait(false);
                        }
                        catch
                        {
                            // 失败后的关闭只是收尾。
                        }
                    }

                    return result;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LastError = ex.Message;
                    IsConnected = false;
                    return Fail<T>(ex.Message);
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        private static bool IsSuccess<T>(T result)
        {
            if (result is DeviceResult plain)
            {
                return plain.Success;
            }

            if (result is DeviceResult<byte[]> bytes)
            {
                return bytes.Success;
            }

            return false;
        }

        private static string ErrorOf<T>(T result)
        {
            if (result is DeviceResult plain)
            {
                return plain.Error;
            }

            if (result is DeviceResult<byte[]> bytes)
            {
                return bytes.Error;
            }

            return string.Empty;
        }

        private static T Fail<T>(string error)
        {
            if (typeof(T) == typeof(DeviceResult))
            {
                return (T)(object)DeviceResult.Fail(error);
            }

            return (T)(object)DeviceResult<byte[]>.Fail(error);
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
                // 释放阶段吞掉关闭异常。
            }

            _gate.Dispose();
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
