using Brouj.Application.Abstractions.Persistence;
using Brouj.Domain.Entities;

namespace Brouj.Application.Features.TimePlans.Services;

public sealed class TimePlanOrdering
{
    private readonly IApplicationDbContext _db;

    public TimePlanOrdering(IApplicationDbContext db)
    {
        _db = db;
    }

    public Task StagesAsync(IReadOnlyList<TimePlanStage> rows, CancellationToken cancellationToken) =>
        PersistAsync(rows, (row, order) => row.SortOrder = order, cancellationToken);

    public Task ItemsAsync(IReadOnlyList<PlanStageItem> rows, CancellationToken cancellationToken) =>
        PersistAsync(rows, (row, order) => row.SortOrder = order, cancellationToken);

    public Task ImagesAsync(IReadOnlyList<StageItemImage> rows, CancellationToken cancellationToken) =>
        PersistAsync(rows, (row, order) => row.SortOrder = order, cancellationToken);

    private async Task PersistAsync<T>(
        IReadOnlyList<T> rows,
        Action<T, int> assign,
        CancellationToken cancellationToken)
    {
        // Each caller is transactional. Negative temporary positions avoid transient unique-index
        // collisions while SQL Server applies swaps; the second save restores one-based ordering.
        for (var index = 0; index < rows.Count; index++)
        {
            assign(rows[index], -(index + 1));
        }

        await _db.SaveChangesAsync(cancellationToken);

        for (var index = 0; index < rows.Count; index++)
        {
            assign(rows[index], index + 1);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
