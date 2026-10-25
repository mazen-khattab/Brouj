using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.TimePlans.Errors;
using Brouj.Application.Features.TimePlans.Services;
using Brouj.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.TimePlans.Commands.Create;

public sealed class CreateTimePlanCommandHandler : IRequestHandler<CreateTimePlanCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly TimePlanDraftBuilder _builder;

    public CreateTimePlanCommandHandler(IApplicationDbContext db, TimePlanDraftBuilder builder)
    {
        _db = db;
        _builder = builder;
    }

    public async Task<Result<Guid>> Handle(CreateTimePlanCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.BaseProjects.AsNoTracking().AnyAsync(project => project.Id == request.ProjectId, cancellationToken))
        {
            return Result<Guid>.Failure(TimePlanErrors.Conflict("The selected project does not exist."));
        }

        if (await _db.TimePlans.AsNoTracking().AnyAsync(plan => plan.ProjectId == request.ProjectId, cancellationToken))
        {
            return Result<Guid>.Failure(TimePlanErrors.Conflict("The project already has a time plan."));
        }

        var entity = new TimePlan
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            Name = request.Input.Name.Trim(),
            DeliveryDate = request.Input.DeliveryDate!.Value
        };

        for (var index = 0; index < request.Stages.Count; index++)
        {
            var stage = await _builder.BuildAsync(entity.Id, request.Stages[index], index + 1, cancellationToken);

            if (stage.IsFailure)
            {
                return Result<Guid>.Failure(stage.Error);
            }

            entity.TimePlanStages.Add(stage.Value);
        }

        // No draft is persisted until every selected template and all manual dates are valid.
        _db.TimePlans.Add(entity);

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
