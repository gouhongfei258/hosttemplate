using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using HostTemplate.Core.Runtime;
using HostTemplate.Core.Security;

namespace HostTemplate.App.Forms
{
    /// <summary>
    /// 账号管理页。增删改查走 <see cref="AuthService"/>，界面只做列表和校验提示。
    ///
    /// 登录功能默认关闭时也能进来提前把账号建好，界面上会标明当前登录开关状态。
    /// </summary>
    public partial class UserForm : Form
    {
        private readonly StationContext _context;
        private readonly AuthService _auth;
        private readonly BindingList<UserAccount> _users = new BindingList<UserAccount>();

        public UserForm(StationContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            InitializeComponent();
            BuildColumns();

            _auth = new AuthService(Path.Combine(_context.Config.ConfigDirectory, "users.xml"));
            _auth.Load(out var message);

            if (!string.IsNullOrWhiteSpace(message))
            {
                _context.Logger.Info(message.Trim());
            }

            lblPath.Text = "账号文件：" + _auth.FilePath;

            var enabled = _context.App.EnableLogin;
            lblLoginState.Text = enabled
                ? "登录功能：已启用"
                : "登录功能：未启用（app.xml → [权限] 01.启用登录）";
            lblLoginState.ForeColor = enabled
                ? Color.FromArgb(22, 163, 74)
                : Color.FromArgb(107, 114, 128);

            Reload();
        }

        private void BuildColumns()
        {
            dgvUsers.AutoGenerateColumns = false;
            dgvUsers.EnableHeadersVisualStyles = false;
            dgvUsers.GridColor = Color.FromArgb(229, 231, 235);
            dgvUsers.ColumnHeadersHeight = 34;
            dgvUsers.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);
            dgvUsers.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(55, 65, 81);
            dgvUsers.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            dgvUsers.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(249, 250, 251);
            dgvUsers.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            dgvUsers.DefaultCellStyle.SelectionForeColor = Color.FromArgb(17, 24, 39);
            dgvUsers.DefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F);
            dgvUsers.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(252, 252, 253);
            dgvUsers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dgvUsers.Columns.Add(TextColumn("UserName", "账号", 20));
            dgvUsers.Columns.Add(TextColumn("DisplayName", "姓名", 22));
            dgvUsers.Columns.Add(TextColumn("Role", "角色", 14));
            dgvUsers.Columns.Add(TextColumn("Enabled", "状态", 12));
            dgvUsers.Columns.Add(TextColumn("MustChangePassword", "首次登录", 14));
            dgvUsers.Columns.Add(TextColumn("LastLogin", "上次登录", 26));

            dgvUsers.CellFormatting += DgvUsers_CellFormatting;
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

        private void Reload()
        {
            _users.RaiseListChangedEvents = false;
            _users.Clear();

            foreach (var user in _auth.Users)
            {
                _users.Add(user);
            }

            _users.RaiseListChangedEvents = true;
            _users.ResetBindings();

            dgvUsers.DataSource = _users;
        }

        private UserAccount Current => dgvUsers.CurrentRow?.DataBoundItem as UserAccount;

        private void DgvUsers_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvUsers.Rows.Count)
            {
                return;
            }

            if (!(dgvUsers.Rows[e.RowIndex].DataBoundItem is UserAccount user))
            {
                return;
            }

            switch (dgvUsers.Columns[e.ColumnIndex].DataPropertyName)
            {
                case "Enabled":
                    e.Value = user.Enabled ? "启用" : "停用";
                    e.FormattingApplied = true;
                    break;

                case "MustChangePassword":
                    e.Value = user.MustChangePassword ? "需修改" : "—";
                    e.FormattingApplied = true;
                    break;

                case "DisplayName":
                    e.Value = string.IsNullOrWhiteSpace(user.DisplayName) ? "—" : user.DisplayName;
                    e.FormattingApplied = true;
                    break;

                case "LastLogin":
                    e.Value = string.IsNullOrWhiteSpace(user.LastLogin) ? "—" : user.LastLogin;
                    e.FormattingApplied = true;
                    break;
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (var form = new UserEditForm(_auth, null))
            {
                if (form.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
            }

            _context.Logger.Info("已新增账号。");
            Reload();
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            EditCurrent(false);
        }

        private void DgvUsers_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                EditCurrent(false);
            }
        }

        private void EditCurrent(bool focusPassword)
        {
            var user = Current;
            if (user == null)
            {
                return;
            }

            using (var form = new UserEditForm(_auth, user, focusPassword))
            {
                if (form.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
            }

            _context.Logger.Info($"已修改账号：{user.UserName}");
            Reload();
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            var user = Current;
            if (user == null)
            {
                return;
            }

            if (MessageBox.Show(this, $"确定删除账号「{user.UserName}」？此操作不可撤销。", "删除账号",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
            {
                return;
            }

            if (!_auth.Remove(user.UserName, out var error))
            {
                MessageBox.Show(this, error, "无法删除", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _context.Logger.Info($"已删除账号：{user.UserName}");
            Reload();
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            _auth.Load(out var message);

            if (!string.IsNullOrWhiteSpace(message))
            {
                _context.Logger.Info(message.Trim());
            }

            Reload();
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
