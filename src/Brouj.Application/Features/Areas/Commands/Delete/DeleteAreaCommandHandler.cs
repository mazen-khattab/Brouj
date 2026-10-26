using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Areas.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Areas.Commands.Delete;

public sealed class DeleteAreaCommandHandler : IRequestHandler<DeleteAreaCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public DeleteAreaCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(DeleteAreaCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Areas.SingleOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(AreaErrors.NotFound);
        }

        if (await _db.BaseProjects.AsNoTracking().AnyAsync(project => project.AreaId == entity.Id, cancellationToken))
        {
            return Result<Guid>.Failure(AreaErrors.Conflict("The resource still has dependent projects."));
        }

        // The approved database cascade removes area amenities.
        _db.Areas.Remove(entity);

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
