using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

/// <summary>Fails after real SQL writes complete, while the application's outer transaction is still open.</summary>
internal sealed class Part9SaveFailureInterceptor : SaveChangesInterceptor
{
    public bool WrittenInsideTransaction { get; private set; }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        WrittenInsideTransaction = result > 0 && eventData.Context!.Database.CurrentTransaction is not null;

        throw new InvalidOperationException("Synthetic failure after SQL persistence, before application commit.");
    }
}
