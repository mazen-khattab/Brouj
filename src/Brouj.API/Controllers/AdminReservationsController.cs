using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Orders.Commands.CreateFromReservation;
using Brouj.Application.Features.Reservations.DTOs;
using Brouj.Application.Features.Reservations.Queries.GetAll;
using Brouj.Application.Features.Reservations.Queries.GetDetails;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/admin/reservations")]
public sealed class AdminReservationsController : ApiControllerBase
{
    private readonly ISender _sender;

    public AdminReservationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetReservationsQuery))]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<ReservationDto>>), 200)]
    public async Task<IActionResult> GetReservations(
        [FromQuery] GetReservationsParameters query,
        CancellationToken cancellationToken)
    {
        var request = new GetReservationsQuery
        {
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            Status = query.Status
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetReservationDetailsQuery))]
    [ProducesResponseType(typeof(ApiResponse<ReservationDto>), 200)]
    public async Task<IActionResult> GetReservationDetails([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new GetReservationDetailsQuery
        {
            Id = id
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPost("{reservationId}/order")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(CreateOrderFromReservationCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
    public async Task<IActionResult> CreateOrderFromReservation(
        [FromRoute] Guid reservationId,
        CancellationToken cancellationToken)
    {
        var request = new CreateOrderFromReservationCommand
        {
            ReservationId = reservationId
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result, 201);
    }
}
