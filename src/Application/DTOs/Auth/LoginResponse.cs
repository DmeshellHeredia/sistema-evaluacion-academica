namespace SistemaEvaluacionAcademica.Application.DTOs.Auth;

public record LoginResponse(
    string Token,
    string Email,
    string FullName,
    string Role,
    DateTime ExpiresAt
);
