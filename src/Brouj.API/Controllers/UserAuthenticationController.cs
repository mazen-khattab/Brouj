using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Users.Commands.Login;
using Brouj.Application.Features.Users.Commands.Logout;
using Brouj.Application.Features.Users.Commands.RefreshSession;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/auth/users")]
public sealed class UserAuthenticationController : ApiControllerBase
{
    private readonly ISender _sender;

    public UserAuthenticationController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitCategories.Authentication)]
    [EndpointContract(typeof(LoginUserCommand))]
    [ProducesResponseType(typeof(ApiResponse<AuthSessionDto>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> LoginUser([FromBody] LoginInput body, CancellationToken cancellationToken)
    {
        var request = new LoginUserCommand
        {
            Input = body
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitCategories.Authentication)]
    [EndpointContract(typeof(RefreshUserSessionCommand))]
    [ProducesResponseType(typeof(ApiResponse<AuthSessionDto>), 200)]
    public async Task<IActionResult> RefreshUserSession(CancellationToken cancellationToken)
    {
        var request = new RefreshUserSessionCommand();
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitCategories.Authentication)]
    [EndpointContract(typeof(LogoutUserCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    public async Task<IActionResult> LogoutUser(CancellationToken cancellationToken)
    {
        var request = new LogoutUserCommand();
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }
}
