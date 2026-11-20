using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HslCommunication.Profinet.OpenProtocol;
using HostTemplate.Core.Abstractions;

namespace HostTemplate.Devices.Hsl.OpenProtocol
{
    /// <summary>
    /// Open Protocol 虚拟服务，包在 HSL <see cref="OpenProtocolServer"/> 上。
    /// 12.8.3 的 ServerStart / ServerClose / Publish / SetVinNumber 都是同步方法，没有 Async 版本。
    /// StartAsync / StopAsync 调用的就是 ServerStart / ServerClose。
    /// </summary>
    public sealed class HslOpenProtocolServer : IDisposable
    {
        private readonly OpenProtocolServer _server = new OpenProtocolServer();
        private int _disposeStarted;

        /// <summary>原始 Open Protocol 服务。</summary>
        public OpenProtocolServer RawServer => _server;

        public bool IsStarted => _server.IsStarted;

        public int Port => _server.Port;

        public Task<DeviceResult> StartAsync(int port, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (port <= 0 || port > 65535)
            {
                return Task.FromResult(DeviceResult.Fail("端口无效"));
            }

            if (_server.IsStarted)
            {
                return Task.FromResult(DeviceResult.Fail("服务已在监听"));
            }

            try
            {
                _server.ServerStart(port);
                return Task.FromResult(_server.IsStarted
                    ? DeviceResult.Ok()
                    : DeviceResult.Fail("Open Protocol 服务未启动"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(DeviceResult.Fail(ex.Message));
            }
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CloseServer();
            return Task.CompletedTask;
        }

        /// <summary>HSL 的 SetVinNumber 是同步方法，直接转发。</summary>
        public void SetVinNumber(string vin)
        {
            _server.SetVinNumber(vin ?? string.Empty);
        }

        /// <summary>HSL 的 Publish 是同步方法，直接转发。参数名 revision 对应库里的 revison。</summary>
        public void Publish(int mid, int revision, int stationId, int spindleId, IReadOnlyList<string> parameters)
        {
            var copy = new List<string>();
            if (parameters != null)
            {
                for (var i = 0; i < parameters.Count; i++)
                {
                    copy.Add(parameters[i]);
                }
            }

            _server.Publish(mid, revision, stationId, spindleId, copy);
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
                _server.ServerClose();
            }
            catch
            {
                // 重复关闭忽略。
            }

            // 12.8.3 的构造函数自己开了一个 2 秒定时器，ServerClose 不会停它。
            // 不停的话对象会被定时器一直抓住。字段是私有的，这里只在关闭时把它停掉。
            try
            {
                var timer = typeof(OpenProtocolServer)
                    .GetField("timer", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(_server) as IDisposable;
                timer?.Dispose();
            }
            catch
            {
                // 停不掉定时器不影响关闭结果。
            }
        }
    }
}
