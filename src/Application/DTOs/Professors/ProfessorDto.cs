namespace SistemaEvaluacionAcademica.Application.DTOs.Professors;

public record ProfessorDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    int SectionCount,
    DateTime CreatedAt
);
