using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Reservations.Errors;
using Brouj.Application.Features.Reservations.Strategies;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Brouj.Application.Features.Reservations.Commands.Create;

public sealed class CreateReservationCommandHandler : IRequestHandler<CreateReservationCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;
    private readonly IEnumerable<IReservationPreparationStrategy> _strategies;
    private readonly ILogger<CreateReservationCommandHandler> _logger;

    public CreateReservationCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        TimeProvider clock,
        ILogger<CreateReservationCommandHandler> logger,
        IEnumerable<IReservationPreparationStrategy> strategies)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _strategies = strategies;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating a new reservation for unit {UnitId} by customer {CustomerId}.", request.UnitId, _currentUser.CustomerId);

        _logger.LogDebug("Checking if the unit exists in the database.");
        var unit = await _db.BaseUnits.AsNoTracking()
            .Where(unit => unit.Id == request.UnitId)
            .Select(unit => new { unit.Id, unit.Project.ProjectType })
            .SingleOrDefaultAsync(cancellationToken);

        if (unit is null)
        {
            _logger.LogWarning("The selected unit {UnitId} does not exist.", request.UnitId);
            return Result<Guid>.Failure(ReservationErrors.Conflict("The selected unit does not exist."));
        }

        _logger.LogDebug("Checking for existing processing reservations for customer {CustomerId} and unit {UnitId}.", _currentUser.CustomerId, unit.Id);
        var hasProcessingReservation = await _db.Reservations
            .AsNoTracking()
            .AnyAsync(reservation => 
                reservation.CustomerId == _currentUser.CustomerId!.Value && 
                reservation.UnitId == unit.Id && 
                reservation.Status == ReservationStatus.Processing, cancellationToken);

        if (hasProcessingReservation)
        {
            _logger.LogWarning("Customer {CustomerId} already has a processing reservation for unit {UnitId}.", _currentUser.CustomerId, unit.Id);
            return Result<Guid>.Failure(ReservationErrors.AlreadyActive);
        }

        _logger.LogDebug("Checking if the unit {UnitId} is sold out based on existing orders.", unit.Id);
        // The approved availability rule applies to all units: every non-Pending order blocks a new reservation.
        var isUnitSoldOut = await _db.Orders
            .AsNoTracking()
            .AnyAsync(order => order.Reservation.UnitId == unit.Id && order.Status != OrderStatus.Pending, cancellationToken);

        if (isUnitSoldOut)
        {
            _logger.LogWarning("The unit {UnitId} is sold out and cannot be reserved.", unit.Id);
            return Result<Guid>.Failure(ReservationErrors.Conflict("The unit is unavailable."));
        }

        _logger.LogInformation("Creating a new reservation entity for unit {UnitId} and customer {CustomerId}.", unit.Id, _currentUser.CustomerId);
        var entity = new Reservation
        {
            Id = Guid.NewGuid(),
            CustomerId = _currentUser.CustomerId!.Value,
            UnitId = unit.Id,
            Number = "R-" + Guid.NewGuid().ToString("N"),
            FullName = request.FullName,
            Phone = request.Phone,
            ReservationDate = _clock.GetUtcNow(),
            Status = ReservationStatus.Processing
        };

        var strategy = _strategies.Single(strategy => strategy.Type == unit.ProjectType);
        var preparation = await strategy.PrepareAsync(entity, request, cancellationToken);
        if (preparation.IsFailure)
        {
            return Result<Guid>.Failure(preparation.Error);
        }

        _logger.LogInformation("Adding the new reservation entity to the database context.");
        _db.Reservations.Add(entity);

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully created a new reservation with ID {ReservationId} for unit {UnitId}.", entity.Id, unit.Id);
        return Result<Guid>.Success(entity.Id);
    }
}
