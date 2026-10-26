using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Units.DTOs;
using Brouj.Application.Features.Units.Queries.GetById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/units")]
public sealed class UnitsController : ApiControllerBase
{
    private readonly ISender _sender;

    public UnitsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetUnitByIdQuery))]
    [ProducesResponseType(typeof(ApiResponse<UnitDto>), 200)]
    public async Task<IActionResult> GetUnitById(
        [FromRoute] Guid id,
        [FromQuery] GetUnitByIdParameters query,
        CancellationToken cancellationToken)
    {
        var request = new GetUnitByIdQuery
        {
            Id = id,
            ProjectId = query.ProjectId
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }
}
