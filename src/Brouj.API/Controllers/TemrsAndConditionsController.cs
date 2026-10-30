using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Terms.DTOs;
using Brouj.Application.Features.Terms.Queries.GetById;
using Brouj.Application.Features.Terms.Queries.GetVersions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/admin/terms-and-conditions")]
public sealed class TemrsAndConditionsController : ApiControllerBase
{
    private readonly ISender _sender;

    public TemrsAndConditionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetTermsAndConditionsVersionsQuery))]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<TermsListDto>>), 200)]
    public async Task<IActionResult> GetTermsAndConditionsVersions(
        [FromQuery] GetTermsAndConditionsVersionsParameters query,
        CancellationToken cancellationToken)
    {
        var request = new GetTermsAndConditionsVersionsQuery
        {
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetTermsAndConditionsByIdQuery))]
    [ProducesResponseType(typeof(ApiResponse<TermsDto>), 200)]
    public async Task<IActionResult> GetTermsAndConditionsById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new GetTermsAndConditionsByIdQuery
        {
            Id = id
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }
}
