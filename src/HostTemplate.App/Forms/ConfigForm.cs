using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using HostTemplate.Core.Abstractions;
using HostTemplate.Core.Configuration;
using HostTemplate.Core.Configuration.Store;
using HostTemplate.Core.Runtime.State;
using HostTemplate.Devices.Hsl;

namespace HostTemplate.App.Forms
{
    /// <summary>
    /// 配置编辑页。四页：基础参数 / PLC 连接 / 地址表 / MES 接口。
    ///
    /// 设计要点：
    ///   1. 所有编辑都在副本上进行，点「取消」不动原配置；
    ///   2. 保存前做校验并自动跳到出错的那一页，避免现场改错地址表；
    ///   3. 保存成功后由主窗调用 StationContext.ReloadConfig() 热生效，不用重启。
    /// </summary>
    public partial class ConfigForm : Form
    {
        private static readonly string[] CpuTypes = { "S1200", "S1500", "S300", "S400", "S200Smart" };
        private static readonly string[] RwValues = { "R", "W", "RW" };
        private static readonly string[] DataTypes = { "Bool", "Word", "Int32", "Real", "String" };

        private static readonly string[] RequiredKeys =
        {
            AddressKeys.Heartbeat,
            AddressKeys.Command,
            AddressKeys.ReserveBool,
            AddressKeys.ReviewResult,
            AddressKeys.MesSerialNumber,
            AddressKeys.PlcSerialNumber,
            AddressKeys.TrayNumber,
        };

        private readonly StationContext _context;
        private readonly BindingList<AddrOption> _addresses = new BindingList<AddrOption>();
        private readonly BindingList<EndpointItem> _endpoints = new BindingList<EndpointItem>();
        private readonly BindingList<HeaderItem> _headers = new BindingList<HeaderItem>();

        private AppConfig _appEdit;
        private PlcConfig _plcEdit;
        private ApiConfig _apiEdit;

        private Button[] _navButtons;
        private Panel[] _pages;

        // PLC 连接页控件（在 BuildPlcEditor 里创建）
        private ComboBox cmbDevice;
        private TextBox txtIp;
        private TextBox txtPort;
        private TextBox txtRack;
        private TextBox txtSlot;
        private TextBox txtStation;
        private TextBox txtConnectTimeout;
        private TextBox txtReceiveTimeout;
        private TextBox txtReconnectDelay;
        private Panel rowRack;
        private Panel rowSlot;
        private Panel rowStation;
        private Label lblDeviceRemark;
        private Label lblPortHint;
        private Label lblStationLabel;
        private Label lblStationHint;
        private Label lblPlcAddressHint;
        private Label lblTestResult;
        private Sunny.UI.UIButton btnTestConnect;
        private Panel _plcSeparator;
        private bool _suspendDeviceChange;
        private readonly List<Panel> _plcRows = new List<Panel>();

        public ConfigForm(StationContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            InitializeComponent();

            BuildColumns();
            BuildPlcEditor();
            BindNav();
            LoadEditors();

            lblPath.Text = "配置文件目录：" + _context.Config.ConfigDirectory;
            lblStatus.Text = $"当前工站：{_context.App.WorkStationName}（{_context.App.EquId}）";
        }

        /// <summary>是否点了保存并成功落盘。</summary>
        public bool Saved { get; private set; }

        private void BindNav()
        {
            _navButtons = new[] { btnNavApp, btnNavPlc, btnNavAddr, btnNavApi };
            _pages = new[] { pageApp, pagePlc, pageAddr, pageApi };

            SelectPage(0);
        }

        private void LoadEditors()
        {
            _appEdit = XmlConfigStore.Clone(_context.App) ?? new AppConfig();
            _plcEdit = XmlConfigStore.Clone(_context.Plc) ?? new PlcConfig();
            _apiEdit = XmlConfigStore.Clone(_context.Api) ?? new ApiConfig();

            ApplyEditors();
        }

        private void ApplyEditors()
        {
            pgApp.SelectedObject = _appEdit;
            pgApi.SelectedObject = _apiEdit;

            LoadPlcEditor();

            Fill(_addresses, _plcEdit.Addresses);
            Fill(_endpoints, _apiEdit.Endpoints);
            Fill(_headers, _apiEdit.Headers);

            dgvAddr.DataSource = _addresses;
            dgvEndpoints.DataSource = _endpoints;
            dgvHeaders.DataSource = _headers;
        }

