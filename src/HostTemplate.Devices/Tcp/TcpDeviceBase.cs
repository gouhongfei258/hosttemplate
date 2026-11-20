using System;
using System.Net.Sockets;
using HostTemplate.Core.Abstractions;

namespace HostTemplate.Devices.Tcp
{
    /// <summary>
    /// TCP 外设基类（打印机、扫码枪、仪器等）。
    /// 子类只需关心「发什么帧、怎么解析回包」，连接与超时由基类处理。
    /// </summary>
    public abstract class TcpDeviceBase : IDevice
    {
        private readonly object _gate = new object();
        private TcpClient _client;
        private NetworkStream _stream;

        protected TcpDeviceBase(string name, string ipAddress, int port, int timeoutMs = 2000)
        {
            Name = name;
            IpAddress = ipAddress;
            Port = port;
            TimeoutMs = timeoutMs;
        }

        public string Name { get; }

        public string IpAddress { get; }

        public int Port { get; }

        public int TimeoutMs { get; }

        public bool IsConnected => _client != null && _client.Connected;

        public bool Open(out string error)
        {
            error = string.Empty;

            lock (_gate)
            {
                try
                {
                    CloseInternal();

                    _client = new TcpClient
                    {
                        ReceiveTimeout = TimeoutMs,
                        SendTimeout = TimeoutMs
                    };

                    var async = _client.BeginConnect(IpAddress, Port, null, null);
                    if (!async.AsyncWaitHandle.WaitOne(TimeoutMs))
                    {
                        error = $"连接 {IpAddress}:{Port} 超时";
                        CloseInternal();
                        return false;
                    }

                    _client.EndConnect(async);
                    _stream = _client.GetStream();
                    return true;
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    CloseInternal();
                    return false;
                }
            }
        }

        public void Close()
        {
            lock (_gate)
            {
                CloseInternal();
            }
        }

        public bool Send(byte[] data, out string error)
        {
            error = string.Empty;

            lock (_gate)
            {
                if (_stream == null)
                {
                    error = "设备未连接";
                    return false;
                }

                try
                {
                    _stream.Write(data, 0, data.Length);
                    _stream.Flush();
                    return true;
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    CloseInternal();
                    return false;
                }
            }
        }

        public bool Receive(out byte[] data, out string error)
        {
            error = string.Empty;
            data = null;

            lock (_gate)
            {
                if (_stream == null)
                {
                    error = "设备未连接";
                    return false;
                }

                try
                {
                    var buffer = new byte[4096];
                    var count = _stream.Read(buffer, 0, buffer.Length);
                    if (count <= 0)
                    {
                        error = "对端已关闭连接";
                        CloseInternal();
                        return false;
                    }

                    data = new byte[count];
                    Array.Copy(buffer, data, count);
                    return true;
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    CloseInternal();
                    return false;
                }
            }
        }

        private void CloseInternal()
        {
            try
            {
                _stream?.Dispose();
            }
            catch
            {
                // 忽略关闭异常。
            }

            try
            {
                _client?.Close();
            }
            catch
            {
                // 忽略关闭异常。
            }

            _stream = null;
            _client = null;
        }

        public virtual void Dispose()
        {
            Close();
        }
    }
}
