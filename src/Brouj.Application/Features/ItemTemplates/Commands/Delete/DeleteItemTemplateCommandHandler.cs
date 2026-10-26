using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.ItemTemplates.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.ItemTemplates.Commands.Delete;

public sealed class DeleteItemTemplateCommandHandler : IRequestHandler<DeleteItemTemplateCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public DeleteItemTemplateCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(DeleteItemTemplateCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.ItemTemplates.SingleOrDefaultAsync(template => template.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(ItemTemplateErrors.NotFound);
        }

        // Only template mappings cascade; time-plan snapshots have no link to this source.
        _db.ItemTemplates.Remove(entity);

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
