using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using HostTemplate.App.Controls;
using HostTemplate.App.Forms;
using HostTemplate.Core.Abstractions;
using HostTemplate.Core.Configuration;
using HostTemplate.Core.Diagnostics;
using HostTemplate.Core.Flow;
using HostTemplate.Core.Runtime;
using HostTemplate.Core.Security;
using Sunny.UI;

namespace HostTemplate.App
{
    /// <summary>
    /// 监控主界面。只做展示与指令下发，业务全部在 StationEngine / IStationStep 里。
    /// 「实时数据」页整页由构造时传入的控件决定，这里不假定它是表格、曲线还是图像。
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly StationContext _context;
        private readonly StationEngine _engine;
        private readonly UiLogSink _logSink;
        private readonly Timer _refreshTimer;
        private readonly ToolTip _alarmTip;
        private readonly Func<StationContext, IPlcClient> _plcFactory;
        private readonly Func<StationContext, IApiClient> _apiFactory;
        private readonly string _versionText;

        private int _tick;
        private bool _apiChecking;
        private bool _mesAvailable;
        private MesLamp _mesLamp = MesLamp.Off;
        private readonly System.Drawing.Color _mesOnColor;
        private MainTheme _theme = MainTheme.Night;

        /// <summary>窗口显示后自动开始监控（配合 --autostart 使用）。</summary>
        public bool AutoStart { get; set; }

        public MainForm(
            StationContext context,
            StationEngine engine,
            Control realtimeView,
            Func<StationContext, IPlcClient> plcFactory = null,
            Func<StationContext, IApiClient> apiFactory = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            if (realtimeView == null)
            {
                throw new ArgumentNullException(nameof(realtimeView), "「实时数据」页由工站自己提供。");
            }

            _plcFactory = plcFactory;
            _apiFactory = apiFactory;
            _versionText = ReadDisplayVersion();

            InitializeComponent();
            _mesOnColor = lightApi.ActiveColor;
            realtimeView.Dock = DockStyle.Fill;
            pageRealtime.Controls.Add(realtimeView);

            _logSink = new UiLogSink(rtbLog);
            _context.Logger.Written += (s, e) => _logSink.Enqueue(e);

            _engine.StatusChanged += OnEngineStatusChanged;
            _engine.StepExecuted += OnStepExecuted;
            _context.Alarms.Changed += OnAlarmChanged;

            _alarmTip = new ToolTip();
            ApplyTheme(MainTheme.For(_context.App.Theme));

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

            lblVersion.Text = "v" + _versionText;
            Text = $"上位机模板 · {_context.App.WorkStationName} · v{_versionText}";
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
            RefreshAlarmBanner();

            _tick++;
            RefreshMesLamp();
        }

        /// <summary>
        /// MES 关闭时每拍都打成报警红，不看 PingAsync。
        /// 接口开着时仍按原来的节拍探测：通为绿，不通为灭。从关闭重新打开时立刻补一次探测，避免红灯滞留。
        /// </summary>
        private void RefreshMesLamp()
        {
            var client = _context.ApiClient;
            var enabled = client != null && client.IsAvailable;

            if (!enabled)
            {
                _mesAvailable = false;
                PublishMesLamp(pingSucceeded: true);
                return;
            }

            var justEnabled = !_mesAvailable;
            _mesAvailable = true;

            var due = _tick % 20 == 0;
            var recoverFromDisabled = justEnabled && _mesLamp == MesLamp.Alarm;
            if ((due || recoverFromDisabled) && !_apiChecking)
            {
                _ = CheckApiAsync();
            }
        }

        private void PublishMesLamp(bool pingSucceeded)
        {
            var client = _context.ApiClient;
            var hasClient = client != null;
            var enabled = hasClient && client.IsAvailable;
            _context.State.ApiConnected = enabled && pingSucceeded;
            ApplyMesLamp(MesLampState.Resolve(hasClient, enabled, pingSucceeded));
        }

        private void ApplyMesLamp(MesLamp lamp)
        {
            if (_mesLamp == lamp)
            {
                return;
            }

            _mesLamp = lamp;

            switch (lamp)
            {
                case MesLamp.Alarm:
                    lightApi.ActiveColor = lightAlarm.ActiveColor;
                    lightApi.Active = true;
                    break;
                case MesLamp.On:
                    lightApi.ActiveColor = _mesOnColor;
                    lightApi.Active = true;
                    break;
                default:
                    lightApi.ActiveColor = _mesOnColor;
                    lightApi.Active = false;
                    break;
            }
        }

