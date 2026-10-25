using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Projects.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Projects.Commands.UpdateImage;

public sealed class UpdateProjectImageCommandHandler : IRequestHandler<UpdateProjectImageCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public UpdateProjectImageCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(UpdateProjectImageCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.ProjectImages.SingleOrDefaultAsync(image => image.Id == request.Id && image.ProjectId == request.ProjectId, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(ProjectErrors.NotFound);
        }

        entity.AltText = request.AltText;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
