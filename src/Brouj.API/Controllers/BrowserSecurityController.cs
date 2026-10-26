using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.Models;
using Brouj.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/auth/csrf")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitCategories.Authentication)]
public sealed class BrowserSecurityController : ApiControllerBase
{
    private readonly AuthenticationCookies _cookies;

    public BrowserSecurityController(AuthenticationCookies cookies)
    {
        _cookies = cookies;
    }

    [HttpGet]
    public IActionResult GetToken()
    {
        // Bootstrap before login and fetch again after login/logout changes the authenticated identity.
        return Ok(new ApiResponse<CsrfTokenResponse>(new CsrfTokenResponse
        {
            RequestToken = _cookies.GetCsrfToken(HttpContext)
        }));
    }
}
