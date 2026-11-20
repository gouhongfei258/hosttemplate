namespace HostTemplate.App.Forms
{
    partial class ConfigForm
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
            this.btnOpenDir = new Sunny.UI.UIButton();
            this.panelHeaderLine = new System.Windows.Forms.Panel();
            this.panelNav = new System.Windows.Forms.Panel();
            this.btnNavApp = new System.Windows.Forms.Button();
            this.btnNavPlc = new System.Windows.Forms.Button();
            this.btnNavAddr = new System.Windows.Forms.Button();
            this.btnNavApi = new System.Windows.Forms.Button();
            this.lblNavTip = new System.Windows.Forms.Label();
            this.panelBody = new System.Windows.Forms.Panel();
            this.pageApp = new System.Windows.Forms.Panel();
            this.pgApp = new System.Windows.Forms.PropertyGrid();
            this.pagePlc = new System.Windows.Forms.Panel();
            this.pageAddr = new System.Windows.Forms.Panel();
            this.dgvAddr = new System.Windows.Forms.DataGridView();
            this.panelAddrTools = new System.Windows.Forms.Panel();
            this.btnAddrAdd = new Sunny.UI.UIButton();
            this.btnAddrDel = new Sunny.UI.UIButton();
            this.btnAddrCopy = new Sunny.UI.UIButton();
            this.btnAddrUp = new Sunny.UI.UIButton();
            this.btnAddrDown = new Sunny.UI.UIButton();
            this.lblAddrHint = new System.Windows.Forms.Label();
            this.pageApi = new System.Windows.Forms.Panel();
            this.tabApi = new System.Windows.Forms.TabControl();
            this.tabPageEndpoints = new System.Windows.Forms.TabPage();
            this.dgvEndpoints = new System.Windows.Forms.DataGridView();
            this.tabPageHeaders = new System.Windows.Forms.TabPage();
            this.dgvHeaders = new System.Windows.Forms.DataGridView();
            this.pgApi = new System.Windows.Forms.PropertyGrid();
            this.panelBottom = new System.Windows.Forms.Panel();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnDefault = new Sunny.UI.UIButton();
            this.btnCancel = new Sunny.UI.UIButton();
            this.btnSave = new Sunny.UI.UIButton();
            this.panelBottomLine = new System.Windows.Forms.Panel();
            this.panelHeader.SuspendLayout();
            this.panelNav.SuspendLayout();
            this.panelBody.SuspendLayout();
            this.pageApp.SuspendLayout();
            this.pagePlc.SuspendLayout();
            this.pageAddr.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAddr)).BeginInit();
            this.panelAddrTools.SuspendLayout();
            this.pageApi.SuspendLayout();
            this.tabApi.SuspendLayout();
            this.tabPageEndpoints.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvEndpoints)).BeginInit();
            this.tabPageHeaders.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHeaders)).BeginInit();
            this.panelBottom.SuspendLayout();
            this.SuspendLayout();
            //
            // panelHeader
            //
            this.panelHeader.BackColor = System.Drawing.Color.FromArgb(249, 250, 251);
            this.panelHeader.Controls.Add(this.lblTitle);
            this.panelHeader.Controls.Add(this.lblPath);
            this.panelHeader.Controls.Add(this.btnOpenDir);
            this.panelHeader.Controls.Add(this.panelHeaderLine);
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Location = new System.Drawing.Point(0, 0);
            this.panelHeader.Name = "panelHeader";
            this.panelHeader.Size = new System.Drawing.Size(1080, 66);
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
            this.lblTitle.Text = "系统配置";
            //
            // lblPath
            //
            this.lblPath.AutoSize = true;
            this.lblPath.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblPath.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
            this.lblPath.Location = new System.Drawing.Point(22, 38);
            this.lblPath.Name = "lblPath";
            this.lblPath.Size = new System.Drawing.Size(300, 17);
            this.lblPath.TabIndex = 1;
            this.lblPath.Text = "config";
            //
            // btnOpenDir
            //
            this.btnOpenDir.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOpenDir.FillColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.btnOpenDir.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.btnOpenDir.ForeColor = System.Drawing.Color.FromArgb(55, 65, 81);
            this.btnOpenDir.Location = new System.Drawing.Point(926, 17);
            this.btnOpenDir.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnOpenDir.Name = "btnOpenDir";
            this.btnOpenDir.Radius = 4;
            this.btnOpenDir.RectColor = System.Drawing.Color.FromArgb(209, 213, 219);
            this.btnOpenDir.Size = new System.Drawing.Size(132, 34);
            this.btnOpenDir.TabIndex = 2;
            this.btnOpenDir.Text = "打开配置目录";
            this.btnOpenDir.Click += new System.EventHandler(this.BtnOpenDir_Click);
            //
            // panelHeaderLine
            //
            this.panelHeaderLine.BackColor = System.Drawing.Color.FromArgb(229, 231, 235);
            this.panelHeaderLine.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelHeaderLine.Location = new System.Drawing.Point(0, 65);
            this.panelHeaderLine.Name = "panelHeaderLine";
            this.panelHeaderLine.Size = new System.Drawing.Size(1080, 1);
            this.panelHeaderLine.TabIndex = 3;
            //
            // panelNav
            //
            this.panelNav.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.panelNav.Controls.Add(this.lblNavTip);
            this.panelNav.Controls.Add(this.btnNavApi);
            this.panelNav.Controls.Add(this.btnNavAddr);
            this.panelNav.Controls.Add(this.btnNavPlc);
            this.panelNav.Controls.Add(this.btnNavApp);
            this.panelNav.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelNav.Location = new System.Drawing.Point(0, 66);
            this.panelNav.Name = "panelNav";
            this.panelNav.Size = new System.Drawing.Size(180, 612);
            this.panelNav.TabIndex = 2;
            //
            // btnNavApp
            //
            this.btnNavApp.BackColor = System.Drawing.Color.White;
            this.btnNavApp.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnNavApp.FlatAppearance.BorderSize = 0;
            this.btnNavApp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNavApp.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnNavApp.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.btnNavApp.Location = new System.Drawing.Point(0, 0);
            this.btnNavApp.Name = "btnNavApp";
            this.btnNavApp.Padding = new System.Windows.Forms.Padding(18, 0, 0, 0);
            this.btnNavApp.Size = new System.Drawing.Size(180, 46);
            this.btnNavApp.TabIndex = 0;
            this.btnNavApp.Text = "基础参数";
            this.btnNavApp.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnNavApp.UseVisualStyleBackColor = false;
            this.btnNavApp.Click += new System.EventHandler(this.BtnNav_Click);
            //
            // btnNavPlc
            //
            this.btnNavPlc.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.btnNavPlc.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnNavPlc.FlatAppearance.BorderSize = 0;
            this.btnNavPlc.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNavPlc.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
            this.btnNavPlc.ForeColor = System.Drawing.Color.FromArgb(75, 85, 99);
            this.btnNavPlc.Location = new System.Drawing.Point(0, 46);
            this.btnNavPlc.Name = "btnNavPlc";
            this.btnNavPlc.Padding = new System.Windows.Forms.Padding(18, 0, 0, 0);
            this.btnNavPlc.Size = new System.Drawing.Size(180, 46);
            this.btnNavPlc.TabIndex = 1;
            this.btnNavPlc.Text = "PLC 连接";
            this.btnNavPlc.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnNavPlc.UseVisualStyleBackColor = false;
            this.btnNavPlc.Click += new System.EventHandler(this.BtnNav_Click);
            //
            // btnNavAddr
            //
            this.btnNavAddr.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.btnNavAddr.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnNavAddr.FlatAppearance.BorderSize = 0;
            this.btnNavAddr.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNavAddr.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
            this.btnNavAddr.ForeColor = System.Drawing.Color.FromArgb(75, 85, 99);
            this.btnNavAddr.Location = new System.Drawing.Point(0, 92);
            this.btnNavAddr.Name = "btnNavAddr";
            this.btnNavAddr.Padding = new System.Windows.Forms.Padding(18, 0, 0, 0);
            this.btnNavAddr.Size = new System.Drawing.Size(180, 46);
            this.btnNavAddr.TabIndex = 2;
            this.btnNavAddr.Text = "PLC 地址表";
            this.btnNavAddr.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnNavAddr.UseVisualStyleBackColor = false;
            this.btnNavAddr.Click += new System.EventHandler(this.BtnNav_Click);
            //
            // btnNavApi
            //
            this.btnNavApi.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.btnNavApi.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnNavApi.FlatAppearance.BorderSize = 0;
            this.btnNavApi.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNavApi.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
            this.btnNavApi.ForeColor = System.Drawing.Color.FromArgb(75, 85, 99);
            this.btnNavApi.Location = new System.Drawing.Point(0, 138);
            this.btnNavApi.Name = "btnNavApi";
            this.btnNavApi.Padding = new System.Windows.Forms.Padding(18, 0, 0, 0);
            this.btnNavApi.Size = new System.Drawing.Size(180, 46);
            this.btnNavApi.TabIndex = 3;
            this.btnNavApi.Text = "MES 接口";
            this.btnNavApi.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnNavApi.UseVisualStyleBackColor = false;
            this.btnNavApi.Click += new System.EventHandler(this.BtnNav_Click);
            //
            // lblNavTip
            //
            this.lblNavTip.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblNavTip.Font = new System.Drawing.Font("Microsoft YaHei UI", 8.5F);
            this.lblNavTip.ForeColor = System.Drawing.Color.FromArgb(156, 163, 175);
            this.lblNavTip.Location = new System.Drawing.Point(18, 552);
            this.lblNavTip.Name = "lblNavTip";
            this.lblNavTip.Size = new System.Drawing.Size(146, 48);
            this.lblNavTip.TabIndex = 4;
            this.lblNavTip.Text = "保存后立即生效\r\n无需重启程序";
            //
            // panelBody
            //
            this.panelBody.BackColor = System.Drawing.Color.White;
            this.panelBody.Controls.Add(this.pageApp);
            this.panelBody.Controls.Add(this.pagePlc);
            this.panelBody.Controls.Add(this.pageAddr);
            this.panelBody.Controls.Add(this.pageApi);
            this.panelBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelBody.Location = new System.Drawing.Point(180, 66);
            this.panelBody.Name = "panelBody";
            this.panelBody.Padding = new System.Windows.Forms.Padding(14);
            this.panelBody.Size = new System.Drawing.Size(900, 612);
            this.panelBody.TabIndex = 0;
            //
            // pageApp
            //
            this.pageApp.Controls.Add(this.pgApp);
            this.pageApp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pageApp.Location = new System.Drawing.Point(14, 14);
            this.pageApp.Name = "pageApp";
            this.pageApp.Size = new System.Drawing.Size(872, 584);
            this.pageApp.TabIndex = 0;
            //
            // pgApp
            //
            this.pgApp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pgApp.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.pgApp.HelpVisible = true;
            this.pgApp.LineColor = System.Drawing.SystemColors.ControlDark;
            this.pgApp.Location = new System.Drawing.Point(0, 0);
            this.pgApp.Name = "pgApp";
            this.pgApp.PropertySort = System.Windows.Forms.PropertySort.Categorized;
            this.pgApp.Size = new System.Drawing.Size(872, 584);
            this.pgApp.TabIndex = 0;
            this.pgApp.ToolbarVisible = false;
            //
            // pagePlc
            //
            this.pagePlc.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pagePlc.Location = new System.Drawing.Point(14, 14);
            this.pagePlc.Name = "pagePlc";
            this.pagePlc.Size = new System.Drawing.Size(872, 584);
            this.pagePlc.TabIndex = 1;
            this.pagePlc.Visible = false;
            //
            // pageAddr
            //
            this.pageAddr.Controls.Add(this.dgvAddr);
            this.pageAddr.Controls.Add(this.panelAddrTools);
            this.pageAddr.Controls.Add(this.lblAddrHint);
            this.pageAddr.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pageAddr.Location = new System.Drawing.Point(14, 14);
            this.pageAddr.Name = "pageAddr";
            this.pageAddr.Size = new System.Drawing.Size(872, 584);
            this.pageAddr.TabIndex = 2;
            this.pageAddr.Visible = false;
            //
            // dgvAddr
            //
            this.dgvAddr.AllowUserToAddRows = false;
            this.dgvAddr.AllowUserToDeleteRows = false;
            this.dgvAddr.AllowUserToResizeRows = false;
            this.dgvAddr.BackgroundColor = System.Drawing.Color.White;
            this.dgvAddr.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvAddr.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvAddr.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvAddr.Location = new System.Drawing.Point(0, 46);
            this.dgvAddr.MultiSelect = false;
            this.dgvAddr.Name = "dgvAddr";
            this.dgvAddr.RowHeadersVisible = false;
            this.dgvAddr.RowTemplate.Height = 30;
            this.dgvAddr.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvAddr.Size = new System.Drawing.Size(872, 492);
            this.dgvAddr.TabIndex = 1;
            //
            // panelAddrTools
            //
            this.panelAddrTools.Controls.Add(this.btnAddrAdd);
            this.panelAddrTools.Controls.Add(this.btnAddrDel);
            this.panelAddrTools.Controls.Add(this.btnAddrCopy);
            this.panelAddrTools.Controls.Add(this.btnAddrUp);
            this.panelAddrTools.Controls.Add(this.btnAddrDown);
            this.panelAddrTools.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelAddrTools.Location = new System.Drawing.Point(0, 0);
            this.panelAddrTools.Name = "panelAddrTools";
            this.panelAddrTools.Size = new System.Drawing.Size(872, 46);
            this.panelAddrTools.TabIndex = 0;
            //
            // btnAddrAdd
            //
            this.btnAddrAdd.FillColor = System.Drawing.Color.FromArgb(37, 99, 235);
            this.btnAddrAdd.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.btnAddrAdd.Location = new System.Drawing.Point(0, 6);
            this.btnAddrAdd.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnAddrAdd.Name = "btnAddrAdd";
            this.btnAddrAdd.Radius = 4;
            this.btnAddrAdd.RectColor = System.Drawing.Color.FromArgb(37, 99, 235);
            this.btnAddrAdd.Size = new System.Drawing.Size(88, 32);
            this.btnAddrAdd.TabIndex = 0;
            this.btnAddrAdd.Text = "新增";
            this.btnAddrAdd.Click += new System.EventHandler(this.BtnAddrAdd_Click);
            //
            // btnAddrDel
            //
            this.btnAddrDel.FillColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.btnAddrDel.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.btnAddrDel.ForeColor = System.Drawing.Color.FromArgb(55, 65, 81);
            this.btnAddrDel.Location = new System.Drawing.Point(96, 6);
            this.btnAddrDel.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnAddrDel.Name = "btnAddrDel";
            this.btnAddrDel.Radius = 4;
            this.btnAddrDel.RectColor = System.Drawing.Color.FromArgb(209, 213, 219);
            this.btnAddrDel.Size = new System.Drawing.Size(88, 32);
            this.btnAddrDel.TabIndex = 1;
            this.btnAddrDel.Text = "删除";
            this.btnAddrDel.Click += new System.EventHandler(this.BtnAddrDel_Click);
            //
            // btnAddrCopy
            //
            this.btnAddrCopy.FillColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.btnAddrCopy.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.btnAddrCopy.ForeColor = System.Drawing.Color.FromArgb(55, 65, 81);
            this.btnAddrCopy.Location = new System.Drawing.Point(192, 6);
            this.btnAddrCopy.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnAddrCopy.Name = "btnAddrCopy";
            this.btnAddrCopy.Radius = 4;
            this.btnAddrCopy.RectColor = System.Drawing.Color.FromArgb(209, 213, 219);
            this.btnAddrCopy.Size = new System.Drawing.Size(88, 32);
            this.btnAddrCopy.TabIndex = 2;
            this.btnAddrCopy.Text = "复制";
            this.btnAddrCopy.Click += new System.EventHandler(this.BtnAddrCopy_Click);
            //
            // btnAddrUp
            //
            this.btnAddrUp.FillColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.btnAddrUp.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.btnAddrUp.ForeColor = System.Drawing.Color.FromArgb(55, 65, 81);
            this.btnAddrUp.Location = new System.Drawing.Point(288, 6);
            this.btnAddrUp.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnAddrUp.Name = "btnAddrUp";
            this.btnAddrUp.Radius = 4;
            this.btnAddrUp.RectColor = System.Drawing.Color.FromArgb(209, 213, 219);
            this.btnAddrUp.Size = new System.Drawing.Size(70, 32);
            this.btnAddrUp.TabIndex = 3;
            this.btnAddrUp.Text = "上移";
            this.btnAddrUp.Click += new System.EventHandler(this.BtnAddrUp_Click);
            //
            // btnAddrDown
            //
            this.btnAddrDown.FillColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.btnAddrDown.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.btnAddrDown.ForeColor = System.Drawing.Color.FromArgb(55, 65, 81);
            this.btnAddrDown.Location = new System.Drawing.Point(364, 6);
            this.btnAddrDown.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnAddrDown.Name = "btnAddrDown";
            this.btnAddrDown.Radius = 4;
            this.btnAddrDown.RectColor = System.Drawing.Color.FromArgb(209, 213, 219);
            this.btnAddrDown.Size = new System.Drawing.Size(70, 32);
            this.btnAddrDown.TabIndex = 4;
            this.btnAddrDown.Text = "下移";
            this.btnAddrDown.Click += new System.EventHandler(this.BtnAddrDown_Click);
            //
            // lblAddrHint
            //
            this.lblAddrHint.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblAddrHint.Font = new System.Drawing.Font("Microsoft YaHei UI", 8.5F);
            this.lblAddrHint.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
            this.lblAddrHint.Location = new System.Drawing.Point(0, 538);
            this.lblAddrHint.Name = "lblAddrHint";
            this.lblAddrHint.Padding = new System.Windows.Forms.Padding(2, 8, 0, 0);
            this.lblAddrHint.Size = new System.Drawing.Size(872, 56);
            this.lblAddrHint.TabIndex = 2;
            this.lblAddrHint.Text = "键名固定：MES_心跳 / PLC_指令 / PLC_预留bool / MES_审查结果 / MES_SN / PLC_SN / PLC_托盘号。\r\nS7 地址如 M100.0、MW200、DB1.DBB0；Modbus 如 0、100、coil=0。读写列填 R / W / RW。";
            //
            // pageApi
            //
            this.pageApi.Controls.Add(this.tabApi);
            this.pageApi.Controls.Add(this.pgApi);
            this.pageApi.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pageApi.Location = new System.Drawing.Point(14, 14);
            this.pageApi.Name = "pageApi";
            this.pageApi.Size = new System.Drawing.Size(872, 584);
            this.pageApi.TabIndex = 3;
            this.pageApi.Visible = false;
            //
            // tabApi
            //
            this.tabApi.Controls.Add(this.tabPageEndpoints);
            this.tabApi.Controls.Add(this.tabPageHeaders);
            this.tabApi.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabApi.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.tabApi.Location = new System.Drawing.Point(0, 214);
            this.tabApi.Name = "tabApi";
            this.tabApi.SelectedIndex = 0;
            this.tabApi.Size = new System.Drawing.Size(872, 370);
            this.tabApi.TabIndex = 1;
            //
            // tabPageEndpoints
            //
            this.tabPageEndpoints.BackColor = System.Drawing.Color.White;
            this.tabPageEndpoints.Controls.Add(this.dgvEndpoints);
            this.tabPageEndpoints.Location = new System.Drawing.Point(4, 26);
            this.tabPageEndpoints.Name = "tabPageEndpoints";
            this.tabPageEndpoints.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageEndpoints.Size = new System.Drawing.Size(864, 340);
            this.tabPageEndpoints.TabIndex = 0;
            this.tabPageEndpoints.Text = "端点表（业务按键名取路径）";
            //
            // dgvEndpoints
            //
            this.dgvEndpoints.BackgroundColor = System.Drawing.Color.White;
            this.dgvEndpoints.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvEndpoints.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvEndpoints.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvEndpoints.Location = new System.Drawing.Point(3, 3);
            this.dgvEndpoints.Name = "dgvEndpoints";
            this.dgvEndpoints.RowHeadersVisible = false;
            this.dgvEndpoints.RowTemplate.Height = 30;
            this.dgvEndpoints.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvEndpoints.Size = new System.Drawing.Size(858, 334);
            this.dgvEndpoints.TabIndex = 0;
            //
            // tabPageHeaders
            //
            this.tabPageHeaders.BackColor = System.Drawing.Color.White;
            this.tabPageHeaders.Controls.Add(this.dgvHeaders);
            this.tabPageHeaders.Location = new System.Drawing.Point(4, 26);
            this.tabPageHeaders.Name = "tabPageHeaders";
            this.tabPageHeaders.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageHeaders.Size = new System.Drawing.Size(864, 340);
            this.tabPageHeaders.TabIndex = 1;
            this.tabPageHeaders.Text = "请求头";
            //
            // dgvHeaders
            //
            this.dgvHeaders.BackgroundColor = System.Drawing.Color.White;
            this.dgvHeaders.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvHeaders.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvHeaders.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvHeaders.Location = new System.Drawing.Point(3, 3);
            this.dgvHeaders.Name = "dgvHeaders";
            this.dgvHeaders.RowHeadersVisible = false;
            this.dgvHeaders.RowTemplate.Height = 30;
            this.dgvHeaders.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvHeaders.Size = new System.Drawing.Size(858, 334);
            this.dgvHeaders.TabIndex = 0;
            //
            // pgApi
            //
            this.pgApi.Dock = System.Windows.Forms.DockStyle.Top;
            this.pgApi.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.pgApi.HelpVisible = true;
            this.pgApi.Location = new System.Drawing.Point(0, 0);
            this.pgApi.Name = "pgApi";
            this.pgApi.PropertySort = System.Windows.Forms.PropertySort.Categorized;
            this.pgApi.Size = new System.Drawing.Size(872, 214);
            this.pgApi.TabIndex = 0;
            this.pgApi.ToolbarVisible = false;
            //
            // panelBottom
            //
            this.panelBottom.BackColor = System.Drawing.Color.FromArgb(249, 250, 251);
            this.panelBottom.Controls.Add(this.lblStatus);
            this.panelBottom.Controls.Add(this.btnDefault);
            this.panelBottom.Controls.Add(this.btnCancel);
            this.panelBottom.Controls.Add(this.btnSave);
            this.panelBottom.Controls.Add(this.panelBottomLine);
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Location = new System.Drawing.Point(0, 678);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Size = new System.Drawing.Size(1080, 62);
            this.panelBottom.TabIndex = 4;
            //
            // lblStatus
            //
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.lblStatus.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblStatus.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
            this.lblStatus.Location = new System.Drawing.Point(20, 21);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(560, 20);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "改动保存后立即生效";
            //
            // btnDefault
            //
            this.btnDefault.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDefault.FillColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.btnDefault.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.btnDefault.ForeColor = System.Drawing.Color.FromArgb(55, 65, 81);
            this.btnDefault.Location = new System.Drawing.Point(688, 13);
            this.btnDefault.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnDefault.Name = "btnDefault";
            this.btnDefault.Radius = 4;
            this.btnDefault.RectColor = System.Drawing.Color.FromArgb(209, 213, 219);
            this.btnDefault.Size = new System.Drawing.Size(118, 36);
            this.btnDefault.TabIndex = 1;
            this.btnDefault.Text = "恢复默认值";
            this.btnDefault.Click += new System.EventHandler(this.BtnDefault_Click);
            //
            // btnCancel
            //
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.FillColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.btnCancel.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.btnCancel.ForeColor = System.Drawing.Color.FromArgb(55, 65, 81);
            this.btnCancel.Location = new System.Drawing.Point(818, 13);
            this.btnCancel.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Radius = 4;
            this.btnCancel.RectColor = System.Drawing.Color.FromArgb(209, 213, 219);
            this.btnCancel.Size = new System.Drawing.Size(104, 36);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "取消";
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            //
            // btnSave
            //
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.FillColor = System.Drawing.Color.FromArgb(37, 99, 235);
            this.btnSave.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnSave.Location = new System.Drawing.Point(932, 13);
            this.btnSave.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnSave.Name = "btnSave";
            this.btnSave.Radius = 4;
            this.btnSave.RectColor = System.Drawing.Color.FromArgb(37, 99, 235);
            this.btnSave.Size = new System.Drawing.Size(126, 36);
            this.btnSave.TabIndex = 3;
            this.btnSave.Text = "保存并生效";
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);
            //
            // panelBottomLine
            //
            this.panelBottomLine.BackColor = System.Drawing.Color.FromArgb(229, 231, 235);
            this.panelBottomLine.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelBottomLine.Location = new System.Drawing.Point(0, 0);
            this.panelBottomLine.Name = "panelBottomLine";
            this.panelBottomLine.Size = new System.Drawing.Size(1080, 1);
            this.panelBottomLine.TabIndex = 4;
            //
            // ConfigForm
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1080, 740);
            this.Controls.Add(this.panelBody);
            this.Controls.Add(this.panelNav);
            this.Controls.Add(this.panelBottom);
            this.Controls.Add(this.panelHeader);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.MinimumSize = new System.Drawing.Size(940, 660);
            this.Name = "ConfigForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "系统配置";
            this.panelHeader.ResumeLayout(false);
            this.panelHeader.PerformLayout();
            this.panelNav.ResumeLayout(false);
            this.panelBody.ResumeLayout(false);
            this.pageApp.ResumeLayout(false);
            this.pagePlc.ResumeLayout(false);
            this.pageAddr.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvAddr)).EndInit();
            this.panelAddrTools.ResumeLayout(false);
            this.pageApi.ResumeLayout(false);
            this.tabApi.ResumeLayout(false);
            this.tabPageEndpoints.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvEndpoints)).EndInit();
            this.tabPageHeaders.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvHeaders)).EndInit();
            this.panelBottom.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblPath;
        private Sunny.UI.UIButton btnOpenDir;
        private System.Windows.Forms.Panel panelHeaderLine;
        private System.Windows.Forms.Panel panelNav;
        private System.Windows.Forms.Button btnNavApp;
        private System.Windows.Forms.Button btnNavPlc;
        private System.Windows.Forms.Button btnNavAddr;
        private System.Windows.Forms.Button btnNavApi;
        private System.Windows.Forms.Label lblNavTip;
        private System.Windows.Forms.Panel panelBody;
        private System.Windows.Forms.Panel pageApp;
        private System.Windows.Forms.PropertyGrid pgApp;
        private System.Windows.Forms.Panel pagePlc;
        private System.Windows.Forms.Panel pageAddr;
        private System.Windows.Forms.DataGridView dgvAddr;
        private System.Windows.Forms.Panel panelAddrTools;
        private Sunny.UI.UIButton btnAddrAdd;
        private Sunny.UI.UIButton btnAddrDel;
        private Sunny.UI.UIButton btnAddrCopy;
        private Sunny.UI.UIButton btnAddrUp;
        private Sunny.UI.UIButton btnAddrDown;
        private System.Windows.Forms.Label lblAddrHint;
        private System.Windows.Forms.Panel pageApi;
        private System.Windows.Forms.TabControl tabApi;
        private System.Windows.Forms.TabPage tabPageEndpoints;
        private System.Windows.Forms.DataGridView dgvEndpoints;
        private System.Windows.Forms.TabPage tabPageHeaders;
        private System.Windows.Forms.DataGridView dgvHeaders;
        private System.Windows.Forms.PropertyGrid pgApi;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.Label lblStatus;
        private Sunny.UI.UIButton btnDefault;
        private Sunny.UI.UIButton btnCancel;
        private Sunny.UI.UIButton btnSave;
        private System.Windows.Forms.Panel panelBottomLine;
    }
}
