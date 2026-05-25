namespace SistemaEvaluacionAcademica.Application.DTOs.Grades;

public record CreateGradeDto(
    Guid StudentId,
    Guid SectionId,
    decimal Value,
    string Period,
    string? Comments
);
