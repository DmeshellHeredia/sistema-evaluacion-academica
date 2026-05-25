using SistemaEvaluacionAcademica.Application.Common;
using SistemaEvaluacionAcademica.Application.DTOs.Periods;

namespace SistemaEvaluacionAcademica.Application.Interfaces;

public interface IAcademicPeriodService
{
    Task<Result<IEnumerable<AcademicPeriodDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<AcademicPeriodDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<AcademicPeriodDto>> CreateAsync(CreateAcademicPeriodDto dto, CancellationToken cancellationToken = default);
    Task<Result<AcademicPeriodDto>> UpdateAsync(Guid id, UpdateAcademicPeriodDto dto, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<AcademicPeriodDto>> OpenEnrollmentAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<AcademicPeriodDto>> CloseEnrollmentAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<EnrollmentStatusDto>> GetEnrollmentStatusAsync(CancellationToken cancellationToken = default);
}
