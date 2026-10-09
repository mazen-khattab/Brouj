using Brouj.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brouj.Infrastructure.Persistence.Configurations;

public sealed class InitiativeUnitConfiguration : IEntityTypeConfiguration<InitiativeUnit>
{
    public void Configure(EntityTypeBuilder<InitiativeUnit> builder)
    {
        builder.ToTable("InitiativeUnits");
        builder.HasKey(entity => entity.UnitId);

        builder.Property(entity => entity.UnitId).HasColumnType("uniqueidentifier").IsRequired(true).ValueGeneratedNever();
        builder.Property(entity => entity.NeighborMeterPrice).HasColumnType("decimal(19,4)").HasPrecision(19, 4).IsRequired(true);
        builder.Property(entity => entity.Percentage).HasColumnType("decimal(9,6)").HasPrecision(9, 6).IsRequired(true);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);
        builder.Property(entity => entity.UpdatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(false);

        builder.HasOne(entity => entity.Unit)
            .WithOne(entity => entity.InitiativeUnit)
            .HasForeignKey<InitiativeUnit>(entity => entity.UnitId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
