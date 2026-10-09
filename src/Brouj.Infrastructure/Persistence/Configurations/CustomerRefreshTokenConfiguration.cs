using Brouj.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brouj.Infrastructure.Persistence.Configurations;

public sealed class CustomerRefreshTokenConfiguration : IEntityTypeConfiguration<CustomerRefreshToken>
{
    public void Configure(EntityTypeBuilder<CustomerRefreshToken> builder)
    {
        builder.ToTable("CustomerRefreshTokens");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.CustomerId).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.Token).HasColumnType("varchar(450)").HasMaxLength(450).IsUnicode(false).IsRequired(true);
        builder.Property(entity => entity.IsActive).HasColumnType("bit").IsRequired(true);
        builder.Property(entity => entity.ExpDate).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);

        builder.HasOne(entity => entity.Customer)
            .WithMany(entity => entity.CustomerRefreshTokens)
            .HasForeignKey(entity => entity.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entity => entity.Token)
            .HasDatabaseName("IX_CustomerRefreshTokens_Token");

        builder.HasIndex(entity => new { entity.CustomerId, entity.IsActive })
            .HasDatabaseName("IX_CustomerRefreshTokens_CustomerId_IsActive");
    }
}
