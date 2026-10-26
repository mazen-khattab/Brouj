using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Areas.DTOs;
using Brouj.Application.Features.Areas.Queries.GetByCity;
using Brouj.Application.Features.Cities.DTOs;
using Brouj.Application.Features.Cities.Queries.GetAll;
using Brouj.Application.Features.Cities.Queries.GetById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/cities")]
public sealed class CitiesController : ApiControllerBase
{
    private readonly ISender _sender;

    public CitiesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetCitiesQuery))]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<CityDto>>), 200)]
    public async Task<IActionResult> GetCities(
        [FromQuery] GetCitiesParameters query,
        CancellationToken cancellationToken)
    {
        var request = new GetCitiesQuery
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
    [EndpointContract(typeof(GetCityByIdQuery))]
    [ProducesResponseType(typeof(ApiResponse<CityDto>), 200)]
    public async Task<IActionResult> GetCityById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new GetCityByIdQuery
        {
            Id = id
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpGet("{cityId}/areas")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetAreasByCityQuery))]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<AreaListDto>>), 200)]
    public async Task<IActionResult> GetAreasByCity(
        [FromRoute] Guid cityId,
        [FromQuery] GetAreasByCityParameters query,
        CancellationToken cancellationToken)
    {
        var request = new GetAreasByCityQuery
        {
            CityId = cityId,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            Search = query.Search
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }
}
