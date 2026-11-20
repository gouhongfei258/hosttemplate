using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using HostTemplate.App.Controls;
using HostTemplate.App.Forms;
using HostTemplate.Core.Abstractions;
using HostTemplate.Core.Flow;
using HostTemplate.Core.Runtime;
using HostTemplate.Core.Security;

namespace HostTemplate.App
{
    /// <summary>
    /// 监控主界面。只做展示与指令下发，业务全部在 StationEngine / IStationStep 里。
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly StationContext _context;
        private readonly StationEngine _engine;
        private readonly UiLogSink _logSink;
        private readonly Timer _refreshTimer;
        private readonly Func<StationContext, IPlcClient> _plcFactory;
        private readonly Func<StationContext, IApiClient> _apiFactory;

        private int _tick;
        private bool _apiChecking;

        /// <summary>窗口显示后自动开始监控（配合 --autostart 使用）。</summary>
        public bool AutoStart { get; set; }

        public MainForm(
            StationContext context,
            StationEngine engine,
            Func<StationContext, IPlcClient> plcFactory = null,
            Func<StationContext, IApiClient> apiFactory = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _plcFactory = plcFactory;
            _apiFactory = apiFactory;

            InitializeComponent();

            _logSink = new UiLogSink(rtbLog);
            _context.Logger.Written += (s, e) => _logSink.Enqueue(e);

            _engine.StatusChanged += OnEngineStatusChanged;
            _engine.StepExecuted += OnStepExecuted;

            _refreshTimer = new Timer { Interval = 300 };
            _refreshTimer.Tick += (s, e) => RefreshState();
            _refreshTimer.Start();

            BindStaticInfo();
            RefreshState();

            _context.Logger.Info("界面已加载。");
        }

        private void BindStaticInfo()
        {
            lblStation.Text = _context.App.WorkStationName;
            lblSub.Text = $"产线 {_context.App.Line}　工站 {_context.App.EquId}　{_context.App.Station}";

            var plcInfo = _context.App.Simulate
                ? "仿真模式（无需真实 PLC）"
                : $"PLC {_context.Plc.Address}:{_context.Plc.Port}";

            // 未启用登录时 Session 是匿名管理员，这里不显示用户信息，避免误解。
            lblHint.Text = _context.Session.IsAuthenticated
                ? $"{plcInfo}　|　{_context.Session.DisplayName}（{_context.Session.Role}）"
                : plcInfo;

            Text = $"上位机模板 · {_context.App.WorkStationName}";
        }

        private void RefreshState()
        {
            if (IsDisposed)
            {
                return;
            }

            var state = _context.State;

            lightPlc.Active = state.PlcConnected;
            lightRun.Active = _engine.IsRunning;
            lightAlarm.Active = _context.Alarms.ActiveCount > 0;

            lblWord.Text = state.ProcessWord.ToString();
            lblWordName.Text = string.IsNullOrEmpty(state.ProcessContent)
                ? "等待进站"
                : state.ProcessContent;

            lblSn.Text = "SN：" + (string.IsNullOrEmpty(state.SerialNumber) ? "--" : state.SerialNumber);
            lblTray.Text = "托盘：" + (string.IsNullOrEmpty(state.TrayNo) ? "--" : state.TrayNo);

            var trace = _context.Trace.Text;
            lblStep.Text = string.IsNullOrEmpty(state.RunningStep)
                ? "轨迹：" + (string.IsNullOrEmpty(trace) ? "--" : trace)
                : $"轨迹：{trace}　执行中：{state.RunningStep}";

            lblCount.Text = $"良品 {state.CycleCount} / 不良 {state.NgCount}";

            _tick++;
            if (_tick % 20 == 0 && !_apiChecking)
            {
                _ = CheckApiAsync();
            }
        }

        private async Task CheckApiAsync()
        {
            _apiChecking = true;

            try
            {
                var client = _context.ApiClient;
                if (client == null)
                {
                    lightApi.Active = false;
                    return;
                }

                var ok = await client.PingAsync(System.Threading.CancellationToken.None).ConfigureAwait(true);
                _context.State.ApiConnected = ok;
                lightApi.Active = ok;
            }
            catch
            {
                lightApi.Active = false;
            }
            finally
            {
                _apiChecking = false;
            }
        }

