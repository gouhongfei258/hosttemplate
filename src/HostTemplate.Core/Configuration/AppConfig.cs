using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace HostTemplate.Core.Configuration
{
    /// <summary>
    /// 主界面日间或夜间。默认夜间，和原来的深色界面一致。
    /// </summary>
    public enum UiThemeMode
    {
        夜间 = 0,
        日间 = 1,
    }

    /// <summary>
    /// 站位类型。
    /// </summary>
    public enum StationPosition
    {
        首站 = 0,
        普通工站 = 1,
        尾站 = 2,
    }

    /// <summary>
    /// 机器与工站级配置。对应原项目 RunConfig 的 [PC] 段。
    /// </summary>
    [DefaultProperty(nameof(WorkStationName))]
    public sealed class AppConfig
    {
        [Category("[PC]"), DisplayName("01.PC 编号"), Description("标识该程序用于哪一台电脑")]
        public int PcNo { get; set; } = 1;

        [Category("[PC]"), DisplayName("02.工站号"), Description("如 ZP02-L08-OP110")]
        public string EquId { get; set; } = "OP010";

        [Category("[PC]"), DisplayName("03.工站名称")]
        public string WorkStationName { get; set; } = "上位机模板工站";

        [Category("[PC]"), DisplayName("04.产线编码")]
        public string Line { get; set; } = "LINE-01";

        [Category("[PC]"), DisplayName("05.站位设置")]
        public StationPosition Station { get; set; } = StationPosition.普通工站;

        [Category("[PC]"), DisplayName("06.段名")]
        public string Section { get; set; } = "default";

        [Category("[运行]"), DisplayName("01.轮询周期(ms)"), Description("建议 50ms，过小会压垮 PLC 通信")]
        public int PollIntervalMs { get; set; } = 50;

        [Category("[运行]"), DisplayName("02.心跳翻转周期(轮询次数)"), Description("10 表示每 10 次轮询翻转一次心跳")]
        public int HeartbeatTicks { get; set; } = 10;

        [Category("[运行]"), DisplayName("03.出站前必须包含的流程字"), Description("留空表示不校验。默认 10-20-30（40 为出站本身，无需包含）")]
        public List<ushort> RequiredSteps { get; set; } = new List<ushort> { 10, 20, 30 };

        [Category("[界面]"), DisplayName("02.测试类型"), Description("「实时数据」页按这个名字加载。默认、空白，或没有登记过的类型，都使用内置页面。其它类型由工站在启动时用 LiveViewCatalog.Register 登记，页面可以是任意控件。")]
        public string TestType { get; set; } = "默认";

        [Category("[条码]"), DisplayName("01.条码长度"), Description("0 表示不校验长度")]
        public int SnLength { get; set; } = 18;

        [Category("[条码]"), DisplayName("02.条码公共前缀")]
        public string SnPrefix { get; set; } = string.Empty;

        [Category("[存储]"), DisplayName("01.保存文本日志")]
        public bool SaveTextLog { get; set; } = true;

        [Category("[存储]"), DisplayName("02.保存数据库日志")]
        public bool SaveDbLog { get; set; } = true;

        [Category("[存储]"), DisplayName("03.数据库相对路径")]
        public string DatabasePath { get; set; } = @"Data\host.db";

        [Category("[调试]"), DisplayName("01.启用仿真模式"), Description("开启后使用内存虚拟 PLC，无需真实硬件")]
        public bool Simulate { get; set; } = false;

        [Category("[调试]"), DisplayName("02.仿真自动推进(秒)"), Description("大于 0 时虚拟 PLC 自动推进流程字")]
        public int SimulateStepSeconds { get; set; } = 0;

        [Category("[权限]"), DisplayName("01.启用登录"), Description("默认关闭。关闭时以匿名管理员身份运行，开启后启动需登录")]
        public bool EnableLogin { get; set; } = false;

        [Category("[权限]"), DisplayName("02.登录超时(分钟)"), Description("0 表示不自动锁屏")]
        public int SessionTimeoutMinutes { get; set; } = 0;

        [Category("[界面]"), DisplayName("01.界面主题"), Description("主界面日间或夜间。主界面右上角的主题按钮会立即写回这里，下次启动保持。默认夜间。")]
        public UiThemeMode Theme { get; set; } = UiThemeMode.夜间;

        [Category("[更新]"), DisplayName("01.自动更新"), Description("开启后，向更新服务查询本工站的目标版本。比当前程序新时在后台下载，等流程字回到等待进站且没有步骤在执行时再安装并重启。默认关闭。")]
        public bool AutoUpdate { get; set; }

        [Category("[更新]"), DisplayName("02.开机自启"), Description("开启后写入当前用户的启动项，登录 Windows 后自动打开本程序。关闭后从启动项移除。已安装的程序指向 current 目录，更新后路径不变；调试运行则指向正在运行的程序。默认关闭。")]
        public bool StartWithWindows { get; set; }

        [Browsable(false)]
        [XmlIgnore]
        public string DisplayName => $"{WorkStationName}（{EquId}）";
    }
}
