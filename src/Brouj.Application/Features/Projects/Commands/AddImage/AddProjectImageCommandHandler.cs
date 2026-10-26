using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Services;
using Brouj.Application.Features.Projects.Errors;
using Brouj.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Projects.Commands.AddImage;

public sealed class AddProjectImageCommandHandler : IRequestHandler<AddProjectImageCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly FileUpload _uploads;

    public AddProjectImageCommandHandler(IApplicationDbContext db, FileUpload uploads)
    {
        _db = db;
        _uploads = uploads;
    }

    public async Task<Result<Guid>> Handle(AddProjectImageCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.BaseProjects.AsNoTracking().AnyAsync(parent => parent.Id == request.ProjectId, cancellationToken))
        {
            return Result<Guid>.Failure(ProjectErrors.NotFound);
        }

        var path = await _uploads.SaveAsync(request.File, cancellationToken);
        var entity = new ProjectImage
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            ImagePath = path,
            AltText = request.AltText
        };

        try
        {
            _db.ProjectImages.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _uploads.RemoveFailedAsync(path);

            throw;
        }

        return Result<Guid>.Success(entity.Id);
    }
}
