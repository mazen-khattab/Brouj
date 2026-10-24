using Brouj.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brouj.Infrastructure.Persistence.Configurations;

public sealed class InitiativeProjectConfiguration : IEntityTypeConfiguration<InitiativeProject>
{
    public void Configure(EntityTypeBuilder<InitiativeProject> builder)
    {
        builder.ToTable("InitiativeProjects");
        builder.HasKey(entity => entity.ProjectId);

        builder.Property(entity => entity.ProjectId).HasColumnType("uniqueidentifier").IsRequired(true).ValueGeneratedNever();
        builder.Property(entity => entity.NeighborPrice).HasColumnType("decimal(19,4)").HasPrecision(19, 4).IsRequired(true);
        builder.Property(entity => entity.DrawDate).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(true);
        builder.Property(entity => entity.UpdatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired(false);

        builder.HasOne(entity => entity.Project)
            .WithOne(entity => entity.InitiativeProject)
            .HasForeignKey<InitiativeProject>(entity => entity.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
