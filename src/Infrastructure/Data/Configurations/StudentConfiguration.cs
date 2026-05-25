using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaEvaluacionAcademica.Domain.Entities;

namespace SistemaEvaluacionAcademica.Infrastructure.Data.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.StudentCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(s => s.StudentCode)
            .IsUnique();

        builder.Property(s => s.Career)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Semester)
            .IsRequired();

        builder.HasMany(s => s.Grades)
            .WithOne(g => g.Student)
            .HasForeignKey(g => g.StudentId);

        builder.ToTable("Students");
    }
}
