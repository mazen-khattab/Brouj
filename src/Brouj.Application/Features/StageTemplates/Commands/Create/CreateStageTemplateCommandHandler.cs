using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.StageTemplates.Errors;
using Brouj.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.StageTemplates.Commands.Create;

public sealed class CreateStageTemplateCommandHandler : IRequestHandler<CreateStageTemplateCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public CreateStageTemplateCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateStageTemplateCommand request, CancellationToken cancellationToken)
    {
        var entity = new StageTemplate { Id = Guid.NewGuid(), IsActive = false };

        if (await _db.StageTemplates.AsNoTracking().AnyAsync(template => template.Id != entity.Id && template.Code == request.Input.Code.Trim(), cancellationToken))
        {
            return Result<Guid>.Failure(StageTemplateErrors.Conflict("The template code is already used."));
        }

        entity.Name = request.Input.Name.Trim();
        entity.Code = request.Input.Code.Trim();

        _db.StageTemplates.Add(entity);

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
