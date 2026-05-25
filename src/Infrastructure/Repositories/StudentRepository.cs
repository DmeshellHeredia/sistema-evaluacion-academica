using Microsoft.EntityFrameworkCore;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Interfaces;
using SistemaEvaluacionAcademica.Infrastructure.Data;

namespace SistemaEvaluacionAcademica.Infrastructure.Repositories;

public class StudentRepository : BaseRepository<Student>, IStudentRepository
{
    public StudentRepository(AppDbContext context) : base(context) { }

    public async Task<Student?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await _dbSet
            .Include(s => s.User).ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive, cancellationToken);

    public async Task<Student?> GetByCodeAsync(string studentCode, CancellationToken cancellationToken = default) =>
        await _dbSet
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.StudentCode == studentCode && s.IsActive, cancellationToken);

    public async Task<Student?> GetWithGradesAsync(Guid studentId, CancellationToken cancellationToken = default) =>
        await _dbSet
            .Include(s => s.User).ThenInclude(u => u.Role)
            .Include(s => s.Grades.Where(g => g.IsActive))
                .ThenInclude(g => g.Subject)
            .FirstOrDefaultAsync(s => s.Id == studentId && s.IsActive, cancellationToken);

    public async Task<IEnumerable<Student>> GetStudentsBySubjectAsync(Guid subjectId, CancellationToken cancellationToken = default) =>
        await _dbSet
            .Include(s => s.User)
            .Where(s => s.SectionEnrollments.Any(e =>
                e.Section.SubjectId == subjectId && e.IsActive) && s.IsActive)
            .ToListAsync(cancellationToken);

    public async Task<(IEnumerable<Student> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(s => s.User)
            .Include(s => s.Grades.Where(g => g.IsActive))
            .Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s =>
                s.StudentCode.ToLower().Contains(term) ||
                s.Career.ToLower().Contains(term) ||
                s.User.FirstName.ToLower().Contains(term) ||
                s.User.LastName.ToLower().Contains(term) ||
                s.User.Email.ToLower().Contains(term));
        }

        query = query.OrderBy(s => s.StudentCode);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default) =>
        await _dbSet.CountAsync(cancellationToken);
}
