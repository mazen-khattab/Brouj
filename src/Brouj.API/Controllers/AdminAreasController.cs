using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Areas.Commands.Create;
using Brouj.Application.Features.Areas.Commands.Delete;
using Brouj.Application.Features.Areas.Commands.ReplaceAmenities;
using Brouj.Application.Features.Areas.Commands.Update;
using Brouj.Application.Features.Areas.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/admin/areas")]
public sealed class AdminAreasController : ApiControllerBase
{
    private readonly ISender _sender;

    public AdminAreasController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(CreateAreaCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
    [Consumes("application/json")]
    public async Task<IActionResult> CreateArea([FromBody] AreaInput body, CancellationToken cancellationToken)
    {
        var request = new CreateAreaCommand
        {
            Input = body
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result, 201);
    }

    [HttpPatch("{id}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(UpdateAreaCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> UpdateArea(
        [FromRoute] Guid id,
        [FromBody] AreaInput body,
        CancellationToken cancellationToken)
    {
        var request = new UpdateAreaCommand
        {
            Id = id,
            Input = body
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(DeleteAreaCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    public async Task<IActionResult> DeleteArea([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new DeleteAreaCommand
        {
            Id = id
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPut("{id}/amenities")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(ReplaceAreaAmenitiesCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> ReplaceAreaAmenities(
        [FromRoute] Guid id,
        [FromBody] ReplaceAreaAmenitiesRequest body,
        CancellationToken cancellationToken)
    {
        var request = new ReplaceAreaAmenitiesCommand
        {
            AreaId = id,
            Amenities = body.Amenities
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }
}
