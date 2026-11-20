using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using HslCommunication.Enthernet;
using HostTemplate.Core.Abstractions;

namespace HostTemplate.Devices.Hsl.Http
{
    /// <summary>
    /// HTTP 服务，包在 HSL <see cref="HttpServer"/> 上。
    /// 12.8.3 只有同步的 Start(int) / Start(string) / Close，没有 StartAsync。
    /// 这里的 StartAsync / StopAsync 调用的就是这两个同步方法。
    /// Start(int) 监听 <c>http://+:端口/</c>；Start(string) 用调用方给的前缀，HSL 不会把端口写回 Port 属性，所以本类自己记住。
    /// </summary>
    public sealed class HslHttpServer : IDisposable
    {
        private readonly HttpServer _server = new HttpServer();
        private int _disposeStarted;

        /// <summary>原始 HSL HTTP 服务。注册 RPC、跨域、日志都从这里走。</summary>
        public HttpServer RawServer => _server;

        public bool IsStarted { get; private set; }

        /// <summary>最近一次成功监听的端口。用前缀启动时按 URL 解析，不读 HSL 的 Port（前缀重载不会更新它）。</summary>
        public int Port { get; private set; }

        /// <summary>
        /// 请求处理函数，转给 HSL 的 HandleRequestFunc。
        /// 返回值会作为响应正文。返回 null 时 HSL 不写正文。
        /// </summary>
        public Func<HttpListenerRequest, HttpListenerResponse, string, object> HandleRequest
        {
            get => _server.HandleRequestFunc;
            set => _server.HandleRequestFunc = value;
        }

        public bool IsCrossDomain
        {
            get => _server.IsCrossDomain;
            set => _server.IsCrossDomain = value;
        }

        public Task<DeviceResult> StartAsync(int port, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (port <= 0 || port > 65535)
            {
                return Task.FromResult(DeviceResult.Fail("端口无效"));
            }

            if (IsStarted)
            {
                return Task.FromResult(DeviceResult.Fail("服务已在监听"));
            }

            try
            {
                _server.Start(port);
                Port = port;
                IsStarted = true;
                return Task.FromResult(DeviceResult.Ok());
            }
            catch (Exception ex)
            {
                IsStarted = false;
                return Task.FromResult(DeviceResult.Fail(ex.Message));
            }
        }

        public Task<DeviceResult> StartAsync(string prefix, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(prefix))
            {
                return Task.FromResult(DeviceResult.Fail("监听前缀为空"));
            }

            if (IsStarted)
            {
                return Task.FromResult(DeviceResult.Fail("服务已在监听"));
            }

            try
            {
                _server.Start(prefix);
                Port = ParsePort(prefix);
                IsStarted = true;
                return Task.FromResult(DeviceResult.Ok());
            }
            catch (Exception ex)
            {
                IsStarted = false;
                return Task.FromResult(DeviceResult.Fail(ex.Message));
            }
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CloseServer();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
            {
                return;
            }

            CloseServer();
        }

        private void CloseServer()
        {
            try
            {
                _server.Close();
            }
            catch
            {
                // 已经关掉时再关一次，忽略。
            }

            IsStarted = false;
        }

        private static int ParsePort(string prefix)
        {
            if (Uri.TryCreate(prefix, UriKind.Absolute, out var uri) && uri.Port > 0)
            {
                return uri.Port;
            }

            return 0;
        }
    }
}
