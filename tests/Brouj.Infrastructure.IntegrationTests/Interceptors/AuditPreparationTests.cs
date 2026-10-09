using System.Text.Json;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Infrastructure.IntegrationTests.Interceptors;

// These tests suppress persistence and verify only tracking/interceptor preparation.
public sealed class AuditPreparationTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 9, 12, 30, 45, 123, TimeSpan.Zero);

    [Fact]
    public async Task Inserts_replace_supplied_audit_timestamps_and_clear_UpdatedAt()
    {
        await using var context = TestContext.Create(clock: new FixedClock(Now.AddTicks(4567)), interceptors: new SuppressSaveInterceptor());
        var city = new City { Name = "Cairo", CreatedAt = Now.AddYears(-1), UpdatedAt = Now.AddDays(-1) };
        context.Cities.Add(city);

        await context.SaveChangesAsync();

        Assert.Equal(Now, city.CreatedAt);
        Assert.Equal(TimeSpan.Zero, city.CreatedAt.Offset);
        Assert.Null(city.UpdatedAt);
    }

    [Fact]
    public async Task Updates_restore_original_CreatedAt_and_replace_UpdatedAt()
    {
        await using var context = TestContext.Create(clock: new FixedClock(Now), interceptors: new SuppressSaveInterceptor());
        var createdAt = Now.AddDays(-1);
        var city = new City { Id = Guid.NewGuid(), Name = "Old", CreatedAt = createdAt };
        context.Attach(city);
        city.Name = "New";
        city.CreatedAt = Now.AddYears(1);
        city.UpdatedAt = Now.AddYears(1);

        await context.SaveChangesAsync();

        Assert.Equal(createdAt, city.CreatedAt);
        Assert.False(context.Entry(city).Property(entity => entity.CreatedAt).IsModified);
        Assert.Equal(Now, city.UpdatedAt);
    }

    [Fact]
    public async Task Timestamp_preparation_handles_every_approved_entity_and_shared_keys()
    {
        await using var context = TestContext.Create(clock: new FixedClock(Now), interceptors: new SuppressSaveInterceptor());
        var entities = context.Model.GetEntityTypes().Select(entity => Activator.CreateInstance(entity.ClrType)!).ToArray();
        context.AddRange(entities);

        await context.SaveChangesAsync();

        foreach (var entity in entities)
        {
            var entry = context.Entry(entity);
            if (entry.Metadata.FindProperty("CreatedAt") is not null)
            {
                Assert.Equal(Now, entry.Property("CreatedAt").CurrentValue);
            }

            if (entry.Metadata.FindProperty("UpdatedAt") is not null)
            {
                Assert.Null(entry.Property("UpdatedAt").CurrentValue);
            }
        }

        Assert.Null(context.Model.FindEntityType(typeof(StageTemplateItem))!.FindProperty("CreatedAt"));
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.SuperAdmin)]
    public async Task Staff_insert_prepares_one_JSON_log_with_the_generated_GUID(UserRole role)
    {
        var actor = TestActor.Staff(role);
        await using var context = TestContext.Create(actor, new FixedClock(Now), new SuppressSaveInterceptor());
        var project = new BaseProject { Name = "private-text", Price = 12.3456m, ProjectType = ProjectType.Initiative };
        context.BaseProjects.Add(project);
        Assert.NotEqual(Guid.Empty, project.Id);

        await context.SaveChangesAsync();

        var log = Assert.Single(context.ActivityLogs.Local);
        Assert.Equal(actor.UserId, log.UserId);
        Assert.Equal(project.Id, log.EntityId);
        Assert.Equal(nameof(BaseProject), log.EntityType);
        Assert.Equal(ActivityAction.Created, log.Action);
        Assert.Equal(Now, log.CreatedAt);
        Assert.Null(log.OldData);
        using var data = JsonDocument.Parse(log.NewData!);
        Assert.Equal(12.3456m, data.RootElement.GetProperty("Price").GetDecimal());
        Assert.Equal("initiative", data.RootElement.GetProperty("ProjectType").GetString());
        Assert.False(data.RootElement.TryGetProperty("Name", out _));
        Assert.Equal(AutoTransactionBehavior.Always, context.Database.AutoTransactionBehavior);
    }

    public static IEnumerable<object[]> NonStaffActors()
    {
        yield return [new TestActor()];
        yield return [TestActor.Customer()];
        yield return [new TestActor(false, Guid.NewGuid(), null, UserRole.Admin)];
        yield return [new TestActor(true, Guid.NewGuid(), Guid.NewGuid(), UserRole.Admin)];
        yield return [new TestActor(true, Guid.NewGuid(), null, null)];
        yield return [new TestActor(true, Guid.Empty, null, UserRole.Admin)];
        yield return [new TestActor(true, Guid.NewGuid(), null, (UserRole)999)];
    }

    [Theory]
    [MemberData(nameof(NonStaffActors))]
    public async Task Customer_anonymous_and_invalid_staff_contexts_prepare_no_logs(TestActor actor)
    {
        await using var context = TestContext.Create(actor, new FixedClock(Now), new SuppressSaveInterceptor());
        context.Cities.Add(new City { Name = "Cairo" });
        await context.SaveChangesAsync();
        Assert.Empty(context.ActivityLogs.Local);
    }

    [Fact]
    public async Task Update_and_delete_snapshots_use_the_original_and_current_values()
    {
        await using var context = TestContext.Create(TestActor.Staff(), new FixedClock(Now), new SuppressSaveInterceptor());
        var project = new BaseProject { Id = Guid.NewGuid(), Name = "old", Price = 10, CreatedAt = Now.AddDays(-1) };
        var city = new City { Id = Guid.NewGuid(), Name = "delete", CreatedAt = Now.AddDays(-1) };
        context.Attach(project);
        context.Attach(city);
        project.Price = 20;
        context.Cities.Remove(city);

        await context.SaveChangesAsync();

        var updated = Assert.Single(context.ActivityLogs.Local, log => log.Action == ActivityAction.Updated);
        using var oldData = JsonDocument.Parse(updated.OldData!);
        using var newData = JsonDocument.Parse(updated.NewData!);
        Assert.Equal(10, oldData.RootElement.GetProperty("Price").GetDecimal());
        Assert.Equal(20, newData.RootElement.GetProperty("Price").GetDecimal());
        var deleted = Assert.Single(context.ActivityLogs.Local, log => log.Action == ActivityAction.Deleted);
        Assert.Equal(city.Id, deleted.EntityId);
        Assert.NotNull(deleted.OldData);
        Assert.Null(deleted.NewData);
    }

    [Fact]
    public async Task Sensitive_fields_and_secrets_embedded_in_free_form_text_are_excluded()
    {
        await using var context = TestContext.Create(TestActor.Staff(), new FixedClock(Now), new SuppressSaveInterceptor());
        const string sensitive = "do-not-serialize-this-test-value";
        var customer = new Customer
        {
            FName = sensitive, LName = sensitive, Phone = sensitive, Email = sensitive,
            IdentityNumber = sensitive, HashPassword = sensitive
        };
        context.Customers.Add(customer);
        context.CustomerRefreshTokens.Add(new CustomerRefreshToken { Customer = customer, Token = sensitive });
        context.Users.Add(new User { Name = sensitive, HashPassword = sensitive, Email = sensitive, Phone = sensitive });
        context.UserRefreshTokens.Add(new UserRefreshToken { Token = sensitive });
        context.BaseProjects.Add(new BaseProject { Name = sensitive, Description = sensitive });

        await context.SaveChangesAsync();

        Assert.Equal(5, context.ActivityLogs.Local.Count);
        foreach (var log in context.ActivityLogs.Local)
        {
            Assert.DoesNotContain(sensitive, log.NewData!);
            using var data = JsonDocument.Parse(log.NewData!);
            Assert.False(data.RootElement.TryGetProperty("HashPassword", out _));
            Assert.False(data.RootElement.TryGetProperty("Token", out _));
            Assert.False(data.RootElement.TryGetProperty("IdentityNumber", out _));
        }
    }

    [Fact]
    public async Task ActivityLog_changes_do_not_recursively_prepare_logs()
    {
        await using var context = TestContext.Create(TestActor.Staff(), new FixedClock(Now), new SuppressSaveInterceptor());
        var log = new ActivityLog { EntityId = Guid.NewGuid(), UserId = Guid.NewGuid(), EntityType = nameof(City), Action = ActivityAction.Created };
        context.ActivityLogs.Add(log);
        await context.SaveChangesAsync();
        Assert.Same(log, Assert.Single(context.ActivityLogs.Local));
        context.ChangeTracker.AcceptAllChanges();
        log.NewData = "{}";
        await context.SaveChangesAsync();
        Assert.Same(log, Assert.Single(context.ActivityLogs.Local));
    }

    [Fact]
    public async Task Temporary_primary_keys_fail_before_preparing_or_persisting_a_log()
    {
        await using var context = TestContext.Create(TestActor.Staff(), new FixedClock(Now), new SuppressSaveInterceptor());
        var city = new City { Name = "Cairo" };
        context.Cities.Add(city);
        context.Entry(city).Property(entity => entity.Id).IsTemporary = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());

        Assert.Empty(context.ActivityLogs.Local);
    }

    [Fact]
    public async Task Failed_completion_callback_discards_prepared_logs_and_retry_does_not_duplicate_them()
    {
        var suppression = new SuppressSaveInterceptor { FailCompletion = true };
        await using var context = TestContext.Create(TestActor.Staff(), new FixedClock(Now), suppression);
        context.Cities.Add(new City { Name = "Cairo" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        Assert.Empty(context.ActivityLogs.Local);
        suppression.FailCompletion = false;
        await context.SaveChangesAsync();
        Assert.Single(context.ActivityLogs.Local);
    }

    [Fact]
    public async Task Cancellation_leaves_no_prepared_log()
    {
        await using var context = TestContext.Create(TestActor.Staff(), new FixedClock(Now), new SuppressSaveInterceptor());
        context.Cities.Add(new City { Name = "Cairo" });
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.SaveChangesAsync(source.Token));

        Assert.Empty(context.ActivityLogs.Local);
    }

    [Theory]
    [InlineData(AutoTransactionBehavior.Never)]
    [InlineData(AutoTransactionBehavior.WhenNeeded)]
    public async Task Weakening_transactions_is_rejected_for_staff_changes(AutoTransactionBehavior behavior)
    {
        await using var context = TestContext.Create(TestActor.Staff(), new FixedClock(Now), new SuppressSaveInterceptor());
        context.Database.AutoTransactionBehavior = behavior;
        context.Cities.Add(new City { Name = "Cairo" });
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        Assert.Empty(context.ActivityLogs.Local);
    }

    [Fact]
    public async Task SQL_Server_MARS_is_rejected_before_staff_audit_preparation()
    {
        await using var context = TestContext.Create(TestActor.Staff(), new FixedClock(Now), new SuppressSaveInterceptor());
        context.Database.SetConnectionString("Server=localhost;Database=ModelOnly;Integrated Security=True;MultipleActiveResultSets=True;");
        context.Cities.Add(new City { Name = "Cairo" });
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        Assert.Empty(context.ActivityLogs.Local);
    }

    [Fact]
    public async Task Empty_shared_primary_keys_are_rejected_before_audit_preparation()
    {
        await using var context = TestContext.Create(TestActor.Staff(), new FixedClock(Now), new SuppressSaveInterceptor());
        context.InitiativeProjects.Add(new InitiativeProject());
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        Assert.Empty(context.ActivityLogs.Local);
    }

    [Fact]
    public void Synchronous_persistence_is_rejected()
    {
        using var context = TestContext.Create();
        Assert.Throws<NotSupportedException>(() => context.SaveChanges());
    }
}
