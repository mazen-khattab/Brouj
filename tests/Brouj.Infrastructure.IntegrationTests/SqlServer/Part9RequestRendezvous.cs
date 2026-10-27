using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

/// <summary>Coordinates test callers at a SQL boundary; it supplies no application locking behavior.</summary>
internal sealed class Part9RequestRendezvous(bool beforeSave = false) : DbCommandInterceptor
{
    private readonly TaskCompletionSource _arrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _count;

    public int Arrivals => Volatile.Read(ref _count);

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        var target = beforeSave
            ? command.CommandText.Contains("INSERT INTO [Reservations]", StringComparison.Ordinal)
            : command.CommandText.Contains("WITH (UPDLOCK, HOLDLOCK)", StringComparison.Ordinal);

        if (target)
        {

            if (Interlocked.Increment(ref _count) == 2)
            {
                _arrived.TrySetResult();
            }

            // A bounded wait makes a missing participant fail the test instead of hanging the suite.
            await _arrived.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
        }

        return result;
    }
}
