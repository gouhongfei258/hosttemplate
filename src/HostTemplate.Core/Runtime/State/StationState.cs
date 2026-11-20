using System;
using HostTemplate.Core.Flow.Process;

namespace HostTemplate.Core.Runtime.State
{
    /// <summary>
    /// 工站实时状态。引擎写、界面读。
    /// 说明：字段为简单值类型，读到的永远是某一时刻的近似快照，用于 HMI 展示足够。
    /// 需要严格一致的场景请走 <see cref="Snapshot"/>。
    /// </summary>
    public sealed class StationState
    {
        public LogicState RunStatus { get; set; } = LogicState.None;

        public bool PlcConnected { get; set; }

        public bool ApiConnected { get; set; }

        /// <summary>当前流程字。</summary>
        public ushort ProcessWord { get; set; }

        /// <summary>上一次处理的流程字。</summary>
        public ushort PreviousProcessWord { get; set; }

        /// <summary>当前流程字的中文描述。</summary>
        public string ProcessContent { get; set; } = string.Empty;

        /// <summary>当前产品 SN。</summary>
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>当前托盘号。</summary>
        public string TrayNo { get; set; } = string.Empty;

        /// <summary>最近一次审查结果码。</summary>
        public ushort ReviewCode { get; set; }

        /// <summary>正在执行的步骤名，空表示空闲。</summary>
        public string RunningStep { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public bool Testing { get; set; }

        /// <summary>良品件数。一件产品记一次，不是每个成功步骤加一。</summary>
        public long CycleCount { get; set; }

        /// <summary>不良件数。一件产品记一次，不是每个失败步骤加一。</summary>
        public long NgCount { get; set; }

        public StationState Snapshot()
        {
            return (StationState)MemberwiseClone();
        }
    }
}
