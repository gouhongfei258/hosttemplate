using System;
using HostTemplate.Backend.Models;
using HostTemplate.Contracts;
using Microsoft.AspNetCore.Http;

namespace HostTemplate.Backend.Application
{
    /// <summary>
    /// 把磁盘上的安装包记成客户端能下载的地址。feed 末尾保留斜杠，Velopack 会在后面拼文件名。
    /// </summary>
    public static class ReleaseLinks
    {
        public static TargetVersion ToTarget(HttpRequest request, string stationId, PackageRecord record, string policy, string publicBaseUrl)
        {
            var feed = PublicBase(request, publicBaseUrl).TrimEnd('/')
                + "/api/feeds/"
                + Uri.EscapeDataString(record.Version)
                + "/";

            return new TargetVersion
            {
                StationId = stationId,
                Version = record.Version,
                Policy = policy ?? string.Empty,
                FeedUrl = feed,
                DownloadUrl = feed + Uri.EscapeDataString(record.FileName),
                FileName = record.FileName,
                Size = record.Size,
                PublishedAt = record.PublishedAtUtc,
            };
        }

        public static string PublicBase(HttpRequest request, string publicBaseUrl)
        {
            if (!string.IsNullOrWhiteSpace(publicBaseUrl))
            {
                return publicBaseUrl.Trim().TrimEnd('/');
            }

            return request.Scheme + "://" + request.Host.Value;
        }
    }
}
