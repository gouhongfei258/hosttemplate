using System.Threading;
using System.Threading.Tasks;

namespace HostTemplate.Core.Abstractions
{
    public sealed class ApiResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public int StatusCode { get; set; }

        public static ApiResponse Ok(string body)
        {
            return new ApiResponse { Success = true, Body = body, StatusCode = 200 };
        }

        public static ApiResponse Fail(string message, int statusCode = 0)
        {
            return new ApiResponse { Success = false, Message = message, StatusCode = statusCode };
        }
    }

    /// <summary>
    /// 上位系统（MES / 自建服务）接口抽象。
    /// 端点由配置表驱动，业务代码只传 Key，不写死 URL。
    /// </summary>
    public interface IApiClient
    {
        bool IsAvailable { get; }

        /// <summary>
        /// 按端点键发起 POST。payload 为 null 时不带请求体。
        /// </summary>
        Task<ApiResponse> PostAsync(string endpointKey, object payload, CancellationToken cancellationToken);

        /// <summary>
        /// 连通性探测，用于界面状态灯与初始化校验。
        /// </summary>
        Task<bool> PingAsync(CancellationToken cancellationToken);
    }
}
