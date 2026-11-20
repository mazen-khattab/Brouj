using Brouj.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brouj.Infrastructure.Persistence.Configurations;

public sealed class ContactSubmissionConfiguration : IEntityTypeConfiguration<ContactSubmission>
{
    public void Configure(EntityTypeBuilder<ContactSubmission> builder)
    {
        builder.ToTable("ContactSubmissions");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(entity => entity.Name).HasColumnType("nvarchar(150)").HasMaxLength(150).IsUnicode().IsRequired();
        builder.Property(entity => entity.Phone).HasColumnType("varchar(11)").HasMaxLength(11).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Inquiry).HasColumnType("nvarchar(2000)").HasMaxLength(2000).IsUnicode().IsRequired();
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetimeoffset(3)").HasPrecision(3).IsRequired();

        builder.HasIndex(entity => new { entity.CreatedAt, entity.Id })
            .IsDescending(true, false)
            .HasDatabaseName("IX_ContactSubmissions_CreatedAt_Id");
    }
}
