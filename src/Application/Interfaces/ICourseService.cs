using SistemaEvaluacionAcademica.Application.Common;
using SistemaEvaluacionAcademica.Application.DTOs.Courses;

namespace SistemaEvaluacionAcademica.Application.Interfaces;

public interface ICourseService
{
    // Resumen
    Task<Result<CourseOverviewDto>> GetOverviewAsync(Guid sectionId, Guid requestingUserId, string userRole, CancellationToken ct = default);

    // Participantes
    Task<Result<IEnumerable<ParticipantDto>>> GetParticipantsAsync(Guid sectionId, Guid requestingUserId, string userRole, CancellationToken ct = default);

    // Anuncios
    Task<Result<IEnumerable<AnnouncementDto>>> GetAnnouncementsAsync(Guid sectionId, Guid requestingUserId, string userRole, CancellationToken ct = default);
    Task<Result<AnnouncementDto>> CreateAnnouncementAsync(Guid sectionId, CreateAnnouncementDto dto, Guid authorId, CancellationToken ct = default);
    Task<Result<AnnouncementDto>> UpdateAnnouncementAsync(Guid id, UpdateAnnouncementDto dto, Guid requestingUserId, CancellationToken ct = default);
    Task<Result<bool>> DeleteAnnouncementAsync(Guid id, Guid requestingUserId, CancellationToken ct = default);

    // Actividades
    Task<Result<IEnumerable<ActivityDto>>> GetActivitiesAsync(Guid sectionId, Guid requestingUserId, string userRole, CancellationToken ct = default);
    Task<Result<ActivityDto>> GetActivityAsync(Guid activityId, Guid requestingUserId, string userRole, CancellationToken ct = default);
    Task<Result<ActivityDto>> CreateActivityAsync(Guid sectionId, CreateActivityDto dto, Guid professorId, CancellationToken ct = default);
    Task<Result<ActivityDto>> UpdateActivityAsync(Guid activityId, UpdateActivityDto dto, Guid requestingUserId, CancellationToken ct = default);
    Task<Result<bool>> DeleteActivityAsync(Guid activityId, Guid requestingUserId, CancellationToken ct = default);

    // Entregas (vista Profesor)
    Task<Result<IEnumerable<SubmissionDto>>> GetSubmissionsForActivityAsync(Guid activityId, Guid requestingUserId, CancellationToken ct = default);
    Task<Result<SubmissionDto>> GradeSubmissionAsync(Guid submissionId, GradeSubmissionDto dto, Guid requestingUserId, CancellationToken ct = default);

    // Entregas (vista Estudiante)
    Task<Result<IEnumerable<StudentSubmissionDto>>> GetMySubmissionsAsync(Guid sectionId, Guid studentUserId, CancellationToken ct = default);
    Task<Result<SubmissionDto>> SubmitAsync(Guid activityId, SubmitDto dto, Guid studentUserId, CancellationToken ct = default);

    // Puente LMS ↔ académico
    Task<Result<IEnumerable<StudentGradeSuggestionDto>>> GetGradeSuggestionsAsync(Guid sectionId, Guid requestingUserId, string userRole, CancellationToken ct = default);
    Task<Result<MySuggestionDto>> GetMyGradeSuggestionAsync(Guid sectionId, Guid studentUserId, CancellationToken ct = default);
}
