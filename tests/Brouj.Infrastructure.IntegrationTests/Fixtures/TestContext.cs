using Brouj.Infrastructure.Interceptors;
using Brouj.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Brouj.Infrastructure.IntegrationTests.Fixtures;

internal static class TestContext
{
    public static ApplicationDbContext Create(
        TestActor? actor = null,
        TimeProvider? clock = null,
        params IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=localhost;Database=BroujModelOnly;Integrated Security=True;TrustServerCertificate=True;")
            .AddInterceptors(interceptors)
            .Options;
        var timeProvider = clock ?? TimeProvider.System;
        return new ApplicationDbContext(
            options,
            new AuditableEntityInterceptor(timeProvider),
            new ActivityLogInterceptor(actor ?? new TestActor(), timeProvider));
    }
}

// Suppression tests inspect audit preparation only. No relational behavior is simulated.
internal sealed class SuppressSaveInterceptor : SaveChangesInterceptor
{
    public bool FailCompletion { get; set; }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(0));

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default) =>
        FailCompletion
            ? throw new InvalidOperationException("Simulated callback failure.")
            : ValueTask.FromResult(result);
}

internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
