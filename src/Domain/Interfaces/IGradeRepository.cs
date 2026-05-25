using SistemaEvaluacionAcademica.Domain.Entities;

namespace SistemaEvaluacionAcademica.Domain.Interfaces;

public interface IGradeRepository : IRepository<Grade>
{
    Task<IEnumerable<Grade>> GetByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Grade>> GetBySubjectAsync(Guid subjectId, string? period = null, CancellationToken cancellationToken = default);
    Task<Grade?> GetByStudentSubjectAndPeriodAsync(Guid studentId, Guid subjectId, string period, CancellationToken cancellationToken = default);
    Task<decimal?> GetStudentAverageAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<decimal> GetSubjectAverageAsync(Guid subjectId, string period, CancellationToken cancellationToken = default);
    Task<IEnumerable<Grade>> GetByStudentWithDetailsAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Grade>> GetBySectionAsync(Guid sectionId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Grade> Items, int TotalCount)> GetPagedByStudentAsync(Guid studentId, int page, int pageSize, string? period, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Grade> Items, int TotalCount)> GetPagedBySubjectAsync(Guid subjectId, string? period, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Grade> Items, int TotalCount)> GetPagedBySectionAsync(Guid sectionId, int page, int pageSize, CancellationToken cancellationToken = default);
}
