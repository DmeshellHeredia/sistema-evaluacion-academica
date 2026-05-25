using SistemaEvaluacionAcademica.Domain.Enums;

namespace SistemaEvaluacionAcademica.Application.DTOs.Enrollments;

public enum EnrollmentStatus
{
    Disponible,
    Inscrita,
    Aprobada,
    Reprobada,
    Bloqueada,
    SemestrePrevio,
}

public record SubjectCatalogItemDto(
    Guid SubjectId,
    string Code,
    string Name,
    string Description,
    int Credits,
    int SemesterLevel,
    IReadOnlyList<SectionSummaryDto> AvailableSections,
    EnrollmentStatus Status,
    decimal? Grade,
    string? Period,
    IReadOnlyList<PrerequisiteInfoDto> MissingPrerequisites,
    bool HasScheduleConflict = false,
    string? ConflictingWith = null
);

public record SectionSummaryDto(
    Guid SectionId,
    string SectionCode,
    string ProfessorName,
    DayOfWeekType DayOfWeek,
    string StartTime,
    string EndTime,
    string Modality,
    int Capacity,
    int EnrolledCount,
    bool IsFull,
    string Classroom = ""
);

public record PrerequisiteInfoDto(Guid SubjectId, string Code, string Name, decimal? YourGrade);

public record EnrolledSectionDto(
    Guid SectionId,
    Guid SubjectId,
    string SubjectCode,
    string SubjectName,
    string SectionCode,
    string ProfessorName,
    DayOfWeekType DayOfWeek,
    string StartTime,
    string EndTime,
    string Modality,
    string Classroom,
    int Credits
);

public record StudentScheduleDto(
    IReadOnlyList<EnrolledSectionDto> Sections,
    string PeriodName,
    bool IsEnrollmentOpen
);
