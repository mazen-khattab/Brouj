using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Reservations.DTOs;
using Brouj.Application.Features.Reservations.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Reservations.Queries.GetById;

public sealed class GetReservationByIdQueryHandler : IRequestHandler<GetReservationByIdQuery, Result<ReservationDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetReservationByIdQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<ReservationDto>> Handle(
        GetReservationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Reservations.AsNoTracking().Where(reservation => reservation.Id == request.Id);

        query = query.Where(reservation => reservation.CustomerId == _currentUser.CustomerId!.Value);

        var dto = await query.Select(reservation => new ReservationDto
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
        }).SingleOrDefaultAsync(cancellationToken);

        if (dto is null)
        {
            return Result<ReservationDto>.Failure(ReservationErrors.NotFound);
        }

        return Result<ReservationDto>.Success(dto);
    }
}
