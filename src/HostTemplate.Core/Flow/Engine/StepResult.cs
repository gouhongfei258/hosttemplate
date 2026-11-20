using HostTemplate.Core.Flow.Process;

namespace HostTemplate.Core.Flow.Engine
{
    /// <summary>
    /// 单个流程步的执行结果。引擎据此回写审查码。
    /// </summary>
    public sealed class StepResult
    {
        public bool Success { get; set; }

        public ReviewCode Review { get; set; }

        public string Message { get; set; } = string.Empty;

        public static StepResult Ok(ReviewCode review, string message = "")
        {
            return new StepResult { Success = true, Review = review, Message = message };
        }

        public static StepResult Ng(ReviewCode review, string message)
        {
            return new StepResult { Success = false, Review = review, Message = message };
        }
    }
}
