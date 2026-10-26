using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.StageTemplates.Errors;
using Brouj.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.StageTemplates.Commands.SetItems;

public sealed class SetStageTemplateItemsCommandHandler : IRequestHandler<SetStageTemplateItemsCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public SetStageTemplateItemsCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(SetStageTemplateItemsCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.StageTemplates.AsNoTracking().AnyAsync(template => template.Id == request.Id, cancellationToken))
        {
            return Result<Guid>.Failure(StageTemplateErrors.NotFound);
        }

        var ids = request.Items.Select(item => item.ItemTemplateId).ToArray();
        var count = await _db.ItemTemplates.AsNoTracking().CountAsync(template => ids.Contains(template.Id), cancellationToken);

        if (count != ids.Length)
        {
            return Result<Guid>.Failure(StageTemplateErrors.Conflict("Every item template must exist."));
        }

        var previous = await _db.StageTemplateItems.Where(item => item.StageTemplateId == request.Id)
            .ToListAsync(cancellationToken);

        // Delete first inside the outer transaction to release both approved unique mapping indexes.
        _db.StageTemplateItems.RemoveRange(previous);
        await _db.SaveChangesAsync(cancellationToken);

        _db.StageTemplateItems.AddRange(request.Items.Select(item => new StageTemplateItem
        {
            Id = Guid.NewGuid(),
            StageTemplateId = request.Id,
            ItemTemplateId = item.ItemTemplateId,
            SortOrder = item.SortOrder
        }));

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(request.Id);
    }
}
