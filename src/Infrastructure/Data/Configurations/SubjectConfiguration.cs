using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaEvaluacionAcademica.Domain.Entities;

namespace SistemaEvaluacionAcademica.Infrastructure.Data.Configurations;

public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    private static readonly JsonSerializerOptions _json = new();

    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(s => s.Code)
            .IsUnique();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Description)
            .HasMaxLength(1000);

        builder.Property(s => s.Credits)
            .IsRequired();

        builder.Property(s => s.SemesterLevel)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(s => s.AppliesToAllCareers)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.Careers)
            .HasColumnName("Careers")
            .HasColumnType("nvarchar(2000)")
            .HasDefaultValueSql("'[]'")
            .HasConversion(
                v => JsonSerializer.Serialize(v, _json),
                v => (IReadOnlyList<string>)(JsonSerializer.Deserialize<List<string>>(v, _json) ?? new List<string>()));

        builder.ToTable("Subjects");
    }
}
