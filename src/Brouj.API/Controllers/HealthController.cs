using Brouj.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("health/live")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitCategories.PublicReads)]
public sealed class HealthController : ApiControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { Status = "Healthy" });
}
