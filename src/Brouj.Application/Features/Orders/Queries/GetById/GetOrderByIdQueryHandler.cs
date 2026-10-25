using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Orders.DTOs;
using Brouj.Application.Features.Orders.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Orders.Queries.GetById;

public sealed class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetOrderByIdQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Orders.AsNoTracking().Where(order => order.Id == request.Id);

        if (_currentUser.CustomerId is not null)
        {
            query = query.Where(order => order.Reservation.CustomerId == _currentUser.CustomerId!.Value);
        }

        var dto = await query.Select(order => new OrderDto
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
        }).SingleOrDefaultAsync(cancellationToken);

        if (dto is null)
        {
            return Result<OrderDto>.Failure(OrderErrors.NotFound);
        }

        return Result<OrderDto>.Success(dto);
    }
}
