namespace HostTemplate.Core.Runtime
{
    /// <summary>
    /// MES 状态灯。界面只按这个结果上色，和 PLC / 报警 / 运行灯一样不在窗体里写探测规则。
    /// </summary>
    public enum MesLamp
    {
        /// <summary>灭。没有客户端，或接口开着但这次探测失败。</summary>
        Off = 0,

        /// <summary>亮（绿）。接口开着且探测成功。</summary>
        On = 1,

        /// <summary>报警红。api.xml 关闭了接口。与探测是否返回 true 无关。</summary>
        Alarm = 2,
    }

    /// <summary>
    /// 由「有没有客户端、接口开没开、探测通不通」得到 MES 灯。
    /// 接口关闭时即使探测返回 true 也是 <see cref="MesLamp.Alarm"/>。
    /// </summary>
    public static class MesLampState
    {
        public static MesLamp Resolve(bool hasClient, bool enabled, bool pingSucceeded)
        {
            if (!hasClient)
            {
                return MesLamp.Off;
            }

            if (!enabled)
            {
                return MesLamp.Alarm;
            }

            return pingSucceeded ? MesLamp.On : MesLamp.Off;
        }
    }
}
