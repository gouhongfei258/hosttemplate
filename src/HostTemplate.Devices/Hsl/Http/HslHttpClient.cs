using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HslCommunication.Core.Net;
using HostTemplate.Core.Abstractions;

namespace HostTemplate.Devices.Hsl.Http
{
    /// <summary>
    /// 通用 HTTP 客户端，包在 HSL <see cref="NetworkWebApiBase"/> 上。
    /// Get / Post / Request 调用 GetAsync / PostAsync / RequestAsync。
    ///
    /// 这不是 MES 的 <see cref="HostTemplate.Devices.Api.HttpApiClient"/>。
    /// MES 客户端按 api.xml 的端点键发 POST，带重试，并且在接口关闭时过站放行。
    /// 这里是给工站直接访问一台设备或服务的 HTTP，主机、端口、超时由调用方传入。
    ///
    /// <c>DeviceWebApi</c> 是「走 WebAPI 的设备」基类，带同样的 Get/Post，另外还继承地址读写。
    /// 通用 HTTP 用 <see cref="NetworkWebApiBase"/>，设备对象从 <see cref="RawClient"/> 拿不到时再自己 new DeviceWebApi。
    /// </summary>
    public sealed class HslHttpClient : IDisposable
    {
        private readonly NetworkWebApiBase _api;
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private int _disposeStarted;

        public HslHttpClient(string host, int port = 80, int timeoutMs = 5000)
            : this(host, port, null, null, timeoutMs)
        {
        }

        public HslHttpClient(string host, int port, string userName, string password, int timeoutMs = 5000)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentNullException(nameof(host));
            }

            _api = string.IsNullOrEmpty(userName)
                ? new NetworkWebApiBase(host, port)
                : new NetworkWebApiBase(host, port, userName, password ?? string.Empty);
            _api.Timeout = timeoutMs > 0 ? timeoutMs : 5000;
        }

        public string Host => _api.Host;

        public int Port => _api.Port;

        /// <summary>原始 HSL HTTP 客户端。要改内容类型、Https、请求头时用它。</summary>
        public NetworkWebApiBase RawClient => _api;

        public Task<DeviceResult<string>> GetAsync(string rawUrl, CancellationToken cancellationToken = default)
        {
            return CallAsync(() => _api.GetAsync(rawUrl), cancellationToken);
        }

        public Task<DeviceResult<string>> PostAsync(string rawUrl, string body, CancellationToken cancellationToken = default)
        {
            return CallAsync(() => _api.PostAsync(rawUrl, body ?? string.Empty), cancellationToken);
        }

        public Task<DeviceResult<string>> RequestAsync(
            HttpMethod method,
            string rawUrl,
            string body,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            if (method == null)
            {
                return Task.FromResult(DeviceResult<string>.Fail("HTTP 方法为空"));
            }

            return CallAsync(() => _api.RequestAsync(method, rawUrl, body, contentType), cancellationToken);
        }

        private async Task<DeviceResult<string>> CallAsync(
            Func<Task<HslCommunication.OperateResult<string>>> action,
            CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var result = await action().ConfigureAwait(false);
                    var mapped = HslResults.From(result);
                    if (!mapped.Success)
                    {
                        return mapped;
                    }

                    return DeviceResult<string>.Ok(mapped.Value ?? string.Empty);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    return DeviceResult<string>.Fail(ex.Message);
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
            {
                return;
            }

            try
            {
                _api.Client?.Dispose();
            }
            catch
            {
                // HttpClient 释放失败不影响调用方。
            }

            _gate.Dispose();
        }
    }
}
