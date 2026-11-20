namespace HostTemplate.Core.Abstractions
{
    /// <summary>
    /// 与 PLC 交互的命名地址键。配置文件中的地址表按这些键名索引，
    /// 引擎只认键名、不认绝对地址，换 PLC 点位时只改配置。
    /// </summary>
    public static class AddressKeys
    {
        /// <summary>上位机写给 PLC 的心跳位（翻转）。</summary>
        public const string Heartbeat = "MES_心跳";

        /// <summary>PLC 写给上位机的流程指令字，占 2 个 Word。</summary>
        public const string Command = "PLC_指令";

        /// <summary>预留 bool 区，16 位。</summary>
        public const string ReserveBool = "PLC_预留bool";

        /// <summary>审查结果回写地址。</summary>
        public const string ReviewResult = "MES_审查结果";

        /// <summary>上位机下发给 PLC 的 SN。</summary>
        public const string MesSerialNumber = "MES_SN";

        /// <summary>PLC 上报的 SN。</summary>
        public const string PlcSerialNumber = "PLC_SN";

        /// <summary>托盘号。</summary>
        public const string TrayNumber = "PLC_托盘号";
    }
}
