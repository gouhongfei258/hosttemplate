using HostTemplate.Core.Flow.Process;

namespace HostTemplate.Core.Runtime.State
{
    /// <summary>
    /// 工站是否空闲。更新只在这个信号为真时重启。
    /// 空闲 = 流程字为等待进站，且没有步骤在执行，也没有测试中的产品。
    /// 地址表不参与判断。
    /// </summary>
    public interface IStationIdle
    {
        bool IsIdle { get; }
    }

    public static class StationIdle
    {
        public static bool Evaluate(StationState state, bool stepInFlight)
        {
            if (stepInFlight || state == null)
            {
                return false;
            }

            if (state.Testing)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(state.RunningStep))
            {
                return false;
            }

            return state.ProcessWord == ProcessWords.Idle;
        }
    }
}
