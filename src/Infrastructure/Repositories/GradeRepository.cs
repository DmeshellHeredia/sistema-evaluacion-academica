using Microsoft.EntityFrameworkCore;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Interfaces;
using SistemaEvaluacionAcademica.Infrastructure.Data;

namespace SistemaEvaluacionAcademica.Infrastructure.Repositories;

public class GradeRepository : BaseRepository<Grade>, IGradeRepository
{
    public GradeRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Grade>> GetByStudentAsync(Guid studentId, CancellationToken cancellationToken = default) =>
        await _dbSet
            .Include(g => g.Subject)
            .Where(g => g.StudentId == studentId && g.IsActive)
            .OrderByDescending(g => g.Period)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Grade>> GetBySubjectAsync(Guid subjectId, string? period = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(g => g.Student).ThenInclude(s => s.User)
            .Include(g => g.Subject)
            .Where(g => g.SubjectId == subjectId && g.IsActive);

        if (!string.IsNullOrEmpty(period))
            query = query.Where(g => g.Period == period);

        return await query.OrderBy(g => g.Student.StudentCode).ToListAsync(cancellationToken);
    }

    public async Task<Grade?> GetByStudentSubjectAndPeriodAsync(Guid studentId, Guid subjectId, string period, CancellationToken cancellationToken = default) =>
        await _dbSet.FirstOrDefaultAsync(
            g => g.StudentId == studentId && g.SubjectId == subjectId && g.Period == period && g.IsActive,
            cancellationToken);

    public async Task<decimal?> GetStudentAverageAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var grades = await _dbSet
            .Where(g => g.StudentId == studentId && g.IsActive)
            .Select(g => g.Value)
            .ToListAsync(cancellationToken);

        return grades.Any() ? Math.Round(grades.Average(), 2) : null;
    }

    public async Task<decimal> GetSubjectAverageAsync(Guid subjectId, string period, CancellationToken cancellationToken = default)
    {
        var grades = await _dbSet
            .Where(g => g.SubjectId == subjectId && g.Period == period && g.IsActive)
            .Select(g => g.Value)
            .ToListAsync(cancellationToken);

        return grades.Any() ? Math.Round(grades.Average(), 2) : 0;
    }

    public async Task<IEnumerable<Grade>> GetByStudentWithDetailsAsync(Guid studentId, CancellationToken cancellationToken = default) =>
        await _dbSet
            .Include(g => g.Student).ThenInclude(s => s.User)
            .Include(g => g.Subject)
            .Include(g => g.GradedByUser)
            .Where(g => g.StudentId == studentId && g.IsActive)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Grade>> GetBySectionAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
        await _dbSet
            .Include(g => g.Student).ThenInclude(s => s.User)
            .Include(g => g.Subject)
            .Where(g => g.SectionId == sectionId && g.IsActive)
            .OrderBy(g => g.Student.StudentCode)
            .ToListAsync(cancellationToken);

    public async Task<(IEnumerable<Grade> Items, int TotalCount)> GetPagedByStudentAsync(
        Guid studentId, int page, int pageSize, string? period, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(g => g.Student).ThenInclude(s => s.User)
            .Include(g => g.Subject)
            .Where(g => g.StudentId == studentId && g.IsActive);

        if (!string.IsNullOrWhiteSpace(period))
            query = query.Where(g => g.Period == period);

        query = query.OrderByDescending(g => g.Period).ThenByDescending(g => g.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public async Task<(IEnumerable<Grade> Items, int TotalCount)> GetPagedBySubjectAsync(
        Guid subjectId, string? period, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(g => g.Student).ThenInclude(s => s.User)
            .Include(g => g.Subject)
            .Where(g => g.SubjectId == subjectId && g.IsActive);

        if (!string.IsNullOrWhiteSpace(period))
            query = query.Where(g => g.Period == period);

        query = query.OrderBy(g => g.Student.StudentCode);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public async Task<(IEnumerable<Grade> Items, int TotalCount)> GetPagedBySectionAsync(
        Guid sectionId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(g => g.Student).ThenInclude(s => s.User)
            .Include(g => g.Subject)
            .Where(g => g.SectionId == sectionId && g.IsActive)
            .OrderBy(g => g.Student.StudentCode);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, totalCount);
    }
}
