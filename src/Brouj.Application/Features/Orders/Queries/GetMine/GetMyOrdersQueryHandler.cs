using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.Orders.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Orders.Queries.GetMine;

public sealed class GetMyOrdersQueryHandler : IRequestHandler<GetMyOrdersQuery, Result<PaginationResult<OrderDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyOrdersQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PaginationResult<OrderDto>>> Handle(
        GetMyOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Orders.AsNoTracking();

        query = query.Where(order => order.Reservation.CustomerId == _currentUser.CustomerId!.Value);

        if (request.Status is not null)
        {
            query = query.Where(order => order.Status == request.Status);
        }

        var page = await Paging.ToPageAsync(query.OrderByDescending(order => order.CreatedAt).ThenBy(order => order.Id).Select(order => new OrderDto
        {
            Id = order.Id,
            ReservationId = order.ReservationId,
            UnitId = order.Reservation.UnitId,
            UnitNumber = order.Reservation.Unit.Number,
            ProjectName = order.Reservation.Unit.Project.Name,
            Number = order.Number,
            Status = order.Status,
            TotalPrice = order.TotalPrice,
            CompletedAt = order.CompletedAt
        }), request.PageNumber, request.PageSize, cancellationToken);

        return Result<PaginationResult<OrderDto>>.Success(page);
    }
}
