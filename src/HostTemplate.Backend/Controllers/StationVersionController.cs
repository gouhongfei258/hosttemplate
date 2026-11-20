using System;
using HostTemplate.Backend.Application;
using HostTemplate.Backend.Infrastructure;
using HostTemplate.Backend.Models;
using HostTemplate.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HostTemplate.Backend.Controllers
{
    public sealed class StationVersionController : ControllerBase
    {
        private readonly FileUpdateStore _store;
        private readonly IVersionPolicy _policy;
        private readonly string _publicBaseUrl;

        public StationVersionController(FileUpdateStore store, IVersionPolicy policy, IOptions<UpdateServiceOptions> options)
        {
            _store = store;
            _policy = policy;
            _publicBaseUrl = options?.Value?.PublicBaseUrl;
        }

        [HttpGet("/api/stations/{stationId}/versions")]
        public IActionResult Versions(string stationId)
        {
            if (!StationFiles.IsSafeId(stationId))
            {
                return ApiText.Status(StatusCodes.Status400BadRequest, "工站编号不合法。");
            }

            var packages = _store.ListPackages();
            var latest = _policy.Select(stationId, packages);
            var response = new VersionListResponse
            {
                StationId = stationId,
                Latest = latest == null ? null : ToTarget(stationId, latest, _policy.Name),
            };

            foreach (var package in packages)
            {
                var isLatest = latest != null && string.Equals(package.Version, latest.Version, StringComparison.OrdinalIgnoreCase);
                response.Versions.Add(ToTarget(stationId, package, isLatest ? _policy.Name : string.Empty));
            }

            return Ok(response);
        }

        [HttpGet("/api/stations/{stationId}/latest")]
        public IActionResult Latest(string stationId)
        {
            if (!StationFiles.IsSafeId(stationId))
            {
                return ApiText.Status(StatusCodes.Status400BadRequest, "工站编号不合法。");
            }

            var latest = _policy.Select(stationId, _store.ListPackages());
            if (latest == null)
            {
                return ApiText.Status(StatusCodes.Status404NotFound, "还没有发布任何版本。");
            }

            return Ok(ToTarget(stationId, latest, _policy.Name));
        }

        private TargetVersion ToTarget(string stationId, PackageRecord record, string policy)
        {
            return ReleaseLinks.ToTarget(Request, stationId, record, policy, _publicBaseUrl);
        }
    }
}