        private void OnEngineStatusChanged(object sender, EngineStatusEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Message))
            {
                return;
            }

            _context.Logger.Info($"[状态] {e.Message}");
        }

        private void OnStepExecuted(object sender, StepExecutedEventArgs e)
        {
            _context.Logger.Debug($"[步骤] {e.Word} {e.StepName} {(e.Result?.Success == true ? "OK" : "NG")} {e.ElapsedMs}ms");
        }

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            try
            {
                btnStart.Enabled = false;
                await _engine.StartAsync();
            }
            catch (Exception ex)
            {
                _context.Logger.Error("启动监控失败", ex);
                MessageBox.Show(this, ex.Message, "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnStart.Enabled = true;
            }
        }

        private async void BtnStop_Click(object sender, EventArgs e)
        {
            try
            {
                btnStop.Enabled = false;
                await _engine.StopAsync();
            }
            catch (Exception ex)
            {
                _context.Logger.Error("停止监控失败", ex);
            }
            finally
            {
                btnStop.Enabled = true;
            }
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            try
            {
                var address = _context.GetAddress(AddressKeys.Command);
                if (!string.IsNullOrEmpty(address) && _context.PlcClient != null)
                {
                    _context.PlcClient.WriteWord(address, ProcessWords.Idle, out _);
                }

                _context.Trace.Clear();
                _context.State.ProcessWord = ProcessWords.Idle;
                _context.State.ProcessContent = ProcessWords.Describe(ProcessWords.Idle);
                _context.State.SerialNumber = string.Empty;
                _context.State.TrayNo = string.Empty;

                _context.Logger.Info("流程已复位。");
            }
            catch (Exception ex)
            {
                _context.Logger.Error("复位流程失败", ex);
            }
        }

        private async void BtnConfig_Click(object sender, EventArgs e)
        {
            try
            {
                using (var form = new ConfigForm(_context))
                {
                    form.ShowDialog(this);

                    if (!form.Saved)
                    {
                        return;
                    }
                }

                await ApplyConfigAsync();
            }
            catch (Exception ex)
            {
                _context.Logger.Error("打开配置页失败", ex);
                MessageBox.Show(this, ex.Message, "打开配置失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 配置保存后的热加载：停监控 → 重读配置 → 重建通信对象 → 重新绑定界面 → 恢复运行。
        /// 现场改个 IP 或地址表不用重启程序。
        /// </summary>
        private async Task ApplyConfigAsync()
        {
            var wasRunning = _engine.IsRunning;

            try
            {
                if (wasRunning)
                {
                    await _engine.StopAsync();
                }

                _context.ReloadConfig(out var message);
                if (!string.IsNullOrWhiteSpace(message))
                {
                    _context.Logger.Warn(message.Trim());
                }

                if (_plcFactory != null)
                {
                    _context.PlcClient?.Dispose();
                    _context.PlcClient = _plcFactory(_context);
                }

                if (_apiFactory != null)
                {
                    if (_context.ApiClient is IDisposable disposableApi)
                    {
                        disposableApi.Dispose();
                    }

                    _context.ApiClient = _apiFactory(_context);
                }

                _engine.ReloadAddresses();

                BindStaticInfo();
                RefreshState();

                _context.Logger.Info("配置已重新加载并生效。");

                if (wasRunning)
                {
                    await _engine.StartAsync();
                }
            }
            catch (Exception ex)
            {
                _context.Logger.Error("应用新配置失败", ex);
                MessageBox.Show(this, ex.Message, "应用配置失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUser_Click(object sender, EventArgs e)
        {
            // 权限判断就这一行：未启用登录时 Session 是匿名管理员，永远放行。
            if (_context.App.EnableLogin && !_context.Session.HasRole(UserRole.管理员))
            {
                MessageBox.Show(this, "只有管理员可以管理账号。", "权限不足",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var form = new UserForm(_context))
                {
                    form.ShowDialog(this);
                }

                BindStaticInfo();
            }
            catch (Exception ex)
            {
                _context.Logger.Error("打开账号管理失败", ex);
                MessageBox.Show(this, ex.Message, "打开账号管理失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (!AutoStart)
            {
                return;
            }

            try
            {
                await _engine.StartAsync();
            }
            catch (Exception ex)
            {
                _context.Logger.Error("自动启动监控失败", ex);
            }
        }

        protected override async void OnFormClosing(FormClosingEventArgs e)
        {
            _refreshTimer.Stop();

            try
            {
                await _engine.StopAsync();
            }
            catch
            {
                // 关闭阶段忽略停止异常。
            }

            _logSink.Dispose();
            base.OnFormClosing(e);
        }
    }
}
