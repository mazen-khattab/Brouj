using Brouj.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brouj.Infrastructure.Persistence.Configurations;

public sealed class TimePlanStageConfiguration : IEntityTypeConfiguration<TimePlanStage>
{
    public void Configure(EntityTypeBuilder<TimePlanStage> builder)
    {
        builder.ToTable("TimePlanStages");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.TimePlanId).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.Name).HasColumnType("nvarchar(100)").HasMaxLength(100).IsUnicode(true).IsRequired(true);
        builder.Property(entity => entity.SortOrder).HasColumnType("int").IsRequired(true);
        builder.Property(entity => entity.DeliveryDate).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);
        builder.Property(entity => entity.UpdatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(false);

        builder.HasOne(entity => entity.TimePlan)
            .WithMany(entity => entity.TimePlanStages)
            .HasForeignKey(entity => entity.TimePlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entity => new { entity.TimePlanId, entity.SortOrder })
            .IsUnique()
            .HasDatabaseName("UQ_TimePlanStages_TimePlanId_SortOrder");
    }
}
