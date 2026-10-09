using Brouj.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brouj.Infrastructure.Persistence.Configurations;

public sealed class UnitImageConfiguration : IEntityTypeConfiguration<UnitImage>
{
    public void Configure(EntityTypeBuilder<UnitImage> builder)
    {
        builder.ToTable("UnitImages");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.UnitId).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.ImagePath).HasColumnType("nvarchar(2048)").HasMaxLength(2048).IsUnicode(true).IsRequired(true);
        builder.Property(entity => entity.AltText).HasColumnType("nvarchar(100)").HasMaxLength(100).IsUnicode(true).IsRequired(false);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);
        builder.Property(entity => entity.UpdatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(false);

        builder.HasOne(entity => entity.Unit)
            .WithMany(entity => entity.UnitImages)
            .HasForeignKey(entity => entity.UnitId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
