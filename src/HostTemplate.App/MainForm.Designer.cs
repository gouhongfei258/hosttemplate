namespace HostTemplate.App
{
    partial class MainForm
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
            this.panelTop = new System.Windows.Forms.Panel();
            this.panelInfo = new System.Windows.Forms.Panel();
            this.panelBottom = new System.Windows.Forms.Panel();
            this.lblStation = new System.Windows.Forms.Label();
            this.lblSub = new System.Windows.Forms.Label();
            this.lightPlc = new HostTemplate.App.Controls.StatusLight();
            this.lightApi = new HostTemplate.App.Controls.StatusLight();
            this.lightAlarm = new HostTemplate.App.Controls.StatusLight();
            this.lightRun = new HostTemplate.App.Controls.StatusLight();
            this.lblWord = new System.Windows.Forms.Label();
            this.lblWordName = new System.Windows.Forms.Label();
            this.lblSn = new System.Windows.Forms.Label();
            this.lblTray = new System.Windows.Forms.Label();
            this.lblStep = new System.Windows.Forms.Label();
            this.lblCount = new System.Windows.Forms.Label();
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            this.btnStart = new Sunny.UI.UIButton();
            this.btnStop = new Sunny.UI.UIButton();
            this.btnReset = new Sunny.UI.UIButton();
            this.btnConfig = new Sunny.UI.UIButton();
            this.btnUser = new Sunny.UI.UIButton();
            this.lblHint = new System.Windows.Forms.Label();
            this.panelTop.SuspendLayout();
            this.panelInfo.SuspendLayout();
            this.panelBottom.SuspendLayout();
            this.SuspendLayout();
            //
            // panelTop
            //
            this.panelTop.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.panelTop.BackColor = System.Drawing.Color.FromArgb(17, 24, 39);
            this.panelTop.Controls.Add(this.lblStation);
            this.panelTop.Controls.Add(this.lblSub);
            this.panelTop.Controls.Add(this.lightPlc);
            this.panelTop.Controls.Add(this.lightApi);
            this.panelTop.Controls.Add(this.lightAlarm);
            this.panelTop.Controls.Add(this.lightRun);
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(1184, 80);
            this.panelTop.TabIndex = 0;
            //
            // lblStation
            //
            this.lblStation.AutoSize = false;
            this.lblStation.Font = new System.Drawing.Font("Microsoft YaHei UI", 15F, System.Drawing.FontStyle.Bold);
            this.lblStation.ForeColor = System.Drawing.Color.FromArgb(249, 250, 251);
            this.lblStation.Location = new System.Drawing.Point(18, 10);
            this.lblStation.Name = "lblStation";
            this.lblStation.Size = new System.Drawing.Size(620, 32);
            this.lblStation.TabIndex = 0;
            this.lblStation.Text = "上位机模板工站";
            this.lblStation.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblSub
            //
            this.lblSub.AutoSize = false;
            this.lblSub.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblSub.ForeColor = System.Drawing.Color.FromArgb(156, 163, 175);
            this.lblSub.Location = new System.Drawing.Point(20, 46);
            this.lblSub.Name = "lblSub";
            this.lblSub.Size = new System.Drawing.Size(620, 22);
            this.lblSub.TabIndex = 1;
            this.lblSub.Text = "产线 -- / 工站 --";
            this.lblSub.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lightPlc
            //
            this.lightPlc.ActiveColor = System.Drawing.Color.FromArgb(34, 197, 94);
            this.lightPlc.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lightPlc.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lightPlc.ForeColor = System.Drawing.Color.FromArgb(209, 213, 219);
            this.lightPlc.Location = new System.Drawing.Point(750, 26);
            this.lightPlc.Name = "lightPlc";
            this.lightPlc.Size = new System.Drawing.Size(96, 28);
            this.lightPlc.TabIndex = 2;
            this.lightPlc.Text = "PLC";
            //
            // lightApi
            //
            this.lightApi.ActiveColor = System.Drawing.Color.FromArgb(34, 197, 94);
            this.lightApi.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lightApi.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lightApi.ForeColor = System.Drawing.Color.FromArgb(209, 213, 219);
            this.lightApi.Location = new System.Drawing.Point(855, 26);
            this.lightApi.Name = "lightApi";
            this.lightApi.Size = new System.Drawing.Size(96, 28);
            this.lightApi.TabIndex = 3;
            this.lightApi.Text = "MES";
            //
            // lightAlarm
            //
            this.lightAlarm.ActiveColor = System.Drawing.Color.FromArgb(239, 68, 68);
            this.lightAlarm.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lightAlarm.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lightAlarm.ForeColor = System.Drawing.Color.FromArgb(209, 213, 219);
            this.lightAlarm.Location = new System.Drawing.Point(960, 26);
            this.lightAlarm.Name = "lightAlarm";
            this.lightAlarm.Size = new System.Drawing.Size(96, 28);
            this.lightAlarm.TabIndex = 4;
            this.lightAlarm.Text = "报警";
            //
            // lightRun
            //
            this.lightRun.ActiveColor = System.Drawing.Color.FromArgb(56, 189, 248);
            this.lightRun.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lightRun.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lightRun.ForeColor = System.Drawing.Color.FromArgb(209, 213, 219);
            this.lightRun.Location = new System.Drawing.Point(1065, 26);
            this.lightRun.Name = "lightRun";
            this.lightRun.Size = new System.Drawing.Size(96, 28);
            this.lightRun.TabIndex = 5;
            this.lightRun.Text = "运行";
            //
            // panelInfo
            //
            this.panelInfo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.panelInfo.BackColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.panelInfo.Controls.Add(this.lblWord);
            this.panelInfo.Controls.Add(this.lblWordName);
            this.panelInfo.Controls.Add(this.lblSn);
            this.panelInfo.Controls.Add(this.lblTray);
            this.panelInfo.Controls.Add(this.lblStep);
            this.panelInfo.Controls.Add(this.lblCount);
            this.panelInfo.Location = new System.Drawing.Point(0, 80);
            this.panelInfo.Name = "panelInfo";
            this.panelInfo.Size = new System.Drawing.Size(1184, 132);
            this.panelInfo.TabIndex = 1;
            //
            // lblWord
            //
            this.lblWord.Font = new System.Drawing.Font("Consolas", 40F, System.Drawing.FontStyle.Bold);
            this.lblWord.ForeColor = System.Drawing.Color.FromArgb(56, 189, 248);
            this.lblWord.Location = new System.Drawing.Point(16, 8);
            this.lblWord.Name = "lblWord";
            this.lblWord.Size = new System.Drawing.Size(150, 72);
            this.lblWord.TabIndex = 0;
            this.lblWord.Text = "0";
            this.lblWord.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblWordName
            //
            this.lblWordName.Font = new System.Drawing.Font("Microsoft YaHei UI", 18F);
            this.lblWordName.ForeColor = System.Drawing.Color.FromArgb(229, 231, 235);
            this.lblWordName.Location = new System.Drawing.Point(176, 22);
            this.lblWordName.Name = "lblWordName";
            this.lblWordName.Size = new System.Drawing.Size(320, 44);
            this.lblWordName.TabIndex = 1;
            this.lblWordName.Text = "等待进站";
            this.lblWordName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblSn
            //
            this.lblSn.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
            this.lblSn.ForeColor = System.Drawing.Color.FromArgb(156, 163, 175);
            this.lblSn.Location = new System.Drawing.Point(16, 92);
            this.lblSn.Name = "lblSn";
            this.lblSn.Size = new System.Drawing.Size(380, 24);
            this.lblSn.TabIndex = 2;
            this.lblSn.Text = "SN：--";
            this.lblSn.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblTray
            //
            this.lblTray.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
            this.lblTray.ForeColor = System.Drawing.Color.FromArgb(156, 163, 175);
            this.lblTray.Location = new System.Drawing.Point(404, 92);
            this.lblTray.Name = "lblTray";
            this.lblTray.Size = new System.Drawing.Size(240, 24);
            this.lblTray.TabIndex = 3;
            this.lblTray.Text = "托盘：--";
            this.lblTray.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblStep
            //
            this.lblStep.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
            this.lblStep.ForeColor = System.Drawing.Color.FromArgb(156, 163, 175);
            this.lblStep.Location = new System.Drawing.Point(652, 92);
            this.lblStep.Name = "lblStep";
            this.lblStep.Size = new System.Drawing.Size(300, 24);
            this.lblStep.TabIndex = 4;
            this.lblStep.Text = "轨迹：--";
            this.lblStep.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblCount
            //
            this.lblCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCount.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
            this.lblCount.ForeColor = System.Drawing.Color.FromArgb(156, 163, 175);
            this.lblCount.Location = new System.Drawing.Point(950, 92);
            this.lblCount.Name = "lblCount";
            this.lblCount.Size = new System.Drawing.Size(220, 24);
            this.lblCount.TabIndex = 5;
            this.lblCount.Text = "良品 0 / 不良 0";
            this.lblCount.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // rtbLog
            //
            this.rtbLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbLog.BackColor = System.Drawing.Color.FromArgb(11, 18, 32);
            this.rtbLog.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbLog.Font = new System.Drawing.Font("Consolas", 10F);
            this.rtbLog.ForeColor = System.Drawing.Color.FromArgb(209, 213, 219);
            this.rtbLog.HideSelection = false;
            this.rtbLog.Location = new System.Drawing.Point(0, 212);
            this.rtbLog.Name = "rtbLog";
            this.rtbLog.ReadOnly = true;
            this.rtbLog.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.rtbLog.Size = new System.Drawing.Size(1184, 461);
            this.rtbLog.TabIndex = 2;
            this.rtbLog.Text = "";
            this.rtbLog.WordWrap = false;
            //
            // panelBottom
            //
            this.panelBottom.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.panelBottom.BackColor = System.Drawing.Color.FromArgb(17, 24, 39);
            this.panelBottom.Controls.Add(this.btnStart);
            this.panelBottom.Controls.Add(this.btnStop);
            this.panelBottom.Controls.Add(this.btnReset);
            this.panelBottom.Controls.Add(this.btnConfig);
            this.panelBottom.Controls.Add(this.btnUser);
            this.panelBottom.Controls.Add(this.lblHint);
            this.panelBottom.Location = new System.Drawing.Point(0, 673);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Size = new System.Drawing.Size(1184, 68);
            this.panelBottom.TabIndex = 3;
            //
            // btnStart
            //
            this.btnStart.FillColor = System.Drawing.Color.FromArgb(22, 163, 74);
            this.btnStart.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F);
            this.btnStart.Location = new System.Drawing.Point(18, 14);
            this.btnStart.Name = "btnStart";
            this.btnStart.Radius = 4;
            this.btnStart.RectColor = System.Drawing.Color.FromArgb(22, 163, 74);
            this.btnStart.Size = new System.Drawing.Size(120, 40);
            this.btnStart.TabIndex = 0;
            this.btnStart.Text = "开始监控";
            this.btnStart.Click += new System.EventHandler(this.BtnStart_Click);
            //
            // btnStop
            //
            this.btnStop.FillColor = System.Drawing.Color.FromArgb(185, 28, 28);
            this.btnStop.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F);
            this.btnStop.Location = new System.Drawing.Point(152, 14);
            this.btnStop.Name = "btnStop";
            this.btnStop.Radius = 4;
            this.btnStop.RectColor = System.Drawing.Color.FromArgb(185, 28, 28);
            this.btnStop.Size = new System.Drawing.Size(120, 40);
            this.btnStop.TabIndex = 1;
            this.btnStop.Text = "停止监控";
            this.btnStop.Click += new System.EventHandler(this.BtnStop_Click);
            //
            // btnReset
            //
            this.btnReset.FillColor = System.Drawing.Color.FromArgb(55, 65, 81);
            this.btnReset.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F);
            this.btnReset.Location = new System.Drawing.Point(286, 14);
            this.btnReset.Name = "btnReset";
            this.btnReset.Radius = 4;
            this.btnReset.RectColor = System.Drawing.Color.FromArgb(75, 85, 99);
            this.btnReset.Size = new System.Drawing.Size(120, 40);
            this.btnReset.TabIndex = 2;
            this.btnReset.Text = "复位流程";
            this.btnReset.Click += new System.EventHandler(this.BtnReset_Click);
            //
            // btnConfig
            //
            this.btnConfig.FillColor = System.Drawing.Color.FromArgb(55, 65, 81);
            this.btnConfig.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F);
            this.btnConfig.Location = new System.Drawing.Point(420, 14);
            this.btnConfig.Name = "btnConfig";
            this.btnConfig.Radius = 4;
            this.btnConfig.RectColor = System.Drawing.Color.FromArgb(75, 85, 99);
            this.btnConfig.Size = new System.Drawing.Size(120, 40);
            this.btnConfig.TabIndex = 3;
            this.btnConfig.Text = "系统配置";
            this.btnConfig.Click += new System.EventHandler(this.BtnConfig_Click);
            //
            // btnUser
            //
            this.btnUser.FillColor = System.Drawing.Color.FromArgb(55, 65, 81);
            this.btnUser.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F);
            this.btnUser.Location = new System.Drawing.Point(554, 14);
            this.btnUser.Name = "btnUser";
            this.btnUser.Radius = 4;
            this.btnUser.RectColor = System.Drawing.Color.FromArgb(75, 85, 99);
            this.btnUser.Size = new System.Drawing.Size(120, 40);
            this.btnUser.TabIndex = 4;
            this.btnUser.Text = "账号管理";
            this.btnUser.Click += new System.EventHandler(this.BtnUser_Click);
            //
            // lblHint
            //
            this.lblHint.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblHint.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblHint.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
            this.lblHint.Location = new System.Drawing.Point(860, 24);
            this.lblHint.Name = "lblHint";
            this.lblHint.Size = new System.Drawing.Size(240, 22);
            this.lblHint.TabIndex = 5;
            this.lblHint.Text = "HostTemplate";
            this.lblHint.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // MainForm
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(11, 18, 32);
            this.ClientSize = new System.Drawing.Size(1184, 741);
            this.Controls.Add(this.panelTop);
            this.Controls.Add(this.panelInfo);
            this.Controls.Add(this.rtbLog);
            this.Controls.Add(this.panelBottom);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.MinimumSize = new System.Drawing.Size(1000, 680);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "上位机模板 · 监控";
            this.panelTop.ResumeLayout(false);
            this.panelInfo.ResumeLayout(false);
            this.panelBottom.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Panel panelInfo;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.Label lblStation;
        private System.Windows.Forms.Label lblSub;
        private HostTemplate.App.Controls.StatusLight lightPlc;
        private HostTemplate.App.Controls.StatusLight lightApi;
        private HostTemplate.App.Controls.StatusLight lightAlarm;
        private HostTemplate.App.Controls.StatusLight lightRun;
        private System.Windows.Forms.Label lblWord;
        private System.Windows.Forms.Label lblWordName;
        private System.Windows.Forms.Label lblSn;
        private System.Windows.Forms.Label lblTray;
        private System.Windows.Forms.Label lblStep;
        private System.Windows.Forms.Label lblCount;
        private System.Windows.Forms.RichTextBox rtbLog;
        private Sunny.UI.UIButton btnStart;
        private Sunny.UI.UIButton btnStop;
        private Sunny.UI.UIButton btnReset;
        private Sunny.UI.UIButton btnConfig;
        private Sunny.UI.UIButton btnUser;
        private System.Windows.Forms.Label lblHint;
    }
}
