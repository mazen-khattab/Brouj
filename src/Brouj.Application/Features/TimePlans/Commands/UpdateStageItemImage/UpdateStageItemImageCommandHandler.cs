using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.TimePlans.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.TimePlans.Commands.UpdateStageItemImage;

public sealed class UpdateStageItemImageCommandHandler : IRequestHandler<UpdateStageItemImageCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public UpdateStageItemImageCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(UpdateStageItemImageCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.StageItemImages.SingleOrDefaultAsync(image => image.Id == request.Id && image.ItemId == request.ItemId, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(TimePlanErrors.NotFound);
        }

        entity.Description = request.Description;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
