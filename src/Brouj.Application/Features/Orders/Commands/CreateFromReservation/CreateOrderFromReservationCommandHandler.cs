using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.Orders.Errors;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Orders.Commands.CreateFromReservation;

public sealed class CreateOrderFromReservationCommandHandler : IRequestHandler<CreateOrderFromReservationCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public CreateOrderFromReservationCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateOrderFromReservationCommand request, CancellationToken cancellationToken)
    {
        var unitId = await _db.Reservations.AsNoTracking()
            .Where(reservation => reservation.Id == request.ReservationId)
            .Select(reservation => (Guid?)reservation.UnitId).SingleOrDefaultAsync(cancellationToken);

        if (unitId is null)
        {
            return Result<Guid>.Failure(OrderErrors.Conflict("The selected reservation does not exist."));
        }

        // Read mutable reservation/order state only after obtaining the cross-instance unit lock.
        await _db.LockUnitForOrderAsync(unitId.Value, cancellationToken);

        var reservation = await _db.Reservations.SingleOrDefaultAsync(
            reservation => reservation.Id == request.ReservationId, cancellationToken);

        if (reservation is null)
        {
            return Result<Guid>.Failure(OrderErrors.Conflict("The selected reservation does not exist."));
        }

        if (reservation.Status != ReservationStatus.Processing)
        {
            return Result<Guid>.Failure(OrderErrors.Conflict("The reservation must be Processing."));
        }

        if (await _db.Orders.AsNoTracking().AnyAsync(order => order.ReservationId == reservation.Id, cancellationToken))
        {
            return Result<Guid>.Failure(OrderErrors.Conflict("The reservation already has an order."));
        }

        var unit = await _db.BaseUnits.AsNoTracking().Where(unit => unit.Id == reservation.UnitId)
            .Select(unit => new { unit.Size, unit.MeterPrice, unit.Project.ProjectType })
            .SingleAsync(cancellationToken);

        // Regular units permit one order of any status. Initiative units can have multiple Pending orders.
        var isRegular = unit.ProjectType == ProjectType.Regular;

        if (await _db.Orders.AsNoTracking().AnyAsync(order => order.Reservation.UnitId == reservation.UnitId &&
            (isRegular || order.Status != OrderStatus.Pending), cancellationToken))
        {
            return Result<Guid>.Failure(OrderErrors.Conflict("The unit is unavailable."));
        }

        var entity = new Order
        {
            Id = Guid.NewGuid(),
            ReservationId = reservation.Id,
            Reservation = reservation,
            Number = "O-" + Guid.NewGuid().ToString("N"),
            TotalPrice = Pricing.Total(unit.Size, unit.MeterPrice),
            Status = unit.ProjectType == ProjectType.Initiative ? OrderStatus.Pending : OrderStatus.Processing
        };

        // Confirmation and its order are saved atomically, including staff audit records.
        reservation.Status = ReservationStatus.Confirmed;
        _db.Orders.Add(entity);

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
