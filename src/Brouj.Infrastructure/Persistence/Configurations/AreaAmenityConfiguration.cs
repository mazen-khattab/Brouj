using Brouj.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brouj.Infrastructure.Persistence.Configurations;

public sealed class AreaAmenityConfiguration : IEntityTypeConfiguration<AreaAmenity>
{
    public void Configure(EntityTypeBuilder<AreaAmenity> builder)
    {
        builder.ToTable("AreaAmenities");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.AreaId).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.Name).HasColumnType("nvarchar(100)").HasMaxLength(100).IsUnicode(true).IsRequired(true);
        builder.Property(entity => entity.Value).HasColumnType("nvarchar(32)").HasMaxLength(32).IsUnicode(true).IsRequired(true);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);
        builder.Property(entity => entity.UpdatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(false);

        builder.HasOne(entity => entity.Area)
            .WithMany(entity => entity.AreaAmenities)
            .HasForeignKey(entity => entity.AreaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
