using Microsoft.AspNetCore.Mvc;

namespace HostTemplate.Backend.Controllers
{
    public sealed class HealthController : ControllerBase
    {
        [HttpGet("/api/health")]
        public IActionResult Get()
        {
            return Ok(new { status = "ok" });
        }
    }
}
