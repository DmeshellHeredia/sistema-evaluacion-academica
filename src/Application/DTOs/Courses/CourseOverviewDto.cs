using SistemaEvaluacionAcademica.Domain.Enums;

namespace SistemaEvaluacionAcademica.Application.DTOs.Courses;

public record CourseOverviewDto(
    Guid SectionId,
    string SubjectCode,
    string SubjectName,
    string SectionCode,
    string ProfessorName,
    DayOfWeekType DayOfWeek,
    string StartTime,
    string EndTime,
    string Modality,
    int EnrolledCount,
    int Capacity
);

public record ParticipantDto(
    Guid StudentId,
    string StudentCode,
    string FullName,
    string Email,
    int SubmissionsCount,
    int GradedCount,
    decimal? AverageScore
);
