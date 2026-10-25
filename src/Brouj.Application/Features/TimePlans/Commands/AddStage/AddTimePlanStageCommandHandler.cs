using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.TimePlans.Errors;
using Brouj.Application.Features.TimePlans.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.TimePlans.Commands.AddStage;

public sealed class AddTimePlanStageCommandHandler : IRequestHandler<AddTimePlanStageCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly TimePlanDraftBuilder _builder;

    public AddTimePlanStageCommandHandler(IApplicationDbContext db, TimePlanDraftBuilder builder)
    {
        _db = db;
        _builder = builder;
    }

    public async Task<Result<Guid>> Handle(AddTimePlanStageCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.TimePlans.AsNoTracking().AnyAsync(plan => plan.Id == request.TimePlanId, cancellationToken))
        {
            return Result<Guid>.Failure(TimePlanErrors.NotFound);
        }

        var last = await _db.TimePlanStages.Where(stage => stage.TimePlanId == request.TimePlanId)
            .Select(stage => (int?)stage.SortOrder).MaxAsync(cancellationToken) ?? 0;
        var result = await _builder.BuildAsync(request.TimePlanId, request.Input, last + 1, cancellationToken);

        if (result.IsFailure)
        {
            return Result<Guid>.Failure(result.Error);
        }

        var entity = result.Value;

        _db.TimePlanStages.Add(entity);

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
