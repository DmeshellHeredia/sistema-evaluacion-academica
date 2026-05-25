using SistemaEvaluacionAcademica.Application.Common;
using SistemaEvaluacionAcademica.Application.DTOs.Enrollments;

namespace SistemaEvaluacionAcademica.Application.Interfaces;

public interface IEnrollmentService
{
    Task<Result<IEnumerable<SubjectCatalogItemDto>>> GetCatalogAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<Result<StudentScheduleDto>> GetMyScheduleAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<Result<Guid>> EnrollAsync(EnrollRequestDto dto, CancellationToken cancellationToken = default);
    Task<Result<bool>> UnenrollAsync(Guid studentId, Guid sectionId, CancellationToken cancellationToken = default);
}
