using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.ActivityLogs.DTOs;
using Brouj.Application.Features.ActivityLogs.Queries.GetAll;
using Brouj.Application.Features.ActivityLogs.Queries.GetById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/admin/activity-logs")]
public sealed class AdminActivityLogsController : ApiControllerBase
{
    private readonly ISender _sender;

    public AdminActivityLogsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetActivityLogsQuery))]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<ActivityLogDto>>), 200)]
    public async Task<IActionResult> GetActivityLogs(
        [FromQuery] GetActivityLogsParameters query,
        CancellationToken cancellationToken)
    {
        var request = new GetActivityLogsQuery
        {
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            UserId = query.UserId,
            EntityType = query.EntityType,
            Action = query.Action,
            DateFrom = query.DateFrom,
            DateTo = query.DateTo
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetActivityLogByIdQuery))]
    [ProducesResponseType(typeof(ApiResponse<ActivityLogDto>), 200)]
    public async Task<IActionResult> GetActivityLogById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new GetActivityLogByIdQuery
        {
            Id = id
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }
}
