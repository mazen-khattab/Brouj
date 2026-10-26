using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Customers.Commands.ChangePassword;
using Brouj.Application.Features.Customers.Commands.UpdateProfile;
using Brouj.Application.Features.Customers.DTOs;
using Brouj.Application.Features.Customers.Queries.GetCurrent;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/customers/me")]
public sealed class CustomerProfileController : ApiControllerBase
{
    private readonly ISender _sender;

    public CustomerProfileController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.CustomerOnly)]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetCurrentCustomerQuery))]
    [ProducesResponseType(typeof(ApiResponse<CurrentCustomerDto>), 200)]
    public async Task<IActionResult> GetCurrentCustomer(CancellationToken cancellationToken)
    {
        var request = new GetCurrentCustomerQuery();
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPatch]
    [Authorize(Policy = PolicyNames.CustomerOnly)]
    [EnableRateLimiting(RateLimitCategories.Authentication)]
    [EndpointContract(typeof(UpdateCustomerProfileCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> UpdateCustomerProfile(
        [FromBody] CustomerProfileInput body,
        CancellationToken cancellationToken)
    {
        var request = new UpdateCustomerProfileCommand
        {
            Input = body
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPost("change-password")]
    [Authorize(Policy = PolicyNames.CustomerOnly)]
    [EnableRateLimiting(RateLimitCategories.Authentication)]
    [EndpointContract(typeof(ChangeCustomerPasswordCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> ChangeCustomerPassword(
        [FromBody] PasswordChangeInput body,
        CancellationToken cancellationToken)
    {
        var request = new ChangeCustomerPasswordCommand
        {
            Input = body
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }
}
