using Brouj.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brouj.Infrastructure.Persistence.Configurations;

public sealed class UnitAmenityConfiguration : IEntityTypeConfiguration<UnitAmenity>
{
    public void Configure(EntityTypeBuilder<UnitAmenity> builder)
    {
        builder.ToTable("UnitAmenities");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.UnitId).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.Name).HasColumnType("nvarchar(100)").HasMaxLength(100).IsUnicode(true).IsRequired(true);
        builder.Property(entity => entity.Value).HasColumnType("nvarchar(32)").HasMaxLength(32).IsUnicode(true).IsRequired(true);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);
        builder.Property(entity => entity.UpdatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(false);

        builder.HasOne(entity => entity.Unit)
            .WithMany(entity => entity.UnitAmenities)
            .HasForeignKey(entity => entity.UnitId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
