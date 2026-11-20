using System;
using System.IO;
using System.Threading.Tasks;
using HostTemplate.Backend.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HostTemplate.Backend.Controllers
{
    public sealed class PackageController : ControllerBase
    {
        private readonly FileUpdateStore _store;

        public PackageController(FileUpdateStore store)
        {
            _store = store;
        }

        [HttpPut("/api/packages/{version}")]
        public async Task<IActionResult> Put(string version)
        {
            if (!FileUpdateStore.IsSafeVersion(version))
            {
                return ApiText.Status(StatusCodes.Status400BadRequest, "版本号不合法。");
            }

            var fileName = Request.Headers["Content-Disposition"].ToString();
            fileName = ExtractFileName(fileName);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = Request.Query["fileName"].ToString();
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = _store.PackageId + "-" + version + "-full.nupkg";
            }

            if (FileUpdateStore.TryParsePackageFileName(fileName, out var fromName)
                && !string.Equals(fromName, version, StringComparison.OrdinalIgnoreCase))
            {
                return ApiText.Status(StatusCodes.Status400BadRequest, "文件名里的版本和地址里的版本不一致。");
            }

            byte[] body;
            using (var buffer = new MemoryStream())
            {
                await Request.Body.CopyToAsync(buffer).ConfigureAwait(false);
                body = buffer.ToArray();
            }

            try
            {
                _store.SavePackage(version, fileName, body);
            }
            catch (InvalidOperationException ex)
            {
                return ApiText.Status(StatusCodes.Status400BadRequest, ex.Message);
            }

            return Ok(new { version, fileName, size = body.Length });
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
