using System;
using System.Drawing;
using System.Windows.Forms;
using HostTemplate.Core.Runtime;
using Sunny.UI;

namespace HostTemplate.App.Steps
{
    /// <summary>
    /// 示例工站的「实时数据」整页，不是模板契约。
    /// 大号 OK/NG、表格、曲线都只属于这个示例：没有行就藏表格，没有曲线就藏图。
    /// 视觉标注、多画面组合或其他布局，在 Program.cs 里换成自己的控件即可，不必使用 CycleLiveData。
    /// </summary>
    public sealed class DemoLiveView : UserControl
    {
        private readonly CycleLiveData _live;
        private readonly Timer _timer;
        private readonly TableLayoutPanel _layout;
        private readonly Panel _verdictPanel;
        private readonly Label _lblVerdict;
        private readonly Label _lblReason;
        private readonly UIDataGridView _grid;
        private readonly UILineChart _chart;
        private readonly Font _resultFont;
        private int _dirty = 1;

        public DemoLiveView(CycleLiveData live)
        {
            _live = live ?? throw new ArgumentNullException(nameof(live));

            BackColor = Color.FromArgb(11, 18, 32);
            Font = new Font("Microsoft YaHei UI", 9F);
            _resultFont = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);

            _verdictPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                BackColor = Color.FromArgb(15, 23, 42),
            };

            _lblVerdict = new Label
            {
                Dock = DockStyle.Left,
                Width = 168,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei UI", 40F, FontStyle.Bold),
                ForeColor = Color.FromArgb(107, 114, 128),
                Text = "—",
            };