        private static void Fill<T>(BindingList<T> target, IEnumerable<T> source)
        {
            target.RaiseListChangedEvents = false;
            target.Clear();

            if (source != null)
            {
                foreach (var item in source)
                {
                    if (item != null)
                    {
                        target.Add(item);
                    }
                }
            }

            target.RaiseListChangedEvents = true;
            target.ResetBindings();
        }

        private void BuildColumns()
        {
            ConfigureGrid(dgvAddr);
            ConfigureGrid(dgvEndpoints);
            ConfigureGrid(dgvHeaders);

            dgvAddr.Columns.Add(TextColumn("Name", "键名", 22));
            dgvAddr.Columns.Add(ComboColumn("Rw", "读写", 10, RwValues));
            dgvAddr.Columns.Add(TextColumn("Address", "PLC 地址", 24));
            dgvAddr.Columns.Add(TextColumn("Length", "长度", 8));
            dgvAddr.Columns.Add(ComboColumn("DataType", "数据类型", 14, DataTypes));
            dgvAddr.Columns.Add(TextColumn("Remark", "备注", 22));

            dgvEndpoints.AllowUserToAddRows = true;
            dgvEndpoints.AllowUserToDeleteRows = true;
            dgvEndpoints.Columns.Add(TextColumn("Key", "键名", 26));
            dgvEndpoints.Columns.Add(TextColumn("Path", "相对路径", 54));
            dgvEndpoints.Columns.Add(ComboColumn("Method", "方法", 20, new[] { "POST", "GET", "PUT" }));

            dgvHeaders.AllowUserToAddRows = true;
            dgvHeaders.AllowUserToDeleteRows = true;
            dgvHeaders.Columns.Add(TextColumn("Name", "参数名", 40));
            dgvHeaders.Columns.Add(TextColumn("Value", "参数值", 60));

            // 下拉列遇到空值时不要弹异常框。
            dgvAddr.DataError += (s, e) => e.ThrowException = false;
            dgvEndpoints.DataError += (s, e) => e.ThrowException = false;
            dgvHeaders.DataError += (s, e) => e.ThrowException = false;
        }

        private static void ConfigureGrid(DataGridView grid)
        {
            grid.AutoGenerateColumns = false;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.GridColor = Color.FromArgb(229, 231, 235);
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersHeight = 34;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(55, 65, 81);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(249, 250, 251);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(17, 24, 39);
            grid.DefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F);
            grid.DefaultCellStyle.Padding = new Padding(2, 0, 2, 0);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(252, 252, 253);
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private static DataGridViewTextBoxColumn TextColumn(string property, string header, int weight)
        {
            return new DataGridViewTextBoxColumn
            {
                DataPropertyName = property,
                HeaderText = header,
                FillWeight = weight,
                SortMode = DataGridViewColumnSortMode.NotSortable,
            };
        }

        private static DataGridViewComboBoxColumn ComboColumn(string property, string header, int weight, string[] items)
        {
            var column = new DataGridViewComboBoxColumn
            {
                DataPropertyName = property,
                HeaderText = header,
                FillWeight = weight,
                FlatStyle = FlatStyle.Flat,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                SortMode = DataGridViewColumnSortMode.NotSortable,
            };

            column.Items.AddRange(items);
            return column;
        }

        // ---------------- PLC 连接页（设备下拉 + 参数 + 连接测试） ----------------

