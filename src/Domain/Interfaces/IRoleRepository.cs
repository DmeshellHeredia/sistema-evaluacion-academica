using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Enums;

namespace SistemaEvaluacionAcademica.Domain.Interfaces;

public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> GetByTypeAsync(RoleType type, CancellationToken cancellationToken = default);
}
