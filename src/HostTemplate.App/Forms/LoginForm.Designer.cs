namespace HostTemplate.App.Forms
{
    partial class LoginForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblStation = new System.Windows.Forms.Label();
            this.lblUser = new System.Windows.Forms.Label();
            this.txtUser = new System.Windows.Forms.TextBox();
            this.lblPwd = new System.Windows.Forms.Label();
            this.txtPwd = new System.Windows.Forms.TextBox();
            this.lblError = new System.Windows.Forms.Label();
            this.btnLogin = new Sunny.UI.UIButton();
            this.btnCancel = new Sunny.UI.UIButton();
            this.lblHint = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // lblTitle
            //
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Microsoft YaHei UI", 17F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(17, 24, 39);
            this.lblTitle.Location = new System.Drawing.Point(28, 22);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(110, 30);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "工站登录";
            //
            // lblStation
            //
            this.lblStation.AutoSize = true;
            this.lblStation.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblStation.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
            this.lblStation.Location = new System.Drawing.Point(31, 56);
            this.lblStation.Name = "lblStation";
            this.lblStation.Size = new System.Drawing.Size(120, 17);
            this.lblStation.TabIndex = 1;
            this.lblStation.Text = "工站";
            //
            // lblUser
            //
            this.lblUser.AutoSize = true;
            this.lblUser.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblUser.ForeColor = System.Drawing.Color.FromArgb(55, 65, 81);
            this.lblUser.Location = new System.Drawing.Point(30, 92);
            this.lblUser.Name = "lblUser";
            this.lblUser.Size = new System.Drawing.Size(35, 17);
            this.lblUser.TabIndex = 2;
            this.lblUser.Text = "账号";
            //
            // txtUser
            //
            this.txtUser.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtUser.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F);
            this.txtUser.Location = new System.Drawing.Point(30, 113);
            this.txtUser.Name = "txtUser";
            this.txtUser.Size = new System.Drawing.Size(348, 26);
            this.txtUser.TabIndex = 3;
            //
            // lblPwd
            //
            this.lblPwd.AutoSize = true;
            this.lblPwd.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblPwd.ForeColor = System.Drawing.Color.FromArgb(55, 65, 81);
            this.lblPwd.Location = new System.Drawing.Point(30, 152);
            this.lblPwd.Name = "lblPwd";
            this.lblPwd.Size = new System.Drawing.Size(35, 17);
            this.lblPwd.TabIndex = 4;
            this.lblPwd.Text = "密码";
            //
            // txtPwd
            //
            this.txtPwd.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtPwd.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F);
            this.txtPwd.Location = new System.Drawing.Point(30, 173);
            this.txtPwd.Name = "txtPwd";
            this.txtPwd.Size = new System.Drawing.Size(348, 26);
            this.txtPwd.TabIndex = 5;
            this.txtPwd.UseSystemPasswordChar = true;
            //
            // lblError
            //
            this.lblError.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblError.ForeColor = System.Drawing.Color.FromArgb(185, 28, 28);
            this.lblError.Location = new System.Drawing.Point(30, 205);
            this.lblError.Name = "lblError";
            this.lblError.Size = new System.Drawing.Size(348, 20);
            this.lblError.TabIndex = 6;
            //
            // btnLogin
            //
            this.btnLogin.FillColor = System.Drawing.Color.FromArgb(37, 99, 235);
            this.btnLogin.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnLogin.Location = new System.Drawing.Point(30, 232);
            this.btnLogin.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnLogin.Name = "btnLogin";
            this.btnLogin.Radius = 4;
            this.btnLogin.RectColor = System.Drawing.Color.FromArgb(37, 99, 235);
            this.btnLogin.Size = new System.Drawing.Size(168, 38);
            this.btnLogin.TabIndex = 7;
            this.btnLogin.Text = "登录";
            this.btnLogin.Click += new System.EventHandler(this.BtnLogin_Click);
            //
            // btnCancel
            //
            this.btnCancel.FillColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.btnCancel.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
            this.btnCancel.ForeColor = System.Drawing.Color.FromArgb(55, 65, 81);
            this.btnCancel.Location = new System.Drawing.Point(210, 232);
            this.btnCancel.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Radius = 4;
            this.btnCancel.RectColor = System.Drawing.Color.FromArgb(209, 213, 219);
            this.btnCancel.Size = new System.Drawing.Size(168, 38);
            this.btnCancel.TabIndex = 8;
            this.btnCancel.Text = "退出";
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            //
            // lblHint
            //
            this.lblHint.Font = new System.Drawing.Font("Microsoft YaHei UI", 8.5F);
            this.lblHint.ForeColor = System.Drawing.Color.FromArgb(156, 163, 175);
            this.lblHint.Location = new System.Drawing.Point(30, 280);
            this.lblHint.Name = "lblHint";
            this.lblHint.Size = new System.Drawing.Size(348, 34);
            this.lblHint.TabIndex = 9;
            this.lblHint.Text = "默认账号 admin / admin123，首次登录后请立即修改。";
            //
            // LoginForm
            //
            this.AcceptButton = this.btnLogin;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(408, 326);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblStation);
            this.Controls.Add(this.lblUser);
            this.Controls.Add(this.txtUser);
            this.Controls.Add(this.lblPwd);
            this.Controls.Add(this.txtPwd);
            this.Controls.Add(this.lblError);
            this.Controls.Add(this.btnLogin);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.lblHint);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LoginForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "登录";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblStation;
        private System.Windows.Forms.Label lblUser;
        private System.Windows.Forms.TextBox txtUser;
        private System.Windows.Forms.Label lblPwd;
        private System.Windows.Forms.TextBox txtPwd;
        private System.Windows.Forms.Label lblError;
        private Sunny.UI.UIButton btnLogin;
        private Sunny.UI.UIButton btnCancel;
        private System.Windows.Forms.Label lblHint;
    }
}