        /// <summary>
        /// 用代码搭 PLC 页：设备下拉框决定显示哪些参数，每行自带默认值提示。
        /// 放代码里是因为行要按设备类型动态显隐，设计器里反而看不清。
        /// </summary>
        private void BuildPlcEditor()
        {
            var labelFont = new Font("Microsoft YaHei UI", 9F);
            var hintFont = new Font("Microsoft YaHei UI", 8.5F);
            var hintColor = Color.FromArgb(107, 114, 128);

            var lblDevice = new Label
            {
                Text = "设备类型",
                Location = new Point(0, 9),
                Size = new Size(108, 20),
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(55, 65, 81),
                Font = labelFont,
            };

            cmbDevice = new ComboBox
            {
                Location = new Point(116, 5),
                Size = new Size(260, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 10F),
            };

            foreach (var device in PlcDeviceCatalog.All)
            {
                cmbDevice.Items.Add(device);
            }

            cmbDevice.SelectedIndexChanged += CmbDevice_SelectedIndexChanged;

            lblDeviceRemark = new Label
            {
                Location = new Point(392, 3),
                Size = new Size(468, 42),
                ForeColor = hintColor,
                Font = hintFont,
            };

            pagePlc.Controls.Add(lblDevice);
            pagePlc.Controls.Add(cmbDevice);
            pagePlc.Controls.Add(lblDeviceRemark);

            txtIp = AddPlcRow(52, "IP 地址", "例：192.168.0.1", 200, out _, out _, out _);
            txtPort = AddPlcRow(94, "端口", string.Empty, 120, out _, out _, out lblPortHint);
            txtRack = AddPlcRow(136, "机架号", "S7-300/400 常用 0", 120, out rowRack, out _, out _);
            txtSlot = AddPlcRow(178, "插槽号", "S7-300/400 常用 2，S1200/1500 用 0", 120, out rowSlot, out _, out _);
            txtStation = AddPlcRow(220, "站号", string.Empty, 120, out rowStation, out lblStationLabel, out lblStationHint);
            txtConnectTimeout = AddPlcRow(262, "连接超时(ms)", "建议 2000，网络差可放大", 120, out _, out _, out _);
            txtReceiveTimeout = AddPlcRow(304, "读写超时(ms)", "建议 2000", 120, out _, out _, out _);
            txtReconnectDelay = AddPlcRow(346, "重连间隔(ms)", "断线后多久重试，建议 2000", 120, out _, out _, out _);

            lblPlcAddressHint = new Label
            {
                Location = new Point(0, 392),
                Size = new Size(860, 44),
                ForeColor = hintColor,
                Font = hintFont,
            };

            var separator = new Panel
            {
                Location = new Point(0, 444),
                Size = new Size(860, 1),
                BackColor = Color.FromArgb(229, 231, 235),
            };

            btnTestConnect = new Sunny.UI.UIButton
            {
                Text = "连接测试",
                Location = new Point(0, 458),
                Size = new Size(120, 38),
                MinimumSize = new Size(1, 1),
                Radius = 4,
                FillColor = Color.FromArgb(37, 99, 235),
                RectColor = Color.FromArgb(37, 99, 235),
                Font = new Font("Microsoft YaHei UI", 9F),
            };
            btnTestConnect.Click += BtnTestConnect_Click;

            lblTestResult = new Label
            {
                Location = new Point(134, 460),
                Size = new Size(726, 64),
                Font = labelFont,
                ForeColor = hintColor,
                Text = "点「连接测试」会用当前参数实际连一次 PLC，并尝试读取「PLC_指令」。",
            };

            pagePlc.Controls.Add(lblPlcAddressHint);
            pagePlc.Controls.Add(separator);
            pagePlc.Controls.Add(btnTestConnect);
            pagePlc.Controls.Add(lblTestResult);

            _plcSeparator = separator;
            RelayoutPlcRows();
        }

        private TextBox AddPlcRow(
            int y,
            string label,
            string hint,
            int boxWidth,
            out Panel row,
            out Label rowLabel,
            out Label hintLabel)
        {
            row = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(860, 38),
            };

            rowLabel = new Label
            {
                Text = label,
                Location = new Point(0, 9),
                Size = new Size(108, 20),
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(55, 65, 81),
                Font = new Font("Microsoft YaHei UI", 9F),
            };

            var box = new TextBox
            {
                Location = new Point(116, 5),
                Size = new Size(boxWidth, 26),
                Font = new Font("Microsoft YaHei UI", 10F),
                BorderStyle = BorderStyle.FixedSingle,
            };

            hintLabel = new Label
            {
                Text = hint,
                Location = new Point(116 + boxWidth + 14, 9),
                Size = new Size(860 - 116 - boxWidth - 14, 20),
                ForeColor = Color.FromArgb(107, 114, 128),
                Font = new Font("Microsoft YaHei UI", 8.5F),
                AutoEllipsis = true,
            };

            row.Controls.Add(rowLabel);
            row.Controls.Add(box);
            row.Controls.Add(hintLabel);
            row.Tag = true;
            pagePlc.Controls.Add(row);
            _plcRows.Add(row);

