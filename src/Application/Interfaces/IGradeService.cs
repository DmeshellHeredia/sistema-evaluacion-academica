using SistemaEvaluacionAcademica.Application.Common;
using SistemaEvaluacionAcademica.Application.DTOs.Grades;

namespace SistemaEvaluacionAcademica.Application.Interfaces;

public interface IGradeService
{
    Task<Result<GradeDto>> CreateAsync(CreateGradeDto dto, Guid gradedByUserId, string userRole, CancellationToken cancellationToken = default);
    Task<Result<GradeDto>> UpdateAsync(Guid id, decimal newValue, string? comments, Guid updatedByUserId, string userRole, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<GradeDto>>> GetByStudentAsync(Guid studentId, int page, int pageSize, string? period, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<GradeDto>>> GetBySubjectAsync(Guid subjectId, string? period, int page, int pageSize, Guid requestingUserId, string userRole, CancellationToken cancellationToken = default);
    Task<Result<decimal?>> GetStudentAverageAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<Result<decimal>> GetSubjectAverageAsync(Guid subjectId, string period, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<GradeDto>>> GetBySectionAsync(Guid sectionId, int page, int pageSize, Guid requestingUserId, string userRole, CancellationToken cancellationToken = default);
}
