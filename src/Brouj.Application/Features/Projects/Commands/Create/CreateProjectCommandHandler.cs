using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Projects.Errors;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Projects.Commands.Create;

public sealed class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public CreateProjectCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var entity = new BaseProject { Id = Guid.NewGuid() };

        if (!await _db.Areas.AsNoTracking().AnyAsync(area => area.Id == request.Input.AreaId, cancellationToken))
        {
            return Result<Guid>.Failure(ProjectErrors.Conflict("The selected area does not exist."));
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
            var initiative = new InitiativeProject
            {
                ProjectId = entity.Id,
                Project = entity,
                NeighborPrice = request.Input.NeighborPrice!.Value,
                DrawDate = request.Input.DrawDate!.Value
            };

            _db.InitiativeProjects.Add(initiative);
        }

        _db.BaseProjects.Add(entity);

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
