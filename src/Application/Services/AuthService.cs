using Microsoft.Extensions.Logging;
using SistemaEvaluacionAcademica.Application.Common;
using SistemaEvaluacionAcademica.Application.DTOs.Auth;
using SistemaEvaluacionAcademica.Application.Interfaces;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Interfaces;

namespace SistemaEvaluacionAcademica.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUnitOfWork unitOfWork, IJwtService jwtService, IPasswordHasher passwordHasher, ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login fallido para el email {Email}", request.Email);
            return Result<LoginResponse>.Unauthorized("Credenciales inválidas.");
        }

        var token = _jwtService.GenerateToken(user);
        var expiresAt = _jwtService.GetTokenExpiration();

        var response = new LoginResponse(token, user.Email, user.FullName, user.Role.Name, expiresAt);
        return Result<LoginResponse>.Success(response);
    }

    public async Task<Result<Guid>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (await _unitOfWork.Users.EmailExistsAsync(request.Email, cancellationToken))
            return Result<Guid>.Failure("El correo electrónico ya está registrado.");

        var role = await _unitOfWork.Roles.GetByTypeAsync(request.Role, cancellationToken);
        if (role is null)
            return Result<Guid>.Failure("El rol especificado no existe o está deshabilitado.");

        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = new User(request.Email, passwordHash, request.FirstName, request.LastName, role.Id);
        await _unitOfWork.Users.AddAsync(user, cancellationToken);

        if (request.Role == Domain.Enums.RoleType.Estudiante)
        {
            var studentCode = Student.GenerateCode();
            var student = new Student(
                studentCode,
                user.Id,
                request.Career ?? "Sin definir",
                request.Semester ?? 1
            );
            await _unitOfWork.Students.AddAsync(student, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(user.Id, 201);
    }

    public async Task<Result<bool>> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result<bool>.NotFound("Usuario no encontrado.");

        if (!_passwordHasher.Verify(currentPassword, user.PasswordHash))
            return Result<bool>.Failure("La contraseña actual es incorrecta.");

        var newHash = _passwordHasher.Hash(newPassword);
        user.UpdatePassword(newHash);

        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }

}
