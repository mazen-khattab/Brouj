using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Reservations.Commands.Cancel;
using Brouj.Application.Features.Reservations.Commands.Create;
using Brouj.Application.Features.Reservations.DTOs;
using Brouj.Application.Features.Reservations.Queries.GetById;
using Brouj.Application.Features.Reservations.Queries.GetMine;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/reservations")]
public sealed class ReservationsController : ApiControllerBase
{
    private readonly ISender _sender;

    public ReservationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.CustomerOnly)]
    [EnableRateLimiting(RateLimitCategories.ReservationWrites)]
    [EndpointContract(typeof(CreateReservationCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
    [Consumes("application/json")]
    public async Task<IActionResult> CreateReservation(
        [FromBody] CreateReservationRequest body,
        CancellationToken cancellationToken)
    {
        var request = new CreateReservationCommand
        {
            UnitId = body.UnitId
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result, 201);
    }

    [HttpGet("me")]
    [Authorize(Policy = PolicyNames.CustomerOnly)]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetMyReservationsQuery))]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<ReservationDto>>), 200)]
    public async Task<IActionResult> GetMyReservations(
        [FromQuery] GetMyReservationsParameters query,
        CancellationToken cancellationToken)
    {
        var request = new GetMyReservationsQuery
        {
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            Status = query.Status
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = PolicyNames.CustomerOnly)]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetReservationByIdQuery))]
    [ProducesResponseType(typeof(ApiResponse<ReservationDto>), 200)]
    public async Task<IActionResult> GetReservationById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new GetReservationByIdQuery
        {
            Id = id
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Policy = PolicyNames.CustomerOnly)]
    [EnableRateLimiting(RateLimitCategories.ReservationWrites)]
    [EndpointContract(typeof(CancelReservationCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    public async Task<IActionResult> CancelReservation([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new CancelReservationCommand
        {
            Id = id
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }
}
