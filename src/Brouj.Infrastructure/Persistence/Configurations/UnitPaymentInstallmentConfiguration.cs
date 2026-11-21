using Brouj.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brouj.Infrastructure.Persistence.Configurations;

public sealed class UnitPaymentInstallmentConfiguration : IEntityTypeConfiguration<UnitPaymentInstallment>
{
    public void Configure(EntityTypeBuilder<UnitPaymentInstallment> builder)
    {
        builder.ToTable("UnitPaymentInstallments");
        builder.HasKey(installment => installment.Id);

        builder.Property(installment => installment.Id).HasColumnType("uniqueidentifier");
        builder.Property(installment => installment.UnitId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(installment => installment.InstallmentName)
            .HasColumnType("nvarchar(100)").HasMaxLength(100).IsUnicode().IsRequired();
        builder.Property(installment => installment.InstallmentNumber).HasColumnType("int").IsRequired();
        builder.Property(installment => installment.InstallmentPercentage)
            .HasColumnType("decimal(7,4)").HasPrecision(7, 4).IsRequired();
        builder.Property(installment => installment.InstallmentPeriodMonths).HasColumnType("int").IsRequired();

        builder.HasOne(installment => installment.Unit).WithMany()
            .HasForeignKey(installment => installment.UnitId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(installment => new { installment.UnitId, installment.InstallmentNumber })
            .IsUnique().HasDatabaseName("UQ_UnitPaymentInstallments_UnitId_InstallmentNumber");
    }
}
