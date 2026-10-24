using Brouj.Domain.Entities;
using Brouj.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brouj.Infrastructure.Persistence.Configurations;

public sealed class PlanStageItemConfiguration : IEntityTypeConfiguration<PlanStageItem>
{
    public void Configure(EntityTypeBuilder<PlanStageItem> builder)
    {
        builder.ToTable("PlanStageItems");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.StageId).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.Name).HasColumnType("nvarchar(100)").HasMaxLength(100).IsUnicode(true).IsRequired(true);
        builder.Property(entity => entity.Status).HasConversion(new PlanStageItemStatusConverter()).HasColumnType("varchar(16)").HasMaxLength(16).IsUnicode(false).IsRequired(true);
        builder.Property(entity => entity.SortOrder).HasColumnType("int").IsRequired(true);
        builder.Property(entity => entity.ConfirmedDate).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(false);
        builder.Property(entity => entity.DueDate).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);
        builder.Property(entity => entity.UpdatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(false);

        builder.HasOne(entity => entity.Stage)
            .WithMany(entity => entity.PlanStageItems)
            .HasForeignKey(entity => entity.StageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entity => new { entity.StageId, entity.Status })
            .HasDatabaseName("IX_PlanStageItems_StageId_Status");

        builder.HasIndex(entity => new { entity.StageId, entity.SortOrder })
            .IsUnique()
            .HasDatabaseName("UQ_PlanStageItems_StageId_SortOrder");
    }
}
