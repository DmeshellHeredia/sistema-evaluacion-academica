using SistemaEvaluacionAcademica.Domain.Enums;

namespace SistemaEvaluacionAcademica.Application.DTOs.Sections;

public record SectionLookupDto(
    Guid Id,
    string SubjectCode,
    string SubjectName,
    string SectionCode,
    DayOfWeekType DayOfWeek,
    string StartTime,
    string EndTime
);

public record SectionDto(
    Guid Id,
    Guid SubjectId,
    string SubjectCode,
    string SubjectName,
    Guid ProfessorId,
    string ProfessorName,
    string SectionCode,
    DayOfWeekType DayOfWeek,
    string StartTime,
    string EndTime,
    string Modality,
    int Capacity,
    int EnrolledCount,
    bool IsFull,
    string Classroom
);

public record CreateSectionDto(
    Guid SubjectId,
    Guid ProfessorId,
    string SectionCode,
    DayOfWeekType DayOfWeek,
    string StartTime,
    string EndTime,
    string Modality,
    int Capacity,
    string Classroom = ""
);

public record UpdateSectionDto(
    DayOfWeekType DayOfWeek,
    string StartTime,
    string EndTime,
    string Modality,
    int Capacity,
    string Classroom = ""
);

public record SectionStudentDto(
    Guid StudentId,
    string StudentCode,
    string FullName,
    string Email,
    decimal? CurrentGrade,
    Guid? CurrentGradeId,
    string? CurrentGradeComments
);
