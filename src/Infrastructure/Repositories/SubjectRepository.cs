using Microsoft.EntityFrameworkCore;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Interfaces;
using SistemaEvaluacionAcademica.Infrastructure.Data;

namespace SistemaEvaluacionAcademica.Infrastructure.Repositories;

public class SubjectRepository : BaseRepository<Subject>, ISubjectRepository
{
    public SubjectRepository(AppDbContext context) : base(context) { }

    public async Task<Subject?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        await _dbSet.FirstOrDefaultAsync(s => s.Code == code && s.IsActive, cancellationToken);

    public async Task<IEnumerable<Subject>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
        await _dbSet.Where(s => ids.Contains(s.Id) && s.IsActive).ToListAsync(cancellationToken);

    public async Task<IEnumerable<Subject>> GetByProfessorAsync(Guid professorId, CancellationToken cancellationToken = default) =>
        await _dbSet
            .Where(s => s.IsActive && s.Sections.Any(sec => sec.ProfessorId == professorId && sec.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Subject>> GetByStudentAsync(Guid studentId, CancellationToken cancellationToken = default) =>
        await _dbSet
            .Where(s => s.IsActive && s.Sections.Any(sec =>
                sec.IsActive &&
                sec.Enrollments.Any(e => e.StudentId == studentId && e.IsActive)))
            .ToListAsync(cancellationToken);

    public async Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default) =>
        await _dbSet.AnyAsync(s => s.Code == code, cancellationToken);

    public async Task<(IEnumerable<Subject> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s =>
                s.Code.Contains(search) ||
                s.Name.Contains(search) ||
                s.Description.Contains(search));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(s => s.Sections.Where(sec => sec.IsActive))
            .OrderBy(s => s.SemesterLevel)
            .ThenBy(s => s.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<(Guid Id, string Code, string Name, int SemesterLevel)>> GetLookupAsync(
        string? search = null, int limit = 50, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s =>
                s.Code.ToLower().Contains(term) ||
                s.Name.ToLower().Contains(term));
        }

        var raw = await query
            .OrderBy(s => s.SemesterLevel)
            .ThenBy(s => s.Code)
            .Take(limit)
            .Select(s => new { s.Id, s.Code, s.Name, s.SemesterLevel })
            .ToListAsync(cancellationToken);

        return raw.Select(x => (x.Id, x.Code, x.Name, x.SemesterLevel)).ToList();
    }
}
