using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HslCommunication.Profinet.OpenProtocol;
using HostTemplate.Core.Abstractions;

namespace HostTemplate.Devices.Hsl.OpenProtocol
{
    /// <summary>
    /// Open Protocol（拧紧枪）客户端，包在 HSL <see cref="OpenProtocolNet"/> 上。
    /// 连接调用 ConnectServerAsync，自定义 MID 调用 ReadCustomerAsync，断开调用 ConnectCloseAsync。
    /// 未连接时读命令直接失败，不隐式重连。
    ///
    /// 12.8.3 的 JobMessage / TighteningResultMessages / ToolMessages / TimeMessages / ParameterSetMessages
    /// 只有同步方法，没有 Async 版本，所以不在这里再包一层。需要时用 <see cref="RawDevice"/>。
    /// </summary>
    public sealed class HslOpenProtocolClient : IDisposable, IAsyncDisposable
    {
        private readonly OpenProtocolNet _net;
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private int _disposeStarted;

        public HslOpenProtocolClient(
            string ipAddress,
            int port = 4545,
            int connectTimeoutMs = 2000,
            int receiveTimeoutMs = 2000,
            int revisionOnConnected = 1)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                throw new ArgumentNullException(nameof(ipAddress));
            }

            var remotePort = port <= 0 ? 4545 : port;
            _net = new OpenProtocolNet(ipAddress, remotePort)
            {
                ConnectTimeOut = connectTimeoutMs > 0 ? connectTimeoutMs : 2000,
                ReceiveTimeOut = receiveTimeoutMs > 0 ? receiveTimeoutMs : 2000,
                RevisonOnConnected = revisionOnConnected,
            };

            Address = ipAddress + ":" + remotePort;
        }

        public string Address { get; }

        public bool IsConnected { get; private set; }

        public string LastError { get; private set; } = string.Empty;

        /// <summary>原始 Open Protocol 对象。订阅助手和 HSL 拼写为 Revison 的属性从这里走。</summary>
        public OpenProtocolNet RawDevice => _net;

        public event EventHandler<OpenEventArgs> MessageReceived
        {
            add => _net.OnReceivedOpenMessage += value;
            remove => _net.OnReceivedOpenMessage -= value;
        }

        public async Task<DeviceResult> OpenAsync(CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var result = await _net.ConnectServerAsync().ConfigureAwait(false);
                    var mapped = HslResults.From(result);
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

        public async Task CloseAsync(CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                IsConnected = false;
                try
                {
                    await _net.ConnectCloseAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    // 关闭失败不影响下次连接。
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>
        /// 按 MID 读一条自定义命令。参数名 revision 对应 HSL 的 revison（库里的拼写）。
        /// </summary>
        public async Task<DeviceResult<string>> ReadCustomerAsync(
            int mid,
            int revision,
            int stationId,
            int spindleId,
            IReadOnlyList<string> parameters,
            CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!IsConnected)
                {
                    LastError = "设备未连接";
                    return DeviceResult<string>.Fail(LastError);
                }

                try
                {
                    List<string> copy = null;
                    if (parameters != null)
                    {
                        copy = new List<string>(parameters.Count);
                        for (var i = 0; i < parameters.Count; i++)
                        {
                            copy.Add(parameters[i]);
                        }
                    }

                    var result = await _net.ReadCustomerAsync(mid, revision, stationId, spindleId, copy).ConfigureAwait(false);
                    var mapped = HslResults.From(result);
                    if (!mapped.Success)
                    {
                        LastError = mapped.Error;
                        return mapped;
                    }

                    return DeviceResult<string>.Ok(mapped.Value ?? string.Empty);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LastError = ex.Message;
                    return DeviceResult<string>.Fail(ex.Message);
                }
            }
            finally
            {
                _gate.Release();
            }
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