            return box;
        }

        /// <summary>按可见行重排，隐藏「站号」或「机架/插槽」后不留空洞。</summary>
        private void RelayoutPlcRows()
        {
            if (lblPlcAddressHint == null || _plcSeparator == null || btnTestConnect == null)
            {
                return;
            }

            var y = 52;

            foreach (var row in _plcRows)
            {
                // 不能用 row.Visible：页面本身隐藏时父级会把 Visible 压成 false。
                if (!(row.Tag is bool shown) || !shown)
                {
                    continue;
                }

                row.Location = new Point(0, y);
                y += 42;
            }

            lblPlcAddressHint.Location = new Point(0, y + 10);
            _plcSeparator.Location = new Point(0, y + 64);
            btnTestConnect.Location = new Point(0, y + 78);
            lblTestResult.Location = new Point(134, y + 80);
        }

        /// <summary>把配置里的值填进控件，并选中对应的设备类型。</summary>
        private void LoadPlcEditor()
        {
            _suspendDeviceChange = true;
            cmbDevice.SelectedItem = PlcDeviceCatalog.Find(_plcEdit.Protocol, _plcEdit.CpuType);
            _suspendDeviceChange = false;

            txtIp.Text = _plcEdit.Address;
            txtPort.Text = _plcEdit.Port.ToString();
            txtRack.Text = _plcEdit.Rack.ToString();
            txtSlot.Text = _plcEdit.Slot.ToString();
            txtStation.Text = _plcEdit.StationNo.ToString();
            txtConnectTimeout.Text = _plcEdit.ConnectTimeoutMs.ToString();
            txtReceiveTimeout.Text = _plcEdit.ReceiveTimeoutMs.ToString();
            txtReconnectDelay.Text = _plcEdit.ReconnectDelayMs.ToString();

            ApplyDeviceLayout(cmbDevice.SelectedItem as PlcDeviceDescriptor, false);
        }

