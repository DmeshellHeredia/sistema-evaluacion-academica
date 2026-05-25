using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Enums;

namespace SistemaEvaluacionAcademica.Infrastructure.Data;

public class AppDbContext : DbContext
{
    private static readonly DateTime SeedDate = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<SubjectSection> SubjectSections => Set<SubjectSection>();
    public DbSet<SectionEnrollment> SectionEnrollments => Set<SectionEnrollment>();
    public DbSet<SubjectPrerequisite> SubjectPrerequisites => Set<SubjectPrerequisite>();
    public DbSet<AcademicPeriod> AcademicPeriods => Set<AcademicPeriod>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<ActivitySubmission> ActivitySubmissions => Set<ActivitySubmission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType) || entityType.IsOwned())
                continue;

            var param = Expression.Parameter(entityType.ClrType, "e");
            var filter = Expression.Lambda(Expression.Property(param, nameof(BaseEntity.IsActive)), param);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }

        modelBuilder.Entity<Role>().HasData(
            new
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Type = RoleType.Admin,
                Name = "Admin",
                Description = "Administrador del sistema",
                CreatedAt = SeedDate,
                IsActive = true,
                UpdatedAt = (DateTime?)null
            },
            new
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Type = RoleType.Profesor,
                Name = "Profesor",
                Description = "Docente que imparte materias",
                CreatedAt = SeedDate,
                IsActive = true,
                UpdatedAt = (DateTime?)null
            },
            new
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                Type = RoleType.Estudiante,
                Name = "Estudiante",
                Description = "Alumno matriculado en el sistema",
                CreatedAt = SeedDate,
                IsActive = true,
                UpdatedAt = (DateTime?)null
            }
        );
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Property(nameof(BaseEntity.UpdatedAt)).CurrentValue = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