            _lblReason = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Microsoft YaHei UI", 16F),
                ForeColor = Color.FromArgb(156, 163, 175),
                Padding = new Padding(8, 0, 12, 0),
                Text = "等待本周期结果",
            };

            _verdictPanel.Controls.Add(_lblReason);
            _verdictPanel.Controls.Add(_lblVerdict);

            _grid = BuildGrid();
            _chart = BuildChart();

            _layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.FromArgb(11, 18, 32),
                Margin = new Padding(0),
                Padding = new Padding(0),
            };
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 108F));
            _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 46F));
            _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 54F));
            _layout.Controls.Add(_verdictPanel, 0, 0);
            _layout.Controls.Add(_grid, 0, 1);
            _layout.Controls.Add(_chart, 0, 2);

            Controls.Add(_layout);

            _live.Changed += OnLiveChanged;

            _timer = new Timer { Interval = 200 };
            _timer.Tick += (s, e) => Flush();
            _timer.Start();
            Flush();
        }

        private static UIDataGridView BuildGrid()
        {
            var grid = new UIDataGridView
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(8, 8, 8, 4),
                Visible = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.FromArgb(11, 18, 32),
                GridColor = Color.FromArgb(31, 41, 55),
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 36,
                RowTemplate = { Height = 34 },
                Font = new Font("Microsoft YaHei UI", 9F),
                RectColor = Color.FromArgb(31, 41, 55),
                ScrollBarColor = Color.FromArgb(75, 85, 99),
                ScrollBarBackColor = Color.FromArgb(17, 24, 39),
                StripeEvenColor = Color.FromArgb(11, 18, 32),
                StripeOddColor = Color.FromArgb(15, 23, 42),
            };

            grid.Style = UIStyle.Black;
            grid.StyleCustomMode = true;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(17, 24, 39);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(156, 163, 175);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(17, 24, 39);
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(156, 163, 175);
            grid.DefaultCellStyle.BackColor = Color.FromArgb(11, 18, 32);
            grid.DefaultCellStyle.ForeColor = Color.FromArgb(229, 231, 235);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(30, 41, 59);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(229, 231, 235);
            grid.DefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F);
            grid.RowsDefaultCellStyle.BackColor = Color.FromArgb(11, 18, 32);
            grid.RowsDefaultCellStyle.ForeColor = Color.FromArgb(229, 231, 235);

            grid.AddColumn("项目", "ItemName", 160, DataGridViewContentAlignment.MiddleLeft);
            grid.AddColumn("标准", "Standard", 140, DataGridViewContentAlignment.MiddleLeft);
            grid.AddColumn("实测值", "Value", 120, DataGridViewContentAlignment.MiddleLeft);
            grid.AddColumn("结果", "Result", 80, DataGridViewContentAlignment.MiddleCenter);

            return grid;
        }

        private static UILineChart BuildChart()
        {
            return new UILineChart
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(8, 4, 8, 8),
                Visible = false,
                ChartStyleType = UIChartStyleType.Dark,
                Font = new Font("Microsoft YaHei UI", 9F),
            };
        }

        private void OnLiveChanged(object sender, EventArgs e)
        {
            System.Threading.Interlocked.Exchange(ref _dirty, 1);
        }

        private void Flush()
        {
            if (IsDisposed || System.Threading.Interlocked.Exchange(ref _dirty, 0) == 0)
            {
                return;
            }

            Render(_live.Snapshot());
        }

        private void Render(CycleLiveSnapshot snapshot)
        {
            ApplyVerdict(snapshot);
            ApplyGrid(snapshot);
            ApplyChart(snapshot);

            var hasRows = snapshot.Rows.Count > 0;
            var hasSeries = snapshot.Series.Count > 0;
            _grid.Visible = hasRows;
            _chart.Visible = hasSeries;
            _layout.RowStyles[1].SizeType = hasRows ? SizeType.Percent : SizeType.Absolute;
            _layout.RowStyles[1].Height = hasRows ? (hasSeries ? 46F : 100F) : 0F;
            _layout.RowStyles[2].SizeType = hasSeries ? SizeType.Percent : SizeType.Absolute;
            _layout.RowStyles[2].Height = hasSeries ? (hasRows ? 54F : 100F) : 0F;
        }

        private void ApplyVerdict(CycleLiveSnapshot snapshot)
        {
            switch (snapshot.Verdict)
            {
                case ProductVerdict.Ok:
                    _verdictPanel.BackColor = Color.FromArgb(6, 48, 32);
                    _lblVerdict.Text = "OK";
                    _lblVerdict.ForeColor = Color.FromArgb(74, 222, 128);
                    _lblReason.ForeColor = Color.FromArgb(187, 247, 208);
                    _lblReason.Text = Describe(snapshot, "本周期合格");
                    break;

                case ProductVerdict.Ng:
                    _verdictPanel.BackColor = Color.FromArgb(69, 10, 10);
                    _lblVerdict.Text = "NG";
                    _lblVerdict.ForeColor = Color.FromArgb(248, 113, 113);
                    _lblReason.ForeColor = Color.FromArgb(254, 202, 202);
                    _lblReason.Text = Describe(snapshot, "本周期不良");
                    break;

                default:
                    _verdictPanel.BackColor = Color.FromArgb(15, 23, 42);
                    _lblVerdict.Text = "—";
                    _lblVerdict.ForeColor = Color.FromArgb(107, 114, 128);
                    _lblReason.ForeColor = Color.FromArgb(156, 163, 175);
                    _lblReason.Text = Describe(snapshot, "等待本周期结果");
                    break;
            }
        }

        private static string Describe(CycleLiveSnapshot snapshot, string fallback)
        {
            var reason = string.IsNullOrEmpty(snapshot.Reason) ? fallback : snapshot.Reason;
            if (string.IsNullOrEmpty(snapshot.SerialNumber))
            {
                return reason;
            }

            return "SN " + snapshot.SerialNumber + "　" + reason;
        }

        private void ApplyGrid(CycleLiveSnapshot snapshot)
        {
            _grid.ClearRows();

            foreach (var row in snapshot.Rows)
            {
                var index = _grid.AddRow(row.ItemName, row.Standard, row.Value, row.Passed ? "OK" : "NG");
                var cell = _grid.Rows[index].Cells[3];
                cell.Style.ForeColor = row.Passed
                    ? Color.FromArgb(74, 222, 128)
                    : Color.FromArgb(248, 113, 113);
                cell.Style.Font = _resultFont;
                cell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        private void ApplyChart(CycleLiveSnapshot snapshot)
        {
            if (snapshot.Series.Count == 0)
            {
                return;
            }

            var option = new UILineOption();
            option.Title.Text = string.Empty;
            option.ToolTip.Visible = true;
            option.XAxis.Name = "采样";
            option.YAxis.Name = "数值";
            option.Legend = new UILegend
            {
                Left = UILeftAlignment.Right,
                Top = UITopAlignment.Top,
                Orient = UIOrient.Horizontal,
            };

            foreach (var series in snapshot.Series)
            {
                var line = option.AddSeries(series.Name);
                line.Smooth = true;
                line.Symbol = UILinePointSymbol.Circle;
                line.SymbolSize = 5;
                line.Width = 2;
                line.SetValueFormat(0, 3);
                option.Legend.AddData(series.Name);

                foreach (var point in series.Points)
                {
                    line.Add(point.X, point.Y);
                }
            }

            _chart.SetOption(option);
            _chart.Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Stop();
                _live.Changed -= OnLiveChanged;
                _timer.Dispose();
            }

            base.Dispose(disposing);

            if (disposing)
            {
                _resultFont.Dispose();
            }
        }
    }
}
