using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Enums;
using SistemaEvaluacionAcademica.Domain.Exceptions;
using SistemaEvaluacionAcademica.Domain.Interfaces;
using SistemaEvaluacionAcademica.Infrastructure.Data;

namespace SistemaEvaluacionAcademica.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    public IUserRepository Users { get; }
    public IStudentRepository Students { get; }
    public ISubjectRepository Subjects { get; }
    public IGradeRepository Grades { get; }
    public IRoleRepository Roles { get; }

    public UnitOfWork(AppDbContext context,
        IUserRepository users,
        IStudentRepository students,
        ISubjectRepository subjects,
        IGradeRepository grades,
        IRoleRepository roles)
    {
        _context = context;
        Users = users;
        Students = students;
        Subjects = subjects;
        Grades = grades;
        Roles = roles;
    }

    public async Task AddSectionEnrollmentAsync(SectionEnrollment enrollment, CancellationToken ct = default) =>
        await _context.SectionEnrollments.AddAsync(enrollment, ct);

    public async Task RemoveSectionEnrollmentAsync(Guid studentId, Guid sectionId, CancellationToken ct = default)
    {
        var record = await _context.SectionEnrollments
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.SectionId == sectionId && e.IsActive, ct);
        if (record is not null)
            record.Deactivate();
    }

    public Task<bool> IsEnrolledInSectionAsync(Guid studentId, Guid sectionId, CancellationToken ct = default) =>
        GuardAsync(() => _context.SectionEnrollments.AnyAsync(
            e => e.StudentId == studentId && e.SectionId == sectionId && e.IsActive, ct));

    public Task<bool> IsEnrolledInSubjectAsync(Guid studentId, Guid subjectId, CancellationToken ct = default) =>
        GuardAsync(() => _context.SectionEnrollments.AnyAsync(
            e => e.StudentId == studentId && e.Section.SubjectId == subjectId && e.IsActive, ct));

    public Task<bool> IsEnrolledInAnyOtherSectionOfSubjectAsync(
        Guid studentId, Guid subjectId, Guid? excludeSectionId, CancellationToken ct = default) =>
        GuardAsync(() => _context.SectionEnrollments.AnyAsync(
            e => e.StudentId == studentId
              && e.Section.SubjectId == subjectId
              && e.IsActive
              && (excludeSectionId == null || e.SectionId != excludeSectionId), ct));

    public async Task<SubjectSection?> GetSectionByIdAsync(Guid sectionId, CancellationToken ct = default)
    {
        var section = await GuardAsync(() => _context.SubjectSections
            .Include(s => s.Subject)
            .Include(s => s.Enrollments.Where(e => e.IsActive))
                .ThenInclude(e => e.Student)
                    .ThenInclude(st => st.User)
            .FirstOrDefaultAsync(s => s.Id == sectionId && s.IsActive, ct));

        if (section is null) return null;
        await _context.Entry(section).Reference(s => s.Professor)
            .Query().IgnoreQueryFilters().LoadAsync(ct);
        return section;
    }

    public async Task<IEnumerable<SubjectSection>> GetSectionsByProfessorAsync(Guid professorUserId, CancellationToken ct = default)
    {
        var sections = await _context.SubjectSections
            .Include(s => s.Subject)
            .Include(s => s.Enrollments.Where(e => e.IsActive))
            .Where(s => s.ProfessorId == professorUserId && s.IsActive)
            .OrderBy(s => s.Subject.SemesterLevel)
            .ThenBy(s => s.Subject.Code)
            .ThenBy(s => s.SectionCode)
            .ToListAsync(ct);
        await LoadProfessorsAsync(sections, ct);
        return sections;
    }

    public async Task<IEnumerable<SubjectSection>> GetAllSectionsAsync(CancellationToken ct = default)
    {
        var sections = await _context.SubjectSections
            .Include(s => s.Subject)
            .Include(s => s.Enrollments.Where(e => e.IsActive))
            .Where(s => s.IsActive)
            .OrderBy(s => s.Subject.SemesterLevel)
            .ThenBy(s => s.Subject.Code)
            .ThenBy(s => s.SectionCode)
            .ToListAsync(ct);
        await LoadProfessorsAsync(sections, ct);
        return sections;
    }

    public async Task<IEnumerable<SubjectSection>> GetSectionsBySubjectAsync(Guid subjectId, CancellationToken ct = default)
    {
        var sections = await _context.SubjectSections
            .Include(s => s.Enrollments.Where(e => e.IsActive))
            .Where(s => s.SubjectId == subjectId && s.IsActive)
            .ToListAsync(ct);
        await LoadProfessorsAsync(sections, ct);
        return sections;
    }

    public async Task AddSectionAsync(SubjectSection section, CancellationToken ct = default) =>
        await _context.SubjectSections.AddAsync(section, ct);

    public async Task<IEnumerable<Subject>> GetSubjectsByCareerAsync(string career, CancellationToken ct = default)
    {
        // Filtra del lado SQL con OPENJSON para no cargar materias no relacionadas en memoria.
        // Las materias con AppliesToAllCareers se detectan por la columna booleana; las específicas
        // se detectan escaneando el array JSON con OPENJSON — ambas rutas aprovechan índices SQL.
        return await _context.Subjects
            .FromSqlInterpolated($"""
                SELECT * FROM Subjects
                WHERE IsActive = 1
                  AND (AppliesToAllCareers = 1
                       OR EXISTS (SELECT 1 FROM OPENJSON(Careers) WHERE [value] = {career}))
                """)
            .AsSplitQuery()
            .Include(s => s.Sections.Where(sec => sec.IsActive))
                .ThenInclude(sec => sec.Professor)
            .Include(s => s.Sections.Where(sec => sec.IsActive))
                .ThenInclude(sec => sec.Enrollments.Where(e => e.IsActive))
            .Include(s => s.Prerequisites)
            .OrderBy(s => s.SemesterLevel)
            .ThenBy(s => s.Code)
            .ToListAsync(ct);
    }

    public async Task<(IEnumerable<SubjectSection> Items, int TotalCount)> GetAllSectionsPagedAsync(
        int page, int pageSize, string? search = null, CancellationToken ct = default)
    {
        var baseQuery = _context.SubjectSections.Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            baseQuery = baseQuery.Where(s =>
                s.Subject.Name.Contains(search) ||
                s.Subject.Code.Contains(search) ||
                s.SectionCode.Contains(search));
        }

        var total = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .Include(s => s.Subject)
            .Include(s => s.Enrollments.Where(e => e.IsActive))
            .OrderBy(s => s.Subject.SemesterLevel)
            .ThenBy(s => s.Subject.Code)
            .ThenBy(s => s.SectionCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        await LoadProfessorsAsync(items, ct);
        return (items, total);
    }

    public async Task<IEnumerable<SubjectPrerequisite>> GetPrerequisitesAsync(Guid subjectId, CancellationToken ct = default) =>
        await _context.SubjectPrerequisites
            .Where(p => p.SubjectId == subjectId && p.IsActive)
            .ToListAsync(ct);

    public async Task<IEnumerable<SubjectPrerequisite>> GetAllActivePrerequisitesAsync(CancellationToken ct = default) =>
        await _context.SubjectPrerequisites
            .Where(p => p.IsActive)
            .ToListAsync(ct);

    public async Task<bool> PrerequisiteExistsAsync(Guid subjectId, Guid prereqSubjectId, CancellationToken ct = default) =>
        await _context.SubjectPrerequisites
            .AnyAsync(p => p.SubjectId == subjectId && p.PrerequisiteSubjectId == prereqSubjectId && p.IsActive, ct);

    public async Task AddPrerequisiteAsync(SubjectPrerequisite prereq, CancellationToken ct = default) =>
        await _context.SubjectPrerequisites.AddAsync(prereq, ct);

    public async Task RemovePrerequisiteAsync(Guid subjectId, Guid prereqSubjectId, CancellationToken ct = default)
    {
        var record = await _context.SubjectPrerequisites
            .FirstOrDefaultAsync(p => p.SubjectId == subjectId && p.PrerequisiteSubjectId == prereqSubjectId && p.IsActive, ct);
        if (record is not null)
            _context.SubjectPrerequisites.Remove(record);
    }

    public async Task RemoveAllPrerequisitesForSubjectAsync(Guid subjectId, CancellationToken ct = default)
    {
        var records = await _context.SubjectPrerequisites
            .Where(p => p.SubjectId == subjectId && p.IsActive)
            .ToListAsync(ct);
        _context.SubjectPrerequisites.RemoveRange(records);
    }

    public async Task<IEnumerable<AcademicPeriod>> GetAllPeriodsAsync(CancellationToken ct = default) =>
        await _context.AcademicPeriods
            .OrderByDescending(p => p.StartDate)
            .ToListAsync(ct);

    public async Task<AcademicPeriod?> GetPeriodByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.AcademicPeriods.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<AcademicPeriod?> GetActivePeriodAsync(CancellationToken ct = default) =>
        await _context.AcademicPeriods.FirstOrDefaultAsync(p => p.IsEnrollmentOpen, ct);

    public async Task<bool> IsEnrollmentOpenAsync(CancellationToken ct = default) =>
        await _context.AcademicPeriods.AnyAsync(p => p.IsEnrollmentOpen, ct);

    public async Task AddPeriodAsync(AcademicPeriod period, CancellationToken ct = default) =>
        await _context.AcademicPeriods.AddAsync(period, ct);

    public async Task RemovePeriodAsync(Guid id, CancellationToken ct = default)
    {
        var period = await _context.AcademicPeriods.FindAsync([id], ct);
        if (period is not null)
            _context.AcademicPeriods.Remove(period);
    }

    public async Task CloseAllPeriodsAsync(CancellationToken ct = default)
    {
        var open = await _context.AcademicPeriods
            .Where(p => p.IsEnrollmentOpen)
            .ToListAsync(ct);
        foreach (var p in open)
            p.CloseEnrollment();
    }

    public async Task<IEnumerable<Announcement>> GetAnnouncementsBySectionAsync(Guid sectionId, CancellationToken ct = default) =>
        await _context.Announcements
            .Include(a => a.Author)
            .Where(a => a.SectionId == sectionId && a.IsActive)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public async Task<Announcement?> GetAnnouncementByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Announcements
            .Include(a => a.Author)
            .FirstOrDefaultAsync(a => a.Id == id && a.IsActive, ct);

    public async Task AddAnnouncementAsync(Announcement announcement, CancellationToken ct = default) =>
        await _context.Announcements.AddAsync(announcement, ct);

    public async Task<IEnumerable<Activity>> GetActivitiesBySectionAsync(Guid sectionId, CancellationToken ct = default) =>
        await _context.Activities
            .Include(a => a.Submissions)
            .Where(a => a.SectionId == sectionId && a.IsActive)
            .OrderBy(a => a.DueDate)
            .ToListAsync(ct);

    public async Task<Activity?> GetActivityByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Activities
            .Include(a => a.Submissions)
                .ThenInclude(s => s.Student)
                    .ThenInclude(st => st.User)
            .FirstOrDefaultAsync(a => a.Id == id && a.IsActive, ct);

    public async Task AddActivityAsync(Activity activity, CancellationToken ct = default) =>
        await _context.Activities.AddAsync(activity, ct);

    public async Task<IEnumerable<ActivitySubmission>> GetSubmissionsByActivityAsync(Guid activityId, CancellationToken ct = default) =>
        await _context.ActivitySubmissions
            .Include(s => s.Student)
                .ThenInclude(st => st.User)
            .Where(s => s.ActivityId == activityId && s.IsActive)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync(ct);

    public async Task<ActivitySubmission?> GetSubmissionAsync(Guid activityId, Guid studentId, CancellationToken ct = default) =>
        await _context.ActivitySubmissions
            .FirstOrDefaultAsync(s => s.ActivityId == activityId && s.StudentId == studentId && s.IsActive, ct);

    public async Task<ActivitySubmission?> GetSubmissionByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.ActivitySubmissions
            .Include(s => s.Student)
                .ThenInclude(st => st.User)
            .Include(s => s.Activity)
            .FirstOrDefaultAsync(s => s.Id == id && s.IsActive, ct);

    public async Task<IEnumerable<ActivitySubmission>> GetSubmissionsByStudentAsync(Guid studentId, Guid sectionId, CancellationToken ct = default) =>
        await _context.ActivitySubmissions
            .Include(s => s.Activity)
            .Where(s => s.StudentId == studentId && s.Activity.SectionId == sectionId && s.IsActive)
            .ToListAsync(ct);

    public async Task AddSubmissionAsync(ActivitySubmission submission, CancellationToken ct = default) =>
        await _context.ActivitySubmissions.AddAsync(submission, ct);

    public async Task<IEnumerable<SubjectSection>> GetEnrolledSectionsAsync(Guid studentId, CancellationToken ct = default)
    {
        var sections = await _context.SubjectSections
            .Include(s => s.Subject)
            .Where(s => s.IsActive && s.Enrollments.Any(e => e.StudentId == studentId && e.IsActive))
            .ToListAsync(ct);
        await LoadProfessorsAsync(sections, ct);
        return sections;
    }

    public async Task<IEnumerable<Student>> GetStudentsBySectionAsync(Guid sectionId, CancellationToken ct = default) =>
        await _context.SectionEnrollments
            .Include(e => e.Student)
                .ThenInclude(st => st.User)
            .Where(e => e.SectionId == sectionId && e.IsActive)
            .Select(e => e.Student)
            .OrderBy(s => s.User.LastName)
            .ThenBy(s => s.User.FirstName)
            .ToListAsync(ct);

    public async Task<Dictionary<Guid, int>> GetSectionCountsByProfessorsAsync(
        IEnumerable<Guid> professorIds, CancellationToken ct = default)
    {
        var ids = professorIds.ToHashSet();
        return await _context.SubjectSections
            .Where(s => s.IsActive && ids.Contains(s.ProfessorId))
            .GroupBy(s => s.ProfessorId)
            .Select(g => new { ProfessorId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProfessorId, x => x.Count, ct);
    }

    public async Task<IReadOnlyList<(Guid Id, string SubjectCode, string SubjectName, string SectionCode, DayOfWeekType DayOfWeek, TimeOnly StartTime, TimeOnly EndTime)>> GetSectionsLookupAsync(
        string? search = null, int limit = 50, CancellationToken ct = default)
    {
        var query = _context.SubjectSections.Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s =>
                s.Subject.Code.ToLower().Contains(term) ||
                s.Subject.Name.ToLower().Contains(term) ||
                s.SectionCode.ToLower().Contains(term));
        }

        var raw = await query
            .OrderBy(s => s.Subject.SemesterLevel)
            .ThenBy(s => s.Subject.Code)
            .ThenBy(s => s.SectionCode)
            .Take(limit)
            .Select(s => new
            {
                s.Id,
                SubjectCode = s.Subject.Code,
                SubjectName = s.Subject.Name,
                s.SectionCode,
                s.DayOfWeek,
                s.StartTime,
                s.EndTime,
            })
            .ToListAsync(ct);

        return raw
            .Select(x => (x.Id, x.SubjectCode, x.SubjectName, x.SectionCode, x.DayOfWeek, x.StartTime, x.EndTime))
            .ToList();
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2627 or 2601 })
        {
            throw new ConcurrencyException("Ya existe una inscripción para este estudiante en esta sección.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 1205 })
        {
            throw new ConcurrencyException("Conflicto de concurrencia al inscribir. Intenta de nuevo.");
        }
        // EF Core envuelve fallos transitorios en InvalidOperationException cuando SaveChanges se
        // llama dentro de una transacción manual (no puede reintentar automáticamente).
        catch (InvalidOperationException ex)
            when (ex.InnerException is DbUpdateException { InnerException: SqlException { Number: 1205 } })
        {
            throw new ConcurrencyException("Conflicto de concurrencia al inscribir. Intenta de nuevo.");
        }
    }

    public async Task BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default) =>
        _transaction = await _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null) return;
        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        catch
        {
            // El servidor puede haber revertido ya la transacción (ej: víctima de deadlock).
            // Se ignora el error de rollback redundante para que los llamadores reciban la excepción original.
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    // Convierte excepciones de deadlock de SQL Server (1205) a ConcurrencyException
    // para que la capa Application no dependa de SqlException directamente.
    // Se aplica a lecturas dentro de transacciones SERIALIZABLE, donde los range-S locks
    // pueden participar en ciclos de deadlock con el INSERT concurrente.
    private static async Task<T> GuardAsync<T>(Func<Task<T>> op)
    {
        try
        {
            return await op();
        }
        catch (SqlException ex) when (ex.Number == 1205)
        {
            throw new ConcurrencyException("Conflicto de concurrencia al inscribir. Intenta de nuevo.");
        }
        catch (InvalidOperationException ex)
            when (ex.InnerException is SqlException { Number: 1205 })
        {
            throw new ConcurrencyException("Conflicto de concurrencia al inscribir. Intenta de nuevo.");
        }
    }

    // Carga los profesores de las secciones en una sola consulta, omitiendo el filtro global IsActive
    // para que las secciones cuyo profesor fue desactivado se devuelvan con la nav Professor no-nula.
    // El mapeo del servicio verifica Professor.IsActive para decidir si mostrar el nombre o un fallback.
    private async Task LoadProfessorsAsync(IList<SubjectSection> sections, CancellationToken ct)
    {
        if (sections.Count == 0) return;

        var ids = sections.Select(s => s.ProfessorId).ToHashSet();
        var professors = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

        foreach (var section in sections)
        {
            var reference = _context.Entry(section).Reference(s => s.Professor);
            if (!reference.IsLoaded)
                reference.CurrentValue = professors.GetValueOrDefault(section.ProfessorId);
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
