using Brouj.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brouj.Infrastructure.Persistence.Configurations;

public sealed class InitiativeReservationConfiguration : IEntityTypeConfiguration<InitiativeReservation>
{
    public void Configure(EntityTypeBuilder<InitiativeReservation> builder)
    {
        builder.ToTable("InitiativeReservations");
        builder.HasKey(entity => entity.ReservationId);

        builder.Property(entity => entity.ReservationId).HasColumnType("uniqueidentifier").IsRequired(true).ValueGeneratedNever();
        builder.Property(entity => entity.IdentityNumber).HasColumnType("varchar(64)").HasMaxLength(64).IsUnicode(false).IsRequired(true);
        builder.Property(entity => entity.AgeGroup).HasColumnType("varchar(16)").HasMaxLength(16).IsUnicode(false).IsRequired(true);
        builder.Property(entity => entity.PlaceOfResidence).HasColumnType("nvarchar(100)").HasMaxLength(100).IsUnicode(true).IsRequired(true);
        builder.Property(entity => entity.FinancialBracket).HasColumnType("int").IsRequired(true);
        builder.Property(entity => entity.RepaymentPeriod).HasColumnType("int").IsRequired(true);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);
        builder.Property(entity => entity.UpdatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(false);

        builder.HasOne(entity => entity.Reservation)
            .WithOne(reservation => reservation.InitiativeReservation)
            .HasForeignKey<InitiativeReservation>(entity => entity.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
