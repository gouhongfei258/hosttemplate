using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HostTemplate.Core.Abstractions;
using HostTemplate.Core.Diagnostics;
using HostTemplate.Core.Runtime;

namespace HostTemplate.Core.Flow
{
    public sealed class EngineStatusEventArgs : EventArgs
    {
        public EngineStatusEventArgs(LogicState state, string message)
        {
            State = state;
            Message = message;
        }

        public LogicState State { get; }

        public string Message { get; }
    }

    public sealed class StepExecutedEventArgs : EventArgs
    {
        public StepExecutedEventArgs(ushort word, string stepName, StepResult result, long elapsedMs)
        {
            Word = word;
            StepName = stepName;
            Result = result;
            ElapsedMs = elapsedMs;
        }

        public ushort Word { get; }

        public string StepName { get; }

        public StepResult Result { get; }

        public long ElapsedMs { get; }
    }

    /// <summary>
    /// 工站主循环引擎。
    ///
    /// 职责边界（刻意做窄）：
    ///   1. 周期轮询 PLC、翻转心跳、读取流程字与预留位；
    ///   2. 流程字变化时按 <see cref="IStationStep.Word"/> 分发到对应步骤；
    ///   3. 单飞防重入：上一步没结束不重入，下一轮重试而不是丢弃；
    ///   4. 步骤结束后统一回写审查码、记录流程轨迹；良品/不良按件记一次，不按步骤累加。
    ///
    /// 不含任何具体业务。业务全部在 IStationStep 实现里。
    /// </summary>
    public sealed class StationEngine : IDisposable, IStationIdle
    {
        private readonly StationContext _context;
        private readonly Dictionary<ushort, IStationStep> _steps = new Dictionary<ushort, IStationStep>();
        private readonly HashSet<ushort> _unknownWords = new HashSet<ushort>();
        private readonly SemaphoreSlim _stepGate = new SemaphoreSlim(1, 1);

        private string _heartbeatAddress;
        private string _commandAddress;
        private string _reserveBoolAddress;
        private string _reviewAddress;

        private CancellationTokenSource _cts;
        private Task _loopTask;
        private bool _heartbeat;
        private long _tick;
        private ushort _handledWord;
        private bool _hasHandled;

        /// <summary>当前这件产品已经进站或执行过步骤，空闲时才能拿最后一枚审查码收口。</summary>
        private bool _pieceOpen;

        /// <summary>当前这件已经记过良品或不良。同一件后续步骤不再累加。</summary>
        private bool _pieceCounted;
        private bool _disposed;
        private DateTime _lastConnectAttempt = DateTime.MinValue;
        private bool[] _reserveBools = Array.Empty<bool>();
        private int _stepInFlight;

        public StationEngine(StationContext context, IEnumerable<IStationStep> steps)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            if (steps != null)
            {
                foreach (var step in steps)
                {
                    if (step == null)
                    {
                        continue;
                    }

                    if (_steps.ContainsKey(step.Word))
                    {
                        throw new InvalidOperationException(
                            $"流程字 {step.Word} 注册了多个步骤：{_steps[step.Word].Name} / {step.Name}");
                    }

                    _steps[step.Word] = step;
                }
            }

            ReloadAddresses();
        }

        /// <summary>
        /// 重新从上下文读取引擎自用的四个地址。
        /// 配置页改完地址表后调用（需先停止监控），避免重启程序。
        /// </summary>
        public void ReloadAddresses()
        {
            _heartbeatAddress = _context.GetAddress(AddressKeys.Heartbeat);
            _commandAddress = _context.GetAddress(AddressKeys.Command);
            _reserveBoolAddress = _context.GetAddress(AddressKeys.ReserveBool);
            _reviewAddress = _context.GetAddress(AddressKeys.ReviewResult);
        }

        public bool IsRunning => _loopTask != null && !_loopTask.IsCompleted;

        /// <summary>
        /// 没有产品在制：流程字为等待进站，测试标志已清，也没有步骤正在执行。
        /// 界面不要自己拼这个判断。
        /// </summary>
        public bool IsIdle => StationIdle.Evaluate(_context.State, Volatile.Read(ref _stepInFlight) > 0);

