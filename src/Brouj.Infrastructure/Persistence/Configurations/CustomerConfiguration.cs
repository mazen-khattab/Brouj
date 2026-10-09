using Brouj.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brouj.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.FName).HasColumnType("nvarchar(100)").HasMaxLength(100).IsUnicode(true).IsRequired(true);
        builder.Property(entity => entity.LName).HasColumnType("nvarchar(100)").HasMaxLength(100).IsUnicode(true).IsRequired(true);
        builder.Property(entity => entity.Phone).HasColumnType("varchar(32)").HasMaxLength(32).IsUnicode(false).IsRequired(true);
        builder.Property(entity => entity.IdentityNumber).HasColumnType("varchar(64)").HasMaxLength(64).IsUnicode(false).IsRequired(true);
        builder.Property(entity => entity.Email).HasColumnType("varchar(254)").HasMaxLength(254).IsUnicode(false).IsRequired(true);
        builder.Property(entity => entity.HashPassword).HasColumnType("varchar(256)").HasMaxLength(256).IsUnicode(false).IsRequired(true);
        builder.Property(entity => entity.DeletedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(false);
        builder.Property(entity => entity.UpdatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(false);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);

        builder.HasIndex(entity => new { entity.FName, entity.LName })
            .HasDatabaseName("IX_Customers_FName_LName");

        builder.HasIndex(entity => entity.Email)
            .IsUnique()
            .HasDatabaseName("UQ_Customers_Email")
            .HasFilter("[DeletedAt] IS NULL");

        builder.HasIndex(entity => entity.Phone)
            .IsUnique()
            .HasDatabaseName("UQ_Customers_Phone")
            .HasFilter("[DeletedAt] IS NULL");

        builder.HasIndex(entity => entity.IdentityNumber)
            .IsUnique()
            .HasDatabaseName("UQ_Customers_IdentityNumber")
            .HasFilter("[DeletedAt] IS NULL");
    }
}
