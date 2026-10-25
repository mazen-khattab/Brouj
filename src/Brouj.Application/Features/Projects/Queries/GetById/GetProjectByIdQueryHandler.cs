using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Projects.DTOs;
using Brouj.Application.Features.Projects.Errors;
using Brouj.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Projects.Queries.GetById;

public sealed class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, Result<ProjectDto>>
{
    private readonly IApplicationDbContext _db;

    public GetProjectByIdQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ProjectDto>> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _db.BaseProjects.AsNoTracking().Where(project => project.Id == request.Id)
            .Select(project => new ProjectDto
            {
                Id = project.Id,
                Name = project.Name,
                AreaId = project.AreaId,
                Description = project.Description,
                Latitude = project.Latitude,
                Longitude = project.Longitude,
                Size = project.Size,
                ProjectType = project.ProjectType,
                Price = project.Price,
                FloorCount = project.FloorCount,
                PlannedUnitCount = project.PlannedUnitCount,
                NeighborPrice = project.InitiativeProject == null ? null : project.InitiativeProject.NeighborPrice,
                DrawDate = project.InitiativeProject == null ? null : project.InitiativeProject.DrawDate
            }).SingleOrDefaultAsync(cancellationToken);

        if (dto is null)
        {
            return Result<ProjectDto>.Failure(ProjectErrors.NotFound);
        }

        // Load independent child collections separately to avoid a cartesian product.
        dto.Amenities = await _db.ProjectAmenities.AsNoTracking()
            .Where(amenity => amenity.ProjectId == request.Id)
            .OrderBy(amenity => amenity.Name).ThenBy(amenity => amenity.Id)
            .Select(amenity => new AmenityDto { Name = amenity.Name, Value = amenity.Value })
            .ToArrayAsync(cancellationToken);
        dto.Images = await _db.ProjectImages.AsNoTracking()
            .Where(image => image.ProjectId == request.Id).OrderBy(image => image.Id)
            .Select(image => new ImageMetadataDto { Id = image.Id, ImagePath = image.ImagePath, AltText = image.AltText })
            .ToArrayAsync(cancellationToken);

        return Result<ProjectDto>.Success(dto);
    }
}
