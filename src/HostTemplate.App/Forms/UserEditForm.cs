using System;
using System.Windows.Forms;
using HostTemplate.Core.Security;

namespace HostTemplate.App.Forms
{
    /// <summary>
    /// 新增 / 编辑账号。
    /// 编辑时密码留空表示不修改；填了就是重置密码（下次登录强制改）。
    /// </summary>
    public partial class UserEditForm : Form
    {
        private readonly AuthService _auth;
        private readonly UserAccount _account;
        private readonly bool _isNew;

        public UserEditForm(AuthService auth, UserAccount account, bool focusPassword = false)
        {
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
            _account = account;
            _isNew = account == null;

            InitializeComponent();

            cmbRole.Items.AddRange(new object[] { UserRole.操作员, UserRole.工程师, UserRole.管理员 });

            if (_isNew)
            {
                Text = "新增账号";
                lblTitle.Text = "新增账号";
                cmbRole.SelectedItem = UserRole.操作员;
                lblPassword.Text = "密码";
                lblConfirm.Visible = true;
                txtConfirm.Visible = true;
            }
            else
            {
                Text = "编辑账号";
                lblTitle.Text = "编辑账号";
                txtUserName.Text = account.UserName;
                txtUserName.ReadOnly = true;
                txtUserName.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
                txtDisplayName.Text = account.DisplayName;
                cmbRole.SelectedItem = account.Role;
                chkEnabled.Checked = account.Enabled;
                lblPassword.Text = "重置密码（留空不改）";
                lblPassword.AutoSize = false;
                lblPassword.Size = new System.Drawing.Size(200, 17);
            }

            if (focusPassword)
            {
                txtPassword.Focus();
            }
            else
            {
                txtUserName.Focus();
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            var userName = txtUserName.Text?.Trim() ?? string.Empty;
            var displayName = txtDisplayName.Text?.Trim() ?? string.Empty;
            var password = txtPassword.Text ?? string.Empty;
            var confirm = txtConfirm.Text ?? string.Empty;

            if (string.IsNullOrEmpty(userName))
            {
                lblError.Text = "账号不能为空。";
                return;
            }

            if (!string.IsNullOrEmpty(password) && !string.Equals(password, confirm, StringComparison.Ordinal))
            {
                lblError.Text = "两次输入的密码不一致。";
                return;
            }

            var role = cmbRole.SelectedItem is UserRole selected ? selected : UserRole.操作员;

            if (!_auth.Upsert(userName, displayName, role, password, chkEnabled.Checked, out var error))
            {
                lblError.Text = error;
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
