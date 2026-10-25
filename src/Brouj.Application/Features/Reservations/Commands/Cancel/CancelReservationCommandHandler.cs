using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Reservations.Errors;
using Brouj.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Reservations.Commands.Cancel;

public sealed class CancelReservationCommandHandler : IRequestHandler<CancelReservationCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;

    public CancelReservationCommandHandler(IApplicationDbContext db, ICurrentUser currentUser, TimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(CancelReservationCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Reservations.SingleOrDefaultAsync(
            reservation => reservation.Id == request.Id && reservation.CustomerId == _currentUser.CustomerId!.Value, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(ReservationErrors.NotFound);
        }

        if (entity.Status == ReservationStatus.Cancelled)
        {
            return Result<Guid>.Failure(ReservationErrors.AlreadyCancelled);
        }

        if (entity.Status != ReservationStatus.Processing || await _db.Orders.AsNoTracking().AnyAsync(order => order.ReservationId == entity.Id, cancellationToken))
        {
            return Result<Guid>.Failure(ReservationErrors.Conflict("Only a Processing reservation without an order can be cancelled."));
        }

        entity.Status = ReservationStatus.Cancelled;
        entity.CancelledAt = _clock.GetUtcNow();

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
