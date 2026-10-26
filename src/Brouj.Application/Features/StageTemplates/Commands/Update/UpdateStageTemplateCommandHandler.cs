using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.StageTemplates.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.StageTemplates.Commands.Update;

public sealed class UpdateStageTemplateCommandHandler : IRequestHandler<UpdateStageTemplateCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public UpdateStageTemplateCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(UpdateStageTemplateCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.StageTemplates.SingleOrDefaultAsync(template => template.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(StageTemplateErrors.NotFound);
        }

        if (await _db.StageTemplates.AsNoTracking().AnyAsync(template => template.Id != entity.Id && template.Code == request.Input.Code.Trim(), cancellationToken))
        {
            return Result<Guid>.Failure(StageTemplateErrors.Conflict("The template code is already used."));
        }

        entity.Name = request.Input.Name.Trim();
        entity.Code = request.Input.Code.Trim();

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
