using System.Collections.Generic;
using System.Linq;

namespace HostTemplate.Devices.Hsl
{
    /// <summary>
    /// 一台可选 PLC 设备的描述：显示名、对应的协议与 CPU 型号、默认参数、地址写法。
    /// 配置页的下拉框直接绑它，选完就知道该显示哪些参数、默认值是多少。
    /// </summary>
    public sealed class PlcDeviceDescriptor
    {
        /// <summary>唯一键，用于配置持久化与反查。</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>下拉框显示名。</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>写入 plc.xml 的协议名。</summary>
        public string Protocol { get; set; } = HslDeviceFactory.ProtocolSiemensS7;

        /// <summary>写入 plc.xml 的 CPU 型号，仅西门子使用。</summary>
        public string CpuType { get; set; } = string.Empty;

        public int DefaultPort { get; set; } = 102;

        public int DefaultRack { get; set; }

        public int DefaultSlot { get; set; }

        /// <summary>是否需要机架号/插槽号（西门子 S7 专用）。</summary>
        public bool UseRackSlot { get; set; }

        /// <summary>是否需要站号/节点号。</summary>
        public bool UseStation { get; set; }

        public int DefaultStation { get; set; } = 1;

        /// <summary>站号那一行的标签文字（Modbus 叫站号，欧姆龙叫节点号）。</summary>
        public string StationLabel { get; set; } = "站号";

        /// <summary>地址写法提示。</summary>
        public string AddressHint { get; set; } = string.Empty;

        /// <summary>参数说明（显示在设备下拉框右侧）。</summary>
        public string Remark { get; set; } = string.Empty;

        public override string ToString()
        {
            return DisplayName;
        }
    }