        private async Task CheckApiAsync()
        {
            _apiChecking = true;

            try
            {
                var client = _context.ApiClient;
                if (client == null || !client.IsAvailable)
                {
                    PublishMesLamp(pingSucceeded: true);
                    return;
                }

                var ok = await client.PingAsync(System.Threading.CancellationToken.None).ConfigureAwait(true);

                client = _context.ApiClient;
                if (client == null || !client.IsAvailable)
                {
                    // 探测期间被关掉。即使这次 Ping 返回 true，灯也要是红的。
                    PublishMesLamp(pingSucceeded: true);
                    return;
                }

                PublishMesLamp(ok);
            }
            catch
            {
                PublishMesLamp(pingSucceeded: false);
            }
            finally
            {
                _apiChecking = false;
            }
        }

        private void RefreshAlarmBanner()
        {
            var active = new List<AlarmItem>();

            foreach (var item in _context.Alarms.Snapshot())
            {
                if (item.Active)
                {
                    active.Add(item);
                }
            }

            active.Sort((a, b) => b.RaisedAt.CompareTo(a.RaisedAt));

            if (active.Count == 0)
            {
                ApplyAlarmBanner(
                    _theme.AlarmIdleBack,
                    _theme.AlarmIdleFore,
                    "无程序报警",
                    "当前没有程序报警");
                return;
            }

            var parts = new List<string>(active.Count);

            foreach (var item in active)
            {
                parts.Add(FormatAlarm(item));
            }

            var text = "程序报警　" + string.Join("；", parts);
            ApplyAlarmBanner(_theme.AlarmActiveBack, _theme.AlarmActiveFore, text, text);
        }

        private void ApplyTheme(MainTheme theme)
        {
            _theme = theme ?? MainTheme.Night;

            BackColor = _theme.WindowBack;
            panelTop.BackColor = _theme.BarBack;
            panelInfo.BackColor = _theme.StripBack;
            panelBottom.BackColor = _theme.BarBack;

            lblStation.ForeColor = _theme.TitleFore;
            lblSub.ForeColor = _theme.SubFore;
            lblWord.ForeColor = _theme.WordFore;
            lblWordName.ForeColor = _theme.TitleFore;
            lblSn.ForeColor = _theme.MutedFore;
            lblTray.ForeColor = _theme.MutedFore;
            lblStep.ForeColor = _theme.MutedFore;
            lblCount.ForeColor = _theme.MutedFore;
            lblHint.ForeColor = _theme.HintFore;
            lblVersion.ForeColor = _theme.SubFore;

            ApplyLight(lightPlc);
            ApplyLight(lightApi);
            ApplyLight(lightAlarm);
            ApplyLight(lightRun);

            pageRealtime.BackColor = _theme.PageBack;
            pageLog.BackColor = _theme.PageBack;
            rtbLog.BackColor = _theme.LogBack;
            rtbLog.ForeColor = _theme.LogInfo;

            // 先套 SunnyUI 的 Black / Blue，再写回本模板的配色，夜间不会被纯黑换掉。
            tabMain.StyleCustomMode = false;
            tabMain.Style = _theme.SunnyStyle;
            tabMain.StyleCustomMode = true;
            tabMain.FillColor = _theme.PageBack;
            tabMain.TabBackColor = _theme.TabBack;
            tabMain.TabSelectedColor = _theme.TabSelected;
            tabMain.TabSelectedForeColor = _theme.TabSelectedFore;
            tabMain.TabUnSelectedForeColor = _theme.TabUnselectedFore;
            tabMain.TabSelectedHighColor = _theme.TabAccent;

            PaintButton(btnStart, _theme.StartFill, System.Drawing.Color.White, _theme.StartFill);
            PaintButton(btnStop, _theme.StopFill, System.Drawing.Color.White, _theme.StopFill);
            PaintButton(btnReset, _theme.ButtonSecondaryFill, _theme.ButtonSecondaryFore, _theme.ButtonSecondaryRect);
            PaintButton(btnConfig, _theme.ButtonSecondaryFill, _theme.ButtonSecondaryFore, _theme.ButtonSecondaryRect);
            PaintButton(btnUser, _theme.ButtonSecondaryFill, _theme.ButtonSecondaryFore, _theme.ButtonSecondaryRect);
            PaintButton(btnTheme, _theme.ThemeButtonFill, _theme.ThemeButtonFore, _theme.ThemeButtonRect);
            btnTheme.Text = _theme.ThemeButtonText;
            _alarmTip.SetToolTip(btnTheme, _theme.ThemeButtonTip);

            _logSink.ApplyColors(_theme.LogInfo, _theme.LogWarn, _theme.LogError, _theme.LogDebug);

            foreach (Control child in pageRealtime.Controls)
            {
                if (child is IMainThemeAware aware)
                {
                    aware.ApplyMainTheme(_theme);
                }
            }

            RefreshAlarmBanner();
            tabMain.Invalidate();
        }

