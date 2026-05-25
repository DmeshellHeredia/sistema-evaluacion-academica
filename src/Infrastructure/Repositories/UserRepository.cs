using Microsoft.EntityFrameworkCore;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Interfaces;
using SistemaEvaluacionAcademica.Infrastructure.Data;

namespace SistemaEvaluacionAcademica.Infrastructure.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await _dbSet
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive, cancellationToken);

    public async Task<User?> GetByIdWithRoleAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _dbSet
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive, cancellationToken);

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        await _dbSet.AnyAsync(u => u.Email == email && u.IsActive, cancellationToken);

    public async Task<IEnumerable<User>> GetByRoleAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        await _dbSet
            .Include(u => u.Role)
            .Where(u => u.RoleId == roleId && u.IsActive)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync(cancellationToken);

    public async Task<(IEnumerable<User> Items, int TotalCount)> GetPagedByRoleAsync(
        Guid roleId, int page, int pageSize, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(u => u.Role)
            .Where(u => u.RoleId == roleId && u.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term));
        }

        query = query.OrderBy(u => u.LastName).ThenBy(u => u.FirstName);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
