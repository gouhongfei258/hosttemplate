using System;
using System.IO;
using HostTemplate.Backend.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HostTemplate.Backend.Controllers
{
    public sealed class FeedController : ControllerBase
    {
        private readonly FileUpdateStore _store;

        public FeedController(FileUpdateStore store)
        {
            _store = store;
        }

        [HttpGet("/api/feeds/{version}/{fileName}")]
        public IActionResult Get(string version, string fileName)
        {
            if (!FileUpdateStore.IsSafeVersion(version) || string.IsNullOrWhiteSpace(fileName))
            {
                return ApiText.Status(StatusCodes.Status400BadRequest, "路径不合法。");
            }

            fileName = Path.GetFileName(fileName);
            if (!_store.TryGetPackage(version, out var record))
            {
                return ApiText.Status(StatusCodes.Status404NotFound, "没有这个版本。");
            }

            if (fileName.Equals(VelopackFeed.IndexFileName, StringComparison.OrdinalIgnoreCase))
            {
                return Content(_store.BuildFeedJson(record), "application/json; charset=utf-8");
            }

            if (!string.Equals(fileName, record.FileName, StringComparison.OrdinalIgnoreCase))
            {
                return ApiText.Status(StatusCodes.Status404NotFound, "没有这个文件。");
            }

            var path = _store.GetPackagePath(record);
            if (path == null)
            {
                return ApiText.Status(StatusCodes.Status404NotFound, "安装包文件丢失。");
            }

            return PhysicalFile(path, "application/octet-stream", record.FileName);
        }
    }
}