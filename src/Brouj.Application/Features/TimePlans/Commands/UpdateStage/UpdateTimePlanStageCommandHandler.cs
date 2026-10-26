using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.TimePlans.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.TimePlans.Commands.UpdateStage;

public sealed class UpdateTimePlanStageCommandHandler : IRequestHandler<UpdateTimePlanStageCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public UpdateTimePlanStageCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(UpdateTimePlanStageCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.TimePlanStages.SingleOrDefaultAsync(stage => stage.Id == request.Id && stage.TimePlanId == request.TimePlanId, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(TimePlanErrors.NotFound);
        }

        entity.Name = request.Input.Name.Trim();
        entity.DeliveryDate = request.Input.DeliveryDate!.Value;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
