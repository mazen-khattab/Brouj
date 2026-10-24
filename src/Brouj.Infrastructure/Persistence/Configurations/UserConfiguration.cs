using Brouj.Domain.Entities;
using Brouj.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brouj.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.Name).HasColumnType("nvarchar(100)").HasMaxLength(100).IsUnicode(true).IsRequired(true);
        builder.Property(entity => entity.Email).HasColumnType("varchar(254)").HasMaxLength(254).IsUnicode(false).IsRequired(true);
        builder.Property(entity => entity.Phone).HasColumnType("varchar(32)").HasMaxLength(32).IsUnicode(false).IsRequired(true);
        builder.Property(entity => entity.Role).HasConversion(new UserRoleConverter()).HasColumnType("varchar(16)").HasMaxLength(16).IsUnicode(false).IsRequired(true);
        builder.Property(entity => entity.HashPassword).HasColumnType("varchar(256)").HasMaxLength(256).IsUnicode(false).IsRequired(true);
        builder.Property(entity => entity.DeletedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(false);
        builder.Property(entity => entity.UpdatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(false);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);

        builder.HasIndex(entity => entity.Name)
            .HasDatabaseName("IX_Users_Name");

        builder.HasIndex(entity => entity.Email)
            .IsUnique()
            .HasDatabaseName("UQ_Users_Email")
            .HasFilter("[DeletedAt] IS NULL");

        builder.HasIndex(entity => entity.Phone)
            .IsUnique()
            .HasDatabaseName("UQ_Users_Phone")
            .HasFilter("[DeletedAt] IS NULL");
    }
}
