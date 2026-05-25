namespace SistemaEvaluacionAcademica.Application.DTOs.Students;

public record StudentDto(
    Guid Id,
    string StudentCode,
    string FullName,
    string Email,
    string Career,
    int Semester,
    decimal? OverallAverage,
    string AverageCategory,
    DateTime CreatedAt
);
