using Brouj.Domain.Entities;
using Brouj.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brouj.Infrastructure.Persistence.Configurations;

public sealed class BaseProjectConfiguration : IEntityTypeConfiguration<BaseProject>
{
    public void Configure(EntityTypeBuilder<BaseProject> builder)
    {
        builder.ToTable("BaseProjects");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.Name).HasColumnType("nvarchar(100)").HasMaxLength(100).IsUnicode(true).IsRequired(true);
        builder.Property(entity => entity.AreaId).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.Description).HasColumnType("nvarchar(max)").IsRequired(false);
        builder.Property(entity => entity.Latitude).HasColumnType("decimal(10,7)").HasPrecision(10, 7).IsRequired(false);
        builder.Property(entity => entity.Longitude).HasColumnType("decimal(10,7)").HasPrecision(10, 7).IsRequired(false);
        builder.Property(entity => entity.Size).HasColumnType("decimal(18,4)").HasPrecision(18, 4).IsRequired(true);
        builder.Property(entity => entity.ProjectType).HasConversion(new ProjectTypeConverter()).HasColumnType("varchar(16)").HasMaxLength(16).IsUnicode(false).IsRequired(true);
        builder.Property(entity => entity.Price).HasColumnType("decimal(19,4)").HasPrecision(19, 4).IsRequired(true);
        builder.Property(entity => entity.FloorCount).HasColumnType("int").IsRequired(true);
        builder.Property(entity => entity.PlannedUnitCount).HasColumnType("int").IsRequired(true);
        builder.Property(entity => entity.GaragePrice).HasColumnType("decimal(19,4)").HasPrecision(19, 4).IsRequired(false);
        builder.Property(entity => entity.IsGarage).HasColumnType("bit").IsRequired(true);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);
        builder.Property(entity => entity.UpdatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(false);

        builder.HasOne(entity => entity.Area)
            .WithMany(entity => entity.BaseProjects)
            .HasForeignKey(entity => entity.AreaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => entity.ProjectType)
            .HasDatabaseName("IX_BaseProjects_ProjectType");
    }
}
