using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaEvaluacionAcademica.Domain.Entities;

namespace SistemaEvaluacionAcademica.Infrastructure.Data.Configurations;

public class SubjectPrerequisiteConfiguration : IEntityTypeConfiguration<SubjectPrerequisite>
{
    public void Configure(EntityTypeBuilder<SubjectPrerequisite> builder)
    {
        builder.HasKey(p => p.Id);

        builder.HasOne(p => p.Subject)
            .WithMany(s => s.Prerequisites)
            .HasForeignKey(p => p.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.PrerequisiteSubject)
            .WithMany()
            .HasForeignKey(p => p.PrerequisiteSubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.SubjectId, p.PrerequisiteSubjectId })
            .IsUnique();

        builder.ToTable("SubjectPrerequisites");
    }
}
