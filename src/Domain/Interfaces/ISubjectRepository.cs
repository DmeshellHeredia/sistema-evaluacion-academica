using SistemaEvaluacionAcademica.Domain.Entities;

namespace SistemaEvaluacionAcademica.Domain.Interfaces;

public interface ISubjectRepository : IRepository<Subject>
{
    Task<Subject?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IEnumerable<Subject>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<IEnumerable<Subject>> GetByProfessorAsync(Guid professorId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Subject>> GetByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Subject> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(Guid Id, string Code, string Name, int SemesterLevel)>> GetLookupAsync(
        string? search = null, int limit = 50, CancellationToken cancellationToken = default);
}
