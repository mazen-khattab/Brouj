using Brouj.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brouj.Infrastructure.Persistence.Configurations;

public sealed class StageTemplateConfiguration : IEntityTypeConfiguration<StageTemplate>
{
    public void Configure(EntityTypeBuilder<StageTemplate> builder)
    {
        builder.ToTable("StageTemplates");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.Name).HasColumnType("nvarchar(100)").HasMaxLength(100).IsUnicode(true).IsRequired(true);
        builder.Property(entity => entity.Code).HasColumnType("nvarchar(64)").HasMaxLength(64).IsUnicode(true).IsRequired(true);
        builder.Property(entity => entity.IsActive).HasColumnType("bit").IsRequired(true);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);
        builder.Property(entity => entity.UpdatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(false);

        builder.HasIndex(entity => entity.IsActive)
            .HasDatabaseName("IX_StageTemplates_IsActive");

        builder.HasIndex(entity => entity.Code)
            .IsUnique()
            .HasDatabaseName("UQ_StageTemplates_Code");
    }
}
