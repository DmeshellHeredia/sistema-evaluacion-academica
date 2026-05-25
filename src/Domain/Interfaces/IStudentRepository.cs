using SistemaEvaluacionAcademica.Domain.Entities;

namespace SistemaEvaluacionAcademica.Domain.Interfaces;

public interface IStudentRepository : IRepository<Student>
{
    Task<Student?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Student?> GetByCodeAsync(string studentCode, CancellationToken cancellationToken = default);
    Task<Student?> GetWithGradesAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Student>> GetStudentsBySubjectAsync(Guid subjectId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Student> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search = null, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
}
