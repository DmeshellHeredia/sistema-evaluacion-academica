using SistemaEvaluacionAcademica.Application.Common;
using SistemaEvaluacionAcademica.Application.DTOs.Sections;

namespace SistemaEvaluacionAcademica.Application.Interfaces;

public interface ISectionService
{
    Task<Result<IEnumerable<SectionDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<PagedResult<SectionDto>>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SectionLookupDto>>> GetLookupAsync(string? search = null, int limit = 50, CancellationToken ct = default);
    Task<Result<IEnumerable<SectionDto>>> GetByProfessorAsync(Guid professorUserId, CancellationToken ct = default);
    Task<Result<SectionDto>> GetByIdAsync(Guid sectionId, CancellationToken ct = default);
    Task<Result<SectionDto>> CreateAsync(CreateSectionDto dto, CancellationToken ct = default);
    Task<Result<SectionDto>> UpdateAsync(Guid sectionId, UpdateSectionDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid sectionId, CancellationToken ct = default);
    Task<Result<IEnumerable<SectionStudentDto>>> GetStudentsAsync(Guid sectionId, Guid requestingUserId, string userRole, CancellationToken ct = default);
}
