using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Terms.Commands.Activate;
using Brouj.Application.Features.Terms.Commands.CreateVersion;
using Brouj.Application.Features.Terms.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/admin/terms-and-conditions")]
public sealed class AdminTermsController : ApiControllerBase
{
    private readonly ISender _sender;

    public AdminTermsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(CreateTermsAndConditionsVersionCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
    [Consumes("application/json")]
    public async Task<IActionResult> CreateTermsAndConditionsVersion(
        [FromBody] CreateTermsVersionInput body,
        CancellationToken cancellationToken)
    {
        var request = new CreateTermsAndConditionsVersionCommand
        {
            Input = body
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result, 201);
    }

    [HttpPost("{id}/activate")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(ActivateTermsAndConditionsCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    public async Task<IActionResult> ActivateTermsAndConditions(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var request = new ActivateTermsAndConditionsCommand
        {
            Id = id
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }
}
