using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Cities.Commands.Create;
using Brouj.Application.Features.Cities.Commands.Delete;
using Brouj.Application.Features.Cities.Commands.Update;
using Brouj.Application.Features.Cities.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/admin/cities")]
public sealed class AdminCitiesController : ApiControllerBase
{
    private readonly ISender _sender;

    public AdminCitiesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(CreateCityCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
    [Consumes("application/json")]
    public async Task<IActionResult> CreateCity([FromBody] CityInput body, CancellationToken cancellationToken)
    {
        var request = new CreateCityCommand
        {
            Input = body
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result, 201);
    }

    [HttpPatch("{id}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(UpdateCityCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> UpdateCity(
        [FromRoute] Guid id,
        [FromBody] CityInput body,
        CancellationToken cancellationToken)
    {
        var request = new UpdateCityCommand
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
    [EndpointContract(typeof(DeleteCityCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    public async Task<IActionResult> DeleteCity([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new DeleteCityCommand
        {
            Id = id
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }
}
