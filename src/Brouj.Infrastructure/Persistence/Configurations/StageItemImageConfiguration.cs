using Brouj.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brouj.Infrastructure.Persistence.Configurations;

public sealed class StageItemImageConfiguration : IEntityTypeConfiguration<StageItemImage>
{
    public void Configure(EntityTypeBuilder<StageItemImage> builder)
    {
        builder.ToTable("StageItemImages");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.ItemId).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.ImagePath).HasColumnType("nvarchar(2048)").HasMaxLength(2048).IsUnicode(true).IsRequired(true);
        builder.Property(entity => entity.Description).HasColumnType("nvarchar(max)").IsRequired(false);
        builder.Property(entity => entity.SortOrder).HasColumnType("int").IsRequired(true);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);
        builder.Property(entity => entity.UpdatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(false);

        builder.HasOne(entity => entity.Item)
            .WithMany(entity => entity.StageItemImages)
            .HasForeignKey(entity => entity.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entity => new { entity.ItemId, entity.SortOrder })
            .IsUnique()
            .HasDatabaseName("UQ_StageItemImages_ItemId_SortOrder");
    }
}
