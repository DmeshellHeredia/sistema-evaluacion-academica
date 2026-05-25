using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaEvaluacionAcademica.Domain.Entities;

namespace SistemaEvaluacionAcademica.Infrastructure.Data.Configurations;

public class ActivitySubmissionConfiguration : IEntityTypeConfiguration<ActivitySubmission>
{
    public void Configure(EntityTypeBuilder<ActivitySubmission> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Content)
            .HasMaxLength(10000);

        builder.Property(s => s.Feedback)
            .HasMaxLength(2000);

        builder.Property(s => s.Score)
            .HasColumnType("decimal(5,2)");

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.HasOne(s => s.Activity)
            .WithMany(a => a.Submissions)
            .HasForeignKey(s => s.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Student)
            .WithMany()
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.ActivityId, s.StudentId })
            .IsUnique();

        builder.ToTable("ActivitySubmissions");
    }
}
