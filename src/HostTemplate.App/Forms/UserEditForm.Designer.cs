namespace HostTemplate.App.Forms
{
    partial class UserEditForm
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
            this.lblUserName = new System.Windows.Forms.Label();
            this.txtUserName = new System.Windows.Forms.TextBox();
            this.lblDisplayName = new System.Windows.Forms.Label();
            this.txtDisplayName = new System.Windows.Forms.TextBox();
            this.lblRole = new System.Windows.Forms.Label();
            this.cmbRole = new System.Windows.Forms.ComboBox();
            this.lblPassword = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.lblConfirm = new System.Windows.Forms.Label();
            this.txtConfirm = new System.Windows.Forms.TextBox();
            this.chkEnabled = new System.Windows.Forms.CheckBox();
            this.lblError = new System.Windows.Forms.Label();
            this.btnOk = new Sunny.UI.UIButton();
            this.btnCancel = new Sunny.UI.UIButton();
            this.SuspendLayout();
            //
            // lblTitle
            //
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Microsoft YaHei UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(17, 24, 39);
            this.lblTitle.Location = new System.Drawing.Point(24, 18);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(100, 25);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "新增账号";
            //
            // lblUserName
            //
            this.lblUserName.AutoSize = true;
            this.lblUserName.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblUserName.ForeColor = System.Drawing.Color.FromArgb(55, 65, 81);
            this.lblUserName.Location = new System.Drawing.Point(26, 58);
            this.lblUserName.Name = "lblUserName";
            this.lblUserName.Size = new System.Drawing.Size(35, 17);
            this.lblUserName.TabIndex = 1;
            this.lblUserName.Text = "账号";
            //
            // txtUserName
            //
            this.txtUserName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtUserName.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
            this.txtUserName.Location = new System.Drawing.Point(26, 78);
            this.txtUserName.Name = "txtUserName";
            this.txtUserName.Size = new System.Drawing.Size(200, 25);
            this.txtUserName.TabIndex = 2;
            //
            // lblDisplayName
            //
            this.lblDisplayName.AutoSize = true;
            this.lblDisplayName.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblDisplayName.ForeColor = System.Drawing.Color.FromArgb(55, 65, 81);
            this.lblDisplayName.Location = new System.Drawing.Point(248, 58);
            this.lblDisplayName.Name = "lblDisplayName";
            this.lblDisplayName.Size = new System.Drawing.Size(35, 17);
            this.lblDisplayName.TabIndex = 3;
            this.lblDisplayName.Text = "姓名";
            //
            // txtDisplayName
            //
            this.txtDisplayName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtDisplayName.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
            this.txtDisplayName.Location = new System.Drawing.Point(248, 78);
            this.txtDisplayName.Name = "txtDisplayName";
            this.txtDisplayName.Size = new System.Drawing.Size(200, 25);
            this.txtDisplayName.TabIndex = 4;
            //
            // lblRole
            //
            this.lblRole.AutoSize = true;
            this.lblRole.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblRole.ForeColor = System.Drawing.Color.FromArgb(55, 65, 81);
            this.lblRole.Location = new System.Drawing.Point(26, 116);
            this.lblRole.Name = "lblRole";
            this.lblRole.Size = new System.Drawing.Size(35, 17);
            this.lblRole.TabIndex = 5;
            this.lblRole.Text = "角色";
            //
            // cmbRole
            //
            this.cmbRole.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbRole.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbRole.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
            this.cmbRole.FormattingEnabled = true;
            this.cmbRole.Location = new System.Drawing.Point(26, 136);
            this.cmbRole.Name = "cmbRole";
            this.cmbRole.Size = new System.Drawing.Size(200, 25);
            this.cmbRole.TabIndex = 6;
            //
            // lblPassword
            //
            this.lblPassword.AutoSize = true;
            this.lblPassword.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblPassword.ForeColor = System.Drawing.Color.FromArgb(55, 65, 81);
            this.lblPassword.Location = new System.Drawing.Point(26, 174);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(35, 17);
            this.lblPassword.TabIndex = 7;
            this.lblPassword.Text = "密码";
            //
            // txtPassword
            //
            this.txtPassword.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtPassword.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
            this.txtPassword.Location = new System.Drawing.Point(26, 194);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(200, 25);
            this.txtPassword.TabIndex = 8;
            this.txtPassword.UseSystemPasswordChar = true;
            //
            // lblConfirm
            //
            this.lblConfirm.AutoSize = true;
            this.lblConfirm.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblConfirm.ForeColor = System.Drawing.Color.FromArgb(55, 65, 81);
            this.lblConfirm.Location = new System.Drawing.Point(248, 174);
            this.lblConfirm.Name = "lblConfirm";
            this.lblConfirm.Size = new System.Drawing.Size(59, 17);
            this.lblConfirm.TabIndex = 9;
            this.lblConfirm.Text = "确认密码";
            //
            // txtConfirm
            //
            this.txtConfirm.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtConfirm.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
            this.txtConfirm.Location = new System.Drawing.Point(248, 194);
            this.txtConfirm.Name = "txtConfirm";
            this.txtConfirm.Size = new System.Drawing.Size(200, 25);
            this.txtConfirm.TabIndex = 10;
            this.txtConfirm.UseSystemPasswordChar = true;
            //
            // chkEnabled
            //
            this.chkEnabled.AutoSize = true;
            this.chkEnabled.Checked = true;
            this.chkEnabled.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkEnabled.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.chkEnabled.ForeColor = System.Drawing.Color.FromArgb(55, 65, 81);
            this.chkEnabled.Location = new System.Drawing.Point(28, 240);
            this.chkEnabled.Name = "chkEnabled";
            this.chkEnabled.Size = new System.Drawing.Size(75, 21);
            this.chkEnabled.TabIndex = 11;
            this.chkEnabled.Text = "启用账号";
            this.chkEnabled.UseVisualStyleBackColor = true;
            //
            // lblError
            //
            this.lblError.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblError.ForeColor = System.Drawing.Color.FromArgb(185, 28, 28);
            this.lblError.Location = new System.Drawing.Point(26, 268);
            this.lblError.Name = "lblError";
            this.lblError.Size = new System.Drawing.Size(422, 20);
            this.lblError.TabIndex = 12;
            //
            // btnOk
            //
            this.btnOk.FillColor = System.Drawing.Color.FromArgb(37, 99, 235);
            this.btnOk.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnOk.Location = new System.Drawing.Point(26, 296);
            this.btnOk.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnOk.Name = "btnOk";
            this.btnOk.Radius = 4;
            this.btnOk.RectColor = System.Drawing.Color.FromArgb(37, 99, 235);
            this.btnOk.Size = new System.Drawing.Size(200, 38);
            this.btnOk.TabIndex = 13;
            this.btnOk.Text = "保存";
            this.btnOk.Click += new System.EventHandler(this.BtnOk_Click);
            //
            // btnCancel
            //
            this.btnCancel.FillColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.btnCancel.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
            this.btnCancel.ForeColor = System.Drawing.Color.FromArgb(55, 65, 81);
            this.btnCancel.Location = new System.Drawing.Point(248, 296);
            this.btnCancel.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Radius = 4;
            this.btnCancel.RectColor = System.Drawing.Color.FromArgb(209, 213, 219);
            this.btnCancel.Size = new System.Drawing.Size(200, 38);
            this.btnCancel.TabIndex = 14;
            this.btnCancel.Text = "取消";
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            //
            // UserEditForm
            //
            this.AcceptButton = this.btnOk;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(474, 354);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblUserName);
            this.Controls.Add(this.txtUserName);
            this.Controls.Add(this.lblDisplayName);
            this.Controls.Add(this.txtDisplayName);
            this.Controls.Add(this.lblRole);
            this.Controls.Add(this.cmbRole);
            this.Controls.Add(this.lblPassword);
            this.Controls.Add(this.txtPassword);
            this.Controls.Add(this.lblConfirm);
            this.Controls.Add(this.txtConfirm);
            this.Controls.Add(this.chkEnabled);
            this.Controls.Add(this.lblError);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UserEditForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "账号";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblUserName;
        private System.Windows.Forms.TextBox txtUserName;
        private System.Windows.Forms.Label lblDisplayName;
        private System.Windows.Forms.TextBox txtDisplayName;
        private System.Windows.Forms.Label lblRole;
        private System.Windows.Forms.ComboBox cmbRole;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label lblConfirm;
        private System.Windows.Forms.TextBox txtConfirm;
        private System.Windows.Forms.CheckBox chkEnabled;
        private System.Windows.Forms.Label lblError;
        private Sunny.UI.UIButton btnOk;
        private Sunny.UI.UIButton btnCancel;
    }
}
