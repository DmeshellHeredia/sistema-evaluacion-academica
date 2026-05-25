using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaEvaluacionAcademica.Domain.Entities;

namespace SistemaEvaluacionAcademica.Infrastructure.Data.Configurations;

public class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Content)
            .IsRequired()
            .HasMaxLength(5000);

        builder.HasOne(a => a.Section)
            .WithMany()
            .HasForeignKey(a => a.SectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Author)
            .WithMany()
            .HasForeignKey(a => a.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.SectionId);

        builder.ToTable("Announcements");
    }
}
