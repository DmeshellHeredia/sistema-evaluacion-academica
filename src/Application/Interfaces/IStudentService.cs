using SistemaEvaluacionAcademica.Application.Common;
using SistemaEvaluacionAcademica.Application.DTOs.Grades;
using SistemaEvaluacionAcademica.Application.DTOs.Students;

namespace SistemaEvaluacionAcademica.Application.Interfaces;

public interface IStudentService
{
    Task<Result<PagedResult<StudentDto>>> GetAllAsync(int page, int pageSize, string? search = null, CancellationToken cancellationToken = default);
    Task<Result<StudentDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<StudentDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<StudentDto>> CreateAsync(CreateStudentDto dto, CancellationToken cancellationToken = default);
    Task<Result<StudentDto>> UpdateAsync(Guid id, UpdateStudentDto dto, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<StudentGradeReportDto>> GetGradeReportAsync(Guid studentId, CancellationToken cancellationToken = default);
}
