using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaEvaluacionAcademica.Domain.Entities;

namespace SistemaEvaluacionAcademica.Infrastructure.Data.Configurations;

public class SectionEnrollmentConfiguration : IEntityTypeConfiguration<SectionEnrollment>
{
    public void Configure(EntityTypeBuilder<SectionEnrollment> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.StudentId, e.SectionId })
            .IsUnique();

        builder.HasIndex(e => new { e.StudentId, e.IsActive });

        builder.HasOne(e => e.Student)
            .WithMany(s => s.SectionEnrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Section)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.SectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("SectionEnrollments");
    }
}
