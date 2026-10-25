using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.Reservations.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Reservations.Queries.GetMine;

public sealed class GetMyReservationsQueryHandler : IRequestHandler<GetMyReservationsQuery, Result<PaginationResult<ReservationDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyReservationsQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PaginationResult<ReservationDto>>> Handle(
        GetMyReservationsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Reservations.AsNoTracking();

        query = query.Where(reservation => reservation.CustomerId == _currentUser.CustomerId!.Value);

        if (request.Status is not null)
        {
            query = query.Where(reservation => reservation.Status == request.Status);
        }

        var page = await Paging.ToPageAsync(query.OrderByDescending(reservation => reservation.ReservationDate).ThenBy(reservation => reservation.Id).Select(reservation => new ReservationDto
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
        }), request.PageNumber, request.PageSize, cancellationToken);

        return Result<PaginationResult<ReservationDto>>.Success(page);
    }
}
