using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HostTemplate.Core.Abstractions;

namespace HostTemplate.Devices.Plc.Simulation
{
    /// <summary>
    /// 内存虚拟 PLC。用途：
    ///   1. 没有真实 PLC 时也能跑通「进站 → 测试 → 出站」全流程；
    ///   2. 新人培训与演示；
    ///   3. 单元测试 / CI 回归。
    ///
    /// 地址表与真实 PLC 一致，业务代码无需任何改动即可切换。
    /// 读写走异步接口，但数据在内存里，不调用 HSL。
    /// </summary>
    public sealed class FakePlcClient : IPlcClient
    {
        private readonly Dictionary<string, object> _memory = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private readonly string _commandAddress;

        private Timer _flowTimer;
        private ushort[] _flowWords;
        private int _flowIndex;
        private int _disposeStarted;

        public FakePlcClient(string commandAddress = "MW200")
        {
            _commandAddress = commandAddress;
        }

        public string Address => "SIMULATOR";

        public bool IsConnected { get; private set; }

        /// <summary>模拟连接耗时，用于验证界面的「连接中」状态。</summary>
        public int OpenDelayMs { get; set; }

        /// <summary>置 true 可模拟连接失败，验证重连与报警。</summary>
        public bool FailOpen { get; set; }

        public async Task<DeviceResult> OpenAsync(CancellationToken cancellationToken = default)
        {
            if (OpenDelayMs > 0)
            {
                await Task.Delay(OpenDelayMs, cancellationToken).ConfigureAwait(false);
            }

            if (FailOpen)
            {
                return DeviceResult.Fail("仿真器：连接被拒绝");
            }

            IsConnected = true;
            return DeviceResult.Ok();
        }

        public Task CloseAsync(CancellationToken cancellationToken = default)
        {
            IsConnected = false;
            return Task.CompletedTask;
        }

        public void SetWord(string address, ushort value)
        {
            _gate.Wait();
            try
            {
                _memory[address] = value;
            }
            finally
            {
                _gate.Release();
            }
        }

        public void SetWords(string address, params ushort[] values)
        {
            _gate.Wait();
            try
            {
                _memory[address] = values ?? Array.Empty<ushort>();
            }
            finally
            {
                _gate.Release();
            }
        }

        public void SetBool(string address, bool value)
        {
            _gate.Wait();
            try
            {
                _memory[address] = value;
            }
            finally
            {
                _gate.Release();
            }
        }

        public void SetBoolArray(string address, params bool[] values)
        {
            _gate.Wait();
            try
            {
                _memory[address] = values ?? Array.Empty<bool>();
            }
            finally
            {
                _gate.Release();
            }
        }

        public void SetString(string address, string value)
        {
            _gate.Wait();
            try
            {
                _memory[address] = value ?? string.Empty;
            }
            finally
            {
                _gate.Release();
            }
        }

        public ushort GetWord(string address)
        {
            _gate.Wait();
            try
            {
                return PeekWord(address);
            }
            finally
            {
                _gate.Release();
            }
        }

