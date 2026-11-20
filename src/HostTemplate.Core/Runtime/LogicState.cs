namespace HostTemplate.Core.Runtime
{
    /// <summary>
    /// 监控运行状态。界面状态栏直接绑定这个枚举。
    /// </summary>
    public enum LogicState
    {
        None = 0,
        初始化中 = 1,
        初始化完成 = 2,
        正在监控 = 3,
        正在停止 = 4,
        监控已停止 = 5,
        系统异常 = 6,
    }
}
