using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.Units.DTOs;
using Brouj.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Units.Queries.GetByProject;

public sealed class GetUnitsByProjectQueryHandler : IRequestHandler<GetUnitsByProjectQuery, Result<PaginationResult<UnitListDto>>>
{
    private readonly IApplicationDbContext _db;

    public GetUnitsByProjectQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PaginationResult<UnitListDto>>> Handle(
        GetUnitsByProjectQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.BaseUnits.AsNoTracking().Where(unit => unit.ProjectId == request.ProjectId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(unit => unit.Number.Contains(request.Search.Trim()));
        }

        var page = await Paging.ToPageAsync(query.OrderBy(unit => unit.Number).ThenBy(unit => unit.Id).Select(unit => new UnitListDto
        {
            Id = unit.Id,
            ProjectId = unit.ProjectId,
            Number = unit.Number,
            MeterPrice = unit.MeterPrice,
            FloorNumber = unit.FloorNumber,
            Size = unit.Size,
            TotalPrice = decimal.Ceiling(unit.Size * unit.MeterPrice * 100m) / 100m,
            NeighborMeterPrice = unit.InitiativeUnit == null ? null : unit.InitiativeUnit.NeighborMeterPrice,
            Percentage = unit.InitiativeUnit == null ? null : unit.InitiativeUnit.Percentage,
            IsAvailable = !unit.Reservations.Any(reservation => reservation.Order != null &&
                reservation.Order.Status != OrderStatus.Pending)
        }), request.PageNumber, request.PageSize, cancellationToken);

        return Result<PaginationResult<UnitListDto>>.Success(page);
    }
}
