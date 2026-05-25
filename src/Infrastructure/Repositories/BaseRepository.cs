using Microsoft.EntityFrameworkCore;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Interfaces;
using SistemaEvaluacionAcademica.Infrastructure.Data;

namespace SistemaEvaluacionAcademica.Infrastructure.Repositories;

public abstract class BaseRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    protected BaseRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _dbSet.FirstOrDefaultAsync(e => e.Id == id && e.IsActive, cancellationToken);

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _dbSet.Where(e => e.IsActive).ToListAsync(cancellationToken);

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.Deactivate();
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _dbSet.AnyAsync(e => e.Id == id && e.IsActive, cancellationToken);

    public IQueryable<T> GetQueryable() =>
        _dbSet.Where(e => e.IsActive).AsQueryable();
}
