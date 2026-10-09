using Brouj.Domain.Entities;
using Brouj.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brouj.Infrastructure.Persistence.Configurations;

public sealed class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("ActivityLogs");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.EntityType).HasColumnType("nvarchar(128)").HasMaxLength(128).IsUnicode(true).IsRequired(true);
        builder.Property(entity => entity.EntityId).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.UserId).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.Action).HasConversion(new ActivityActionConverter()).HasColumnType("varchar(16)").HasMaxLength(16).IsUnicode(false).IsRequired(true);
        builder.Property(entity => entity.NewData).HasColumnType("nvarchar(max)").IsRequired(false);
        builder.Property(entity => entity.OldData).HasColumnType("nvarchar(max)").IsRequired(false);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);

        builder.HasOne(entity => entity.User)
            .WithMany(entity => entity.ActivityLogs)
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => entity.CreatedAt)
            .HasDatabaseName("IX_ActivityLogs_CreatedAt");

        builder.HasIndex(entity => new { entity.UserId, entity.CreatedAt })
            .HasDatabaseName("IX_ActivityLogs_UserId_CreatedAt");

        builder.HasIndex(entity => new { entity.EntityType, entity.EntityId, entity.CreatedAt })
            .HasDatabaseName("IX_ActivityLogs_EntityType_EntityId_CreatedAt");

        builder.HasIndex(entity => new { entity.Action, entity.CreatedAt })
            .HasDatabaseName("IX_ActivityLogs_Action_CreatedAt");
    }
}
