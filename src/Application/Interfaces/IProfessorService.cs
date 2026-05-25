using SistemaEvaluacionAcademica.Application.Common;
using SistemaEvaluacionAcademica.Application.DTOs.Professors;

namespace SistemaEvaluacionAcademica.Application.Interfaces;

public interface IProfessorService
{
    Task<Result<PagedResult<ProfessorDto>>> GetAllAsync(
        int page, int pageSize, string? search = null, CancellationToken cancellationToken = default);
    Task<Result<ProfessorDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<ProfessorDto>> CreateAsync(CreateProfessorDto dto, CancellationToken cancellationToken = default);
    Task<Result<ProfessorDto>> UpdateAsync(Guid id, UpdateProfessorDto dto, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
