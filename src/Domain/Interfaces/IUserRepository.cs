using SistemaEvaluacionAcademica.Domain.Entities;

namespace SistemaEvaluacionAcademica.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithRoleAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetByRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<User> Items, int TotalCount)> GetPagedByRoleAsync(
        Guid roleId, int page, int pageSize, string? search = null, CancellationToken cancellationToken = default);
}
