using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Orders.DTOs;
using Brouj.Application.Features.Orders.Queries.GetMine;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/orders")]
public sealed class OrdersController : ApiControllerBase
{
    private readonly ISender _sender;

    public OrdersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("me")]
    [Authorize(Policy = PolicyNames.CustomerOnly)]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetMyOrdersQuery))]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<OrderDto>>), 200)]
    public async Task<IActionResult> GetMyOrders([FromQuery] GetMyOrdersParameters query, CancellationToken cancellationToken)
    {
        var request = new GetMyOrdersQuery
        {
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            Status = query.Status
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }
}
