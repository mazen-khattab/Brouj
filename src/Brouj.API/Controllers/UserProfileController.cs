using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Users.Commands.ChangePassword;
using Brouj.Application.Features.Users.DTOs;
using Brouj.Application.Features.Users.Queries.GetCurrent;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/users/me")]
public sealed class UserProfileController : ApiControllerBase
{
    private readonly ISender _sender;

    public UserProfileController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.StaffOnly)]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetCurrentUserQuery))]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var request = new GetCurrentUserQuery();
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPost("change-password")]
    [Authorize(Policy = PolicyNames.StaffOnly)]
    [EnableRateLimiting(RateLimitCategories.Authentication)]
    [EndpointContract(typeof(ChangeUserPasswordCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> ChangeUserPassword(
        [FromBody] PasswordChangeInput body,
        CancellationToken cancellationToken)
    {
        var request = new ChangeUserPasswordCommand
        {
            Input = body
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }
}
