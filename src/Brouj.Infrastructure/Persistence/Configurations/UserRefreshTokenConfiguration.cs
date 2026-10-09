using Brouj.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brouj.Infrastructure.Persistence.Configurations;

public sealed class UserRefreshTokenConfiguration : IEntityTypeConfiguration<UserRefreshToken>
{
    public void Configure(EntityTypeBuilder<UserRefreshToken> builder)
    {
        builder.ToTable("UserRefreshTokens");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.UserId).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.Token).HasColumnType("varchar(450)").HasMaxLength(450).IsUnicode(false).IsRequired(true);
        builder.Property(entity => entity.IsActive).HasColumnType("bit").IsRequired(true);
        builder.Property(entity => entity.ExpDate).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);

        builder.HasOne(entity => entity.User)
            .WithMany(entity => entity.UserRefreshTokens)
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entity => entity.Token)
            .HasDatabaseName("IX_UserRefreshTokens_Token");

        builder.HasIndex(entity => new { entity.UserId, entity.IsActive })
            .HasDatabaseName("IX_UserRefreshTokens_UserId_IsActive");
    }
}
