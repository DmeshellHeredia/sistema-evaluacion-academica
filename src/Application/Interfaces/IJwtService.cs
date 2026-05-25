using System.Security.Claims;
using SistemaEvaluacionAcademica.Domain.Entities;

namespace SistemaEvaluacionAcademica.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
    ClaimsPrincipal? ValidateToken(string token);
    DateTime GetTokenExpiration();
}
