namespace HostTemplate.Core.Flow
{
    /// <summary>
    /// 审查结果码。沿用原项目 ReviewCode 取值，PLC 侧判码逻辑不变。
    /// </summary>
    public enum ReviewCode : ushort
    {
        OK_InStation = 1,
        NG_InStation = 2,
        OK_TestStart = 3,
        NG_TestStart = 4,
        OK_TestFinish = 5,
        NG_TestFinish = 6,
        OK_OutStation = 7,
        NG_OutStation = 8,
        IsSample = 9,
        NG_PowerReTest = 10,
        OK_Done = 20,
        NG_Product = 30,
    }
}
