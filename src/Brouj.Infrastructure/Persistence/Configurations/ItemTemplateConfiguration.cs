using Brouj.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brouj.Infrastructure.Persistence.Configurations;

public sealed class ItemTemplateConfiguration : IEntityTypeConfiguration<ItemTemplate>
{
    public void Configure(EntityTypeBuilder<ItemTemplate> builder)
    {
        builder.ToTable("ItemTemplates");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.Name).HasColumnType("nvarchar(100)").HasMaxLength(100).IsUnicode(true).IsRequired(true);
        builder.Property(entity => entity.Code).HasColumnType("nvarchar(64)").HasMaxLength(64).IsUnicode(true).IsRequired(true);
        builder.Property(entity => entity.IsActive).HasColumnType("bit").IsRequired(true);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);
        builder.Property(entity => entity.UpdatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(false);

        builder.HasIndex(entity => entity.IsActive)
            .HasDatabaseName("IX_ItemTemplates_IsActive");

        builder.HasIndex(entity => entity.Code)
            .IsUnique()
            .HasDatabaseName("UQ_ItemTemplates_Code");
    }
}
