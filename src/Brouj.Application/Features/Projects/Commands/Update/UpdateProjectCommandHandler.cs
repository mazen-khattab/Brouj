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
        await _db.LockProjectAsync(request.Id, cancellationToken);
        var baseProject = await _db.BaseProjects.SingleOrDefaultAsync(project => project.Id == request.Id, cancellationToken);

        if (baseProject is null)
        {
            return Result<Guid>.Failure(ProjectErrors.NotFound);
        }

        var hasUnits = await _db.BaseUnits
            .AsNoTracking()
            .AnyAsync(unit => unit.ProjectId == baseProject.Id, cancellationToken);

        // The project type can only change before any units exist, otherwise it would invalidate existing unit percentages.
        if (hasUnits && baseProject.ProjectType != request.Input.ProjectType)
        {
            return Result<Guid>.Failure(ProjectErrors.Conflict("Project type can change only before any units exist."));
        }

        var initiative = await _db.InitiativeProjects
            .SingleOrDefaultAsync(project => project.ProjectId == baseProject.Id, cancellationToken);

        var hasArea = await _db.Areas
            .AsNoTracking()
            .AnyAsync(area => area.Id == request.Input.AreaId, cancellationToken);

        if (!hasArea)
        {
            return Result<Guid>.Failure(ProjectErrors.Conflict("The selected area does not exist."));
        }

        // Validate every recalculated percentage before changing any tracked values.
        var units = await _db.BaseUnits
            .AsNoTracking()
            .Where(unit => unit.ProjectId == baseProject.Id)
            .Select(unit => new { unit.Id, unit.Size, unit.MeterPrice })
            .ToListAsync(cancellationToken);

        if (request.Input.ProjectType == ProjectType.Initiative && units.Any(unit => Pricing.Total(unit.Size, unit.MeterPrice) > request.Input.Price))
        {
            return Result<Guid>.Failure(ProjectErrors.Validation("Unit percentages must remain between 0 and 100."));
        }

        baseProject.Name = request.Input.Name.Trim();
        baseProject.AreaId = request.Input.AreaId;
        baseProject.Description = request.Input.Description;
        baseProject.Latitude = request.Input.Latitude;
        baseProject.Longitude = request.Input.Longitude;
        baseProject.Size = request.Input.Size;
        baseProject.ProjectType = request.Input.ProjectType;
        baseProject.Price = request.Input.Price;
        baseProject.FloorCount = request.Input.FloorCount;
        baseProject.PlannedUnitCount = request.Input.PlannedUnitCount;

        // These values are deliberately server controlled while the approved garage rule is in effect.
        baseProject.IsGarage = true;
        baseProject.GaragePrice = 0m;

        if (baseProject.ProjectType == ProjectType.Initiative)
        {
            if (initiative is null)
            {
                initiative = new InitiativeProject { ProjectId = baseProject.Id, Project = baseProject };
                _db.InitiativeProjects.Add(initiative);
            }

            initiative.NeighborPrice = request.Input.NeighborPrice!.Value;
            initiative.DrawDate = request.Input.DrawDate!.Value;
        }
        else if (initiative is not null)
        {
            _db.InitiativeProjects.Remove(initiative);
        }

        if (baseProject.ProjectType == ProjectType.Initiative)
        {
            var children = await _db.InitiativeUnits
                .Where(unit => unit.Unit.ProjectId == baseProject.Id)
                .ToListAsync(cancellationToken);

            var totals = units.ToDictionary(unit => unit.Id, unit => Pricing.Total(unit.Size, unit.MeterPrice));

            foreach (var child in children)
            {
                child.Percentage = Pricing.Percentage(totals[child.UnitId], baseProject.Price);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(baseProject.Id);
    }
}
