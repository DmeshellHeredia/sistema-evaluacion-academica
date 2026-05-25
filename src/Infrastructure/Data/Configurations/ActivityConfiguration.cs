using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaEvaluacionAcademica.Domain.Entities;

namespace SistemaEvaluacionAcademica.Infrastructure.Data.Configurations;

public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Description)
            .IsRequired()
            .HasMaxLength(5000);

        builder.Property(a => a.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(a => a.MaxScore)
            .HasColumnType("decimal(5,2)");

        builder.Property(a => a.Weight)
            .HasColumnType("decimal(5,2)");

        builder.Property(a => a.ResourceUrl)
            .HasMaxLength(500);

        builder.HasOne(a => a.Section)
            .WithMany()
            .HasForeignKey(a => a.SectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.SectionId);

        builder.ToTable("Activities");
    }
}
