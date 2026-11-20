using System;
using System.Windows.Forms;
using HostTemplate.Core.Security;

namespace HostTemplate.App.Forms
{
    /// <summary>
    /// 修改密码。默认密码登录后强制弹出（可「稍后再说」），也可从权限页手动调用。
    /// </summary>
    public partial class ChangePasswordForm : Form
    {
        private readonly AuthService _auth;
        private readonly UserAccount _account;

        public ChangePasswordForm(AuthService auth, UserAccount account, bool forced = false)
        {
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
            _account = account ?? throw new ArgumentNullException(nameof(account));

            InitializeComponent();

            lblAccount.Text = $"账号：{account.UserName}（{account.Role}）";
            Text = forced ? "首次登录 · 请修改密码" : "修改密码";
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            var oldPassword = txtOld.Text ?? string.Empty;
            var newPassword = txtNew.Text ?? string.Empty;
            var confirm = txtConfirm.Text ?? string.Empty;

            if (!string.Equals(newPassword, confirm, StringComparison.Ordinal))
            {
                lblError.Text = "两次输入的新密码不一致。";
                return;
            }

            if (!AuthService.IsAcceptable(newPassword, out var error))
            {
                lblError.Text = error;
                return;
            }

            if (string.Equals(oldPassword, newPassword, StringComparison.Ordinal))
            {
                lblError.Text = "新密码不能与原密码相同。";
                return;
            }

            if (!_auth.ChangePassword(_account.UserName, oldPassword, newPassword, out error))
            {
                lblError.Text = error;
                return;
            }

            MessageBox.Show(this, "密码已修改，请牢记新密码。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
