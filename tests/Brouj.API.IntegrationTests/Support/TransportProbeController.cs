using Brouj.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.IntegrationTests.Support;

[ApiController]
[Route("__tests/transport")]
public sealed class TransportProbeController : ControllerBase
{
    [HttpGet("connection")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    public IActionResult GetConnection() => Ok(new
    {
        Scheme = Request.Scheme,
        Address = HttpContext.Connection.RemoteIpAddress?.ToString()
    });
}
