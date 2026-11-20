using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace HostTemplate.Core.Configuration
{
    /// <summary>
    /// 单个 PLC 交互点位。沿用原项目 AddrOption 的结构，现场配置可直接迁移。
    /// </summary>
    public sealed class AddrOption
    {
        [DisplayName("1.名称"), Description("与 AddressKeys 中的键名一致")]
        public string Name { get; set; } = string.Empty;

        [DisplayName("2.读写"), Description("R / W / RW")]
        public string Rw { get; set; } = "R";

        [DisplayName("3.地址"), Description("如 M100.0、MW200、DB1.DBW0")]
        public string Address { get; set; } = string.Empty;

        [DisplayName("4.长度"), Description("数组或字符串长度，单点为 0")]
        public int Length { get; set; }

        [DisplayName("5.数据类型"), Description("Bool / Word / Int32 / Real / String")]
        public string DataType { get; set; } = "Word";

        [DisplayName("6.备注"), Description("仅用于现场备注，程序不读取")]
        public string Remark { get; set; } = string.Empty;

        public override string ToString()
        {
            return string.IsNullOrEmpty(Name) ? "新点位" : $"{Name} ({Address})";
        }
    }

    /// <summary>
    /// PLC 连接与地址表配置。
    /// </summary>
    [DefaultProperty(nameof(Address))]
    public sealed class PlcConfig
    {
        [Category("[连接]"), DisplayName("01.协议"), Description("SiemensS7 / ModbusTcp")]
        public string Protocol { get; set; } = "SiemensS7";

        [Category("[连接]"), DisplayName("02.IP 地址")]
        public string Address { get; set; } = "192.168.0.1";

        [Category("[连接]"), DisplayName("03.端口"), Description("S7 标准端口 102，Modbus TCP 502")]
        public int Port { get; set; } = 102;

        [Category("[连接]"), DisplayName("04.CPU 型号"), Description("S1200 / S1500 / S300 / S400 / S200Smart，仅 SiemensS7 使用")]
        public string CpuType { get; set; } = "S1200";

        [Category("[连接]"), DisplayName("05.机架号(Rack)"), Description("S7-300/400 常用 0，S1200/1500 用 0")]
        public int Rack { get; set; } = 0;

        [Category("[连接]"), DisplayName("06.插槽号(Slot)"), Description("S7-300/400 常用 2，S1200/1500 用 0")]
        public int Slot { get; set; } = 0;

        [Category("[连接]"), DisplayName("07.Modbus 站号"), Description("仅 ModbusTcp 使用，1 ~ 247")]
        public int StationNo { get; set; } = 1;

        [Category("[连接]"), DisplayName("08.连接超时(ms)")]
        public int ConnectTimeoutMs { get; set; } = 2000;

        [Category("[连接]"), DisplayName("09.读写超时(ms)")]
        public int ReceiveTimeoutMs { get; set; } = 2000;

        [Category("[连接]"), DisplayName("10.重连间隔(ms)")]
        public int ReconnectDelayMs { get; set; } = 2000;

        [Browsable(false)]
        [DisplayName("01.交互点位")]
        public List<AddrOption> Addresses { get; set; } = new List<AddrOption>();

        /// <summary>
        /// 构建「键名 → 点位」索引。重名时保留第一个并给出提示。
        /// </summary>
        public Dictionary<string, AddrOption> BuildIndex(out string warning)
        {
            warning = string.Empty;
            var index = new Dictionary<string, AddrOption>(StringComparer.Ordinal);

            if (Addresses == null)
            {
                return index;
            }

            foreach (var item in Addresses)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Name))
                {
                    continue;
                }

                if (index.ContainsKey(item.Name))
                {
                    warning += $"地址键名重复，已忽略后者：{item.Name}\r\n";
                    continue;
                }

                index[item.Name] = item;
            }

            return index;
        }
    }
}
