using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Reservations.DTOs;
using Brouj.Application.Features.Reservations.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Brouj.Application.Features.Reservations.Queries.GetById;

public sealed class GetReservationByIdQueryHandler : IRequestHandler<GetReservationByIdQuery, Result<ReservationDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetReservationByIdQueryHandler> _logger;

    public GetReservationByIdQueryHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        ILogger<GetReservationByIdQueryHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<ReservationDto>> Handle(GetReservationByIdQuery request, CancellationToken cancellationToken)
    {
        var customerId = _currentUser.CustomerId!.Value;

        _logger.LogInformation("Fetching reservation {ReservationId} for customer {CustomerId}.", request.Id, customerId);

        var dto = await _db.Reservations
            .AsNoTracking()
            .Where(reservation => reservation.Id == request.Id && reservation.CustomerId == customerId)
            .Select(reservation => new ReservationDto
            {
                Id = reservation.Id,
                UnitId = reservation.UnitId,
                UnitNumber = reservation.Unit.Number,
                ProjectName = reservation.Unit.Project.Name,
                Number = reservation.Number,
                Status = reservation.Status,
                ReservationDate = reservation.ReservationDate,
                CancelledAt = reservation.CancelledAt,
                OrderId = reservation.Order == null ? null : reservation.Order.Id
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (dto is null)
        {
            _logger.LogWarning("Reservation {ReservationId} was not found, or customer {CustomerId} is not authorized to access it.", request.Id, customerId);

            return Result<ReservationDto>.Failure(ReservationErrors.NotFound);
        }

        _logger.LogInformation("Successfully retrieved reservation {ReservationId} for customer {CustomerId}.", request.Id, customerId);

        return Result<ReservationDto>.Success(dto);
    }
}