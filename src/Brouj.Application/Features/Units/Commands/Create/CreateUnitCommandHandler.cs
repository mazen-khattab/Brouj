using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.Units.Errors;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Units.Commands.Create;

public sealed class CreateUnitCommandHandler : IRequestHandler<CreateUnitCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public CreateUnitCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateUnitCommand request, CancellationToken cancellationToken)
    {
        var project = await _db.BaseProjects.AsNoTracking()
            .SingleOrDefaultAsync(project => project.Id == request.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result<Guid>.Failure(UnitErrors.Conflict("The selected project does not exist."));
        }

        var entity = new BaseUnit { Id = Guid.NewGuid(), ProjectId = project.Id };
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
            var initiative = new InitiativeUnit
            {
                UnitId = entity.Id,
                Unit = entity,
                NeighborMeterPrice = request.Input.NeighborMeterPrice!.Value,
                Percentage = Pricing.Percentage(total, project.Price)
            };

            _db.InitiativeUnits.Add(initiative);
        }

        _db.BaseUnits.Add(entity);

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
