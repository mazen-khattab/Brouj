using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

/// <summary>Simulates losing the commit acknowledgement after SQL Server has committed the transaction.</summary>
internal sealed class Part9CommitFailureInterceptor : DbTransactionInterceptor
{
    public bool Committed { get; private set; }

    public override Task TransactionCommittedAsync(
        DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        Committed = true;

        throw new InvalidOperationException("Synthetic lost commit acknowledgement.");
    }
}
