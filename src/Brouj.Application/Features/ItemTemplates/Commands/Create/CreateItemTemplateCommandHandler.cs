using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.ItemTemplates.Errors;
using Brouj.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.ItemTemplates.Commands.Create;

public sealed class CreateItemTemplateCommandHandler : IRequestHandler<CreateItemTemplateCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public CreateItemTemplateCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateItemTemplateCommand request, CancellationToken cancellationToken)
    {
        var entity = new ItemTemplate { Id = Guid.NewGuid(), IsActive = false };

        if (await _db.ItemTemplates.AsNoTracking().AnyAsync(template => template.Id != entity.Id && template.Code == request.Input.Code.Trim(), cancellationToken))
        {
            return Result<Guid>.Failure(ItemTemplateErrors.Conflict("The template code is already used."));
        }

        entity.Name = request.Input.Name.Trim();
        entity.Code = request.Input.Code.Trim();

        _db.ItemTemplates.Add(entity);

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
