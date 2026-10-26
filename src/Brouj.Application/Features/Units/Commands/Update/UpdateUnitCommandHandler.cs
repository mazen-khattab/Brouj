using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.Units.Errors;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Units.Commands.Update;

public sealed class UpdateUnitCommandHandler : IRequestHandler<UpdateUnitCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public UpdateUnitCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(UpdateUnitCommand request, CancellationToken cancellationToken)
    {
        var project = await _db.BaseProjects.AsNoTracking()
            .SingleOrDefaultAsync(project => project.Id == request.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result<Guid>.Failure(UnitErrors.Conflict("The selected project does not exist."));
        }

        var entity = await _db.BaseUnits.SingleOrDefaultAsync(unit => unit.Id == request.Id && unit.ProjectId == request.ProjectId, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(UnitErrors.NotFound);
        }

        var total = Pricing.Total(request.Input.Size, request.Input.MeterPrice);
        var isInitiative = project.ProjectType == ProjectType.Initiative;

        if (isInitiative && (project.Price <= 0 || total > project.Price || request.Input.NeighborMeterPrice is null))
        {
            return Result<Guid>.Failure(UnitErrors.Validation("Initiative units require a display neighbor price and a percentage between 0 and 100."));
        }

        if (!isInitiative && request.Input.NeighborMeterPrice is not null)
        {
            return Result<Guid>.Failure(UnitErrors.Validation("Neighbor meter price applies only to initiative units."));
        }

        if (await _db.BaseUnits.AsNoTracking().AnyAsync(unit => unit.ProjectId == project.Id && unit.Id != entity.Id && unit.Number == request.Input.Number.Trim(), cancellationToken))
        {
            return Result<Guid>.Failure(UnitErrors.Conflict("The unit number is already used in this project."));
        }

        entity.Number = request.Input.Number.Trim();
        entity.MeterPrice = request.Input.MeterPrice;
        entity.FloorNumber = request.Input.FloorNumber;
        entity.Size = request.Input.Size;

        if (isInitiative)
        {
            var initiative = await _db.InitiativeUnits.SingleOrDefaultAsync(unit => unit.UnitId == entity.Id, cancellationToken);

            if (initiative is null)
            {
                initiative = new InitiativeUnit { UnitId = entity.Id, Unit = entity };
                _db.InitiativeUnits.Add(initiative);
            }

            initiative.NeighborMeterPrice = request.Input.NeighborMeterPrice!.Value;
            initiative.Percentage = Pricing.Percentage(total, project.Price);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
