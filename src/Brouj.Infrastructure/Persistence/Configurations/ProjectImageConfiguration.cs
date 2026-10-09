using Brouj.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brouj.Infrastructure.Persistence.Configurations;

public sealed class ProjectImageConfiguration : IEntityTypeConfiguration<ProjectImage>
{
    public void Configure(EntityTypeBuilder<ProjectImage> builder)
    {
        builder.ToTable("ProjectImages");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.ProjectId).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.ImagePath).HasColumnType("nvarchar(2048)").HasMaxLength(2048).IsUnicode(true).IsRequired(true);
        builder.Property(entity => entity.AltText).HasColumnType("nvarchar(100)").HasMaxLength(100).IsUnicode(true).IsRequired(false);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);
        builder.Property(entity => entity.UpdatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(false);

        builder.HasOne(entity => entity.Project)
            .WithMany(entity => entity.ProjectImages)
            .HasForeignKey(entity => entity.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
