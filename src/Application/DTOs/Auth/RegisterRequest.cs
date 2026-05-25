using SistemaEvaluacionAcademica.Domain.Enums;

namespace SistemaEvaluacionAcademica.Application.DTOs.Auth;

public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    RoleType Role,
    string? Career = null,
    int? Semester = null
);
