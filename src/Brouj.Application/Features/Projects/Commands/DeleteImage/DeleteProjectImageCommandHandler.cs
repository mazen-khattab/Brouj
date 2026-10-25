using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Services;
using Brouj.Application.Features.Projects.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Projects.Commands.DeleteImage;

public sealed class DeleteProjectImageCommandHandler : IRequestHandler<DeleteProjectImageCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly PendingFileCleanup _cleanup;

    public DeleteProjectImageCommandHandler(IApplicationDbContext db, PendingFileCleanup cleanup)
    {
        _db = db;
        _cleanup = cleanup;
    }

    public async Task<Result<Guid>> Handle(DeleteProjectImageCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.ProjectImages.SingleOrDefaultAsync(image => image.Id == request.Id && image.ProjectId == request.ProjectId, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(ProjectErrors.NotFound);
        }

        _cleanup.Add(entity.ImagePath);
        _db.ProjectImages.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