        private void CmbDevice_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suspendDeviceChange)
            {
                return;
            }

            ApplyDeviceLayout(cmbDevice.SelectedItem as PlcDeviceDescriptor, true);
            SetTestResult(Color.FromArgb(107, 114, 128), "设备类型已切换，点「连接测试」验证参数。");
        }

        /// <summary>
        /// 按设备类型调整界面：显隐机架/插槽/站号，刷新默认值与提示。
        /// applyDefaults 为 true 时把端口等参数重置成该设备的默认值。
        /// </summary>
        private void ApplyDeviceLayout(PlcDeviceDescriptor device, bool applyDefaults)
        {
            if (device == null)
            {
                return;
            }

            rowRack.Visible = device.UseRackSlot;
            rowSlot.Visible = device.UseRackSlot;
            rowStation.Visible = device.UseStation;

            // 记录显隐状态供重排使用（页面隐藏时 Visible 不可靠）。
            rowRack.Tag = device.UseRackSlot;
            rowSlot.Tag = device.UseRackSlot;
            rowStation.Tag = device.UseStation;
            lblDeviceRemark.Text = device.Remark;
            lblPortHint.Text = $"默认 {device.DefaultPort}";
            lblStationLabel.Text = device.StationLabel;
            lblStationHint.Text = string.Equals(device.Protocol, HslDeviceFactory.ProtocolOmronFins, StringComparison.OrdinalIgnoreCase)
                ? "0 = 自动取 IP 末位"
                : "从站地址 1 ~ 247";

            lblPlcAddressHint.Text = "地址写法：" + device.AddressHint;

            RelayoutPlcRows();

            if (!applyDefaults)
            {
                return;
            }

            txtPort.Text = device.DefaultPort.ToString();
            txtRack.Text = device.DefaultRack.ToString();
            txtSlot.Text = device.DefaultSlot.ToString();
            txtStation.Text = device.DefaultStation.ToString();
        }

        /// <summary>把控件里的值写回 _plcEdit，返回格式错误。</summary>
        private List<string> CollectPlcEditor()
        {
            var errors = new List<string>();

            if (cmbDevice.SelectedItem is PlcDeviceDescriptor device)
            {
                _plcEdit.Protocol = device.Protocol;

                // CPU 型号只对西门子有意义；S7-200 Smart 没有机架/插槽但仍是 S7 协议。
                _plcEdit.CpuType = string.Equals(device.Protocol, HslDeviceFactory.ProtocolSiemensS7, StringComparison.OrdinalIgnoreCase)
                    ? device.CpuType
                    : string.Empty;
            }

            _plcEdit.Address = (txtIp.Text ?? string.Empty).Trim();

            if (TryParse(txtPort, "端口", errors, out var port))
            {
                _plcEdit.Port = port;
            }

            if (TryParse(txtRack, "机架号", errors, out var rack))
            {
                _plcEdit.Rack = rack;
            }

            if (TryParse(txtSlot, "插槽号", errors, out var slot))
            {
                _plcEdit.Slot = slot;
            }

            if (TryParse(txtStation, "站号", errors, out var station))
            {
                _plcEdit.StationNo = station;
            }

            if (TryParse(txtConnectTimeout, "连接超时", errors, out var connectTimeout))
            {
                _plcEdit.ConnectTimeoutMs = connectTimeout;
            }

            if (TryParse(txtReceiveTimeout, "读写超时", errors, out var receiveTimeout))
            {
                _plcEdit.ReceiveTimeoutMs = receiveTimeout;
            }

            if (TryParse(txtReconnectDelay, "重连间隔", errors, out var reconnectDelay))
            {
                _plcEdit.ReconnectDelayMs = reconnectDelay;
            }

            return errors;
        }

        private static bool TryParse(TextBox box, string name, List<string> errors, out int value)
        {
            if (int.TryParse((box.Text ?? string.Empty).Trim(), out value))
            {
                return true;
            }

            errors.Add($"PLC 连接：{name}必须是数字。");
            return false;
        }

        private async void BtnTestConnect_Click(object sender, EventArgs e)
        {
            var errors = CollectPlcEditor();
            if (errors.Count > 0)
            {
                SetTestResult(Color.FromArgb(185, 28, 28), string.Join(" ", errors));
                return;
            }

            // 用副本去测，测完不影响编辑中的对象。
            var probe = XmlConfigStore.Clone(_plcEdit);
            var commandAddress = _addresses
                .FirstOrDefault(a => string.Equals(a.Name, AddressKeys.Command, StringComparison.Ordinal)
                    && !string.IsNullOrWhiteSpace(a.Address))?.Address;

            btnTestConnect.Enabled = false;
            SetTestResult(Color.FromArgb(107, 114, 128), "正在连接，请稍候…");

            var stopwatch = Stopwatch.StartNew();
            var result = await ProbeAsync(probe, commandAddress);
            stopwatch.Stop();

            btnTestConnect.Enabled = true;

            if (!result.Ok)
            {
                SetTestResult(Color.FromArgb(185, 28, 28), $"✘ 连接失败（{stopwatch.ElapsedMilliseconds} ms）：{result.Message}");
                return;
            }

            var color = Color.FromArgb(21, 128, 61);
            var text = $"✔ 连接成功（{stopwatch.ElapsedMilliseconds} ms）";

            if (!string.IsNullOrEmpty(result.Detail))
            {
                text += "　" + result.Detail;

                if (result.Detail.Contains("失败"))
                {
                    color = Color.FromArgb(180, 83, 9);
                }
            }

            SetTestResult(color, text);
        }

        /// <summary>
        /// 连接测试本体：建客户端 → 连接 → 读一次 PLC_指令。
        /// 连接和读取走 HSL 异步接口，不占用界面线程。
        /// 失败时用 Ping 区分「网络不通」和「端口/站号不对」，现场排查省一半时间。
        /// </summary>
        private static async Task<(bool Ok, string Message, string Detail)> ProbeAsync(PlcConfig config, string commandAddress)
        {
            IPlcClient client = null;

            try
            {
                if (!HslDeviceFactory.TryCreate(config, out client, out var createError))
                {
                    return (false, "创建客户端失败：" + createError, string.Empty);
                }

                var opened = await client.OpenAsync().ConfigureAwait(false);
                if (!opened.Success)
                {
                    var ping = client is HslDeviceBase hsl && hsl.Ping();
                    var hint = ping
                        ? "（IP 能 Ping 通，检查端口 / 站号 / 机架槽号）"
                        : "（IP 不通，检查网线、网段和 PLC 电源）";

                    return (false, opened.Error + hint, string.Empty);
                }

                if (string.IsNullOrWhiteSpace(commandAddress))
                {
                    return (true, string.Empty, "已连接，地址表暂无点位，未做读取");
                }

                var read = await client.ReadWordAsync(commandAddress).ConfigureAwait(false);
                if (read.Success)
                {
                    return (true, string.Empty, $"读取 {commandAddress} = {read.Value}");
                }

                return (true, string.Empty, $"已连接，但读取 {commandAddress} 失败：{read.Error}");
            }
            catch (Exception ex)
            {
                return (false, ex.Message, string.Empty);
            }
            finally
            {
                if (client != null)
                {
                    await client.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        private void SetTestResult(Color color, string text)
        {
            lblTestResult.ForeColor = color;
            lblTestResult.Text = text;
        }

        private void BtnNav_Click(object sender, EventArgs e)
        {
            var index = Array.IndexOf(_navButtons, sender as Button);
            if (index >= 0)
            {
                SelectPage(index);
            }
        }

        /// <summary>切换页签：0 基础参数 / 1 PLC 连接 / 2 地址表 / 3 MES 接口。</summary>
        public void SelectPage(int index)
        {
            if (_pages == null || index < 0 || index >= _pages.Length)
            {
                return;
            }

            dgvAddr.EndEdit();
            dgvEndpoints.EndEdit();
            dgvHeaders.EndEdit();

            for (var i = 0; i < _pages.Length; i++)
            {
                var selected = i == index;
                _pages[i].Visible = selected;

                var button = _navButtons[i];
                button.BackColor = selected ? Color.White : Color.FromArgb(243, 244, 246);
                button.ForeColor = selected ? Color.FromArgb(15, 23, 42) : Color.FromArgb(75, 85, 99);
                button.Font = new Font("Microsoft YaHei UI", 10F, selected ? FontStyle.Bold : FontStyle.Regular);
            }

            if (index == 2)
            {
                lblStatus.Text = $"地址表共 {_addresses.Count} 个点位";
            }
        }

        private void BtnAddrAdd_Click(object sender, EventArgs e)
        {
            var item = new AddrOption
            {
                Name = "新键名",
                Rw = "R",
                Address = "MW0",
                Length = 0,
                DataType = "Word",
                Remark = string.Empty,
            };

            _addresses.Add(item);

            var row = dgvAddr.Rows.Count - 1;
            if (row >= 0)
            {
                dgvAddr.CurrentCell = dgvAddr.Rows[row].Cells[0];
                dgvAddr.BeginEdit(true);
            }
        }

        private void BtnAddrDel_Click(object sender, EventArgs e)
        {
            var index = dgvAddr.CurrentRow?.Index ?? -1;
            if (index < 0 || index >= _addresses.Count)
            {
                return;
            }

            var name = _addresses[index].Name;
            if (MessageBox.Show(this, $"确定删除点位「{name}」？", "确认", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
            {
                return;
            }

            _addresses.RemoveAt(index);
        }

        private void BtnAddrCopy_Click(object sender, EventArgs e)
        {
            var index = dgvAddr.CurrentRow?.Index ?? -1;
            if (index < 0 || index >= _addresses.Count)
            {
                return;
            }

            var source = _addresses[index];
            _addresses.Insert(index + 1, new AddrOption
            {
                Name = source.Name + "_copy",
                Rw = source.Rw,
                Address = source.Address,
                Length = source.Length,
                DataType = source.DataType,
                Remark = source.Remark,
            });
        }

        private void BtnAddrUp_Click(object sender, EventArgs e)
        {
            MoveAddress(-1);
        }

        private void BtnAddrDown_Click(object sender, EventArgs e)
        {
            MoveAddress(1);
        }

        private void MoveAddress(int offset)
        {
            var index = dgvAddr.CurrentRow?.Index ?? -1;
            var target = index + offset;

            if (index < 0 || target < 0 || target >= _addresses.Count)
            {
                return;
            }

            var item = _addresses[index];
            _addresses.RemoveAt(index);
            _addresses.Insert(target, item);

            dgvAddr.CurrentCell = dgvAddr.Rows[target].Cells[0];
        }

        private void BtnDefault_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "将四页配置全部恢复为出厂默认值，确定？", "恢复默认值",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
            {
                return;
            }

            _appEdit = ConfigManager.CreateDefaultApp();
            _plcEdit = ConfigManager.CreateDefaultPlc();
            _apiEdit = ConfigManager.CreateDefaultApi();

            ApplyEditors();
            SelectPage(0);
            lblStatus.Text = "已载入默认值，点「保存并生效」后写入配置文件。";
        }

        private void BtnOpenDir_Click(object sender, EventArgs e)
        {
            try
            {
                var directory = _context.Config.ConfigDirectory;
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = directory,
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                _context.Logger.Error("打开配置目录失败", ex);
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            dgvAddr.EndEdit();
            dgvEndpoints.EndEdit();
            dgvHeaders.EndEdit();

            // PLC 页是自定义控件，先把界面值写回配置对象再校验。
            var parseErrors = CollectPlcEditor();
            var errors = ValidateAll(out var errorPage);

            if (parseErrors.Count > 0)
            {
                errors.InsertRange(0, parseErrors);
                errorPage = 1;
            }

            if (errors.Count > 0)
            {
                SelectPage(errorPage);
                MessageBox.Show(this,
                    "配置有 " + errors.Count + " 处需要修正：\r\n\r\n" + string.Join("\r\n", errors.Take(8)),
                    "校验未通过", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 表格数据回写到配置对象。
            _plcEdit.Addresses = _addresses.ToList();
            _apiEdit.Endpoints = _endpoints.ToList();
            _apiEdit.Headers = _headers.ToList();

            if (!_context.Config.SaveAll(_appEdit, _plcEdit, _apiEdit, out var error))
            {
                MessageBox.Show(this, "保存失败：\r\n" + error, "保存失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Saved = true;
            _context.Logger.Info("配置已保存：" + _context.Config.ConfigDirectory);

            var warnings = CollectWarnings();
            if (warnings.Count > 0)
            {
                MessageBox.Show(this,
                    "配置已保存。以下项目需要注意：\r\n\r\n" + string.Join("\r\n", warnings),
                    "已保存", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// 校验全部页面。返回错误清单与首个出错页面索引（0 基础参数 / 1 PLC / 2 地址表 / 3 接口）。
        /// </summary>
        private List<string> ValidateAll(out int errorPage)
        {
            var errors = new List<string>();
            var firstPage = 0;

            void Add(string message, int page)
            {
                errors.Add(message);
                if (errors.Count == 1)
                {
                    firstPage = page;
                }
            }

            if (_appEdit.PollIntervalMs < 10 || _appEdit.PollIntervalMs > 5000)
            {
                Add("基础参数：轮询周期需在 10 ~ 5000 ms 之间。", 0);
            }

            if (_appEdit.HeartbeatTicks < 1)
            {
                Add("基础参数：心跳翻转周期至少为 1。", 0);
            }

            if (_appEdit.SnLength < 0 || _appEdit.SnLength > 64)
            {
                Add("基础参数：条码长度需在 0 ~ 64 之间（0 表示不校验）。", 0);
            }

            if (string.IsNullOrWhiteSpace(_appEdit.DatabasePath))
            {
                Add("基础参数：数据库路径不能为空。", 0);
            }

            if (string.IsNullOrWhiteSpace(_plcEdit.Address))
            {
                Add("PLC 连接：IP 地址不能为空。", 1);
            }

            if (_plcEdit.Port < 1 || _plcEdit.Port > 65535)
            {
                Add("PLC 连接：端口需在 1 ~ 65535 之间。", 1);
            }

            var protocol = (_plcEdit.Protocol ?? string.Empty).Trim();

            if (!HslDeviceFactory.SupportedProtocols.Contains(protocol, StringComparer.OrdinalIgnoreCase))
            {
                Add($"PLC 连接：协议只支持 {string.Join(" / ", HslDeviceFactory.SupportedProtocols)}。", 1);
            }
            else if (string.Equals(protocol, HslDeviceFactory.ProtocolSiemensS7, StringComparison.OrdinalIgnoreCase)
                && !CpuTypes.Contains(_plcEdit.CpuType, StringComparer.OrdinalIgnoreCase))
            {
                Add($"PLC 连接：CPU 型号只支持 {string.Join(" / ", CpuTypes)}。", 1);
            }

            if (_plcEdit.Rack < 0 || _plcEdit.Rack > 7)
            {
                Add("PLC 连接：机架号需在 0 ~ 7 之间。", 1);
            }

            if (_plcEdit.Slot < 0 || _plcEdit.Slot > 31)
            {
                Add("PLC 连接：插槽号需在 0 ~ 31 之间。", 1);
            }

            if (_plcEdit.StationNo < 1 || _plcEdit.StationNo > 247)
            {
                Add("PLC 连接：Modbus 站号需在 1 ~ 247 之间。", 1);
            }

            if (_plcEdit.ConnectTimeoutMs <= 0 || _plcEdit.ReceiveTimeoutMs <= 0)
            {
                Add("PLC 连接：超时时间需大于 0。", 1);
            }

            if (_addresses.Count == 0)
            {
                Add("地址表：至少需要配置「PLC_指令」一个点位。", 2);
            }

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < _addresses.Count; i++)
            {
                var item = _addresses[i];
                var row = i + 1;

                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    Add($"地址表第 {row} 行：键名不能为空。", 2);
                    continue;
                }

                if (!names.Add(item.Name.Trim()))
                {
                    Add($"地址表第 {row} 行：键名「{item.Name}」重复。", 2);
                }

                if (string.IsNullOrWhiteSpace(item.Address))
                {
                    Add($"地址表第 {row} 行（{item.Name}）：地址不能为空。", 2);
                }

                if (!string.IsNullOrWhiteSpace(item.Rw)
                    && !RwValues.Contains(item.Rw.Trim(), StringComparer.OrdinalIgnoreCase))
                {
                    Add($"地址表第 {row} 行（{item.Name}）：读写列只能填 R / W / RW。", 2);
                }
            }

            if (!names.Contains(AddressKeys.Command))
            {
                Add($"地址表：缺少「{AddressKeys.Command}」点位，监控无法启动。", 2);
            }

            if (_apiEdit.Enabled)
            {
                if (!Uri.TryCreate(_apiEdit.BaseUrl, UriKind.Absolute, out var uri)
                    || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    Add("MES 接口：基地址需是 http/https 开头的完整地址。", 3);
                }

                if (_apiEdit.TimeoutMs <= 0)
                {
                    Add("MES 接口：超时时间需大于 0。", 3);
                }

                if (_apiEdit.RetryCount < 0)
                {
                    Add("MES 接口：重试次数不能为负数。", 3);
                }

                var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (var i = 0; i < _endpoints.Count; i++)
                {
                    var item = _endpoints[i];
                    if (string.IsNullOrWhiteSpace(item.Key))
                    {
                        continue;
                    }

                    if (!keys.Add(item.Key.Trim()))
                    {
                        Add($"MES 接口第 {i + 1} 行：端点键名「{item.Key}」重复。", 3);
                    }

                    if (string.IsNullOrWhiteSpace(item.Path))
                    {
                        Add($"MES 接口第 {i + 1} 行（{item.Key}）：相对路径不能为空。", 3);
                    }
                }
            }

            errorPage = firstPage;
            return errors;
        }

        /// <summary>不阻断保存、但值得提醒的问题。</summary>
        private List<string> CollectWarnings()
        {
            var warnings = new List<string>();
            var names = new HashSet<string>(
                _addresses.Where(a => !string.IsNullOrWhiteSpace(a.Name)).Select(a => a.Name.Trim()),
                StringComparer.OrdinalIgnoreCase);

            var missing = RequiredKeys.Where(k => !names.Contains(k)).ToList();
            if (missing.Count > 0)
            {
                warnings.Add("地址表缺少键名：" + string.Join("、", missing) + "，相关功能将不可用。");
            }

            if (_appEdit.EnableLogin)
            {
                warnings.Add("已启用登录：下次启动程序需要输入账号密码（默认 admin / admin123）。");
            }

            if (_appEdit.Simulate)
            {
                warnings.Add("仿真模式处于开启状态，现场运行时请关闭，否则不会连接真实 PLC。");
            }

            return warnings;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!Saved && DialogResult != DialogResult.Cancel)
            {
                var result = MessageBox.Show(this, "配置尚未保存，确定关闭？", "提示",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Question);

                if (result != DialogResult.OK)
                {
                    e.Cancel = true;
                    return;
                }
            }

            base.OnFormClosing(e);
        }
    }
}
