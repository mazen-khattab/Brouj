using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.ItemTemplates.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.ItemTemplates.Commands.SetActive;

public sealed class SetItemTemplateActiveCommandHandler : IRequestHandler<SetItemTemplateActiveCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public SetItemTemplateActiveCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(SetItemTemplateActiveCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.ItemTemplates.SingleOrDefaultAsync(template => template.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(ItemTemplateErrors.NotFound);
        }

        entity.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
