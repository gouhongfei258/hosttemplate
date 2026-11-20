using System;
using System.Windows.Forms;
using HostTemplate.Core.Security;

namespace HostTemplate.App.Forms
{
    /// <summary>
    /// 登录窗。默认不启用（AppConfig.EnableLogin = false），
    /// 需要时在 Program 中打开开关即可，无需改业务代码。
    /// </summary>
    public partial class LoginForm : Form
    {
        private readonly AuthService _auth;
        private int _failCount;

        public LoginForm(AuthService auth, string stationName)
        {
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));

            InitializeComponent();

            lblStation.Text = string.IsNullOrEmpty(stationName) ? "上位机" : stationName;
            txtUser.Text = AuthService.DefaultAdminName;
            txtUser.SelectAll();
        }

        /// <summary>登录成功后的会话。</summary>
        public UserSession Session { get; private set; }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            var userName = txtUser.Text?.Trim() ?? string.Empty;
            var password = txtPwd.Text ?? string.Empty;

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
            {
                lblError.Text = "请输入账号和密码。";
                return;
            }

            if (!_auth.Verify(userName, password, out var session, out var error))
            {
                _failCount++;
                lblError.Text = error + (_failCount >= 3 ? $"（连续失败 {_failCount} 次）" : string.Empty);
                txtPwd.Clear();
                txtPwd.Focus();
                return;
            }

            Session = session;

            var account = _auth.Find(userName);
            if (account != null && account.MustChangePassword)
            {
                using (var change = new ChangePasswordForm(_auth, account, true))
                {
                    change.ShowDialog(this);
                }
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
