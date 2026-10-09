using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Brouj.Infrastructure.Interceptors;

public sealed class AuditableEntityInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyTimestamps(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ApplyTimestamps(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void ApplyTimestamps(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        context.ChangeTracker.DetectChanges();
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        // Match the approved datetimeoffset(3) storage precision.
        now = now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMillisecond));

        foreach (var entry in context.ChangeTracker.Entries().ToArray())
        {
            var createdAt = entry.Metadata.FindProperty("CreatedAt");
            var updatedAt = entry.Metadata.FindProperty("UpdatedAt");

            if (entry.State == EntityState.Added)
            {
                if (createdAt?.ClrType == typeof(DateTimeOffset))
                {
                    entry.Property(createdAt.Name).CurrentValue = now;
                }

                if (updatedAt?.ClrType == typeof(DateTimeOffset?))
                {
                    entry.Property(updatedAt.Name).CurrentValue = null;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                if (createdAt?.ClrType == typeof(DateTimeOffset))
                {
                    var property = entry.Property(createdAt.Name);
                    property.CurrentValue = property.OriginalValue;
                    property.IsModified = false;
                }

                if (updatedAt?.ClrType == typeof(DateTimeOffset?))
                {
                    entry.Property(updatedAt.Name).CurrentValue = now;
                }
            }
        }
    }
}
