using System.Data;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Enums;

namespace SistemaEvaluacionAcademica.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IStudentRepository Students { get; }
    ISubjectRepository Subjects { get; }
    IGradeRepository Grades { get; }
    IRoleRepository Roles { get; }

    // Inscripción a sección
    Task AddSectionEnrollmentAsync(SectionEnrollment enrollment, CancellationToken ct = default);
    Task RemoveSectionEnrollmentAsync(Guid studentId, Guid sectionId, CancellationToken ct = default);
    Task<bool> IsEnrolledInSectionAsync(Guid studentId, Guid sectionId, CancellationToken ct = default);
    Task<bool> IsEnrolledInSubjectAsync(Guid studentId, Guid subjectId, CancellationToken ct = default);
    Task<bool> IsEnrolledInAnyOtherSectionOfSubjectAsync(Guid studentId, Guid subjectId, Guid? excludeSectionId, CancellationToken ct = default);

    // Sections
    Task<SubjectSection?> GetSectionByIdAsync(Guid sectionId, CancellationToken ct = default);
    Task<IEnumerable<SubjectSection>> GetSectionsByProfessorAsync(Guid professorUserId, CancellationToken ct = default);
    Task<IEnumerable<SubjectSection>> GetAllSectionsAsync(CancellationToken ct = default);
    Task<(IEnumerable<SubjectSection> Items, int TotalCount)> GetAllSectionsPagedAsync(
        int page, int pageSize, string? search = null, CancellationToken ct = default);
    Task<IEnumerable<SubjectSection>> GetSectionsBySubjectAsync(Guid subjectId, CancellationToken ct = default);
    Task AddSectionAsync(SubjectSection section, CancellationToken ct = default);

    // Materias por carrera (para catálogo)
    Task<IEnumerable<Subject>> GetSubjectsByCareerAsync(string career, CancellationToken ct = default);

    // Prerequisites
    Task<IEnumerable<SubjectPrerequisite>> GetPrerequisitesAsync(Guid subjectId, CancellationToken ct = default);
    Task<IEnumerable<SubjectPrerequisite>> GetAllActivePrerequisitesAsync(CancellationToken ct = default);
    Task<bool> PrerequisiteExistsAsync(Guid subjectId, Guid prereqSubjectId, CancellationToken ct = default);
    Task AddPrerequisiteAsync(SubjectPrerequisite prereq, CancellationToken ct = default);
    Task RemovePrerequisiteAsync(Guid subjectId, Guid prereqSubjectId, CancellationToken ct = default);
    Task RemoveAllPrerequisitesForSubjectAsync(Guid subjectId, CancellationToken ct = default);

    // Períodos académicos
    Task<IEnumerable<AcademicPeriod>> GetAllPeriodsAsync(CancellationToken ct = default);
    Task<AcademicPeriod?> GetPeriodByIdAsync(Guid id, CancellationToken ct = default);
    Task<AcademicPeriod?> GetActivePeriodAsync(CancellationToken ct = default);
    Task<bool> IsEnrollmentOpenAsync(CancellationToken ct = default);
    Task AddPeriodAsync(AcademicPeriod period, CancellationToken ct = default);
    Task RemovePeriodAsync(Guid id, CancellationToken ct = default);
    Task CloseAllPeriodsAsync(CancellationToken ct = default);

    // Announcements
    Task<IEnumerable<Announcement>> GetAnnouncementsBySectionAsync(Guid sectionId, CancellationToken ct = default);
    Task<Announcement?> GetAnnouncementByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAnnouncementAsync(Announcement announcement, CancellationToken ct = default);

    // Activities
    Task<IEnumerable<Activity>> GetActivitiesBySectionAsync(Guid sectionId, CancellationToken ct = default);
    Task<Activity?> GetActivityByIdAsync(Guid id, CancellationToken ct = default);
    Task AddActivityAsync(Activity activity, CancellationToken ct = default);

    // Submissions
    Task<IEnumerable<ActivitySubmission>> GetSubmissionsByActivityAsync(Guid activityId, CancellationToken ct = default);
    Task<ActivitySubmission?> GetSubmissionAsync(Guid activityId, Guid studentId, CancellationToken ct = default);
    Task<ActivitySubmission?> GetSubmissionByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<ActivitySubmission>> GetSubmissionsByStudentAsync(Guid studentId, Guid sectionId, CancellationToken ct = default);
    Task AddSubmissionAsync(ActivitySubmission submission, CancellationToken ct = default);

    // Secciones inscritas (para horario y detección de conflictos)
    Task<IEnumerable<SubjectSection>> GetEnrolledSectionsAsync(Guid studentId, CancellationToken ct = default);

    // Participantes de la sección
    Task<IEnumerable<Student>> GetStudentsBySectionAsync(Guid sectionId, CancellationToken ct = default);
    Task<Dictionary<Guid, int>> GetSectionCountsByProfessorsAsync(IEnumerable<Guid> professorIds, CancellationToken ct = default);

    // Consultas de búsqueda rápida (sin navegaciones Include)
    Task<IReadOnlyList<(Guid Id, string SubjectCode, string SubjectName, string SectionCode, DayOfWeekType DayOfWeek, TimeOnly StartTime, TimeOnly EndTime)>> GetSectionsLookupAsync(
        string? search = null, int limit = 50, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
