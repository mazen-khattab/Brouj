using Brouj.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brouj.Infrastructure.Persistence.Configurations;

public sealed class StageTemplateItemConfiguration : IEntityTypeConfiguration<StageTemplateItem>
{
    public void Configure(EntityTypeBuilder<StageTemplateItem> builder)
    {
        builder.ToTable("StageTemplateItems");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.StageTemplateId).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.ItemTemplateId).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.SortOrder).HasColumnType("int").IsRequired(true);

        builder.HasOne(entity => entity.StageTemplate)
            .WithMany(entity => entity.StageTemplateItems)
            .HasForeignKey(entity => entity.StageTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entity => entity.ItemTemplate)
            .WithMany(entity => entity.StageTemplateItems)
            .HasForeignKey(entity => entity.ItemTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entity => new { entity.StageTemplateId, entity.ItemTemplateId })
            .IsUnique()
            .HasDatabaseName("UQ_StageTemplateItems_StageTemplateId_ItemTemplateId");

        builder.HasIndex(entity => new { entity.StageTemplateId, entity.SortOrder })
            .IsUnique()
            .HasDatabaseName("UQ_StageTemplateItems_StageTemplateId_SortOrder");
    }
}
