using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaEvaluacionAcademica.Domain.Entities;

namespace SistemaEvaluacionAcademica.Infrastructure.Data.Configurations;

public class SubjectSectionConfiguration : IEntityTypeConfiguration<SubjectSection>
{
    public void Configure(EntityTypeBuilder<SubjectSection> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.SectionCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(s => s.DayOfWeek)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(s => s.Modality)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(s => s.Capacity)
            .IsRequired();

        builder.Property(s => s.Classroom)
            .HasMaxLength(50)
            .HasDefaultValue(string.Empty);

        builder.HasOne(s => s.Subject)
            .WithMany(sub => sub.Sections)
            .HasForeignKey(s => s.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Professor)
            .WithMany()
            .HasForeignKey(s => s.ProfessorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.SubjectId, s.SectionCode })
            .IsUnique();

        builder.HasIndex(s => new { s.SubjectId, s.IsActive });

        builder.Ignore(s => s.ActiveEnrollmentCount);
        builder.Ignore(s => s.IsFull);

        builder.ToTable("SubjectSections");
    }
}
