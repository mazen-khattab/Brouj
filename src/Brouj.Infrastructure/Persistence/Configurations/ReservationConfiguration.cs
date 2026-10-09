using Brouj.Domain.Entities;
using Brouj.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brouj.Infrastructure.Persistence.Configurations;

public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.UnitId).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.CustomerId).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.ReservationDate).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);
        builder.Property(entity => entity.Number).HasColumnType("nvarchar(64)").HasMaxLength(64).IsUnicode(true).IsRequired(true);
        builder.Property(entity => entity.Status).HasConversion(new ReservationStatusConverter()).HasColumnType("varchar(16)").HasMaxLength(16).IsUnicode(false).IsRequired(true);
        builder.Property(entity => entity.CancelledAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(false);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);
        builder.Property(entity => entity.UpdatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(false);

        builder.HasOne(entity => entity.Unit)
            .WithMany(entity => entity.Reservations)
            .HasForeignKey(entity => entity.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.Customer)
            .WithMany(entity => entity.Reservations)
            .HasForeignKey(entity => entity.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => new { entity.CustomerId, entity.UnitId, entity.Status })
            .HasDatabaseName("IX_Reservations_CustomerId_UnitId_Status");

        builder.HasIndex(entity => new { entity.UnitId, entity.Status })
            .HasDatabaseName("IX_Reservations_UnitId_Status");

        builder.HasIndex(entity => entity.Status)
            .HasDatabaseName("IX_Reservations_Status");

        builder.HasIndex(entity => entity.Number)
            .IsUnique()
            .HasDatabaseName("UQ_Reservations_Number");

        builder.HasIndex(entity => new { entity.CustomerId, entity.UnitId })
            .IsUnique()
            .HasDatabaseName("UQ_Reservations_CustomerId_UnitId_Processing")
            .HasFilter("[Status] = 'processing'");
    }
}
