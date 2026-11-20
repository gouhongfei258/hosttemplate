using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HostTemplate.Core.Abstractions;
using HostTemplate.Core.Configuration;
using HostTemplate.Core.Diagnostics;

namespace HostTemplate.Devices.Api
{
    /// <summary>
    /// HTTP 接口客户端。端点、请求头、超时、重试全部来自 api.xml。
    /// 关闭 ApiConfig.Enabled 时过站调用直接放行，便于无 MES 环境离线调试。
    /// 连通探测在关闭时返回 false，避免状态灯把「已关闭」显示成连通。
    /// </summary>
    public sealed class HttpApiClient : IApiClient, IDisposable
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            WriteIndented = false
        };

        private readonly ApiConfig _config;
        private readonly IAppLogger _logger;
        private readonly HttpClient _http;

        public HttpApiClient(ApiConfig config, IAppLogger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _http = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(Math.Max(500, config.TimeoutMs))
            };

            if (config.Headers != null)
            {
                foreach (var header in config.Headers)
                {
                    if (header == null || string.IsNullOrWhiteSpace(header.Name))
                    {
                        continue;
                    }

                    try
                    {
                        _http.DefaultRequestHeaders.TryAddWithoutValidation(header.Name, header.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn($"添加请求头 {header.Name} 失败：{ex.Message}");
                    }
                }
            }
        }

        public bool IsAvailable => _config.Enabled;

        public async Task<ApiResponse> PostAsync(string endpointKey, object payload, CancellationToken cancellationToken)
        {
            if (!_config.Enabled)
            {
                _logger.Debug($"接口调用已关闭，跳过 {endpointKey}。");
                return ApiResponse.Ok("{\"Success\":true,\"Mesg\":\"接口已关闭\"}");
            }

            var path = _config.GetPath(endpointKey);
            if (string.IsNullOrWhiteSpace(path))
            {
                return ApiResponse.Fail($"api.xml 未配置端点：{endpointKey}");
            }

            var url = _config.BaseUrl.TrimEnd('/') + path;
            var json = payload == null ? "{}" : JsonSerializer.Serialize(payload, JsonOptions);
            var maxTry = Math.Max(1, _config.RetryCount);

            for (var attempt = 1; attempt <= maxTry; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                    using (var response = await _http.PostAsync(url, content, cancellationToken).ConfigureAwait(false))
                    {
                        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                        var status = (int)response.StatusCode;

                        if (response.IsSuccessStatusCode)
                        {
                            return new ApiResponse { Success = true, Body = body, StatusCode = status };
                        }

                        // 4xx 是参数问题，重试没有意义。
                        if (status >= 400 && status < 500)
                        {
                            _logger.Warn($"接口 {endpointKey} 返回 {status}：{body}");
                            return new ApiResponse
                            {
                                Success = false,
                                Message = $"HTTP {status}",
                                Body = body,
                                StatusCode = status
                            };
                        }

                        _logger.Warn($"接口 {endpointKey} 第 {attempt} 次失败：HTTP {status}");
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.Warn($"接口 {endpointKey} 第 {attempt} 次异常：{ex.Message}");
                }

                if (attempt < maxTry)
                {
                    var delay = TimeSpan.FromSeconds(Math.Max(1, _config.RetryBackoffSeconds) * attempt);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }

            return ApiResponse.Fail($"接口 {endpointKey} 重试 {maxTry} 次后仍失败");
        }

        public async Task<bool> PingAsync(CancellationToken cancellationToken)
        {
            // 过站在 PostAsync 里仍然放行；探测必须反映「MES 没开」，界面才能把灯打红。
            if (!_config.Enabled)
            {
                return false;
            }

            try
            {
                using (var response = await _http
                           .GetAsync(_config.BaseUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                           .ConfigureAwait(false))
                {
                    return (int)response.StatusCode < 500;
                }
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _http.Dispose();
        }
    }
}
