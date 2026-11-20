using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HostTemplate.Contracts;

namespace HostTemplate.Core.Updates
{
    public static class UpdateJson
    {
        public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };
    }

    /// <summary>
    /// 更新服务的 HTTP 客户端。令牌由调用方放进 HttpClient 的默认请求头。
    /// </summary>
    public sealed class HttpStationServiceClient : IStationServiceClient
    {
        public const string TokenHeaderName = "X-Host-Token";

        private readonly HttpClient _http;

        public HttpStationServiceClient(HttpClient http)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
        }

        public async Task<ConfigPackage> GetConfigAsync(string stationId, CancellationToken cancellationToken)
        {
            using (var response = await _http.GetAsync(ConfigPath(stationId), cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                await EnsureSuccess(response).ConfigureAwait(false);
                var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                var package = ConfigPackage.FromZip(stationId, bytes);
                return package.Files.Count == 0 ? null : package;
            }
        }

        public async Task<VersionListResponse> GetVersionsAsync(string stationId, CancellationToken cancellationToken)
        {
            using (var response = await _http.GetAsync(VersionsPath(stationId), cancellationToken).ConfigureAwait(false))
            {
                await EnsureSuccess(response).ConfigureAwait(false);
                var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return JsonSerializer.Deserialize<VersionListResponse>(json, UpdateJson.Options) ?? new VersionListResponse();
            }
        }

        public async Task<TargetVersion> GetLatestAsync(string stationId, CancellationToken cancellationToken)
        {
            using (var response = await _http.GetAsync(LatestPath(stationId), cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                await EnsureSuccess(response).ConfigureAwait(false);
                var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return JsonSerializer.Deserialize<TargetVersion>(json, UpdateJson.Options);
            }
        }

        public static string ConfigPath(string stationId)
        {
            return "api/stations/" + Uri.EscapeDataString(stationId ?? string.Empty) + "/config";
        }

        public static string VersionsPath(string stationId)
        {
            return "api/stations/" + Uri.EscapeDataString(stationId ?? string.Empty) + "/versions";
        }

        public static string LatestPath(string stationId)
        {
            return "api/stations/" + Uri.EscapeDataString(stationId ?? string.Empty) + "/latest";
        }

        private static async Task EnsureSuccess(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var body = string.Empty;
            try
            {
                body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (HttpRequestException)
            {
            }

            throw new HttpRequestException(
                "更新服务返回 " + (int)response.StatusCode + (string.IsNullOrWhiteSpace(body) ? string.Empty : "：" + body));
        }
    }
}
