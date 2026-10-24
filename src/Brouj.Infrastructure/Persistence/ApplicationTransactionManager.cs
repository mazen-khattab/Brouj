using Brouj.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Brouj.Infrastructure.Persistence;

/// <summary>Wraps the Application transaction behavior in the scoped DbContext's relational transaction.</summary>
public sealed class ApplicationTransactionManager(ApplicationDbContext context) : IApplicationTransactionManager
{
    /// <inheritdoc />
    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        // Nested commands participate in the outer boundary instead of opening a competing transaction.
        if (context.Database.CurrentTransaction is not null) return await operation(cancellationToken);
        // The scoped handler can contain side effects that are not replayable. Do not blindly retry the
        // whole delegate; refresh authentication uses its own isolated, replay-safe execution strategy.
        var strategy = new NonRetryingExecutionStrategy(context);
        return await strategy.ExecuteAsync(async ct =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(ct);
            try
            {
                var result = await operation(ct);
                await transaction.CommitAsync(ct);
                return result;
            }
            catch
            {
                context.ChangeTracker.Clear();
                throw;
            }
        }, cancellationToken);
    }
}
