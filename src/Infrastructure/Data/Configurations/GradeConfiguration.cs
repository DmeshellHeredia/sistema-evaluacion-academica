using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaEvaluacionAcademica.Domain.Entities;

namespace SistemaEvaluacionAcademica.Infrastructure.Data.Configurations;

public class GradeConfiguration : IEntityTypeConfiguration<Grade>
{
    public void Configure(EntityTypeBuilder<Grade> builder)
    {
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Value)
            .IsRequired()
            .HasPrecision(4, 2);

        builder.Property(g => g.Period)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(g => g.Comments)
            .HasMaxLength(500);

        builder.HasIndex(g => new { g.StudentId, g.SubjectId, g.Period })
            .IsUnique();

        builder.HasOne(g => g.Student)
            .WithMany(s => s.Grades)
            .HasForeignKey(g => g.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.Subject)
            .WithMany(s => s.Grades)
            .HasForeignKey(g => g.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.Section)
            .WithMany(s => s.Grades)
            .HasForeignKey(g => g.SectionId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne(g => g.GradedByUser)
            .WithMany()
            .HasForeignKey(g => g.GradedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("Grades");
    }
}
