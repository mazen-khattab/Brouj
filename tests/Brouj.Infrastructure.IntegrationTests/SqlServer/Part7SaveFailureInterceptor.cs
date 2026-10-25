using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

internal sealed class Part7SaveFailureInterceptor : SaveChangesInterceptor
{
    private int _saves;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        _saves++;

        if (_saves == 2)
        {
            throw new InvalidOperationException("Synthetic failure before final positions are saved.");
        }

        return ValueTask.FromResult(result);
    }
}
