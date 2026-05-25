namespace SistemaEvaluacionAcademica.Application.DTOs.Professors;

public record CreateProfessorDto(
    string Email,
    string Password,
    string FirstName,
    string LastName
);
