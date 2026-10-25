using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Projects.Errors;
using Brouj.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Projects.Commands.ReplaceAmenities;

public sealed class ReplaceProjectAmenitiesCommandHandler : IRequestHandler<ReplaceProjectAmenitiesCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public ReplaceProjectAmenitiesCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(ReplaceProjectAmenitiesCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.BaseProjects.AsNoTracking().AnyAsync(entity => entity.Id == request.ProjectId, cancellationToken))
        {
            return Result<Guid>.Failure(ProjectErrors.NotFound);
        }

        var previous = await _db.ProjectAmenities.Where(amenity => amenity.ProjectId == request.ProjectId).ToListAsync(cancellationToken);

        _db.ProjectAmenities.RemoveRange(previous);

        // One SaveChanges keeps replacement and auditing atomic.
        _db.ProjectAmenities.AddRange(request.Amenities.Select(input => new ProjectAmenity
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            Name = input.Name.Trim(),
            Value = input.Value
        }));

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(request.ProjectId);
    }
}
