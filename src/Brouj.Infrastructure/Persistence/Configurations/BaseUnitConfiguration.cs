using Brouj.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brouj.Infrastructure.Persistence.Configurations;

public sealed class BaseUnitConfiguration : IEntityTypeConfiguration<BaseUnit>
{
    public void Configure(EntityTypeBuilder<BaseUnit> builder)
    {
        builder.ToTable("BaseUnits");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.ProjectId).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.Number).HasColumnType("nvarchar(64)").HasMaxLength(64).IsUnicode(true).IsRequired(true);
        builder.Property(entity => entity.MeterPrice).HasColumnType("decimal(19,4)").HasPrecision(19, 4).IsRequired(true);
        builder.Property(entity => entity.FloorNumber).HasColumnType("int").IsRequired(true);
        builder.Property(entity => entity.Size).HasColumnType("decimal(18,4)").HasPrecision(18, 4).IsRequired(true);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);
        builder.Property(entity => entity.UpdatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(false);

        builder.HasOne(entity => entity.Project)
            .WithMany(entity => entity.BaseUnits)
            .HasForeignKey(entity => entity.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entity => new { entity.ProjectId, entity.Number })
            .IsUnique()
            .HasDatabaseName("UQ_BaseUnits_ProjectId_Number");
    }
}
