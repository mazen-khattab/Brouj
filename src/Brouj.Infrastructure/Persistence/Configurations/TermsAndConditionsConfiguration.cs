using Brouj.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brouj.Infrastructure.Persistence.Configurations;

public sealed class TermsAndConditionsConfiguration : IEntityTypeConfiguration<TermsAndConditions>
{
    public void Configure(EntityTypeBuilder<TermsAndConditions> builder)
    {
        builder.ToTable("TermsAndConditions");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.Title).HasColumnType("nvarchar(300)").HasMaxLength(300).IsUnicode(true).IsRequired(true);
        builder.Property(entity => entity.Content).HasColumnType("nvarchar(max)").IsRequired(true);
        builder.Property(entity => entity.VersionNumber).HasColumnType("int").IsRequired(true);
        builder.Property(entity => entity.IsActive).HasColumnType("bit").IsRequired(true);
        builder.Property(entity => entity.EffectiveDate).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);
        builder.Property(entity => entity.UpdatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(false);

        builder.HasIndex(entity => new { entity.IsActive, entity.EffectiveDate })
            .HasDatabaseName("IX_TermsAndConditions_IsActive_EffectiveDate");

        builder.HasIndex(entity => entity.VersionNumber)
            .IsUnique()
            .HasDatabaseName("UQ_TermsAndConditions_VersionNumber")
            .HasFilter("[IsActive] = 1");
    }
}
