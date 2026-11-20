using System;
using System.IO;
using System.Threading.Tasks;
using HostTemplate.Backend.Infrastructure;
using HostTemplate.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HostTemplate.Backend.Controllers
{
    public sealed class StationConfigController : ControllerBase
    {
        private readonly FileUpdateStore _store;

        public StationConfigController(FileUpdateStore store)
        {
            _store = store;
        }

        [HttpGet("/api/stations/{stationId}/config")]
        public IActionResult Get(string stationId)
        {
            if (!StationFiles.IsSafeId(stationId))
            {
                return ApiText.Status(StatusCodes.Status400BadRequest, "工站编号不合法。");
            }

            var package = _store.ReadStationConfig(stationId);
            if (package == null || !package.HasRequiredFiles())
            {
                return ApiText.Status(StatusCodes.Status404NotFound, "没有该工站的配置。");
            }

            return File(package.ToZip(), "application/zip", stationId + "-config.zip");
        }

        [HttpPut("/api/stations/{stationId}/config")]
        public async Task<IActionResult> Put(string stationId)
        {
            if (!StationFiles.IsSafeId(stationId))
            {
                return ApiText.Status(StatusCodes.Status400BadRequest, "工站编号不合法。");
            }

            byte[] body;
            using (var buffer = new MemoryStream())
            {
                await Request.Body.CopyToAsync(buffer).ConfigureAwait(false);
                body = buffer.ToArray();
            }

            ConfigPackage package;
            try
            {
                package = ConfigPackage.FromZip(stationId, body);
            }
            catch (InvalidDataException ex)
            {
                return ApiText.Status(StatusCodes.Status400BadRequest, "不是合法的 zip：" + ex.Message);
            }

            if (!package.HasRequiredFiles())
            {
                return ApiText.Status(StatusCodes.Status400BadRequest, "zip 里需要包含 app.xml、plc.xml、api.xml。");
            }

            _store.SaveStationConfig(stationId, package);
            return Ok(new { stationId, files = package.Files.Count });
        }
    }
}
