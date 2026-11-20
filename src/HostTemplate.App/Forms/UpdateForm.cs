using System;
using System.Drawing;
using System.Windows.Forms;
using HostTemplate.Contracts;

namespace HostTemplate.App.Forms
{
    /// <summary>
    /// 手动选择要安装的版本，包括回退到旧版本。
    /// </summary>
    public sealed class UpdateForm : Form
    {
        private readonly ListView _list;
        private readonly Button _install;

        public UpdateForm(VersionListResponse catalog, string currentVersion)
        {
            Text = "程序更新";
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft YaHei UI", 9F);
            ClientSize = new Size(720, 420);
            MinimumSize = new Size(640, 360);
            MaximizeBox = false;
            MinimizeBox = false;

            var header = new Label
            {
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(16, 12, 16, 0),
                Text = "当前版本 v" + (currentVersion ?? "1.0.0") + "。选择一个版本安装，可以选择比当前更旧的版本回退。",
            };

            _list = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HideSelection = false,
                MultiSelect = false,
            };
            _list.Columns.Add("版本", 140);
            _list.Columns.Add("发布时间 (UTC)", 180);
            _list.Columns.Add("大小", 100);
            _list.Columns.Add("说明", 220);
            _list.SelectedIndexChanged += (s, e) => UpdateInstallEnabled();
            _list.DoubleClick += (s, e) => AcceptSelection();

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(12, 8, 12, 8),
            };

            var close = new Button
            {
                Text = "关闭",
                Width = 96,
                Height = 32,
                DialogResult = DialogResult.Cancel,
            };

            _install = new Button
            {
                Text = "安装此版本",
                Width = 120,
                Height = 32,
                Enabled = false,
            };
            _install.Click += (s, e) => AcceptSelection();

            buttons.Controls.Add(close);
            buttons.Controls.Add(_install);

            Controls.Add(_list);
            Controls.Add(buttons);
            Controls.Add(header);
            CancelButton = close;

            if (catalog?.Versions != null)
            {
                var latest = catalog.Latest?.Version;
                foreach (var version in catalog.Versions)
                {
                    if (version == null || string.IsNullOrWhiteSpace(version.Version))
                    {
                        continue;
                    }

                    var note = string.Equals(version.Version, latest, StringComparison.OrdinalIgnoreCase)
                        ? "服务目标版本"
                        : string.Empty;
                    var size = version.Size > 0 ? (version.Size / 1024.0 / 1024.0).ToString("0.0") + " MB" : "--";
                    var published = version.PublishedAt == default
                        ? "--"
                        : version.PublishedAt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm");

                    var item = new ListViewItem(version.Version);
                    item.SubItems.Add(published);
                    item.SubItems.Add(size);
                    item.SubItems.Add(note);
                    item.Tag = version;
                    _list.Items.Add(item);
                }
            }

            if (_list.Items.Count == 0)
            {
                header.Text = "服务端还没有可安装的版本。当前版本 v" + (currentVersion ?? "1.0.0") + "。";
            }
        }

        public TargetVersion SelectedVersion { get; private set; }

        private void UpdateInstallEnabled()
        {
            _install.Enabled = _list.SelectedItems.Count == 1;
        }

        private void AcceptSelection()
        {
            if (_list.SelectedItems.Count != 1)
            {
                return;
            }

            SelectedVersion = _list.SelectedItems[0].Tag as TargetVersion;
            if (SelectedVersion == null)
            {
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
