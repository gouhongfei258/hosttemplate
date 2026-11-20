using System.Collections.Generic;
using System.Net.Http;
using HostTemplate.Core.Updates;
using Velopack.Sources;

namespace HostTemplate.App.Updates
{
    /// <summary>
    /// 给 Velopack 的下载补上工站令牌。服务没设令牌时什么都不加。
    /// </summary>
    public sealed class TokenFileDownloader : HttpClientFileDownloader
    {
        private readonly string _token;

        public TokenFileDownloader(string token)
        {
            _token = token ?? string.Empty;
        }

        protected override HttpClient CreateHttpClient(IDictionary<string, string> headers, double timeout)
        {
            if (!string.IsNullOrEmpty(_token))
            {
                headers ??= new Dictionary<string, string>();
                headers[HttpStationServiceClient.TokenHeaderName] = _token;
            }

            return base.CreateHttpClient(headers, timeout);
        }
    }
}
