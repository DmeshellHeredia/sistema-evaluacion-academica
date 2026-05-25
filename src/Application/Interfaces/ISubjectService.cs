using SistemaEvaluacionAcademica.Application.Common;
using SistemaEvaluacionAcademica.Application.DTOs.Subjects;

namespace SistemaEvaluacionAcademica.Application.Interfaces;

public interface ISubjectService
{
    Task<Result<IEnumerable<SubjectDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<PagedResult<SubjectDto>>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<SubjectLookupDto>>> GetLookupAsync(string? search = null, int limit = 50, CancellationToken cancellationToken = default);
    Task<Result<SubjectDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<SubjectDto>> CreateAsync(CreateSubjectDto dto, CancellationToken cancellationToken = default);
    Task<Result<SubjectDto>> UpdateAsync(Guid id, CreateSubjectDto dto, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    // Prerequisitos
    Task<Result<IEnumerable<PrerequisiteDto>>> GetPrerequisitesAsync(Guid subjectId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<PrerequisiteDto>>> SetPrerequisitesAsync(Guid subjectId, IReadOnlyList<Guid> prerequisiteIds, CancellationToken cancellationToken = default);
    Task<Result<PrerequisiteDto>> AddPrerequisiteAsync(Guid subjectId, Guid prerequisiteSubjectId, CancellationToken cancellationToken = default);
    Task<Result<bool>> RemovePrerequisiteAsync(Guid subjectId, Guid prerequisiteSubjectId, CancellationToken cancellationToken = default);
}
