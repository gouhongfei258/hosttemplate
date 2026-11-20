namespace HostTemplate.App.Forms
{
    partial class ChangePasswordForm
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
            this.lblAccount = new System.Windows.Forms.Label();
            this.lblOld = new System.Windows.Forms.Label();
            this.txtOld = new System.Windows.Forms.TextBox();
            this.lblNew = new System.Windows.Forms.Label();
            this.txtNew = new System.Windows.Forms.TextBox();
            this.lblConfirm = new System.Windows.Forms.Label();
            this.txtConfirm = new System.Windows.Forms.TextBox();
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
            this.lblTitle.Size = new System.Drawing.Size(90, 25);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "修改密码";
            //
            // lblAccount
            //
            this.lblAccount.AutoSize = true;
            this.lblAccount.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblAccount.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
            this.lblAccount.Location = new System.Drawing.Point(27, 48);
            this.lblAccount.Name = "lblAccount";
            this.lblAccount.Size = new System.Drawing.Size(60, 17);
            this.lblAccount.TabIndex = 1;
            this.lblAccount.Text = "账号";
            //
            // lblOld
            //
            this.lblOld.AutoSize = true;
            this.lblOld.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblOld.ForeColor = System.Drawing.Color.FromArgb(55, 65, 81);
            this.lblOld.Location = new System.Drawing.Point(26, 78);
            this.lblOld.Name = "lblOld";
            this.lblOld.Size = new System.Drawing.Size(47, 17);
            this.lblOld.TabIndex = 2;
            this.lblOld.Text = "原密码";
            //
            // txtOld
            //
            this.txtOld.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtOld.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
            this.txtOld.Location = new System.Drawing.Point(26, 98);
            this.txtOld.Name = "txtOld";
            this.txtOld.Size = new System.Drawing.Size(340, 25);
            this.txtOld.TabIndex = 3;
            this.txtOld.UseSystemPasswordChar = true;
            //
            // lblNew
            //
            this.lblNew.AutoSize = true;
            this.lblNew.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblNew.ForeColor = System.Drawing.Color.FromArgb(55, 65, 81);
            this.lblNew.Location = new System.Drawing.Point(26, 132);
            this.lblNew.Name = "lblNew";
            this.lblNew.Size = new System.Drawing.Size(47, 17);
            this.lblNew.TabIndex = 4;
            this.lblNew.Text = "新密码";
            //
            // txtNew
            //
            this.txtNew.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtNew.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
            this.txtNew.Location = new System.Drawing.Point(26, 152);
            this.txtNew.Name = "txtNew";
            this.txtNew.Size = new System.Drawing.Size(340, 25);
            this.txtNew.TabIndex = 5;
            this.txtNew.UseSystemPasswordChar = true;
            //
            // lblConfirm
            //
            this.lblConfirm.AutoSize = true;
            this.lblConfirm.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblConfirm.ForeColor = System.Drawing.Color.FromArgb(55, 65, 81);
            this.lblConfirm.Location = new System.Drawing.Point(26, 186);
            this.lblConfirm.Name = "lblConfirm";
            this.lblConfirm.Size = new System.Drawing.Size(59, 17);
            this.lblConfirm.TabIndex = 6;
            this.lblConfirm.Text = "确认密码";
            //
            // txtConfirm
            //
            this.txtConfirm.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtConfirm.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
            this.txtConfirm.Location = new System.Drawing.Point(26, 206);
            this.txtConfirm.Name = "txtConfirm";
            this.txtConfirm.Size = new System.Drawing.Size(340, 25);
            this.txtConfirm.TabIndex = 7;
            this.txtConfirm.UseSystemPasswordChar = true;
            //
            // lblError
            //
            this.lblError.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblError.ForeColor = System.Drawing.Color.FromArgb(185, 28, 28);
            this.lblError.Location = new System.Drawing.Point(26, 238);
            this.lblError.Name = "lblError";
            this.lblError.Size = new System.Drawing.Size(340, 20);
            this.lblError.TabIndex = 8;
            //
            // btnOk
            //
            this.btnOk.FillColor = System.Drawing.Color.FromArgb(37, 99, 235);
            this.btnOk.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnOk.Location = new System.Drawing.Point(26, 266);
            this.btnOk.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnOk.Name = "btnOk";
            this.btnOk.Radius = 4;
            this.btnOk.RectColor = System.Drawing.Color.FromArgb(37, 99, 235);
            this.btnOk.Size = new System.Drawing.Size(164, 36);
            this.btnOk.TabIndex = 9;
            this.btnOk.Text = "确定";
            this.btnOk.Click += new System.EventHandler(this.BtnOk_Click);
            //
            // btnCancel
            //
            this.btnCancel.FillColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.btnCancel.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
            this.btnCancel.ForeColor = System.Drawing.Color.FromArgb(55, 65, 81);
            this.btnCancel.Location = new System.Drawing.Point(202, 266);
            this.btnCancel.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Radius = 4;
            this.btnCancel.RectColor = System.Drawing.Color.FromArgb(209, 213, 219);
            this.btnCancel.Size = new System.Drawing.Size(164, 36);
            this.btnCancel.TabIndex = 10;
            this.btnCancel.Text = "稍后再说";
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            //
            // ChangePasswordForm
            //
            this.AcceptButton = this.btnOk;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(392, 322);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblAccount);
            this.Controls.Add(this.lblOld);
            this.Controls.Add(this.txtOld);
            this.Controls.Add(this.lblNew);
            this.Controls.Add(this.txtNew);
            this.Controls.Add(this.lblConfirm);
            this.Controls.Add(this.txtConfirm);
            this.Controls.Add(this.lblError);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ChangePasswordForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "修改密码";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblAccount;
        private System.Windows.Forms.Label lblOld;
        private System.Windows.Forms.TextBox txtOld;
        private System.Windows.Forms.Label lblNew;
        private System.Windows.Forms.TextBox txtNew;
        private System.Windows.Forms.Label lblConfirm;
        private System.Windows.Forms.TextBox txtConfirm;
        private System.Windows.Forms.Label lblError;
        private Sunny.UI.UIButton btnOk;
        private Sunny.UI.UIButton btnCancel;
    }
}
