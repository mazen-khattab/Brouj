using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Areas.DTOs;
using Brouj.Application.Features.Areas.Queries.GetAll;
using Brouj.Application.Features.Areas.Queries.GetById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/areas")]
public sealed class AreasController : ApiControllerBase
{
    private readonly ISender _sender;

    public AreasController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetAreasQuery))]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<AreaListDto>>), 200)]
    public async Task<IActionResult> GetAreas([FromQuery] GetAreasParameters query, CancellationToken cancellationToken)
    {
        var request = new GetAreasQuery
        {
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            Search = query.Search
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetAreaByIdQuery))]
    [ProducesResponseType(typeof(ApiResponse<AreaDto>), 200)]
    public async Task<IActionResult> GetAreaById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new GetAreaByIdQuery
        {
            Id = id
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }
}
