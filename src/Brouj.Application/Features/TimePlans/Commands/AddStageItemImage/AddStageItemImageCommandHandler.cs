using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Services;
using Brouj.Application.Features.TimePlans.Errors;
using Brouj.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.TimePlans.Commands.AddStageItemImage;

public sealed class AddStageItemImageCommandHandler : IRequestHandler<AddStageItemImageCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly FileUpload _uploads;

    public AddStageItemImageCommandHandler(IApplicationDbContext db, FileUpload uploads)
    {
        _db = db;
        _uploads = uploads;
    }

    public async Task<Result<Guid>> Handle(AddStageItemImageCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.PlanStageItems.AsNoTracking().AnyAsync(parent => parent.Id == request.ItemId, cancellationToken))
        {
            return Result<Guid>.Failure(TimePlanErrors.NotFound);
        }

        var order = await _db.StageItemImages.Where(image => image.ItemId == request.ItemId)
            .Select(image => (int?)image.SortOrder).MaxAsync(cancellationToken) ?? 0;

        var path = await _uploads.SaveAsync(request.File, cancellationToken);
        var entity = new StageItemImage
        {
            Id = Guid.NewGuid(),
            ItemId = request.ItemId,
            ImagePath = path,
            Description = request.Description,
            SortOrder = order + 1
        };

        try
        {
            _db.StageItemImages.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _uploads.RemoveFailedAsync(path);

            throw;
        }

        return Result<Guid>.Success(entity.Id);
    }
}
