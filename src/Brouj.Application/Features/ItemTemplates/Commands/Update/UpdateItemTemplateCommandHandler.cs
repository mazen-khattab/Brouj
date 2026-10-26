using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.ItemTemplates.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.ItemTemplates.Commands.Update;

public sealed class UpdateItemTemplateCommandHandler : IRequestHandler<UpdateItemTemplateCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public UpdateItemTemplateCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(UpdateItemTemplateCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.ItemTemplates.SingleOrDefaultAsync(template => template.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(ItemTemplateErrors.NotFound);
        }

        if (await _db.ItemTemplates.AsNoTracking().AnyAsync(template => template.Id != entity.Id && template.Code == request.Input.Code.Trim(), cancellationToken))
        {
            return Result<Guid>.Failure(ItemTemplateErrors.Conflict("The template code is already used."));
        }

        entity.Name = request.Input.Name.Trim();
        entity.Code = request.Input.Code.Trim();

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
