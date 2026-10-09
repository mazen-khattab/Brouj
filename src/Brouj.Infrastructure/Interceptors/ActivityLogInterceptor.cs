using System.Runtime.CompilerServices;
using System.Text.Json;
using Brouj.Application.Abstractions.Authentication;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Brouj.Infrastructure.Interceptors;

public sealed class ActivityLogInterceptor(ICurrentUser currentUser, TimeProvider timeProvider) : SaveChangesInterceptor
{
    private readonly ConditionalWeakTable<DbContext, List<ActivityLog>> _pendingLogs = new();

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        PrepareLogs(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        PrepareLogs(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        ForgetPendingLogs(eventData.Context);
        return result;
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        ForgetPendingLogs(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData) =>
        DiscardPendingLogs(eventData.Context);

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        DiscardPendingLogs(eventData.Context);
        return Task.CompletedTask;
    }

    public override void SaveChangesCanceled(DbContextEventData eventData) =>
        DiscardPendingLogs(eventData.Context);

    public override Task SaveChangesCanceledAsync(
        DbContextEventData eventData,
        CancellationToken cancellationToken = default)
    {
        DiscardPendingLogs(eventData.Context);
        return Task.CompletedTask;
    }

    internal void DiscardPendingLogs(DbContext? context)
    {
        if (context is null || !_pendingLogs.TryGetValue(context, out var logs))
        {
            return;
        }

        foreach (var log in logs)
        {
            context.Entry(log).State = EntityState.Detached;
        }

        _pendingLogs.Remove(context);
    }

    private void ForgetPendingLogs(DbContext? context)
    {
        if (context is not null)
        {
            _pendingLogs.Remove(context);
        }
    }

    private void PrepareLogs(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        DiscardPendingLogs(context);

        if (!currentUser.IsAuthenticated ||
            currentUser.CustomerId is not null ||
            currentUser.UserId is not { } userId ||
            userId == Guid.Empty ||
            currentUser.Role is not (UserRole.Admin or UserRole.SuperAdmin))
        {
            return;
        }

        context.ChangeTracker.DetectChanges();
        var changes = context.ChangeTracker.Entries()
            .Where(entry => entry.Entity is not ActivityLog &&
                entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToArray();

        if (changes.Length == 0)
        {
            return;
        }

        if (context.Database.AutoTransactionBehavior != AutoTransactionBehavior.Always)
        {
            throw new InvalidOperationException("Staff auditing requires AutoTransactionBehavior.Always.");
        }

        if (context.Database.GetDbConnection() is SqlConnection connection &&
            new SqlConnectionStringBuilder(connection.ConnectionString).MultipleActiveResultSets)
        {
            throw new InvalidOperationException("Staff auditing requires SQL Server MARS to be disabled for reliable rollback.");
        }

        var now = timeProvider.GetUtcNow().ToUniversalTime();
        now = now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMillisecond));
        var logs = new List<ActivityLog>(changes.Length);

        foreach (var entry in changes)
        {
            var key = entry.Metadata.FindPrimaryKey()
                ?? throw new InvalidOperationException("Audited entities require a primary key.");

            if (key.Properties.Count != 1 ||
                entry.Property(key.Properties[0].Name) is not { IsTemporary: false, CurrentValue: Guid entityId } ||
                entityId == Guid.Empty)
            {
                // Every approved PK is a client-generated Guid or an inherited shared Guid.
                // Reject unsafe keys before any SQL rather than record a temporary/wrong EntityId.
                throw new InvalidOperationException("Audited entities require a final, non-empty GUID primary key before saving.");
            }

            logs.Add(new ActivityLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EntityType = entry.Metadata.ClrType.Name,
                EntityId = entityId,
                Action = entry.State switch
                {
                    EntityState.Added => ActivityAction.Created,
                    EntityState.Modified => ActivityAction.Updated,
                    EntityState.Deleted => ActivityAction.Deleted,
                    _ => throw new InvalidOperationException("Unsupported audit state.")
                },
                OldData = entry.State == EntityState.Added ? null : Snapshot(entry, original: true),
                NewData = entry.State == EntityState.Deleted ? null : Snapshot(entry, original: false),
                CreatedAt = now
            });
        }

        _pendingLogs.Add(context, logs);
        context.Set<ActivityLog>().AddRange(logs);
    }

    private static string Snapshot(EntityEntry entry, bool original)
    {
        var data = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var property in entry.Properties)
        {
            var type = Nullable.GetUnderlyingType(property.Metadata.ClrType) ?? property.Metadata.ClrType;

            // Free-form text can contain credentials, tokens, identity numbers, or embedded secrets.
            // Include only approved structured scalar values; never serialize navigations or text.
            if (type != typeof(Guid) && type != typeof(DateTimeOffset) &&
                type != typeof(decimal) && type != typeof(int) && type != typeof(bool) && !type.IsEnum)
            {
                continue;
            }

            var value = original ? property.OriginalValue : property.CurrentValue;
            if (value is not null && type.IsEnum)
            {
                var converter = property.Metadata.GetTypeMapping().Converter
                    ?? throw new InvalidOperationException("Audited enums require the approved string converter.");
                value = converter.ConvertToProvider(value);
            }

            data.Add(property.Metadata.Name, value);
        }

        // Field names explain text-only changes without exposing their potentially sensitive values.
        data.Add("ChangedProperties", entry.Properties
            .Where(property => entry.State != EntityState.Modified || property.IsModified)
            .Select(property => property.Metadata.Name)
            .Order(StringComparer.Ordinal)
            .ToArray());

        return JsonSerializer.Serialize(data);
    }
}
