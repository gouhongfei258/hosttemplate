namespace HostTemplate.App.Forms
{
    partial class UserForm
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
            this.panelHeader = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblPath = new System.Windows.Forms.Label();
            this.lblLoginState = new System.Windows.Forms.Label();
            this.panelHeaderLine = new System.Windows.Forms.Panel();
            this.panelTools = new System.Windows.Forms.Panel();
            this.btnAdd = new Sunny.UI.UIButton();
            this.btnEdit = new Sunny.UI.UIButton();
            this.btnDelete = new Sunny.UI.UIButton();
            this.btnRefresh = new Sunny.UI.UIButton();
            this.dgvUsers = new System.Windows.Forms.DataGridView();
            this.panelBottom = new System.Windows.Forms.Panel();
            this.lblHint = new System.Windows.Forms.Label();
            this.btnClose = new Sunny.UI.UIButton();
            this.panelBottomLine = new System.Windows.Forms.Panel();
            this.panelHeader.SuspendLayout();
            this.panelTools.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvUsers)).BeginInit();
            this.panelBottom.SuspendLayout();
            this.SuspendLayout();
            //
            // panelHeader
            //
            this.panelHeader.BackColor = System.Drawing.Color.FromArgb(249, 250, 251);
            this.panelHeader.Controls.Add(this.lblTitle);
            this.panelHeader.Controls.Add(this.lblPath);
            this.panelHeader.Controls.Add(this.lblLoginState);
            this.panelHeader.Controls.Add(this.panelHeaderLine);
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Location = new System.Drawing.Point(0, 0);
            this.panelHeader.Name = "panelHeader";
            this.panelHeader.Size = new System.Drawing.Size(940, 66);
            this.panelHeader.TabIndex = 3;
            //
            // lblTitle
            //
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Microsoft YaHei UI", 15F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(17, 24, 39);
            this.lblTitle.Location = new System.Drawing.Point(20, 8);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(110, 27);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "账号管理";
            //
            // lblPath
            //
            this.lblPath.AutoEllipsis = true;
            this.lblPath.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblPath.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
            this.lblPath.Location = new System.Drawing.Point(22, 38);
            this.lblPath.Name = "lblPath";
            this.lblPath.Size = new System.Drawing.Size(560, 17);
            this.lblPath.TabIndex = 1;
            this.lblPath.Text = "users.xml";
            //
            // lblLoginState
            //
            this.lblLoginState.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblLoginState.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblLoginState.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
            this.lblLoginState.Location = new System.Drawing.Point(600, 22);
            this.lblLoginState.Name = "lblLoginState";
            this.lblLoginState.Size = new System.Drawing.Size(318, 22);
            this.lblLoginState.TabIndex = 2;
            this.lblLoginState.Text = "登录功能：未启用";
            this.lblLoginState.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // panelHeaderLine
            //
            this.panelHeaderLine.BackColor = System.Drawing.Color.FromArgb(229, 231, 235);
            this.panelHeaderLine.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelHeaderLine.Location = new System.Drawing.Point(0, 65);
            this.panelHeaderLine.Name = "panelHeaderLine";
            this.panelHeaderLine.Size = new System.Drawing.Size(940, 1);
            this.panelHeaderLine.TabIndex = 3;
            //
            // panelTools
            //
            this.panelTools.Controls.Add(this.btnAdd);
            this.panelTools.Controls.Add(this.btnEdit);
            this.panelTools.Controls.Add(this.btnDelete);
            this.panelTools.Controls.Add(this.btnRefresh);
            this.panelTools.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTools.Location = new System.Drawing.Point(0, 66);
            this.panelTools.Name = "panelTools";
            this.panelTools.Size = new System.Drawing.Size(940, 52);
            this.panelTools.TabIndex = 2;
            //
            // btnAdd
            //
            this.btnAdd.FillColor = System.Drawing.Color.FromArgb(37, 99, 235);
            this.btnAdd.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.btnAdd.Location = new System.Drawing.Point(20, 8);
            this.btnAdd.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Radius = 4;
            this.btnAdd.RectColor = System.Drawing.Color.FromArgb(37, 99, 235);
            this.btnAdd.Size = new System.Drawing.Size(96, 34);
            this.btnAdd.TabIndex = 0;
            this.btnAdd.Text = "新增账号";
            this.btnAdd.Click += new System.EventHandler(this.BtnAdd_Click);
            //
            // btnEdit
            //
            this.btnEdit.FillColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.btnEdit.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.btnEdit.ForeColor = System.Drawing.Color.FromArgb(55, 65, 81);
            this.btnEdit.Location = new System.Drawing.Point(124, 8);
            this.btnEdit.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnEdit.Name = "btnEdit";
            this.btnEdit.Radius = 4;
            this.btnEdit.RectColor = System.Drawing.Color.FromArgb(209, 213, 219);
            this.btnEdit.Size = new System.Drawing.Size(96, 34);
            this.btnEdit.TabIndex = 1;
            this.btnEdit.Text = "编辑";
            this.btnEdit.Click += new System.EventHandler(this.BtnEdit_Click);
            //
            // btnDelete
            //
            this.btnDelete.FillColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.btnDelete.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.btnDelete.ForeColor = System.Drawing.Color.FromArgb(185, 28, 28);
            this.btnDelete.Location = new System.Drawing.Point(228, 8);
            this.btnDelete.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Radius = 4;
            this.btnDelete.RectColor = System.Drawing.Color.FromArgb(209, 213, 219);
            this.btnDelete.Size = new System.Drawing.Size(96, 34);
            this.btnDelete.TabIndex = 2;
            this.btnDelete.Text = "删除";
            this.btnDelete.Click += new System.EventHandler(this.BtnDelete_Click);
            //
            // btnRefresh
            //
            this.btnRefresh.FillColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.btnRefresh.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.btnRefresh.ForeColor = System.Drawing.Color.FromArgb(55, 65, 81);
            this.btnRefresh.Location = new System.Drawing.Point(332, 8);
            this.btnRefresh.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Radius = 4;
            this.btnRefresh.RectColor = System.Drawing.Color.FromArgb(209, 213, 219);
            this.btnRefresh.Size = new System.Drawing.Size(96, 34);
            this.btnRefresh.TabIndex = 3;
            this.btnRefresh.Text = "刷新";
            this.btnRefresh.Click += new System.EventHandler(this.BtnRefresh_Click);
            //
            // dgvUsers
            //
            this.dgvUsers.AllowUserToAddRows = false;
            this.dgvUsers.AllowUserToDeleteRows = false;
            this.dgvUsers.AllowUserToResizeRows = false;
            this.dgvUsers.BackgroundColor = System.Drawing.Color.White;
            this.dgvUsers.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvUsers.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvUsers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvUsers.Location = new System.Drawing.Point(0, 118);
            this.dgvUsers.MultiSelect = false;
            this.dgvUsers.Name = "dgvUsers";
            this.dgvUsers.ReadOnly = true;
            this.dgvUsers.RowHeadersVisible = false;
            this.dgvUsers.RowTemplate.Height = 32;
            this.dgvUsers.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvUsers.Size = new System.Drawing.Size(940, 380);
            this.dgvUsers.TabIndex = 1;
            this.dgvUsers.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DgvUsers_CellDoubleClick);
            //
            // panelBottom
            //
            this.panelBottom.BackColor = System.Drawing.Color.FromArgb(249, 250, 251);
            this.panelBottom.Controls.Add(this.lblHint);
            this.panelBottom.Controls.Add(this.btnClose);
            this.panelBottom.Controls.Add(this.panelBottomLine);
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Location = new System.Drawing.Point(0, 498);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Size = new System.Drawing.Size(940, 62);
            this.panelBottom.TabIndex = 0;
            //
            // lblHint
            //
            this.lblHint.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.lblHint.Font = new System.Drawing.Font("Microsoft YaHei UI", 8.5F);
            this.lblHint.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
            this.lblHint.Location = new System.Drawing.Point(20, 14);
            this.lblHint.Name = "lblHint";
            this.lblHint.Size = new System.Drawing.Size(770, 36);
            this.lblHint.TabIndex = 0;
            this.lblHint.Text = "密码以 PBKDF2 加盐保存，无法查看，只能重置。";
            //
            // btnClose
            //
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.FillColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.btnClose.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.btnClose.ForeColor = System.Drawing.Color.FromArgb(55, 65, 81);
            this.btnClose.Location = new System.Drawing.Point(804, 13);
            this.btnClose.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnClose.Name = "btnClose";
            this.btnClose.Radius = 4;
            this.btnClose.RectColor = System.Drawing.Color.FromArgb(209, 213, 219);
            this.btnClose.Size = new System.Drawing.Size(114, 36);
            this.btnClose.TabIndex = 1;
            this.btnClose.Text = "关闭";
            this.btnClose.Click += new System.EventHandler(this.BtnClose_Click);
            //
            // panelBottomLine
            //
            this.panelBottomLine.BackColor = System.Drawing.Color.FromArgb(229, 231, 235);
            this.panelBottomLine.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelBottomLine.Location = new System.Drawing.Point(0, 0);
            this.panelBottomLine.Name = "panelBottomLine";
            this.panelBottomLine.Size = new System.Drawing.Size(940, 1);
            this.panelBottomLine.TabIndex = 2;
            //
            // UserForm
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(940, 560);
            this.Controls.Add(this.dgvUsers);
            this.Controls.Add(this.panelTools);
            this.Controls.Add(this.panelBottom);
            this.Controls.Add(this.panelHeader);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.MinimumSize = new System.Drawing.Size(820, 480);
            this.Name = "UserForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "账号管理";
            this.panelHeader.ResumeLayout(false);
            this.panelHeader.PerformLayout();
            this.panelTools.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvUsers)).EndInit();
            this.panelBottom.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblPath;
        private System.Windows.Forms.Label lblLoginState;
        private System.Windows.Forms.Panel panelHeaderLine;
        private System.Windows.Forms.Panel panelTools;
        private Sunny.UI.UIButton btnAdd;
        private Sunny.UI.UIButton btnEdit;
        private Sunny.UI.UIButton btnDelete;
        private Sunny.UI.UIButton btnRefresh;
        private System.Windows.Forms.DataGridView dgvUsers;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.Label lblHint;
        private Sunny.UI.UIButton btnClose;
        private System.Windows.Forms.Panel panelBottomLine;
    }
}