        private void ApplyLight(StatusLight light)
        {
            light.ForeColor = _theme.LightFore;
            light.InactiveColor = _theme.LightInactive;
        }

        private static void PaintButton(UIButton button, System.Drawing.Color fill, System.Drawing.Color fore, System.Drawing.Color rect)
        {
            button.StyleCustomMode = true;
            button.FillColor = fill;
            button.RectColor = rect;
            button.ForeColor = fore;
        }

        private void BtnTheme_Click(object sender, EventArgs e)
        {
            var next = _context.App.Theme == UiThemeMode.夜间 ? UiThemeMode.日间 : UiThemeMode.夜间;
            _context.App.Theme = next;
            ApplyTheme(MainTheme.For(next));

            if (!_context.Config.SaveApp(out var error))
            {
                _context.Logger.Warn("保存界面主题失败：" + error);
                return;
            }

            _context.Logger.Info(next == UiThemeMode.夜间 ? "已切换为夜间界面。" : "已切换为日间界面。");
        }

        private void ApplyAlarmBanner(System.Drawing.Color back, System.Drawing.Color fore, string text, string tip)
        {
            if (panelAlarm.BackColor != back)
            {
                panelAlarm.BackColor = back;
            }

            if (lblAlarm.ForeColor != fore)
            {
                lblAlarm.ForeColor = fore;
            }

            if (lblAlarm.Text != text)
            {
                lblAlarm.Text = text;
            }

            _alarmTip.SetToolTip(lblAlarm, tip);
        }

        private static string FormatAlarm(AlarmItem item)
        {
            if (string.IsNullOrWhiteSpace(item.Message) || string.Equals(item.Message, item.Key, StringComparison.Ordinal))
            {
                return item.Key;
            }

            return item.Key + "：" + item.Message;
        }

        private void OnAlarmChanged(object sender, AlarmChangedEventArgs e)
        {
            var activated = e != null && e.Activated;
            RunOnUi(() =>
            {
                RefreshAlarmBanner();

                if (activated)
                {
                    ShowRealtimeTab();
                }
            });
        }

        private void ShowRealtimeTab()
        {
            if (tabMain.SelectedTab != pageRealtime)
            {
                tabMain.SelectedTab = pageRealtime;
            }
        }

        private void RunOnUi(Action action)
        {
            if (action == null || IsDisposed || !IsHandleCreated)
            {
                return;
            }

            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(action);
                    return;
                }

                action();
            }
            catch (ObjectDisposedException)
            {
                // 关闭窗口时后台线程还可能推一次状态。
            }
        }

        private static string ReadDisplayVersion()
        {
            var assembly = typeof(MainForm).Assembly;
            var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            if (string.IsNullOrWhiteSpace(informational))
            {
                var version = assembly.GetName().Version;
                return version == null ? "1.0.0" : version.ToString(3);
            }

            var plus = informational.IndexOf('+');
            if (plus < 0)
            {
                return informational;
            }

            var head = informational.Substring(0, plus);
            var tail = informational.Substring(plus + 1);
            if (tail.Length > 7)
            {
                tail = tail.Substring(0, 7);
            }

            return string.IsNullOrEmpty(tail) ? head : head + "+" + tail;
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

            if (e.Result != null && !e.Result.Success)
            {
                RunOnUi(ShowRealtimeTab);
            }
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
            _context.Alarms.Changed -= OnAlarmChanged;

            try
            {
                await _engine.StopAsync();
            }
            catch
            {
                // 关闭阶段忽略停止异常。
            }

            _alarmTip.Dispose();
            _logSink.Dispose();
            base.OnFormClosing(e);
        }
    }
}
