using SistemaEvaluacionAcademica.Application.Common;
using SistemaEvaluacionAcademica.Application.DTOs.Auth;

namespace SistemaEvaluacionAcademica.Application.Interfaces;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<Guid>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
}
