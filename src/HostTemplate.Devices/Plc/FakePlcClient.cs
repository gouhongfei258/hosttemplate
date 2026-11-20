using System;
using System.Collections.Generic;
using System.Threading;
using HostTemplate.Core.Abstractions;

namespace HostTemplate.Devices.Plc
{
    /// <summary>
    /// 内存虚拟 PLC。用途：
    ///   1. 没有真实 PLC 时也能跑通「进站 → 测试 → 出站」全流程；
    ///   2. 新人培训与演示；
    ///   3. 单元测试 / CI 回归。
    ///
    /// 地址表与真实 PLC 一致，业务代码无需任何改动即可切换。
    /// </summary>
    public sealed class FakePlcClient : IPlcClient
    {
        private readonly Dictionary<string, object> _memory = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private readonly object _gate = new object();
        private readonly string _commandAddress;

        private Timer _flowTimer;
        private ushort[] _flowWords;
        private int _flowIndex;
        private bool _disposed;

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

        public bool Open(out string error)
        {
            error = string.Empty;

            if (OpenDelayMs > 0)
            {
                Thread.Sleep(OpenDelayMs);
            }

            if (FailOpen)
            {
                error = "仿真器：连接被拒绝";
                return false;
            }

            IsConnected = true;
            return true;
        }

        public void Close()
        {
            IsConnected = false;
        }

        public void SetWord(string address, ushort value)
        {
            lock (_gate)
            {
                _memory[address] = value;
            }
        }

        public void SetWords(string address, params ushort[] values)
        {
            lock (_gate)
            {
                _memory[address] = values ?? Array.Empty<ushort>();
            }
        }

        public void SetBool(string address, bool value)
        {
            lock (_gate)
            {
                _memory[address] = value;
            }
        }

        public void SetBoolArray(string address, params bool[] values)
        {
            lock (_gate)
            {
                _memory[address] = values ?? Array.Empty<bool>();
            }
        }

        public void SetString(string address, string value)
        {
            lock (_gate)
            {
                _memory[address] = value ?? string.Empty;
            }
        }

        public ushort GetWord(string address)
        {
            return ReadWord(address, out var value, out _) ? value : (ushort)0;
        }

        public string GetString(string address)
        {
            return ReadString(address, 0, out var value, out _) ? value : string.Empty;
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

        public bool ReadBool(string address, out bool value, out string error)
        {
            error = string.Empty;
            value = false;

            lock (_gate)
            {
                if (!_memory.TryGetValue(address, out var raw))
                {
                    return true;
                }

                if (raw is bool b)
                {
                    value = b;
                }
                else if (raw is bool[] array && array.Length > 0)
                {
                    value = array[0];
                }

                return true;
            }
        }

        public bool ReadBoolArray(string address, int count, out bool[] values, out string error)
        {
            error = string.Empty;
            values = new bool[Math.Max(0, count)];

            lock (_gate)
            {
                if (!_memory.TryGetValue(address, out var raw))
                {
                    return true;
                }

                if (raw is bool[] array)
                {
                    for (var i = 0; i < values.Length && i < array.Length; i++)
                    {
                        values[i] = array[i];
                    }
                }

                return true;
            }
        }

        public bool ReadWord(string address, out ushort value, out string error)
        {
            error = string.Empty;
            value = 0;

            lock (_gate)
            {
                if (!_memory.TryGetValue(address, out var raw))
                {
                    return true;
                }

                switch (raw)
                {
                    case ushort word:
                        value = word;
                        break;
                    case ushort[] array when array.Length > 0:
                        value = array[0];
                        break;
                    case int intValue:
                        value = (ushort)intValue;
                        break;
                }

                return true;
            }
        }

        public bool ReadWordArray(string address, int count, out ushort[] values, out string error)
        {
            error = string.Empty;
            values = new ushort[Math.Max(0, count)];

            lock (_gate)
            {
                if (!_memory.TryGetValue(address, out var raw))
                {
                    return true;
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

                return true;
            }
        }

        public bool ReadInt32(string address, out int value, out string error)
        {
            error = string.Empty;
            value = 0;

            lock (_gate)
            {
                if (_memory.TryGetValue(address, out var raw) && raw is int intValue)
                {
                    value = intValue;
                }

                return true;
            }
        }

        public bool ReadReal(string address, out float value, out string error)
        {
            error = string.Empty;
            value = 0f;

            lock (_gate)
            {
                if (_memory.TryGetValue(address, out var raw) && raw is float floatValue)
                {
                    value = floatValue;
                }

                return true;
            }
        }

        public bool ReadString(string address, int length, out string value, out string error)
        {
            error = string.Empty;
            value = string.Empty;

            lock (_gate)
            {
                if (_memory.TryGetValue(address, out var raw) && raw is string text)
                {
                    value = text;
                }

                return true;
            }
        }

        public bool WriteBool(string address, bool value, out string error)
        {
            error = string.Empty;
            SetBool(address, value);
            return true;
        }

        public bool WriteWord(string address, ushort value, out string error)
        {
            error = string.Empty;
            SetWord(address, value);
            return true;
        }

        public bool WriteInt32(string address, int value, out string error)
        {
            error = string.Empty;

            lock (_gate)
            {
                _memory[address] = value;
            }

            return true;
        }

        public bool WriteReal(string address, float value, out string error)
        {
            error = string.Empty;

            lock (_gate)
            {
                _memory[address] = value;
            }

            return true;
        }

        public bool WriteString(string address, string value, out string error)
        {
            error = string.Empty;
            SetString(address, value);
            return true;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            StopAutoFlow();
        }
    }
}
