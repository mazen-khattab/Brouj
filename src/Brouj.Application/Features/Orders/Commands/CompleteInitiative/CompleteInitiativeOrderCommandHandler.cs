using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Orders.Errors;
using Brouj.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Orders.Commands.CompleteInitiative;

public sealed class CompleteInitiativeOrderCommandHandler : IRequestHandler<CompleteInitiativeOrderCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly TimeProvider _clock;

    public CompleteInitiativeOrderCommandHandler(IApplicationDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(CompleteInitiativeOrderCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Orders.SingleOrDefaultAsync(order => order.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(OrderErrors.NotFound);
        }

        var context = await _db.Reservations.AsNoTracking().Where(reservation => reservation.Id == entity.ReservationId)
            .Select(reservation => new { reservation.UnitId, reservation.Unit.Project.ProjectType })
            .SingleAsync(cancellationToken);

        if (context.ProjectType != ProjectType.Initiative)
        {
            return Result<Guid>.Failure(OrderErrors.Conflict("Only initiative orders use this completion flow."));
        }

        if (entity.Status == OrderStatus.Completed)
        {
            return Result<Guid>.Failure(OrderErrors.Conflict("The order is already completed."));
        }

        if (entity.Status != OrderStatus.Pending)
        {
            return Result<Guid>.Failure(OrderErrors.Conflict("Only a pending initiative order can be selected."));
        }

        var others = await _db.Orders.Where(order => order.Id != entity.Id && order.Reservation.UnitId == context.UnitId)
            .ToListAsync(cancellationToken);

        if (others.Any(order => order.Status == OrderStatus.Completed))
        {
            return Result<Guid>.Failure(OrderErrors.Conflict("An initiative unit already has a winner."));
        }

        entity.Status = OrderStatus.Completed;
        entity.CompletedAt = _clock.GetUtcNow();

        foreach (var other in others)
        {
            other.Status = OrderStatus.Cancelled;
            other.CompletedAt = null;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