        public string GetString(string address)
        {
            _gate.Wait();
            try
            {
                return _memory.TryGetValue(address, out var raw) && raw is string text ? text : string.Empty;
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>
        /// 自动推进流程字，默认循环 10 → 20 → 30 → 40 → 0。
        /// </summary>
        public void StartAutoFlow(TimeSpan interval, params ushort[] words)
        {
            StopAutoFlow();

            _flowWords = words != null && words.Length > 0
                ? words
                : new ushort[] { 10, 20, 30, 40, 0 };
            _flowIndex = 0;

            _flowTimer = new Timer(_ => AdvanceFlow(), null, interval, interval);
        }

        public void StopAutoFlow()
        {
            _flowTimer?.Dispose();
            _flowTimer = null;
        }

        private void AdvanceFlow()
        {
            var words = _flowWords;
            if (words == null || words.Length == 0)
            {
                return;
            }

            var word = words[_flowIndex % words.Length];
            _flowIndex++;

            SetWords(_commandAddress, word, 0);
        }

        public Task<DeviceResult<bool>> ReadBoolAsync(string address, CancellationToken cancellationToken = default)
        {
            return ReadAsync(cancellationToken, () =>
            {
                var value = false;
                if (!_memory.TryGetValue(address, out var raw))
                {
                    return value;
                }

                if (raw is bool bit)
                {
                    value = bit;
                }
                else if (raw is bool[] array && array.Length > 0)
                {
                    value = array[0];
                }

                return value;
            });
        }

        public Task<DeviceResult<bool[]>> ReadBoolArrayAsync(string address, int count, CancellationToken cancellationToken = default)
        {
            return ReadAsync(cancellationToken, () =>
            {
                var values = new bool[Math.Max(0, count)];
                if (_memory.TryGetValue(address, out var raw) && raw is bool[] array)
                {
                    for (var i = 0; i < values.Length && i < array.Length; i++)
                    {
                        values[i] = array[i];
                    }
                }

                return values;
            });
        }

        public Task<DeviceResult<ushort>> ReadWordAsync(string address, CancellationToken cancellationToken = default)
        {
            return ReadAsync(cancellationToken, () => PeekWord(address));
        }

        public Task<DeviceResult<ushort[]>> ReadWordArrayAsync(string address, int count, CancellationToken cancellationToken = default)
        {
            return ReadAsync(cancellationToken, () =>
            {
                var values = new ushort[Math.Max(0, count)];
                if (!_memory.TryGetValue(address, out var raw))
                {
                    return values;
                }

                switch (raw)
                {
                    case ushort[] array:
                        for (var i = 0; i < values.Length && i < array.Length; i++)
                        {
                            values[i] = array[i];
                        }

                        break;
                    case ushort word when values.Length > 0:
                        values[0] = word;
                        break;
                }

                return values;
            });
        }

        public Task<DeviceResult<int>> ReadInt32Async(string address, CancellationToken cancellationToken = default)
        {
            return ReadAsync(cancellationToken, () =>
                _memory.TryGetValue(address, out var raw) && raw is int intValue ? intValue : 0);
        }

        public Task<DeviceResult<float>> ReadRealAsync(string address, CancellationToken cancellationToken = default)
        {
            return ReadAsync(cancellationToken, () =>
                _memory.TryGetValue(address, out var raw) && raw is float floatValue ? floatValue : 0f);
        }

        public Task<DeviceResult<string>> ReadStringAsync(string address, int length, CancellationToken cancellationToken = default)
        {
            return ReadAsync(cancellationToken, () =>
                _memory.TryGetValue(address, out var raw) && raw is string text ? text : string.Empty);
        }

        public Task<DeviceResult> WriteBoolAsync(string address, bool value, CancellationToken cancellationToken = default)
        {
            return WriteAsync(cancellationToken, () => _memory[address] = value);
        }

        public Task<DeviceResult> WriteWordAsync(string address, ushort value, CancellationToken cancellationToken = default)
        {
            return WriteAsync(cancellationToken, () => _memory[address] = value);
        }

        public Task<DeviceResult> WriteInt32Async(string address, int value, CancellationToken cancellationToken = default)
        {
            return WriteAsync(cancellationToken, () => _memory[address] = value);
        }

        public Task<DeviceResult> WriteRealAsync(string address, float value, CancellationToken cancellationToken = default)
        {
            return WriteAsync(cancellationToken, () => _memory[address] = value);
        }

        public Task<DeviceResult> WriteStringAsync(string address, string value, CancellationToken cancellationToken = default)
        {
            return WriteAsync(cancellationToken, () => _memory[address] = value ?? string.Empty);
        }

        private ushort PeekWord(string address)
        {
            if (!_memory.TryGetValue(address, out var raw))
            {
                return 0;
            }

            switch (raw)
            {
                case ushort word:
                    return word;
                case ushort[] array when array.Length > 0:
                    return array[0];
                case int intValue:
                    return (ushort)intValue;
                default:
                    return 0;
            }
        }

        private async Task<DeviceResult<T>> ReadAsync<T>(CancellationToken cancellationToken, Func<T> read)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return DeviceResult<T>.Ok(read());
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task<DeviceResult> WriteAsync(CancellationToken cancellationToken, Action write)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                write();
                return DeviceResult.Ok();
            }
            finally
            {
                _gate.Release();
            }
        }

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
            {
                return ValueTask.CompletedTask;
            }

            StopAutoFlow();
            _gate.Dispose();
            return ValueTask.CompletedTask;
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