    /// <summary>
    /// 可选 PLC 设备清单。新增品牌只需在 <see cref="All"/> 里加一条，
    /// 配置页下拉、参数显隐、默认值、工厂分支都会自动跟上。
    /// </summary>
    public static class PlcDeviceCatalog
    {
        public static IReadOnlyList<PlcDeviceDescriptor> All { get; } = new List<PlcDeviceDescriptor>
        {
            new PlcDeviceDescriptor
            {
                Key = "S7-1200",
                DisplayName = "西门子 S7-1200",
                Protocol = HslDeviceFactory.ProtocolSiemensS7,
                CpuType = "S1200",
                DefaultPort = 102,
                UseRackSlot = true,
                DefaultRack = 0,
                DefaultSlot = 0,
                AddressHint = "M100.0（位）／MW200（字）／MD300（双字）／DB1.DBW0（数据块）",
                Remark = "默认端口 102。S7-1200 直连时 Rack=0、Slot=0；需在 TIA 里勾选「允许 PUT/GET 通信」。",
            },
            new PlcDeviceDescriptor
            {
                Key = "S7-1500",
                DisplayName = "西门子 S7-1500",
                Protocol = HslDeviceFactory.ProtocolSiemensS7,
                CpuType = "S1500",
                DefaultPort = 102,
                UseRackSlot = true,
                DefaultRack = 0,
                DefaultSlot = 0,
                AddressHint = "M100.0（位）／MW200（字）／DB1.DBW0（数据块，需取消「优化的块访问」）",
                Remark = "默认端口 102。DB 块必须关闭「优化的块访问」，否则绝对地址读写会失败。",
            },
            new PlcDeviceDescriptor
            {
                Key = "S7-300",
                DisplayName = "西门子 S7-300",
                Protocol = HslDeviceFactory.ProtocolSiemensS7,
                CpuType = "S300",
                DefaultPort = 102,
                UseRackSlot = true,
                DefaultRack = 0,
                DefaultSlot = 2,
                AddressHint = "M100.0／MW200／DB1.DBW0",
                Remark = "默认端口 102。S7-300 一般 Rack=0、Slot=2，连不上先改插槽号。",
            },
            new PlcDeviceDescriptor
            {
                Key = "S7-400",
                DisplayName = "西门子 S7-400",
                Protocol = HslDeviceFactory.ProtocolSiemensS7,
                CpuType = "S400",
                DefaultPort = 102,
                UseRackSlot = true,
                DefaultRack = 0,
                DefaultSlot = 2,
                AddressHint = "M100.0／MW200／DB1.DBW0",
                Remark = "默认端口 102。多机架时 Rack 按硬件组态填写，通常 0。",
            },
            new PlcDeviceDescriptor
            {
                Key = "S7-200Smart",
                DisplayName = "西门子 S7-200 Smart",
                Protocol = HslDeviceFactory.ProtocolSiemensS7,
                CpuType = "S200Smart",
                DefaultPort = 102,
                UseRackSlot = false,
                AddressHint = "V100.0／VW200／VD300",
                Remark = "默认端口 102。地址用 V 区，不是 DB 区。",
            },
            new PlcDeviceDescriptor
            {
                Key = "ModbusTcp",
                DisplayName = "Modbus TCP（通用）",
                Protocol = HslDeviceFactory.ProtocolModbusTcp,
                DefaultPort = 502,
                UseStation = true,
                DefaultStation = 1,
                StationLabel = "站号",
                AddressHint = "保持寄存器 0／100；线圈 0 或 coil=0；离散输入 dis=0",
                Remark = "默认端口 502。站号即从站地址（1~247），大部分设备是 1。",
            },
            new PlcDeviceDescriptor
            {
                Key = "InovanceTcp",
                DisplayName = "汇川 Inovance（AM/H5U）",
                Protocol = HslDeviceFactory.ProtocolInovanceTcp,
                DefaultPort = 502,
                UseStation = true,
                DefaultStation = 1,
                StationLabel = "站号",
                AddressHint = "D0 写 0、D100 写 100；M0 用线圈读写",
                Remark = "默认端口 502。汇川走 Modbus TCP 兼容模式，D 区对应保持寄存器。",
            },
            new PlcDeviceDescriptor
            {
                Key = "DeltaTcp",
                DisplayName = "台达 Delta（DVP/AS）",
                Protocol = HslDeviceFactory.ProtocolDeltaTcp,
                DefaultPort = 502,
                UseStation = true,
                DefaultStation = 1,
                StationLabel = "站号",
                AddressHint = "D0 写 0、D100 写 100；M0 用线圈读写",
                Remark = "默认端口 502。台达走 Modbus TCP 兼容模式，D 区对应保持寄存器。",
            },
            new PlcDeviceDescriptor
            {
                Key = "MelsecMc",
                DisplayName = "三菱 MC（Q/FX5U/iQ-R）",
                Protocol = HslDeviceFactory.ProtocolMelsecMc,
                DefaultPort = 6000,
                UseStation = false,
                AddressHint = "D100（数据寄存器）／M100（内部继电器）／X0／Y0／W100",
                Remark = "默认端口 6000，走 MC 3E 帧。网络号/PLC 号用默认值即可直连。",
            },
            new PlcDeviceDescriptor
            {
                Key = "OmronFins",
                DisplayName = "欧姆龙 FINS（NJ/NX/CJ）",
                Protocol = HslDeviceFactory.ProtocolOmronFins,
                DefaultPort = 9600,
                UseStation = true,
                DefaultStation = 0,
                StationLabel = "节点号",
                AddressHint = "D100（DM 区）／W100（工作区）／CIO100",
                Remark = "默认端口 9600。节点号填 0 时自动取 IP 最后一段，连不上先确认节点号。",
            },
        };

        /// <summary>支持的协议名（去重），配置校验用。</summary>
        public static string[] Protocols { get; } =
            All.Select(d => d.Protocol).Distinct().ToArray();

        /// <summary>按协议 + CPU 型号反查设备描述，用于回填下拉框。</summary>
        public static PlcDeviceDescriptor Find(string protocol, string cpuType)
        {
            var p = (protocol ?? string.Empty).Trim();

            if (string.Equals(p, HslDeviceFactory.ProtocolSiemensS7, System.StringComparison.OrdinalIgnoreCase))
            {
                var c = (cpuType ?? string.Empty).Trim();
                var matched = All.FirstOrDefault(d =>
                    string.Equals(d.Protocol, p, System.StringComparison.OrdinalIgnoreCase)
                    && string.Equals(d.CpuType, c, System.StringComparison.OrdinalIgnoreCase));

                if (matched != null)
                {
                    return matched;
                }
            }

            return All.FirstOrDefault(d => string.Equals(d.Protocol, p, System.StringComparison.OrdinalIgnoreCase))
                ?? All[0];
        }

        public static PlcDeviceDescriptor FindByKey(string key)
        {
            return All.FirstOrDefault(d => string.Equals(d.Key, key, System.StringComparison.OrdinalIgnoreCase))
                ?? All[0];
        }
    }
}
