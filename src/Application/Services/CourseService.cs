using Microsoft.Extensions.Logging;
using SistemaEvaluacionAcademica.Application.Common;
using SistemaEvaluacionAcademica.Application.DTOs.Courses;
using SistemaEvaluacionAcademica.Application.Interfaces;
using SistemaEvaluacionAcademica.Domain.Constants;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Enums;
using SistemaEvaluacionAcademica.Domain.Interfaces;

namespace SistemaEvaluacionAcademica.Application.Services;

public class CourseService : ICourseService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CourseService> _logger;

    public CourseService(IUnitOfWork uow, ILogger<CourseService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    // Ayudantes de autorización

    private async Task<(SubjectSection? section, bool authorized)> ResolveAccessAsync(
        Guid sectionId, Guid userId, string role, CancellationToken ct)
    {
        var section = await _uow.GetSectionByIdAsync(sectionId, ct);
        if (section is null)
        {
            _logger.LogWarning("Sección no encontrada. SecciónId={SectionId} SolicitadoPor={UserId} Rol={Role}",
                sectionId, userId, role);
            return (null, false);
        }

        if (role == Roles.Admin) return (section, true);

        if (role == Roles.Profesor)
        {
            var authorized = section.ProfessorId == userId;
            if (!authorized)
                _logger.LogWarning(
                    "Acceso no autorizado de profesor a sección. UsuarioId={UserId} SecciónId={SectionId} PropietarioId={OwnerId}",
                    userId, sectionId, section.ProfessorId);
            return (section, authorized);
        }

        if (role == Roles.Estudiante)
        {
            var student = await _uow.Students.GetByUserIdAsync(userId, ct);
            if (student is null)
            {
                _logger.LogWarning("Registro de estudiante no encontrado para el usuario. UsuarioId={UserId} SecciónId={SectionId}", userId, sectionId);
                return (section, false);
            }
            var enrolled = await _uow.IsEnrolledInSectionAsync(student.Id, sectionId, ct);
            if (!enrolled)
                _logger.LogWarning(
                    "Acceso no autorizado de estudiante a sección. UsuarioId={UserId} SecciónId={SectionId}",
                    userId, sectionId);
            return (section, enrolled);
        }

        return (section, false);
    }

    private async Task<bool> IsProfessorOfSectionAsync(Guid sectionId, Guid userId, CancellationToken ct)
    {
        var section = await _uow.GetSectionByIdAsync(sectionId, ct);
        return section is not null && section.ProfessorId == userId;
    }

    // Overview
    public async Task<Result<CourseOverviewDto>> GetOverviewAsync(Guid sectionId, Guid requestingUserId, string userRole, CancellationToken ct = default)
    {
        var (section, authorized) = await ResolveAccessAsync(sectionId, requestingUserId, userRole, ct);
        if (section is null) return Result<CourseOverviewDto>.NotFound("Sección no encontrada.");
        if (!authorized) return Result<CourseOverviewDto>.Forbidden();

        var dto = new CourseOverviewDto(
            section.Id,
            section.Subject.Code,
            section.Subject.Name,
            section.SectionCode,
            section.Professor is { IsActive: true } p ? p.FullName : "Sin profesor asignado",
            section.DayOfWeek,
            section.StartTime.ToString("HH:mm"),
            section.EndTime.ToString("HH:mm"),
            section.Modality,
            section.ActiveEnrollmentCount,
            section.Capacity);

        return Result<CourseOverviewDto>.Success(dto);
    }

    // Participants
    public async Task<Result<IEnumerable<ParticipantDto>>> GetParticipantsAsync(Guid sectionId, Guid requestingUserId, string userRole, CancellationToken ct = default)
    {
        var (section, authorized) = await ResolveAccessAsync(sectionId, requestingUserId, userRole, ct);
        if (section is null) return Result<IEnumerable<ParticipantDto>>.NotFound("Sección no encontrada.");
        if (!authorized) return Result<IEnumerable<ParticipantDto>>.Forbidden();

        var students = await _uow.GetStudentsBySectionAsync(sectionId, ct);
        var activities = await _uow.GetActivitiesBySectionAsync(sectionId, ct);

        var dtos = students.Select(s =>
        {
            var subs = activities
                .SelectMany(a => a.Submissions.Where(sub => sub.StudentId == s.Id))
                .ToList();
            var graded = subs.Where(sub => sub.Status == SubmissionStatus.Calificada).ToList();
            decimal? avg = graded.Any() ? Math.Round(graded.Average(sub => sub.Score ?? 0), 2) : null;

            return new ParticipantDto(
                s.Id,
                s.StudentCode,
                s.User.FullName,
                s.User.Email,
                subs.Count,
                graded.Count,
                avg);
        });

        return Result<IEnumerable<ParticipantDto>>.Success(dtos);
    }

    // Announcements
    public async Task<Result<IEnumerable<AnnouncementDto>>> GetAnnouncementsAsync(Guid sectionId, Guid requestingUserId, string userRole, CancellationToken ct = default)
    {
        var (section, authorized) = await ResolveAccessAsync(sectionId, requestingUserId, userRole, ct);
        if (section is null) return Result<IEnumerable<AnnouncementDto>>.NotFound("Sección no encontrada.");
        if (!authorized) return Result<IEnumerable<AnnouncementDto>>.Forbidden();

        var items = await _uow.GetAnnouncementsBySectionAsync(sectionId, ct);
        return Result<IEnumerable<AnnouncementDto>>.Success(items.Select(MapAnnouncement));
    }

    public async Task<Result<AnnouncementDto>> CreateAnnouncementAsync(Guid sectionId, CreateAnnouncementDto dto, Guid authorId, CancellationToken ct = default)
    {
        if (!await IsProfessorOfSectionAsync(sectionId, authorId, ct))
            return Result<AnnouncementDto>.Forbidden("Solo el profesor de la sección puede crear anuncios.");

        var announcement = new Announcement(sectionId, authorId, dto.Title, dto.Content, dto.IsPublished);
        await _uow.AddAnnouncementAsync(announcement, ct);
        await _uow.SaveChangesAsync(ct);

        var created = await _uow.GetAnnouncementByIdAsync(announcement.Id, ct);
        return Result<AnnouncementDto>.Success(MapAnnouncement(created!), 201);
    }

    public async Task<Result<AnnouncementDto>> UpdateAnnouncementAsync(Guid id, UpdateAnnouncementDto dto, Guid requestingUserId, CancellationToken ct = default)
    {
        var announcement = await _uow.GetAnnouncementByIdAsync(id, ct);
        if (announcement is null) return Result<AnnouncementDto>.NotFound("Anuncio no encontrado.");
        if (announcement.AuthorId != requestingUserId) return Result<AnnouncementDto>.Forbidden();

        announcement.Update(dto.Title, dto.Content, dto.IsPublished);
        await _uow.SaveChangesAsync(ct);
        return Result<AnnouncementDto>.Success(MapAnnouncement(announcement));
    }

    public async Task<Result<bool>> DeleteAnnouncementAsync(Guid id, Guid requestingUserId, CancellationToken ct = default)
    {
        var announcement = await _uow.GetAnnouncementByIdAsync(id, ct);
        if (announcement is null) return Result<bool>.NotFound("Anuncio no encontrado.");
        if (announcement.AuthorId != requestingUserId)
        {
            _logger.LogWarning(
                "Intento de eliminación no autorizado de anuncio. AnuncioId={Id} SolicitadoPor={UserId} AutorId={AuthorId}",
                id, requestingUserId, announcement.AuthorId);
            return Result<bool>.Forbidden();
        }

        _logger.LogInformation("Anuncio eliminado. AnuncioId={Id} EliminadoPor={UserId}", id, requestingUserId);
        announcement.Deactivate();
        await _uow.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    // Activities
    public async Task<Result<IEnumerable<ActivityDto>>> GetActivitiesAsync(Guid sectionId, Guid requestingUserId, string userRole, CancellationToken ct = default)
    {
        var (section, authorized) = await ResolveAccessAsync(sectionId, requestingUserId, userRole, ct);
        if (section is null) return Result<IEnumerable<ActivityDto>>.NotFound("Sección no encontrada.");
        if (!authorized) return Result<IEnumerable<ActivityDto>>.Forbidden();

        var items = await _uow.GetActivitiesBySectionAsync(sectionId, ct);
        return Result<IEnumerable<ActivityDto>>.Success(items.Select(MapActivity));
    }

    public async Task<Result<ActivityDto>> GetActivityAsync(Guid activityId, Guid requestingUserId, string userRole, CancellationToken ct = default)
    {
        var activity = await _uow.GetActivityByIdAsync(activityId, ct);
        if (activity is null) return Result<ActivityDto>.NotFound("Actividad no encontrada.");

        var (_, authorized) = await ResolveAccessAsync(activity.SectionId, requestingUserId, userRole, ct);
        if (!authorized) return Result<ActivityDto>.Forbidden();

        return Result<ActivityDto>.Success(MapActivity(activity));
    }

    public async Task<Result<ActivityDto>> CreateActivityAsync(Guid sectionId, CreateActivityDto dto, Guid professorId, CancellationToken ct = default)
    {
        if (!await IsProfessorOfSectionAsync(sectionId, professorId, ct))
            return Result<ActivityDto>.Forbidden("Solo el profesor de la sección puede crear actividades.");

        if (!Enum.TryParse<ActivityType>(dto.Type, out var activityType))
            return Result<ActivityDto>.Failure($"Tipo de actividad inválido: {dto.Type}");

        var activity = new Activity(sectionId, dto.Title, dto.Description, activityType, dto.DueDate, dto.MaxScore, dto.Weight, dto.IsPublished, dto.ResourceUrl);
        await _uow.AddActivityAsync(activity, ct);
        await _uow.SaveChangesAsync(ct);

        var created = await _uow.GetActivityByIdAsync(activity.Id, ct);
        return Result<ActivityDto>.Success(MapActivity(created!), 201);
    }

    public async Task<Result<ActivityDto>> UpdateActivityAsync(Guid activityId, UpdateActivityDto dto, Guid requestingUserId, CancellationToken ct = default)
    {
        var activity = await _uow.GetActivityByIdAsync(activityId, ct);
        if (activity is null) return Result<ActivityDto>.NotFound("Actividad no encontrada.");

        if (!await IsProfessorOfSectionAsync(activity.SectionId, requestingUserId, ct))
            return Result<ActivityDto>.Forbidden();

        if (!Enum.TryParse<ActivityType>(dto.Type, out var activityType))
            return Result<ActivityDto>.Failure($"Tipo de actividad inválido: {dto.Type}");

        activity.Update(dto.Title, dto.Description, activityType, dto.DueDate, dto.MaxScore, dto.Weight, dto.IsPublished, dto.ResourceUrl);
        await _uow.SaveChangesAsync(ct);
        return Result<ActivityDto>.Success(MapActivity(activity));
    }

    public async Task<Result<bool>> DeleteActivityAsync(Guid activityId, Guid requestingUserId, CancellationToken ct = default)
    {
        var activity = await _uow.GetActivityByIdAsync(activityId, ct);
        if (activity is null) return Result<bool>.NotFound("Actividad no encontrada.");

        if (!await IsProfessorOfSectionAsync(activity.SectionId, requestingUserId, ct))
        {
            _logger.LogWarning(
                "Intento de eliminación no autorizado de actividad. ActividadId={ActivityId} SolicitadoPor={UserId}",
                activityId, requestingUserId);
            return Result<bool>.Forbidden();
        }

        _logger.LogInformation("Actividad eliminada. ActividadId={ActivityId} EliminadoPor={UserId}", activityId, requestingUserId);
        activity.Deactivate();
        await _uow.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    // Submissions (Professor)
    public async Task<Result<IEnumerable<SubmissionDto>>> GetSubmissionsForActivityAsync(Guid activityId, Guid requestingUserId, CancellationToken ct = default)
    {
        var activity = await _uow.GetActivityByIdAsync(activityId, ct);
        if (activity is null) return Result<IEnumerable<SubmissionDto>>.NotFound("Actividad no encontrada.");

        if (!await IsProfessorOfSectionAsync(activity.SectionId, requestingUserId, ct))
            return Result<IEnumerable<SubmissionDto>>.Forbidden();

        var submissions = await _uow.GetSubmissionsByActivityAsync(activityId, ct);
        return Result<IEnumerable<SubmissionDto>>.Success(submissions.Select(s => MapSubmission(s, activity)));
    }

    public async Task<Result<SubmissionDto>> GradeSubmissionAsync(Guid submissionId, GradeSubmissionDto dto, Guid requestingUserId, CancellationToken ct = default)
    {
        var submission = await _uow.GetSubmissionByIdAsync(submissionId, ct);
        if (submission is null) return Result<SubmissionDto>.NotFound("Entrega no encontrada.");

        if (!await IsProfessorOfSectionAsync(submission.Activity.SectionId, requestingUserId, ct))
            return Result<SubmissionDto>.Forbidden();

        if (dto.Score < 0 || dto.Score > submission.Activity.MaxScore)
            return Result<SubmissionDto>.Failure($"La nota debe estar entre 0 y {submission.Activity.MaxScore}.");

        submission.Grade(dto.Score, dto.Feedback);
        await _uow.SaveChangesAsync(ct);
        return Result<SubmissionDto>.Success(MapSubmission(submission, submission.Activity));
    }

    // Submissions (Student)
    public async Task<Result<IEnumerable<StudentSubmissionDto>>> GetMySubmissionsAsync(Guid sectionId, Guid studentUserId, CancellationToken ct = default)
    {
        var student = await _uow.Students.GetByUserIdAsync(studentUserId, ct);
        if (student is null) return Result<IEnumerable<StudentSubmissionDto>>.Forbidden("No estás inscrito en esta sección.");
        var enrolled = await _uow.IsEnrolledInSectionAsync(student.Id, sectionId, ct);
        if (!enrolled) return Result<IEnumerable<StudentSubmissionDto>>.Forbidden("No estás inscrito en esta sección.");

        var activities = await _uow.GetActivitiesBySectionAsync(sectionId, ct);
        var mySubmissions = await _uow.GetSubmissionsByStudentAsync(student.Id, sectionId, ct);

        var submissionMap = mySubmissions.ToDictionary(s => s.ActivityId);

        var dtos = activities.Select(a =>
        {
            submissionMap.TryGetValue(a.Id, out var sub);
            return new StudentSubmissionDto(
                a.Id,
                a.Title,
                a.Type.ToString(),
                a.DueDate,
                a.MaxScore,
                a.Weight,
                sub?.Id,
                sub?.Content,
                sub?.SubmittedAt,
                sub?.Score,
                sub?.Feedback,
                sub?.Status.ToString() ?? SubmissionStatus.Pendiente.ToString());
        });

        return Result<IEnumerable<StudentSubmissionDto>>.Success(dtos);
    }

    public async Task<Result<SubmissionDto>> SubmitAsync(Guid activityId, SubmitDto dto, Guid studentUserId, CancellationToken ct = default)
    {
        var activity = await _uow.GetActivityByIdAsync(activityId, ct);
        if (activity is null) return Result<SubmissionDto>.NotFound("Actividad no encontrada.");
        if (!activity.IsPublished) return Result<SubmissionDto>.Failure("La actividad no está disponible.");

        var student = await _uow.Students.GetByUserIdAsync(studentUserId, ct);
        if (student is null) return Result<SubmissionDto>.Forbidden("No estás inscrito en esta sección.");
        var enrolled = await _uow.IsEnrolledInSectionAsync(student.Id, activity.SectionId, ct);
        if (!enrolled) return Result<SubmissionDto>.Forbidden("No estás inscrito en esta sección.");

        var existing = await _uow.GetSubmissionAsync(activityId, student.Id, ct);
        if (existing is not null)
        {
            if (existing.Status == SubmissionStatus.Cerrada)
                return Result<SubmissionDto>.Failure("La actividad está cerrada, no se pueden modificar entregas.");
            existing.Submit(dto.Content);
        }
        else
        {
            var submission = new ActivitySubmission(activityId, student.Id, dto.Content);
            await _uow.AddSubmissionAsync(submission, ct);
        }

        await _uow.SaveChangesAsync(ct);

        var saved = await _uow.GetSubmissionAsync(activityId, student.Id, ct);
        var full = await _uow.GetSubmissionByIdAsync(saved!.Id, ct);
        return Result<SubmissionDto>.Success(MapSubmission(full!, activity));
    }

    // Puente LMS ↔ académico

    public async Task<Result<IEnumerable<StudentGradeSuggestionDto>>> GetGradeSuggestionsAsync(
        Guid sectionId, Guid requestingUserId, string userRole, CancellationToken ct = default)
    {
        var (section, authorized) = await ResolveAccessAsync(sectionId, requestingUserId, userRole, ct);
        if (section is null) return Result<IEnumerable<StudentGradeSuggestionDto>>.NotFound("Sección no encontrada.");
        if (!authorized) return Result<IEnumerable<StudentGradeSuggestionDto>>.Forbidden();

        // Solo Admin/Profesor pueden ver la lista completa
        if (userRole == Roles.Estudiante) return Result<IEnumerable<StudentGradeSuggestionDto>>.Forbidden();

        var students    = await _uow.GetStudentsBySectionAsync(sectionId, ct);
        var activities  = (await _uow.GetActivitiesBySectionAsync(sectionId, ct)).ToList();
        var grades      = (await _uow.Grades.GetBySectionAsync(sectionId, ct)).ToList();

        var totalWeight = activities.Sum(a => a.Weight);

        var dtos = students.Select(student =>
        {
            var submissions = activities
                .SelectMany(a => a.Submissions.Where(s => s.StudentId == student.Id && s.IsActive))
                .ToList();

            var gradedSubs = submissions.Where(s => s.Status == SubmissionStatus.Calificada).ToList();

            decimal? suggested = null;
            if (gradedSubs.Count > 0 && totalWeight > 0)
            {
                var weighted = gradedSubs.Sum(s =>
                {
                    var act = activities.First(a => a.Id == s.ActivityId);
                    return (s.Score!.Value / act.MaxScore) * act.Weight;
                });
                suggested = Math.Round(weighted / totalWeight * 10, 2);
            }

            var officialGrade = grades
                .Where(g => g.StudentId == student.Id)
                .OrderByDescending(g => g.CreatedAt)
                .FirstOrDefault()?.Value;

            return new StudentGradeSuggestionDto(
                student.Id,
                student.StudentCode,
                student.User.FullName,
                suggested,
                officialGrade,
                gradedSubs.Count,
                activities.Count,
                totalWeight);
        });

        return Result<IEnumerable<StudentGradeSuggestionDto>>.Success(dtos);
    }

    public async Task<Result<MySuggestionDto>> GetMyGradeSuggestionAsync(
        Guid sectionId, Guid studentUserId, CancellationToken ct = default)
    {
        var student = await _uow.Students.GetByUserIdAsync(studentUserId, ct);
        if (student is null) return Result<MySuggestionDto>.Forbidden("No estás inscrito en esta sección.");

        var enrolled = await _uow.IsEnrolledInSectionAsync(student.Id, sectionId, ct);
        if (!enrolled) return Result<MySuggestionDto>.Forbidden("No estás inscrito en esta sección.");

        var activities  = (await _uow.GetActivitiesBySectionAsync(sectionId, ct)).ToList();
        var submissions = (await _uow.GetSubmissionsByStudentAsync(student.Id, sectionId, ct)).ToList();
        var grades      = (await _uow.Grades.GetBySectionAsync(sectionId, ct))
            .Where(g => g.StudentId == student.Id)
            .OrderByDescending(g => g.CreatedAt)
            .ToList();

        var totalWeight = activities.Sum(a => a.Weight);
        var gradedSubs  = submissions.Where(s => s.Status == SubmissionStatus.Calificada).ToList();

        decimal? suggested = null;
        if (gradedSubs.Count > 0 && totalWeight > 0)
        {
            var weighted = gradedSubs.Sum(s =>
            {
                var act = activities.First(a => a.Id == s.ActivityId);
                return (s.Score!.Value / act.MaxScore) * act.Weight;
            });
            suggested = Math.Round(weighted / totalWeight * 10, 2);
        }

        var officialGrade = grades.FirstOrDefault()?.Value;

        return Result<MySuggestionDto>.Success(new MySuggestionDto(
            suggested,
            officialGrade,
            gradedSubs.Count,
            activities.Count,
            totalWeight));
    }

    // Mappers
    private static AnnouncementDto MapAnnouncement(Announcement a) =>
        new(a.Id, a.SectionId, a.Title, a.Content, a.AuthorId, a.Author.FullName, a.IsPublished, a.CreatedAt, a.UpdatedAt);

    private static ActivityDto MapActivity(Activity a) =>
        new(a.Id, a.SectionId, a.Title, a.Description, a.Type.ToString(), a.DueDate, a.MaxScore, a.Weight, a.IsPublished, a.ResourceUrl,
            a.Submissions.Count,
            a.Submissions.Count(s => s.Status == SubmissionStatus.Calificada),
            a.CreatedAt);

    private static SubmissionDto MapSubmission(ActivitySubmission s, Activity activity) =>
        new(s.Id, s.ActivityId, activity.Title, s.StudentId,
            s.Student?.User?.FullName ?? "—",
            s.Student?.StudentCode ?? "—",
            s.Content, s.SubmittedAt, s.Score, activity.MaxScore, s.Feedback, s.Status.ToString(), s.CreatedAt);
}
