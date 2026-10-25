using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.Projects.Errors;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Projects.Commands.Update;

public sealed class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public UpdateProjectCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.BaseProjects.SingleOrDefaultAsync(project => project.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(ProjectErrors.NotFound);
        }

        var hasUnits = await _db.BaseUnits.AsNoTracking().AnyAsync(unit => unit.ProjectId == entity.Id, cancellationToken);

        if (hasUnits && entity.ProjectType != request.Input.ProjectType)
        {
            return Result<Guid>.Failure(ProjectErrors.Conflict("Project type can change only before any units exist."));
        }

        var initiative = await _db.InitiativeProjects.SingleOrDefaultAsync(project => project.ProjectId == entity.Id, cancellationToken);

        if (!await _db.Areas.AsNoTracking().AnyAsync(area => area.Id == request.Input.AreaId, cancellationToken))
        {
            return Result<Guid>.Failure(ProjectErrors.Conflict("The selected area does not exist."));
        }

        // Validate every recalculated percentage before changing any tracked values.
        var units = await _db.BaseUnits.AsNoTracking().Where(unit => unit.ProjectId == entity.Id)
            .Select(unit => new { unit.Id, unit.Size, unit.MeterPrice }).ToListAsync(cancellationToken);

        if (request.Input.ProjectType == ProjectType.Initiative && units.Any(unit => Pricing.Total(unit.Size, unit.MeterPrice) > request.Input.Price))
        {
            return Result<Guid>.Failure(ProjectErrors.Validation("Unit percentages must remain between 0 and 100."));
        }

        entity.Name = request.Input.Name.Trim();
        entity.AreaId = request.Input.AreaId;
        entity.Description = request.Input.Description;
        entity.Latitude = request.Input.Latitude;
        entity.Longitude = request.Input.Longitude;
        entity.Size = request.Input.Size;
        entity.ProjectType = request.Input.ProjectType;
        entity.Price = request.Input.Price;
        entity.FloorCount = request.Input.FloorCount;
        entity.PlannedUnitCount = request.Input.PlannedUnitCount;

        // These values are deliberately server controlled while the approved garage rule is in effect.
        entity.IsGarage = true;
        entity.GaragePrice = 0m;

        if (entity.ProjectType == ProjectType.Initiative)
        {
            if (initiative is null)
            {
                initiative = new InitiativeProject { ProjectId = entity.Id, Project = entity };
                _db.InitiativeProjects.Add(initiative);
            }

            initiative.NeighborPrice = request.Input.NeighborPrice!.Value;
            initiative.DrawDate = request.Input.DrawDate!.Value;
        }
        else if (initiative is not null)
        {
            _db.InitiativeProjects.Remove(initiative);
        }

        if (entity.ProjectType == ProjectType.Initiative)
        {
            var children = await _db.InitiativeUnits.Where(unit => unit.Unit.ProjectId == entity.Id)
                .ToListAsync(cancellationToken);
            var totals = units.ToDictionary(unit => unit.Id, unit => Pricing.Total(unit.Size, unit.MeterPrice));

            foreach (var child in children)
            {
                child.Percentage = Pricing.Percentage(totals[child.UnitId], entity.Price);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
