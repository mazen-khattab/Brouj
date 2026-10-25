using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Reservations.Errors;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Reservations.Commands.Create;

public sealed class CreateReservationCommandHandler : IRequestHandler<CreateReservationCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;

    public CreateReservationCommandHandler(IApplicationDbContext db, ICurrentUser currentUser, TimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
    {
        var unit = await _db.BaseUnits.AsNoTracking()
            .SingleOrDefaultAsync(unit => unit.Id == request.UnitId, cancellationToken);

        if (unit is null)
        {
            return Result<Guid>.Failure(ReservationErrors.Conflict("The selected unit does not exist."));
        }

        if (await _db.Reservations.AsNoTracking().AnyAsync(reservation => reservation.CustomerId == _currentUser.CustomerId!.Value && reservation.UnitId == unit.Id && reservation.Status == ReservationStatus.Processing, cancellationToken))
        {
            return Result<Guid>.Failure(ReservationErrors.AlreadyActive);
        }

        // The approved availability rule applies to all units: every non-Pending order blocks a new reservation.

        if (await _db.Orders.AsNoTracking().AnyAsync(order => order.Reservation.UnitId == unit.Id && order.Status != OrderStatus.Pending, cancellationToken))
        {
            return Result<Guid>.Failure(ReservationErrors.Conflict("The unit is unavailable."));
        }

        var entity = new Reservation
        {
            Id = Guid.NewGuid(),
            CustomerId = _currentUser.CustomerId!.Value,
            UnitId = unit.Id,
            Number = "R-" + Guid.NewGuid().ToString("N"),
            ReservationDate = _clock.GetUtcNow(),
            Status = ReservationStatus.Processing
        };

        _db.Reservations.Add(entity);

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
