using System.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaEvaluacionAcademica.Application.Common;
using SistemaEvaluacionAcademica.Application.DTOs.Enrollments;
using SistemaEvaluacionAcademica.Application.Interfaces;
using SistemaEvaluacionAcademica.Application.Settings;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Exceptions;
using SistemaEvaluacionAcademica.Domain.Interfaces;

namespace SistemaEvaluacionAcademica.Application.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EnrollmentService> _logger;
    private readonly IOptions<AcademicSettings> _academicSettings;

    public EnrollmentService(
        IUnitOfWork unitOfWork,
        ILogger<EnrollmentService> logger,
        IOptions<AcademicSettings> academicSettings)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _academicSettings = academicSettings;
    }

    public async Task<Result<IEnumerable<SubjectCatalogItemDto>>> GetCatalogAsync(
        Guid studentId, CancellationToken cancellationToken = default)
    {
        var student = await _unitOfWork.Students.GetWithGradesAsync(studentId, cancellationToken);
        if (student is null)
            return Result<IEnumerable<SubjectCatalogItemDto>>.NotFound("Estudiante no encontrado.");

        var subjects = await _unitOfWork.GetSubjectsByCareerAsync(student.Career, cancellationToken);
        var grades   = student.Grades.Where(g => g.IsActive).ToList();
        var enrolledSubjectIds = await GetEnrolledSubjectIdsAsync(studentId, cancellationToken);

        // Carga todas las secciones inscritas para detectar choques de horario
        var enrolledSections = await _unitOfWork.GetEnrolledSectionsAsync(studentId, cancellationToken);

        // Carga todos los prerequisitos y sus materias en una sola consulta — evita N+1
        var allPrereqsMap = (await _unitOfWork.GetAllActivePrerequisitesAsync(cancellationToken))
            .GroupBy(p => p.SubjectId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var prereqSubjectIds = allPrereqsMap.Values
            .SelectMany(ps => ps.Select(p => p.PrerequisiteSubjectId))
            .Distinct();
        var prereqSubjectsMap = (await _unitOfWork.Subjects.GetByIdsAsync(prereqSubjectIds, cancellationToken))
            .ToDictionary(s => s.Id);

        var items = new List<SubjectCatalogItemDto>();

        foreach (var subject in subjects)
        {
            var existingGrade = grades
                .Where(g => g.SubjectId == subject.Id)
                .OrderByDescending(g => g.CreatedAt)
                .FirstOrDefault();

            var isEnrolled = enrolledSubjectIds.Contains(subject.Id);
            var isApproved = existingGrade is not null && existingGrade.Value >= 7;
            var isFailed   = existingGrade is not null && existingGrade.Value < 7;
            var semesterOk = student.Semester >= subject.SemesterLevel;

            var (missingPrereqs, allPrereqsMet) = CheckPrerequisitesBatch(
                subject, grades, allPrereqsMap, prereqSubjectsMap);

            EnrollmentStatus status;
            if (isApproved)
                status = EnrollmentStatus.Aprobada;
            else if (isEnrolled)
                status = EnrollmentStatus.Inscrita;
            else if (!semesterOk)
                status = EnrollmentStatus.SemestrePrevio;
            else if (!allPrereqsMet)
                status = EnrollmentStatus.Bloqueada;
            else if (isFailed)
                status = EnrollmentStatus.Reprobada;
            else
                status = EnrollmentStatus.Disponible;

            // Detecta choques de horario para las secciones disponibles
            bool hasConflict = false;
            string? conflictingWith = null;

            var availableSections = subject.Sections
                .Where(s => s.IsActive)
                .Select(s =>
                {
                    var sectionConflict = enrolledSections
                        .FirstOrDefault(es => es.Id != s.Id && es.OverlapsWith(s));
                    if (sectionConflict is not null && !isEnrolled)
                    {
                        hasConflict = true;
                        conflictingWith ??= $"{sectionConflict.Subject.Code} - Sec. {sectionConflict.SectionCode} ({sectionConflict.DayOfWeek} {sectionConflict.StartTime:HH:mm}-{sectionConflict.EndTime:HH:mm})";
                    }
                    return new SectionSummaryDto(
                        s.Id,
                        s.SectionCode,
                        s.Professor?.FullName ?? string.Empty,
                        s.DayOfWeek,
                        s.StartTime.ToString("HH:mm"),
                        s.EndTime.ToString("HH:mm"),
                        s.Modality,
                        s.Capacity,
                        s.Enrollments.Count(e => e.IsActive),
                        s.IsFull,
                        s.Classroom);
                })
                .ToList();

            items.Add(new SubjectCatalogItemDto(
                subject.Id,
                subject.Code,
                subject.Name,
                subject.Description,
                subject.Credits,
                subject.SemesterLevel,
                availableSections,
                status,
                existingGrade?.Value,
                existingGrade?.Period,
                missingPrereqs,
                hasConflict,
                conflictingWith
            ));
        }

        return Result<IEnumerable<SubjectCatalogItemDto>>.Success(items);
    }

    public async Task<Result<StudentScheduleDto>> GetMyScheduleAsync(
        Guid studentId, CancellationToken cancellationToken = default)
    {
        var student = await _unitOfWork.Students.GetWithGradesAsync(studentId, cancellationToken);
        if (student is null)
            return Result<StudentScheduleDto>.NotFound("Estudiante no encontrado.");

        var enrolledSections = await _unitOfWork.GetEnrolledSectionsAsync(studentId, cancellationToken);
        var activePeriod = await _unitOfWork.GetActivePeriodAsync(cancellationToken);

        var sections = enrolledSections.Select(s => new EnrolledSectionDto(
            s.Id,
            s.SubjectId,
            s.Subject.Code,
            s.Subject.Name,
            s.SectionCode,
            s.Professor is { IsActive: true } p ? p.FullName : "Sin profesor asignado",
            s.DayOfWeek,
            s.StartTime.ToString("HH:mm"),
            s.EndTime.ToString("HH:mm"),
            s.Modality,
            s.Classroom,
            s.Subject.Credits
        )).ToList();

        var dto = new StudentScheduleDto(
            sections,
            activePeriod?.Name ?? "Sin período activo",
            activePeriod?.IsEnrollmentOpen ?? false
        );

        return Result<StudentScheduleDto>.Success(dto);
    }

    public async Task<Result<Guid>> EnrollAsync(
        EnrollRequestDto dto, CancellationToken cancellationToken = default)
    {
        // verificaciones previas sin bloqueo (baratas, sin lock)
        var isOpen = await _unitOfWork.IsEnrollmentOpenAsync(cancellationToken);
        if (!isOpen)
        {
            _logger.LogWarning("Intento de inscripción fuera del período activo. EstudianteId={StudentId} SecciónId={SectionId}",
                dto.StudentId, dto.SectionId);
            return Result<Guid>.Failure("La selección de materias no está activa actualmente.");
        }

        var student = await _unitOfWork.Students.GetWithGradesAsync(dto.StudentId, cancellationToken);
        if (student is null)
            return Result<Guid>.NotFound("Estudiante no encontrado.");

        var section = await _unitOfWork.GetSectionByIdAsync(dto.SectionId, cancellationToken);
        if (section is null)
            return Result<Guid>.NotFound("Sección no encontrada.");

        // section.Subject es null cuando la materia fue eliminada lógicamente después de crear la sección
        // (el filtro global de EF Core excluye materias inactivas de los includes de navegación)
        if (section.Subject is null)
            return Result<Guid>.NotFound("La materia de esta sección ya no está disponible.");

        var subject = section.Subject;

        if (!subject.AppliesToCareer(student.Career))
            return Result<Guid>.Failure(
                $"La materia '{subject.Name}' no aplica a la carrera '{student.Career}'.");

        if (student.Semester < subject.SemesterLevel)
            return Result<Guid>.Failure(
                $"La materia '{subject.Name}' es de semestre {subject.SemesterLevel}. " +
                $"El estudiante está en semestre {student.Semester}.");

        if (await _unitOfWork.IsEnrolledInSectionAsync(dto.StudentId, dto.SectionId, cancellationToken))
            return Result<Guid>.Failure("El estudiante ya está inscrito en esta sección.");

        if (await _unitOfWork.IsEnrolledInAnyOtherSectionOfSubjectAsync(
            dto.StudentId, subject.Id, dto.SectionId, cancellationToken))
            return Result<Guid>.Failure("El estudiante ya está inscrito en otra sección de esta materia.");

        var grades = student.Grades.Where(g => g.IsActive).ToList();
        var existing = grades
            .Where(g => g.SubjectId == subject.Id)
            .OrderByDescending(g => g.CreatedAt)
            .FirstOrDefault();

        if (existing is not null && existing.Value >= 7)
            return Result<Guid>.Failure("El estudiante ya aprobó esta materia.");

        var prereqs = (await _unitOfWork.GetPrerequisitesAsync(subject.Id, cancellationToken)).ToList();
        var prereqIds = prereqs.Select(p => p.PrerequisiteSubjectId).Distinct();
        var subjectsMap = (await _unitOfWork.Subjects.GetByIdsAsync(prereqIds, cancellationToken)).ToDictionary(s => s.Id);
        var prereqsMap = new Dictionary<Guid, List<SubjectPrerequisite>> { [subject.Id] = prereqs };

        var (missing, allMet) = CheckPrerequisitesBatch(subject, grades, prereqsMap, subjectsMap);
        if (!allMet)
        {
            var names = string.Join(", ", missing.Select(p => $"{p.Code} ({p.Name})"));
            return Result<Guid>.Failure($"Prerequisitos no cumplidos: {names}. Se requiere nota >= 7.");
        }

        var enrolledSections = await _unitOfWork.GetEnrolledSectionsAsync(dto.StudentId, cancellationToken);
        var conflicting = enrolledSections.FirstOrDefault(es => es.OverlapsWith(section));
        if (conflicting is not null)
        {
            _logger.LogWarning(
                "Choque de horario. EstudianteId={StudentId} SecciónSolicitada={SectionId} ConflictoCon={ConflictSectionId}",
                dto.StudentId, dto.SectionId, conflicting.Id);
            return Result<Guid>.Failure(
                $"Choque de horario: '{subject.Name}' ({section.DayOfWeek} {section.StartTime:HH:mm}-{section.EndTime:HH:mm}) " +
                $"conflicta con '{conflicting.Subject.Code}' ({conflicting.DayOfWeek} {conflicting.StartTime:HH:mm}-{conflicting.EndTime:HH:mm}).");
        }

        var maxCredits = _academicSettings.Value.MaxCreditsPerPeriod;
        var currentCredits = enrolledSections.Sum(s => s.Subject.Credits);
        if (currentCredits + subject.Credits > maxCredits)
            return Result<Guid>.Failure(
                $"Límite de carga académica: máximo {maxCredits} créditos por período. " +
                $"Ya tienes {currentCredits} crédito(s) inscritos y esta materia suma {subject.Credits} más.");

        if (section.IsFull)
        {
            _logger.LogWarning("Sección al límite de capacidad. EstudianteId={StudentId} SecciónId={SectionId} Capacidad={Capacity}",
                dto.StudentId, dto.SectionId, section.Capacity);
            return Result<Guid>.Failure(
                $"La sección '{section.SectionCode}' ha alcanzado su capacidad máxima ({section.Capacity} estudiantes).");
        }

        // transacción serializable — reverificación autoritativa bajo lock
        Guid enrollmentId = Guid.Empty;
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            // Re-verificar período abierto bajo lock — cierre concurrente entre pre-check y TX
            if (!await _unitOfWork.IsEnrollmentOpenAsync(cancellationToken))
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result<Guid>.Failure("La selección de materias no está activa actualmente.");
            }

            if (await _unitOfWork.IsEnrolledInSectionAsync(dto.StudentId, dto.SectionId, cancellationToken))
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result<Guid>.Failure("El estudiante ya está inscrito en esta sección.");
            }

            if (await _unitOfWork.IsEnrolledInAnyOtherSectionOfSubjectAsync(
                dto.StudentId, subject.Id, dto.SectionId, cancellationToken))
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result<Guid>.Failure("El estudiante ya está inscrito en otra sección de esta materia.");
            }

            var freshSection = await _unitOfWork.GetSectionByIdAsync(dto.SectionId, cancellationToken);
            if (freshSection is null || freshSection.IsFull)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result<Guid>.Failure(
                    $"La sección '{section.SectionCode}' ha alcanzado su capacidad máxima ({section.Capacity} estudiantes).");
            }

            // Re-verificar créditos y horario bajo lock — los checks pre-tx usan datos posiblemente obsoletos
            var freshEnrolledSections = await _unitOfWork.GetEnrolledSectionsAsync(dto.StudentId, cancellationToken);

            var freshCredits = freshEnrolledSections.Sum(s => s.Subject.Credits);
            if (freshCredits + subject.Credits > maxCredits)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result<Guid>.Failure(
                    $"Límite de carga académica: máximo {maxCredits} créditos por período. " +
                    $"Ya tienes {freshCredits} crédito(s) inscritos y esta materia suma {subject.Credits} más.");
            }

            var freshConflict = freshEnrolledSections.FirstOrDefault(es => es.OverlapsWith(section));
            if (freshConflict is not null)
            {
                _logger.LogWarning(
                    "Choque de horario (bajo lock). EstudianteId={StudentId} SecciónSolicitada={SectionId} ConflictoCon={ConflictSectionId}",
                    dto.StudentId, dto.SectionId, freshConflict.Id);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result<Guid>.Failure(
                    $"Choque de horario: '{subject.Name}' ({section.DayOfWeek} {section.StartTime:HH:mm}-{section.EndTime:HH:mm}) " +
                    $"conflicta con '{freshConflict.Subject.Code}' ({freshConflict.DayOfWeek} {freshConflict.StartTime:HH:mm}-{freshConflict.EndTime:HH:mm}).");
            }

            var enrollment = new SectionEnrollment(dto.StudentId, dto.SectionId);
            await _unitOfWork.AddSectionEnrollmentAsync(enrollment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            enrollmentId = enrollment.Id;
        }
        // thrown by UnitOfWork.SaveChangesAsync when SQL Server error 2627/2601 (unique constraint) or 1205 (deadlock)
        catch (ConcurrencyException ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return Result<Guid>.Failure(ex.Message);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        _logger.LogInformation("Inscripción creada. EstudianteId={StudentId} SecciónId={SectionId} Materia={SubjectCode}",
            dto.StudentId, dto.SectionId, subject.Code);
        return Result<Guid>.Success(enrollmentId);
    }

    public async Task<Result<bool>> UnenrollAsync(
        Guid studentId, Guid sectionId, CancellationToken cancellationToken = default)
    {
        var isOpen = await _unitOfWork.IsEnrollmentOpenAsync(cancellationToken);
        if (!isOpen)
            return Result<bool>.Failure("La selección de materias no está activa. No se puede cancelar la inscripción fuera del período.");

        if (!await _unitOfWork.IsEnrolledInSectionAsync(studentId, sectionId, cancellationToken))
            return Result<bool>.Failure("El estudiante no está inscrito en esta sección.");

        var section = await _unitOfWork.GetSectionByIdAsync(sectionId, cancellationToken);
        var student = await _unitOfWork.Students.GetWithGradesAsync(studentId, cancellationToken);

        if (section is not null && student is not null)
        {
            var hasGrade = student.Grades.Any(g => g.SubjectId == section.SubjectId && g.IsActive);
            if (hasGrade)
                return Result<bool>.Failure(
                    "No se puede desinscribir de una materia que ya tiene calificación.");
        }

        await _unitOfWork.RemoveSectionEnrollmentAsync(studentId, sectionId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }

    private static (IReadOnlyList<PrerequisiteInfoDto> missing, bool allMet) CheckPrerequisitesBatch(
        Subject subject,
        List<Grade> grades,
        Dictionary<Guid, List<SubjectPrerequisite>> prereqsBySubject,
        Dictionary<Guid, Subject> prereqSubjectsMap)
    {
        if (!prereqsBySubject.TryGetValue(subject.Id, out var prerequisites) || prerequisites.Count == 0)
            return (Array.Empty<PrerequisiteInfoDto>(), true);

        var missing = new List<PrerequisiteInfoDto>();
        foreach (var prereq in prerequisites)
        {
            if (!prereqSubjectsMap.TryGetValue(prereq.PrerequisiteSubjectId, out var prereqSubject))
            {
                // Subject was deactivated after the prerequisite was configured — block enrollment
                // to prevent bypassing requirements via data inconsistency.
                missing.Add(new PrerequisiteInfoDto(prereq.PrerequisiteSubjectId, "?", "[Materia inactiva]", null));
                continue;
            }

            var prereqGrade = grades
                .Where(g => g.SubjectId == prereq.PrerequisiteSubjectId)
                .OrderByDescending(g => g.CreatedAt)
                .FirstOrDefault();

            if (prereqGrade is null || prereqGrade.Value < 7)
                missing.Add(new PrerequisiteInfoDto(
                    prereqSubject.Id, prereqSubject.Code, prereqSubject.Name, prereqGrade?.Value));
        }

        return (missing, missing.Count == 0);
    }

    private async Task<HashSet<Guid>> GetEnrolledSubjectIdsAsync(
        Guid studentId, CancellationToken cancellationToken)
    {
        var subjects = await _unitOfWork.Subjects.GetByStudentAsync(studentId, cancellationToken);
        return subjects.Select(s => s.Id).ToHashSet();
    }
}
