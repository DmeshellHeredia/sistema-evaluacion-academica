namespace SistemaEvaluacionAcademica.Application.DTOs.Grades;

public record GradeDto(
    Guid Id,
    string StudentCode,
    string StudentFullName,
    string SubjectCode,
    string SubjectName,
    decimal Value,
    string Category,
    string Period,
    string? Comments,
    DateTime CreatedAt
);
