using System.Text.Json;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

[Collection(SqlServerCollection.Name)]
[Trait("Category", "SqlServer")]
public sealed class AuditPersistenceTests(SqlServerFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 10, 9, 12, 30, 45, 123, TimeSpan.Zero);

    [SqlServerFact]
    public async Task Audit_timestamps_persist_in_UTC_and_CreatedAt_cannot_be_overwritten()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        Guid id;
        await using (var writer = database.CreateContext(clock: new FixedClock(Now)))
        {
            var city = new City { Name = "timestamp", CreatedAt = Now.AddYears(-1), UpdatedAt = Now.AddYears(-1) };
            writer.Cities.Add(city);
            await writer.SaveChangesAsync();
            id = city.Id;
        }

        await using (var writer = database.CreateContext(clock: new FixedClock(Now.AddHours(1))))
        {
            var city = await writer.Cities.SingleAsync(city => city.Id == id);
            Assert.Equal(Now, city.CreatedAt);
            Assert.Null(city.UpdatedAt);
            city.Name = "updated";
            city.CreatedAt = Now.AddYears(1);
            city.UpdatedAt = Now.AddYears(1);
            await writer.SaveChangesAsync();
        }

        await using var reader = database.CreateContext();
        var persisted = await reader.Cities.AsNoTracking().SingleAsync(city => city.Id == id);
        Assert.Equal(Now, persisted.CreatedAt);
        Assert.Equal(TimeSpan.Zero, persisted.CreatedAt.Offset);
        Assert.Equal(Now.AddHours(1), persisted.UpdatedAt);
    }

    [SqlServerTheory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.SuperAdmin)]
    public async Task Staff_create_update_delete_generate_only_their_corresponding_JSON_logs(UserRole role)
    {
        await using var database = await fixture.CreateDatabaseAsync();
        TestGraph graph;
        await using (var seed = database.CreateContext())
        {
            graph = await TestGraph.SeedAsync(seed, reservations: false);
        }
        var actor = new TestActor(true, graph.User.Id, null, role);
        Guid cityId;
        await using (var writer = database.CreateContext(actor))
        {
            var city = new City { Name = "staff insert" };
            writer.Cities.Add(city);
            await writer.SaveChangesAsync();
            cityId = city.Id;
            graph = graph with { Project = await writer.BaseProjects.SingleAsync(project => project.Id == graph.Project.Id) };
            graph.Project.Price = 200;
            await writer.SaveChangesAsync();
            writer.Cities.Remove(city);
            await writer.SaveChangesAsync();
        }

        await using var reader = database.CreateContext();
        var logs = await reader.ActivityLogs.AsNoTracking().ToListAsync();
        Assert.Equal(3, logs.Count);
        Assert.All(logs, log => Assert.Equal(graph.User.Id, log.UserId));
        var created = Assert.Single(logs, log => log.Action == ActivityAction.Created);
        Assert.Equal(cityId, created.EntityId);
        Assert.NotEqual(Guid.Empty, created.EntityId);
        Assert.Null(created.OldData);
        using var createdData = JsonDocument.Parse(created.NewData!);
        Assert.Equal(cityId, createdData.RootElement.GetProperty("Id").GetGuid());
        var updated = Assert.Single(logs, log => log.Action == ActivityAction.Updated);
        using var oldData = JsonDocument.Parse(updated.OldData!);
        using var newData = JsonDocument.Parse(updated.NewData!);
        Assert.Equal(100, oldData.RootElement.GetProperty("Price").GetDecimal());
        Assert.Equal(200, newData.RootElement.GetProperty("Price").GetDecimal());
        var deleted = Assert.Single(logs, log => log.Action == ActivityAction.Deleted);
        Assert.Equal(cityId, deleted.EntityId);
        Assert.NotNull(deleted.OldData);
        Assert.Null(deleted.NewData);
        Assert.DoesNotContain(logs, log => log.EntityType == nameof(ActivityLog));
    }

    [SqlServerFact]
    public async Task Customer_changes_do_not_persist_staff_activity_logs()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        TestGraph graph;
        await using (var seed = database.CreateContext())
        {
            graph = await TestGraph.SeedAsync(seed, reservations: false);
        }

        await using (var writer = database.CreateContext(new TestActor(true, null, graph.Customer.Id)))
        {
            writer.Reservations.Add(graph.NewReservation("customer"));
            await writer.SaveChangesAsync();
        }

        await using var reader = database.CreateContext();
        Assert.Equal(1, await reader.Reservations.AsNoTracking().CountAsync());
        Assert.Equal(0, await reader.ActivityLogs.AsNoTracking().CountAsync());
    }

    [SqlServerFact]
    public async Task Sensitive_values_are_excluded_from_persisted_old_and_new_JSON()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        TestGraph graph;
        await using (var seed = database.CreateContext())
        {
            graph = await TestGraph.SeedAsync(seed, reservations: false);
        }
        const string secret = "synthetic-do-not-audit";

        await using (var writer = database.CreateContext(new TestActor(true, graph.User.Id, null, UserRole.Admin)))
        {
            var customer = await writer.Customers.SingleAsync(customer => customer.Id == graph.Customer.Id);
            customer.HashPassword = secret;
            customer.IdentityNumber = secret;
            customer.FName = secret;
            var token = await writer.CustomerRefreshTokens.SingleAsync();
            token.Token = secret;
            var project = await writer.BaseProjects.SingleAsync();
            project.Description = secret;
            await writer.SaveChangesAsync();
        }

        await using var reader = database.CreateContext();
        var logs = await reader.ActivityLogs.AsNoTracking().ToListAsync();
        Assert.Equal(3, logs.Count);
        foreach (var log in logs)
        {
            foreach (var json in new[] { log.OldData, log.NewData })
            {
                Assert.DoesNotContain(secret, json!);
                using var data = JsonDocument.Parse(json!);
                Assert.False(data.RootElement.TryGetProperty("HashPassword", out _));
                Assert.False(data.RootElement.TryGetProperty("IdentityNumber", out _));
                Assert.False(data.RootElement.TryGetProperty("Token", out _));
            }
        }
    }

    [SqlServerFact]
    public async Task Transaction_rollback_removes_both_business_change_and_audit_record()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        TestGraph graph;
        await using (var seed = database.CreateContext())
        {
            graph = await TestGraph.SeedAsync(seed, reservations: false);
        }

        await using (var writer = database.CreateContext(new TestActor(true, graph.User.Id, null, UserRole.Admin)))
        {
            await using var transaction = await writer.Database.BeginTransactionAsync();
            writer.Cities.Add(new City { Name = "rolled back" });
            await writer.SaveChangesAsync();
            Assert.Equal(1, await writer.ActivityLogs.AsNoTracking().CountAsync());
            await transaction.RollbackAsync();
        }

        await using var reader = database.CreateContext();
        Assert.Equal(0, await reader.ActivityLogs.AsNoTracking().CountAsync());
        Assert.False(await reader.Cities.AsNoTracking().AnyAsync(city => city.Name == "rolled back"));
    }

    [SqlServerFact]
    public async Task Failed_staff_save_leaves_no_audit_row_and_retry_creates_exactly_one()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        TestGraph graph;
        await using (var seed = database.CreateContext())
        {
            graph = await TestGraph.SeedAsync(seed, reservations: false);
        }

        await using (var writer = database.CreateContext(new TestActor(true, graph.User.Id, null, UserRole.Admin)))
        {
            var duplicate = new City { Name = graph.City.Name };
            writer.Cities.Add(duplicate);
            await Assert.ThrowsAsync<DbUpdateException>(() => writer.SaveChangesAsync());
            Assert.Empty(writer.ActivityLogs.Local);
            await using (var reader = database.CreateContext())
            {
                Assert.Equal(0, await reader.ActivityLogs.AsNoTracking().CountAsync());
            }
            duplicate.Name = "retry-success";
            await writer.SaveChangesAsync();
        }

        await using var verification = database.CreateContext();
        Assert.Equal(1, await verification.ActivityLogs.AsNoTracking().CountAsync());
    }

    [SqlServerFact]
    public async Task Outer_transaction_remains_usable_after_a_failed_staff_save()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        TestGraph graph;
        await using (var seed = database.CreateContext())
        {
            graph = await TestGraph.SeedAsync(seed, reservations: false);
        }

        await using (var writer = database.CreateContext(new TestActor(true, graph.User.Id, null, UserRole.Admin)))
        {
            await using var transaction = await writer.Database.BeginTransactionAsync();
            var duplicate = new City { Name = graph.City.Name };
            writer.Cities.Add(duplicate);
            await Assert.ThrowsAsync<DbUpdateException>(() => writer.SaveChangesAsync());
            Assert.Equal(0, await writer.ActivityLogs.AsNoTracking().CountAsync());
            duplicate.Name = "after-savepoint";
            await writer.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using var reader = database.CreateContext();
        Assert.Equal(1, await reader.ActivityLogs.AsNoTracking().CountAsync());
        Assert.True(await reader.Cities.AsNoTracking().AnyAsync(city => city.Name == "after-savepoint"));
    }
}
