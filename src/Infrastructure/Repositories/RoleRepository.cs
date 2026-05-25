using Microsoft.EntityFrameworkCore;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Enums;
using SistemaEvaluacionAcademica.Domain.Interfaces;
using SistemaEvaluacionAcademica.Infrastructure.Data;

namespace SistemaEvaluacionAcademica.Infrastructure.Repositories;

public class RoleRepository : BaseRepository<Role>, IRoleRepository
{
    public RoleRepository(AppDbContext context) : base(context) { }

    public async Task<Role?> GetByTypeAsync(RoleType type, CancellationToken cancellationToken = default) =>
        await _dbSet.FirstOrDefaultAsync(r => r.Type == type && r.IsActive, cancellationToken);
}
