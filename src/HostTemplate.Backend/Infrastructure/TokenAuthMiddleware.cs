using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HostTemplate.Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace HostTemplate.Backend.Infrastructure
{
    /// <summary>
    /// Token 为空时不校验，方便本机调试。配置了令牌后，除健康检查外都要带 X-Host-Token。
    /// </summary>
    public sealed class TokenAuthMiddleware
    {
        public const string HeaderName = "X-Host-Token";

        private readonly RequestDelegate _next;
        private readonly string _token;

        public TokenAuthMiddleware(RequestDelegate next, IOptions<UpdateServiceOptions> options)
        {
            _next = next;
            _token = options?.Value?.Token ?? string.Empty;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (string.IsNullOrEmpty(_token) || IsHealth(context.Request.Path))
            {
                await _next(context).ConfigureAwait(false);
                return;
            }

            if (!context.Request.Headers.TryGetValue(HeaderName, out var provided) || !FixedEquals(provided.ToString(), _token))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("缺少或错误的 X-Host-Token。").ConfigureAwait(false);
                return;
            }

            await _next(context).ConfigureAwait(false);
        }

        private static bool IsHealth(PathString path)
        {
            return path.StartsWithSegments("/api/health", StringComparison.OrdinalIgnoreCase);
        }

        private static bool FixedEquals(string left, string right)
        {
            var a = Encoding.UTF8.GetBytes(left ?? string.Empty);
            var b = Encoding.UTF8.GetBytes(right ?? string.Empty);
            if (a.Length != b.Length)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(a, b);
        }
    }
}