        public IReadOnlyDictionary<ushort, IStationStep> Steps => _steps;

        public IReadOnlyList<bool> ReserveBools => _reserveBools;

        public event EventHandler<EngineStatusEventArgs> StatusChanged;

        public event EventHandler<StepExecutedEventArgs> StepExecuted;

        public event EventHandler ReserveBoolsChanged;

        public Task StartAsync()
        {
            if (IsRunning)
            {
                _context.Logger.Warn("监控已在运行，忽略重复启动。");
                return Task.CompletedTask;
            }

            if (string.IsNullOrEmpty(_commandAddress))
            {
                var message = $"地址表缺少「{AddressKeys.Command}」，无法启动监控。";
                _context.Logger.Error(message);
                RaiseStatus(LogicState.系统异常, message);
                return Task.CompletedTask;
            }

            _hasHandled = false;
            _handledWord = 0;
            _pieceOpen = false;
            _pieceCounted = false;
            _tick = 0;
            _context.Trace.Clear();
            _context.Alarms.Clear();

            _cts = new CancellationTokenSource();
            RaiseStatus(LogicState.初始化中, "正在初始化…");
            _loopTask = Task.Run(() => LoopAsync(_cts.Token));

            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            if (_cts == null)
            {
                return;
            }

            RaiseStatus(LogicState.正在停止, "正在停止监控…");
            _cts.Cancel();

            try
            {
                if (_loopTask != null)
                {
                    await _loopTask.ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常停止路径。
            }

            _cts.Dispose();
            _cts = null;
            _loopTask = null;

            _context.State.RunningStep = string.Empty;
            _context.State.PlcConnected = false;
            _context.PlcClient?.Close();

            _context.Logger.Info("监控已停止。");
            RaiseStatus(LogicState.监控已停止, "监控已停止");
        }

        private async Task LoopAsync(CancellationToken token)
        {
            var pollMs = Math.Max(10, _context.App.PollIntervalMs);
            var heartbeatTicks = Math.Max(1, _context.App.HeartbeatTicks);

            _context.Logger.Info($"主循环启动：轮询 {pollMs}ms，心跳 {heartbeatTicks} 次轮询翻转一次。");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(pollMs, token).ConfigureAwait(false);

                    if (!EnsureConnected())
                    {
                        continue;
                    }

                    _tick++;

                    if (_tick % heartbeatTicks == 0)
                    {
                        WriteHeartbeat();
                    }

                    if (!ReadCommand(out var word))
                    {
                        continue;
                    }

                    _context.State.ProcessWord = word;
                    _context.State.ProcessContent = ProcessWords.Describe(word);

                    if (word == ProcessWords.Idle)
                    {
                        _context.State.Testing = false;
                    }

                    if (!_hasHandled || word != _handledWord)
                    {
                        if (await DispatchAsync(word, token).ConfigureAwait(false))
                        {
                            _handledWord = word;
                            _hasHandled = true;
                        }
                    }

                    ReadReserveBools();

                    _context.Alarms.Set("PLC轮询异常", false);
                    RaiseStatus(LogicState.正在监控, string.Empty);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _context.State.RunStatus = LogicState.系统异常;
                    _context.Alarms.Set("PLC轮询异常", true, ex.Message);
                    _context.Logger.Error("主循环异常", ex);
                }
            }
        }

        private bool EnsureConnected()
        {
            var plc = _context.PlcClient;
            if (plc == null)
            {
                _context.Alarms.Set("PLC未配置", true, "未注入 IPlcClient");
                return false;
            }

            if (plc.IsConnected)
            {
                _context.State.PlcConnected = true;
                return true;
            }

            var reconnectDelay = Math.Max(200, _context.Plc.ReconnectDelayMs);
            if ((DateTime.Now - _lastConnectAttempt).TotalMilliseconds < reconnectDelay)
            {
                return false;
            }

            _lastConnectAttempt = DateTime.Now;

            if (!plc.Open(out var error))
            {
                _context.State.PlcConnected = false;
                _context.Alarms.Set("连接PLC失败", true, error);
                _context.Logger.Warn("连接 PLC 失败：" + error);
                return false;
            }

            _context.State.PlcConnected = true;
            _context.Alarms.Set("连接PLC失败", false);
            _context.Logger.Info($"PLC 已连接：{plc.Address}");
            return true;
        }

        private void WriteHeartbeat()
        {
            if (string.IsNullOrEmpty(_heartbeatAddress))
            {
                return;
            }

            _heartbeat = !_heartbeat;

            if (_context.PlcClient.WriteBool(_heartbeatAddress, _heartbeat, out var error))
            {
                _context.Alarms.Set("写心跳失败", false);
                return;
            }

            _context.Alarms.Set("写心跳失败", true, error);
            DropConnection();
        }

        private bool ReadCommand(out ushort word)
        {
            word = 0;

            if (!_context.PlcClient.ReadWordArray(_commandAddress, 2, out var words, out var error)
                || words == null
                || words.Length == 0)
            {
                _context.Alarms.Set("读取流程字失败", true, error);
                DropConnection();
                return false;
            }

            _context.Alarms.Set("读取流程字失败", false);
            word = words[0];
            return true;
        }

        private void ReadReserveBools()
        {
            if (string.IsNullOrEmpty(_reserveBoolAddress))
            {
                return;
            }

            var length = 16;
            if (_context.TryGetAddress(AddressKeys.ReserveBool, out var option) && option.Length > 0)
            {
                length = option.Length;
            }

            if (!_context.PlcClient.ReadBoolArray(_reserveBoolAddress, length, out var values, out var error)
                || values == null)
            {
                _context.Alarms.Set("读取预留位失败", true, error);
                return;
            }

            _context.Alarms.Set("读取预留位失败", false);

            var changed = values.Length != _reserveBools.Length;
            if (!changed)
            {
                for (var i = 0; i < values.Length; i++)
                {
                    if (values[i] != _reserveBools[i])
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (changed)
            {
                _reserveBools = values;
                ReserveBoolsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 分发流程字。返回 true 表示「本次已处理或无需处理」，引擎据此推进 _handledWord；
        /// 返回 false 表示上一步仍在执行，下一轮重试。
        /// </summary>
        private async Task<bool> DispatchAsync(ushort word, CancellationToken token)
        {
            // 空闲表示这一件已经离开工站。还没记过数时，用最后一枚审查码收口。
            if (word == ProcessWords.Idle)
            {
                CloseOpenPiece();
                _pieceOpen = false;
                _pieceCounted = false;
                return true;
            }

            if (!_steps.TryGetValue(word, out var step))
            {
                if (_unknownWords.Add(word))
                {
                    _context.Logger.Warn($"流程字 {word} 未注册处理步骤，已忽略。");
                }

                return true;
            }

            if (!await _stepGate.WaitAsync(0, token).ConfigureAwait(false))
            {
                return false;
            }

            var stopwatch = Stopwatch.StartNew();
            Interlocked.Increment(ref _stepInFlight);

            try
            {
                if (word == ProcessWords.InStation)
                {
                    // 下一件开始。上一件若停在中间步骤，最后一枚审查码就是它的最终结果。
                    CloseOpenPiece();
                    _pieceOpen = true;
                    _pieceCounted = false;
                    _context.Trace.Clear();
                }
                else
                {
                    _pieceOpen = true;
                }

                _context.State.RunningStep = step.Name;
                _context.State.ReviewCode = 0;
                _context.Logger.Info($"▶ 流程 {word} 开始：{step.Name}");

                var stepContext = new StepContext(_context, word, token);
                var result = await step.ExecuteAsync(stepContext).ConfigureAwait(false);

                _context.Trace.Add(word);
                _context.Alarms.Set($"步骤异常:{step.Name}", false);

                if (result != null)
                {
                    WriteReview(result.Review);
                    CountPiece(result.Review, cycleClosed: false);

                    var level = result.Success ? "✔" : "✘";
                    _context.Logger.Info(
                        $"{level} 流程 {word} 结束：{step.Name}，审查码 {(ushort)result.Review}，耗时 {stopwatch.ElapsedMilliseconds}ms" +
                        (string.IsNullOrEmpty(result.Message) ? string.Empty : $"，{result.Message}"));

                    StepExecuted?.Invoke(
                        this,
                        new StepExecutedEventArgs(word, step.Name, result, stopwatch.ElapsedMilliseconds));
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                return true;
            }
            catch (Exception ex)
            {
                _context.Alarms.Set($"步骤异常:{step.Name}", true, ex.Message);
                _context.Logger.Error($"步骤 {step.Name} 执行异常", ex);
                return true;
            }
            finally
            {
                _context.State.RunningStep = string.Empty;
                Interlocked.Decrement(ref _stepInFlight);
                _stepGate.Release();
            }
        }

        /// <summary>
        /// 一件产品只记一次良品或不良。
        /// 周期还没结束时，只有 NG_*（结果已经是不良）和收尾 OK（OK_OutStation / OK_Done）才计数。
        /// 进站、测试开始、测试结束的 OK_* 是中间步骤，不加件数。
        /// 周期结束（回到空闲，或下一件进站）时，若这件还没记过，最后一枚审查码按 OK_* / NG_* 收口。
        /// 同一件先出现 NG_* 之后，后面的 OK 不再记成良品。
        /// </summary>
        private void CloseOpenPiece()
        {
            if (!_pieceOpen || _pieceCounted)
            {
                return;
            }

            CountPiece((ReviewCode)_context.State.ReviewCode, cycleClosed: true);
        }

        private void CountPiece(ReviewCode code, bool cycleClosed)
        {
            if (_pieceCounted || !ShouldCountPiece(code, cycleClosed))
            {
                return;
            }

            if (IsNgReview(code))
            {
                _context.State.NgCount++;
            }
            else
            {
                _context.State.CycleCount++;
            }

            _pieceCounted = true;
        }

        private static bool ShouldCountPiece(ReviewCode code, bool cycleClosed)
        {
            if (IsNgReview(code))
            {
                return true;
            }

            if (!IsOkReview(code))
            {
                return false;
            }

            if (cycleClosed)
            {
                return true;
            }

            return code == ReviewCode.OK_OutStation || code == ReviewCode.OK_Done;
        }

        private static bool IsOkReview(ReviewCode code)
        {
            switch (code)
            {
                case ReviewCode.OK_InStation:
                case ReviewCode.OK_TestStart:
                case ReviewCode.OK_TestFinish:
                case ReviewCode.OK_OutStation:
                case ReviewCode.OK_Done:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsNgReview(ReviewCode code)
        {
            switch (code)
            {
                case ReviewCode.NG_InStation:
                case ReviewCode.NG_TestStart:
                case ReviewCode.NG_TestFinish:
                case ReviewCode.NG_OutStation:
                case ReviewCode.NG_PowerReTest:
                case ReviewCode.NG_Product:
                    return true;
                default:
                    return false;
            }
        }

        private void WriteReview(ReviewCode code)
        {
            _context.State.ReviewCode = (ushort)code;

            if (string.IsNullOrEmpty(_reviewAddress))
            {
                return;
            }

            if (_context.PlcClient.WriteWord(_reviewAddress, (ushort)code, out var error))
            {
                _context.Alarms.Set("回写审查结果失败", false);
                return;
            }

            _context.Alarms.Set("回写审查结果失败", true, error);
        }

        private void DropConnection()
        {
            _context.State.PlcConnected = false;
            _context.PlcClient?.Close();
        }

        private void RaiseStatus(LogicState state, string message)
        {
            _context.State.RunStatus = state;
            StatusChanged?.Invoke(this, new EngineStatusEventArgs(state, message));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                _cts?.Cancel();
            }
            catch
            {
                // 忽略停止期异常。
            }

            _stepGate.Dispose();
            _cts?.Dispose();
        }
    }
}
