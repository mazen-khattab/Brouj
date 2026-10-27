using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Reservations.Errors;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Brouj.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

[Collection(SqlServerCollection.Name)]
[Trait("Category", "SqlServer")]
public sealed class Part9TransactionTests(SqlServerFixture fixture)
{
    [SqlServerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Approved_unique_conflicts_are_sanitized_after_rollback(bool duplicateOrder)
    {
        await using var database = await fixture.CreateDatabaseAsync();
        TestGraph graph;

        await using (var seed = database.CreateContext())
        {
            graph = await TestGraph.SeedAsync(seed);
        }

        using var logs = new SecurityLogProvider();
        using var logging = LoggerFactory.Create(builder => builder.AddProvider(logs));
        await using var writer = database.CreateContext(enableRetry: true);
        var manager = new ApplicationTransactionManager(writer, logging.CreateLogger<ApplicationTransactionManager>());
        var calls = 0;
        var conflict = await Assert.ThrowsAsync<PersistenceConflictException>(() => manager.ExecuteAsync(async token =>
        {
            calls++;
            writer.Cities.Add(new City { Name = "must roll back" });
            await writer.SaveChangesAsync(token);

            if (duplicateOrder)
            {
                writer.Orders.Add(new Order
                {
                    ReservationId = graph.Reservation.Id,
                    Number = "duplicate-order",
                    TotalPrice = 500
                });
            }
            else
            {
                writer.Reservations.Add(graph.NewReservation("duplicate-active"));
            }

            return await writer.SaveChangesAsync(token);
        }, CancellationToken.None));

        Assert.Equal(duplicateOrder ? PersistenceConflict.ReservationOrder : PersistenceConflict.ActiveReservation,
            conflict.Conflict);
        Assert.Null(conflict.InnerException);
        Assert.DoesNotContain("duplicate", conflict.Message);
        Assert.DoesNotContain(logs.Entries, entry => entry.Contains("duplicate-order", StringComparison.Ordinal) ||
            entry.Contains("duplicate-active", StringComparison.Ordinal));
        Assert.Equal(1, calls);
        Assert.Empty(writer.ChangeTracker.Entries());
        Assert.Null(writer.Database.CurrentTransaction);

        await using var reader = database.CreateContext();

        Assert.Equal(1, await reader.Cities.CountAsync());
        Assert.Equal(1, await reader.Orders.CountAsync());
        Assert.Equal(2, await reader.Reservations.CountAsync());
    }

    [SqlServerFact]
    public async Task Business_failure_rolls_back_earlier_saves_instead_of_committing_them()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var writer = database.CreateContext(enableRetry: true);
        var manager = Manager(writer);
        var result = await manager.ExecuteAsync(async token =>
        {
            writer.Cities.Add(new City { Name = "must roll back" });
            await writer.SaveChangesAsync(token);

            return Result<Guid>.Failure(ReservationErrors.AlreadyActive);
        }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(writer.ChangeTracker.Entries());

        await using var reader = database.CreateContext();

        Assert.False(await reader.Cities.AnyAsync());
    }

    [SqlServerFact]
    public async Task Unrelated_unique_failure_is_not_misreported_as_an_active_reservation_conflict()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        TestGraph graph;

        await using (var seed = database.CreateContext())
        {
            graph = await TestGraph.SeedAsync(seed, order: false);
        }

        await using var writer = database.CreateContext(enableRetry: true);

        await Assert.ThrowsAsync<DbUpdateException>(() => Manager(writer).ExecuteAsync(async token =>
        {
            writer.Reservations.Add(new Reservation
            {
                CustomerId = graph.Customer.Id,
                UnitId = graph.OtherUnit.Id,
                Number = graph.Reservation.Number,
                Status = ReservationStatus.Confirmed,
                ReservationDate = DateTimeOffset.UtcNow
            });

            return await writer.SaveChangesAsync(token);
        }, CancellationToken.None));
    }

    [SqlServerFact]
    public async Task Real_deadlock_returns_one_safe_conflict_without_replaying_either_delegate()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var first = new City { Name = "first" };
        var second = new City { Name = "second" };

        await using (var seed = database.CreateContext())
        {
            seed.Cities.AddRange(first, second);
            await seed.SaveChangesAsync();
        }

        var bothLocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var arrivals = 0;
        var calls = 0;

        async Task<bool> UpdateAsync(Guid firstId, Guid secondId)
        {
            await using var writer = database.CreateContext(enableRetry: true);

            try
            {
                return await Manager(writer).ExecuteAsync(async token =>
                {
                    Interlocked.Increment(ref calls);
                    await writer.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE [Cities] WITH (ROWLOCK) SET [Name] = [Name] WHERE [Id] = {firstId}", token);

                    if (Interlocked.Increment(ref arrivals) == 2)
                    {
                        bothLocked.TrySetResult();
                    }

                    // Each independent connection holds one row and then requests the other's row.
                    await bothLocked.Task.WaitAsync(TimeSpan.FromSeconds(20), token);
                    await writer.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE [Cities] WITH (ROWLOCK) SET [Name] = [Name] WHERE [Id] = {secondId}", token);

                    return true;
                }, CancellationToken.None);
            }
            catch (PersistenceConflictException exception)
            {
                Assert.Equal(PersistenceConflict.ConcurrentWrite, exception.Conflict);
                Assert.Null(exception.InnerException);
                Assert.Null(writer.Database.CurrentTransaction);

                return false;
            }
        }

        var attempts = await Task.WhenAll(UpdateAsync(first.Id, second.Id), UpdateAsync(second.Id, first.Id));

        Assert.Single(attempts, success => success);
        Assert.Single(attempts, success => !success);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Unit_lock_requires_an_active_transaction_before_issuing_SQL()
    {
        await using var context = TestContext.Create();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.LockUnitForOrderAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [SqlServerFact]
    public async Task Lost_commit_acknowledgement_does_not_replay_a_committed_business_operation()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var fault = new Part9CommitFailureInterceptor();
        await using var writer = database.CreateContext(enableRetry: true, interceptors: [fault]);
        var calls = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() => Manager(writer).ExecuteAsync(async token =>
        {
            calls++;
            writer.Cities.Add(new City { Name = "committed once" });

            return await writer.SaveChangesAsync(token);
        }, CancellationToken.None));

        Assert.True(fault.Committed);
        Assert.Equal(1, calls);
        Assert.Empty(writer.ChangeTracker.Entries());

        await using var reader = database.CreateContext();

        Assert.Equal(1, await reader.Cities.CountAsync());
    }

    private static ApplicationTransactionManager Manager(ApplicationDbContext context)
    {
        return new ApplicationTransactionManager(context, NullLogger<ApplicationTransactionManager>.Instance);
    }
}
