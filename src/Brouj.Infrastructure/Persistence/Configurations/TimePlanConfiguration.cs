using Brouj.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brouj.Infrastructure.Persistence.Configurations;

public sealed class TimePlanConfiguration : IEntityTypeConfiguration<TimePlan>
{
    public void Configure(EntityTypeBuilder<TimePlan> builder)
    {
        builder.ToTable("TimePlans");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.ProjectId).HasColumnType("uniqueidentifier").IsRequired(true);
        builder.Property(entity => entity.Name).HasColumnType("nvarchar(100)").HasMaxLength(100).IsUnicode(true).IsRequired(true);
        builder.Property(entity => entity.DeliveryDate).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);
        builder.Property(entity => entity.UpdatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(false);

        builder.HasOne(entity => entity.Project)
            .WithOne(entity => entity.TimePlan)
            .HasForeignKey<TimePlan>(entity => entity.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entity => entity.ProjectId)
            .IsUnique()
            .HasDatabaseName("UQ_TimePlans_ProjectId");
    }
}
