using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.StageTemplates.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.StageTemplates.Commands.SetActive;

public sealed class SetStageTemplateActiveCommandHandler : IRequestHandler<SetStageTemplateActiveCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public SetStageTemplateActiveCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(SetStageTemplateActiveCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.StageTemplates.SingleOrDefaultAsync(template => template.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(StageTemplateErrors.NotFound);
        }

        entity.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
