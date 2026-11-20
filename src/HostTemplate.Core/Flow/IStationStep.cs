using System.Threading.Tasks;

namespace HostTemplate.Core.Flow
{
    /// <summary>
    /// 一个流程步 = 一个类。新项目做业务时只实现这个接口，
    /// 不需要动引擎，也不会再出现 5000 行的 RunLogic。
    /// </summary>
    public interface IStationStep
    {
        /// <summary>对应的流程字，如 10 / 20 / 30 / 40。</summary>
        ushort Word { get; }

        /// <summary>显示名称，用于日志与界面。</summary>
        string Name { get; }

        Task<StepResult> ExecuteAsync(StepContext context);
    }
}
