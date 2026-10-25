using Brouj.Application.Abstractions.Persistence;

namespace Brouj.Application.UnitTests.Features.Support;

/// <summary>Records pipeline entry only; real rollback and SQL constraints belong to SQL Server tests.</summary>
internal sealed class FeatureTransactionManager : IApplicationTransactionManager
{
    public int ExecutionCount { get; private set; }

    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        ExecutionCount++;

        return operation(cancellationToken);
    }
}
