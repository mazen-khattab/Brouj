using Brouj.Domain.Entities;
using Brouj.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brouj.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.ReservationId).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.Status).HasConversion(new OrderStatusConverter()).HasColumnType("varchar(16)").HasMaxLength(16).IsUnicode(false).IsRequired(true);
        builder.Property(entity => entity.Number).HasColumnType("nvarchar(64)").HasMaxLength(64).IsUnicode(true).IsRequired(true);
        builder.Property(entity => entity.TotalPrice).HasColumnType("decimal(19,4)").HasPrecision(19, 4).IsRequired(true);
        builder.Property(entity => entity.CompletedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(false);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);
        builder.Property(entity => entity.UpdatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(false);

        builder.HasOne(entity => entity.Reservation)
            .WithOne(entity => entity.Order)
            .HasForeignKey<Order>(entity => entity.ReservationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => entity.Status)
            .HasDatabaseName("IX_Orders_Status");

        builder.HasIndex(entity => entity.ReservationId)
            .IsUnique()
            .HasDatabaseName("UQ_Orders_ReservationId");

        builder.HasIndex(entity => entity.Number)
            .IsUnique()
            .HasDatabaseName("UQ_Orders_Number");
    }
}
