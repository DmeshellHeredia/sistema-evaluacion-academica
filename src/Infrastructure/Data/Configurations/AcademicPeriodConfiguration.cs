using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaEvaluacionAcademica.Domain.Entities;

namespace SistemaEvaluacionAcademica.Infrastructure.Data.Configurations;

public class AcademicPeriodConfiguration : IEntityTypeConfiguration<AcademicPeriod>
{
    public void Configure(EntityTypeBuilder<AcademicPeriod> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(p => p.Code)
            .IsUnique();

        builder.Property(p => p.StartDate)
            .IsRequired();

        builder.Property(p => p.EndDate)
            .IsRequired();

        builder.Property(p => p.IsEnrollmentOpen)
            .IsRequired()
            .HasDefaultValue(false);

        // A lo sumo un período puede tener IsEnrollmentOpen = true a la vez.
        // La capa Application (CloseAllPeriodsAsync) lo previene en operación normal;
        // este índice único filtrado es el respaldo a nivel de BD para peticiones concurrentes.
        builder.HasIndex(p => p.IsEnrollmentOpen)
            .HasFilter("[IsEnrollmentOpen] = 1")
            .IsUnique();

        builder.ToTable("AcademicPeriods");
    }
}
