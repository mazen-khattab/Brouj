using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Orders.Commands.CompleteInitiative;
using Brouj.Application.Features.Orders.DTOs;
using Brouj.Application.Features.Orders.Queries.GetAll;
using Brouj.Application.Features.Orders.Queries.GetById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/admin/orders")]
public sealed class AdminOrdersController : ApiControllerBase
{
    private readonly ISender _sender;

    public AdminOrdersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetOrdersQuery))]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<OrderDto>>), 200)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] GetOrdersParameters query,
        CancellationToken cancellationToken)
    {
        var request = new GetOrdersQuery
        {
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            Status = query.Status
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = PolicyNames.MemberOnly)]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetOrderByIdQuery))]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), 200)]
    public async Task<IActionResult> GetOrderById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new GetOrderByIdQuery
        {
            Id = id
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPost("{id}/complete")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(CompleteInitiativeOrderCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    public async Task<IActionResult> CompleteInitiativeOrder([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new CompleteInitiativeOrderCommand
        {
            Id = id
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }
}
