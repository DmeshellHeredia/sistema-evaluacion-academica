using SistemaEvaluacionAcademica.Application.Common;
using SistemaEvaluacionAcademica.Application.DTOs.Professors;
using SistemaEvaluacionAcademica.Application.Interfaces;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Enums;
using SistemaEvaluacionAcademica.Domain.Interfaces;

namespace SistemaEvaluacionAcademica.Application.Services;

public class ProfessorService : IProfessorService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public ProfessorService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<PagedResult<ProfessorDto>>> GetAllAsync(
        int page, int pageSize, string? search = null, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var profesorRoleId = await GetProfesorRoleIdAsync(cancellationToken);
        var (professors, totalCount) = await _unitOfWork.Users.GetPagedByRoleAsync(
            profesorRoleId, page, pageSize, search, cancellationToken);

        var professorList = professors.ToList();
        var countByProfessor = await _unitOfWork.GetSectionCountsByProfessorsAsync(
            professorList.Select(p => p.Id), cancellationToken);

        var dtos = professorList.Select(p => MapToDto(p, countByProfessor.GetValueOrDefault(p.Id, 0)));
        var paged = PagedResult<ProfessorDto>.Create(dtos, totalCount, page, pageSize);
        return Result<PagedResult<ProfessorDto>>.Success(paged);
    }

    public async Task<Result<ProfessorDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var profesorRoleId = await GetProfesorRoleIdAsync(cancellationToken);
        var user = await _unitOfWork.Users.GetByIdWithRoleAsync(id, cancellationToken);
        if (user is null || user.RoleId != profesorRoleId)
            return Result<ProfessorDto>.NotFound("Profesor no encontrado.");

        var sections = await _unitOfWork.GetSectionsByProfessorAsync(id, cancellationToken);
        return Result<ProfessorDto>.Success(MapToDto(user, sections.Count()));
    }

    public async Task<Result<ProfessorDto>> CreateAsync(CreateProfessorDto dto, CancellationToken cancellationToken = default)
    {
        if (await _unitOfWork.Users.EmailExistsAsync(dto.Email, cancellationToken))
            return Result<ProfessorDto>.Failure("El correo electrónico ya está registrado.");

        var profesorRoleId = await GetProfesorRoleIdAsync(cancellationToken);
        var passwordHash = _passwordHasher.Hash(dto.Password);
        var user = new User(dto.Email, passwordHash, dto.FirstName, dto.LastName, profesorRoleId);
        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ProfessorDto>.Success(MapToDto(user, 0), 201);
    }

    public async Task<Result<ProfessorDto>> UpdateAsync(Guid id, UpdateProfessorDto dto, CancellationToken cancellationToken = default)
    {
        var profesorRoleId = await GetProfesorRoleIdAsync(cancellationToken);
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user is null || user.RoleId != profesorRoleId)
            return Result<ProfessorDto>.NotFound("Profesor no encontrado.");

        user.UpdateProfile(dto.FirstName, dto.LastName);
        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var sections = await _unitOfWork.GetSectionsByProfessorAsync(id, cancellationToken);
        return Result<ProfessorDto>.Success(MapToDto(user, sections.Count()));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var profesorRoleId = await GetProfesorRoleIdAsync(cancellationToken);
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user is null || user.RoleId != profesorRoleId)
            return Result<bool>.NotFound("Profesor no encontrado.");

        user.Deactivate();
        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    private async Task<Guid> GetProfesorRoleIdAsync(CancellationToken cancellationToken)
    {
        var role = await _unitOfWork.Roles.GetByTypeAsync(RoleType.Profesor, cancellationToken)
            ?? throw new InvalidOperationException("El rol 'Profesor' no existe en la base de datos.");
        return role.Id;
    }

    private static ProfessorDto MapToDto(User user, int sectionCount) =>
        new(user.Id, user.FirstName, user.LastName, user.FullName, user.Email, sectionCount, user.CreatedAt);
}
