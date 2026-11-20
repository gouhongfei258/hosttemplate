using System;
using System.IO;
using System.Threading.Tasks;
using HostTemplate.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HostTemplate.UpdateService
{
    public static class UpdateApi
    {
        public static void Map(WebApplication app)
        {
            app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

            app.MapGet("/api/stations/{stationId}/config", (string stationId, FileUpdateStore store) =>
            {
                if (!StationFiles.IsSafeId(stationId))
                {
                    return Results.BadRequest("工站编号不合法。");
                }

                var package = store.ReadStationConfig(stationId);
                if (package == null || !package.HasRequiredFiles())
                {
                    return Results.NotFound("没有该工站的配置。");
                }

                return Results.File(package.ToZip(), "application/zip", stationId + "-config.zip");
            });

            app.MapPut("/api/stations/{stationId}/config", async (string stationId, HttpRequest request, FileUpdateStore store) =>
            {
                if (!StationFiles.IsSafeId(stationId))
                {
                    return Results.BadRequest("工站编号不合法。");
                }

                byte[] body;
                using (var buffer = new MemoryStream())
                {
                    await request.Body.CopyToAsync(buffer).ConfigureAwait(false);
                    body = buffer.ToArray();
                }

                ConfigPackage package;
                try
                {
                    package = ConfigPackage.FromZip(stationId, body);
                }
                catch (InvalidDataException ex)
                {
                    return Results.BadRequest("不是合法的 zip：" + ex.Message);
                }

                if (!package.HasRequiredFiles())
                {
                    return Results.BadRequest("zip 里需要包含 app.xml、plc.xml、api.xml。");
                }

                store.SaveStationConfig(stationId, package);
                return Results.Ok(new { stationId, files = package.Files.Count });
            });

            app.MapGet("/api/stations/{stationId}/versions", (string stationId, HttpRequest request, FileUpdateStore store, IVersionPolicy policy) =>
            {
                if (!StationFiles.IsSafeId(stationId))
                {
                    return Results.BadRequest("工站编号不合法。");
                }

                var packages = store.ListPackages();
                var latest = policy.Select(stationId, packages);
                var response = new VersionListResponse
                {
                    StationId = stationId,
                    Latest = latest == null ? null : ToTarget(request, store, stationId, latest, policy.Name),
                };

                foreach (var package in packages)
                {
                    var isLatest = latest != null && string.Equals(package.Version, latest.Version, StringComparison.OrdinalIgnoreCase);
                    response.Versions.Add(ToTarget(request, store, stationId, package, isLatest ? policy.Name : string.Empty));
                }

                return Results.Ok(response);
            });

            app.MapGet("/api/stations/{stationId}/latest", (string stationId, HttpRequest request, FileUpdateStore store, IVersionPolicy policy) =>
            {
                if (!StationFiles.IsSafeId(stationId))
                {
                    return Results.BadRequest("工站编号不合法。");
                }

                var latest = policy.Select(stationId, store.ListPackages());
                if (latest == null)
                {
                    return Results.NotFound("还没有发布任何版本。");
                }

                return Results.Ok(ToTarget(request, store, stationId, latest, policy.Name));
            });

            app.MapPut("/api/packages/{version}", async (string version, HttpRequest request, FileUpdateStore store) =>
            {
                if (!FileUpdateStore.IsSafeVersion(version))
                {
                    return Results.BadRequest("版本号不合法。");
                }

                var fileName = request.Headers["Content-Disposition"].ToString();
                fileName = ExtractFileName(fileName);
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = request.Query["fileName"].ToString();
                }

                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = store.PackageId + "-" + version + "-full.nupkg";
                }

                if (FileUpdateStore.TryParsePackageFileName(fileName, out var fromName)
                    && !string.Equals(fromName, version, StringComparison.OrdinalIgnoreCase))
                {
                    return Results.BadRequest("文件名里的版本和地址里的版本不一致。");
                }

                byte[] body;
                using (var buffer = new MemoryStream())
                {
                    await request.Body.CopyToAsync(buffer).ConfigureAwait(false);
                    body = buffer.ToArray();
                }

                try
                {
                    store.SavePackage(version, fileName, body);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(ex.Message);
                }

                return Results.Ok(new { version, fileName, size = body.Length });
            });

            app.MapGet("/api/feeds/{version}/{fileName}", (string version, string fileName, FileUpdateStore store) =>
            {
                if (!FileUpdateStore.IsSafeVersion(version) || string.IsNullOrWhiteSpace(fileName))
                {
                    return Results.BadRequest("路径不合法。");
                }

                fileName = Path.GetFileName(fileName);
                if (!store.TryGetPackage(version, out var record))
                {
                    return Results.NotFound("没有这个版本。");
                }

                if (fileName.Equals(VelopackFeed.IndexFileName, StringComparison.OrdinalIgnoreCase))
                {
                    return Results.Text(store.BuildFeedJson(record), "application/json; charset=utf-8");
                }

                if (!string.Equals(fileName, record.FileName, StringComparison.OrdinalIgnoreCase))
                {
                    return Results.NotFound("没有这个文件。");
                }

                var path = store.GetPackagePath(record);
                if (path == null)
                {
                    return Results.NotFound("安装包文件丢失。");
                }

                return Results.File(path, "application/octet-stream", record.FileName);
            });
        }

        private static TargetVersion ToTarget(HttpRequest request, FileUpdateStore store, string stationId, PackageRecord record, string policy)
        {
            var feed = PublicBase(request).TrimEnd('/') + "/api/feeds/" + Uri.EscapeDataString(record.Version) + "/";
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

        private static string PublicBase(HttpRequest request)
        {
            var configured = request.HttpContext.RequestServices
                .GetService(typeof(Microsoft.Extensions.Options.IOptions<UpdateServiceOptions>))
                as Microsoft.Extensions.Options.IOptions<UpdateServiceOptions>;

            var url = configured?.Value?.PublicBaseUrl;
            if (!string.IsNullOrWhiteSpace(url))
            {
                return url.Trim().TrimEnd('/');
            }

            return request.Scheme + "://" + request.Host.Value;
        }

        private static string ExtractFileName(string contentDisposition)
        {
            if (string.IsNullOrWhiteSpace(contentDisposition))
            {
                return string.Empty;
            }

            const string marker = "filename=";
            var index = contentDisposition.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return string.Empty;
            }

            var value = contentDisposition.Substring(index + marker.Length).Trim().Trim('"');
            var semi = value.IndexOf(';');
            if (semi >= 0)
            {
                value = value.Substring(0, semi);
            }

            return Path.GetFileName(value.Trim().Trim('"'));
        }
    }
}
