using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Brouj.Infrastructure.Persistence;

/// <summary>Establishes EF's ambient execution scope without replaying a business transaction.</summary>
internal sealed class NonReplayingExecutionStrategy : ExecutionStrategy
{
    public NonReplayingExecutionStrategy(DbContext context) : base(context, 0, TimeSpan.Zero)
    {
    }

    // Unlike NonRetryingExecutionStrategy, ExecutionStrategy establishes Current for nested EF calls.
    // This allows explicit transactions even when the host enables SQL retries for ordinary reads.
    protected override bool ShouldRetryOn(Exception exception) => false;
}
