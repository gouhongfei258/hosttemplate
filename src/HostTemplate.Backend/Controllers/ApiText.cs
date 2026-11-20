using Microsoft.AspNetCore.Mvc;

namespace HostTemplate.Backend.Controllers
{
    /// <summary>
    /// 错误正文保持纯文本，和原来的最小 API 一样，不包成 JSON 字符串。
    /// </summary>
    internal static class ApiText
    {
        public static ContentResult Status(int statusCode, string message)
        {
            return new ContentResult
            {
                StatusCode = statusCode,
                Content = message ?? string.Empty,
                ContentType = "text/plain; charset=utf-8",
            };
        }
    }
}
