using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.TimePlans.Errors;
using Brouj.Application.Features.TimePlans.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.TimePlans.Commands.ReorderStages;

public sealed class ReorderTimePlanStagesCommandHandler : IRequestHandler<ReorderTimePlanStagesCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly TimePlanOrdering _ordering;

    public ReorderTimePlanStagesCommandHandler(IApplicationDbContext db, TimePlanOrdering ordering)
    {
        _db = db;
        _ordering = ordering;
    }

    public async Task<Result<Guid>> Handle(ReorderTimePlanStagesCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.TimePlans.AsNoTracking().AnyAsync(parent => parent.Id == request.TimePlanId, cancellationToken))
        {
            return Result<Guid>.Failure(TimePlanErrors.NotFound);
        }

        var rows = await _db.TimePlanStages.Where(row => row.TimePlanId == request.TimePlanId).ToListAsync(cancellationToken);
        var ids = request.Items.Select(item => item.Id).ToHashSet();

        if (rows.Count != ids.Count || rows.Any(row => !ids.Contains(row.Id)))
        {
            return Result<Guid>.Failure(TimePlanErrors.Validation("Supply each sibling exactly once; every item must belong to this parent."));
        }

        var positions = request.Items.ToDictionary(item => item.Id, item => item.SortOrder);
        var ordered = rows.OrderBy(row => positions[row.Id]).ToArray();

        await _ordering.StagesAsync(ordered, cancellationToken);

        return Result<Guid>.Success(request.TimePlanId);
    }
}
