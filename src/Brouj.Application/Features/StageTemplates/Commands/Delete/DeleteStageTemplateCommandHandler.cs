using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.StageTemplates.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.StageTemplates.Commands.Delete;

public sealed class DeleteStageTemplateCommandHandler : IRequestHandler<DeleteStageTemplateCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public DeleteStageTemplateCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(DeleteStageTemplateCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.StageTemplates.SingleOrDefaultAsync(template => template.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(StageTemplateErrors.NotFound);
        }

        // Only template mappings cascade; time-plan snapshots have no link to this source.
        _db.StageTemplates.Remove(entity);

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
