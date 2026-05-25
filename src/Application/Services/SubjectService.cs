using SistemaEvaluacionAcademica.Application.Common;
using SistemaEvaluacionAcademica.Application.DTOs.Subjects;
using SistemaEvaluacionAcademica.Application.Interfaces;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Interfaces;

namespace SistemaEvaluacionAcademica.Application.Services;

public class SubjectService : ISubjectService
{
    private readonly IUnitOfWork _unitOfWork;

    public SubjectService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // CRUD
    public async Task<Result<IEnumerable<SubjectDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var subjects = await _unitOfWork.Subjects.GetAllAsync(cancellationToken);
        return Result<IEnumerable<SubjectDto>>.Success(subjects.Select(MapToDto));
    }

    public async Task<Result<PagedResult<SubjectDto>>> GetPagedAsync(
        int page, int pageSize, string? search, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, total) = await _unitOfWork.Subjects.GetPagedAsync(page, pageSize, search, cancellationToken);
        return Result<PagedResult<SubjectDto>>.Success(
            PagedResult<SubjectDto>.Create(items.Select(MapToDto), total, page, pageSize));
    }

    public async Task<Result<IReadOnlyList<SubjectLookupDto>>> GetLookupAsync(
        string? search = null, int limit = 50, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        var raw = await _unitOfWork.Subjects.GetLookupAsync(search, limit, cancellationToken);
        var dtos = raw.Select(x => new SubjectLookupDto(x.Id, x.Code, x.Name, x.SemesterLevel)).ToList();
        return Result<IReadOnlyList<SubjectLookupDto>>.Success(dtos);
    }

    public async Task<Result<SubjectDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var subject = await _unitOfWork.Subjects.GetByIdAsync(id, cancellationToken);
        if (subject is null)
            return Result<SubjectDto>.NotFound("Materia no encontrada.");

        return Result<SubjectDto>.Success(MapToDto(subject));
    }

    public async Task<Result<SubjectDto>> CreateAsync(CreateSubjectDto dto, CancellationToken cancellationToken = default)
    {
        if (await _unitOfWork.Subjects.CodeExistsAsync(dto.Code, cancellationToken))
            return Result<SubjectDto>.Failure($"Ya existe una materia con el código '{dto.Code}'.");

        var subject = new Subject(dto.Code, dto.Name, dto.Description, dto.Credits,
            dto.Careers ?? Array.Empty<string>(), dto.SemesterLevel, dto.AppliesToAllCareers);
        await _unitOfWork.Subjects.AddAsync(subject, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<SubjectDto>.Success(MapToDto(subject), 201);
    }

    public async Task<Result<SubjectDto>> UpdateAsync(Guid id, CreateSubjectDto dto, CancellationToken cancellationToken = default)
    {
        var subject = await _unitOfWork.Subjects.GetByIdAsync(id, cancellationToken);
        if (subject is null)
            return Result<SubjectDto>.NotFound("Materia no encontrada.");

        if (!string.Equals(subject.Code, dto.Code, StringComparison.OrdinalIgnoreCase) &&
            await _unitOfWork.Subjects.CodeExistsAsync(dto.Code, cancellationToken))
            return Result<SubjectDto>.Failure($"Ya existe otra materia con el código '{dto.Code}'.");

        subject.Update(dto.Name, dto.Description, dto.Credits,
            dto.Careers ?? Array.Empty<string>(), dto.AppliesToAllCareers, dto.SemesterLevel);
        await _unitOfWork.Subjects.UpdateAsync(subject, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<SubjectDto>.Success(MapToDto(subject));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var subject = await _unitOfWork.Subjects.GetByIdAsync(id, cancellationToken);
        if (subject is null)
            return Result<bool>.NotFound("Materia no encontrada.");

        await _unitOfWork.Subjects.DeleteAsync(subject, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }

    // Prerequisites
    public async Task<Result<IEnumerable<PrerequisiteDto>>> GetPrerequisitesAsync(
        Guid subjectId, CancellationToken cancellationToken = default)
    {
        var subject = await _unitOfWork.Subjects.GetByIdAsync(subjectId, cancellationToken);
        if (subject is null)
            return Result<IEnumerable<PrerequisiteDto>>.NotFound("Materia no encontrada.");

        var prereqs = await _unitOfWork.GetPrerequisitesAsync(subjectId, cancellationToken);
        var dtos = new List<PrerequisiteDto>();
        foreach (var p in prereqs)
        {
            var s = await _unitOfWork.Subjects.GetByIdAsync(p.PrerequisiteSubjectId, cancellationToken);
            if (s is not null) dtos.Add(MapToPrerequisiteDto(s));
        }
        return Result<IEnumerable<PrerequisiteDto>>.Success(dtos);
    }

    public async Task<Result<PrerequisiteDto>> AddPrerequisiteAsync(
        Guid subjectId, Guid prerequisiteSubjectId, CancellationToken cancellationToken = default)
    {
        if (subjectId == prerequisiteSubjectId)
            return Result<PrerequisiteDto>.Failure("Una materia no puede ser prerequisito de sí misma.");

        var subject = await _unitOfWork.Subjects.GetByIdAsync(subjectId, cancellationToken);
        if (subject is null)
            return Result<PrerequisiteDto>.NotFound("Materia no encontrada.");

        var prereqSubject = await _unitOfWork.Subjects.GetByIdAsync(prerequisiteSubjectId, cancellationToken);
        if (prereqSubject is null)
            return Result<PrerequisiteDto>.NotFound("La materia prerequisito no existe.");

        if (await _unitOfWork.PrerequisiteExistsAsync(subjectId, prerequisiteSubjectId, cancellationToken))
            return Result<PrerequisiteDto>.Failure("Este prerequisito ya está asignado.");

        var allPrereqs = await _unitOfWork.GetAllActivePrerequisitesAsync(cancellationToken);
        if (HasPath(allPrereqs, prerequisiteSubjectId, subjectId))
            return Result<PrerequisiteDto>.Failure(
                "La relación de prerequisitos crea un ciclo académico.");

        await _unitOfWork.AddPrerequisiteAsync(new SubjectPrerequisite(subjectId, prerequisiteSubjectId), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<PrerequisiteDto>.Success(MapToPrerequisiteDto(prereqSubject), 201);
    }

    public async Task<Result<bool>> RemovePrerequisiteAsync(
        Guid subjectId, Guid prerequisiteSubjectId, CancellationToken cancellationToken = default)
    {
        var subject = await _unitOfWork.Subjects.GetByIdAsync(subjectId, cancellationToken);
        if (subject is null)
            return Result<bool>.NotFound("Materia no encontrada.");

        if (!await _unitOfWork.PrerequisiteExistsAsync(subjectId, prerequisiteSubjectId, cancellationToken))
            return Result<bool>.NotFound("El prerequisito especificado no existe.");

        await _unitOfWork.RemovePrerequisiteAsync(subjectId, prerequisiteSubjectId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }

    public async Task<Result<IEnumerable<PrerequisiteDto>>> SetPrerequisitesAsync(
        Guid subjectId, IReadOnlyList<Guid> prerequisiteIds, CancellationToken cancellationToken = default)
    {
        var subject = await _unitOfWork.Subjects.GetByIdAsync(subjectId, cancellationToken);
        if (subject is null)
            return Result<IEnumerable<PrerequisiteDto>>.NotFound("Materia no encontrada.");

        if (prerequisiteIds.Contains(subjectId))
            return Result<IEnumerable<PrerequisiteDto>>.Failure("Una materia no puede ser prerequisito de sí misma.");

        if (prerequisiteIds.Count != prerequisiteIds.Distinct().Count())
            return Result<IEnumerable<PrerequisiteDto>>.Failure("La lista de prerequisitos contiene IDs duplicados.");

        var prereqSubjects = new List<Subject>();
        foreach (var id in prerequisiteIds)
        {
            var s = await _unitOfWork.Subjects.GetByIdAsync(id, cancellationToken);
            if (s is null)
                return Result<IEnumerable<PrerequisiteDto>>.NotFound($"Materia prerequisito no encontrada: {id}");
            prereqSubjects.Add(s);
        }

        // Construir grafo propuesto: todos los prereqs existentes (excluyendo los anteriores para subjectId) + los nuevos
        var allPrereqs = await _unitOfWork.GetAllActivePrerequisitesAsync(cancellationToken);
        var existingEdges = allPrereqs
            .Where(p => p.SubjectId != subjectId)
            .Select(p => (p.SubjectId, p.PrerequisiteSubjectId))
            .ToList();
        var proposedEdges = existingEdges
            .Concat(prerequisiteIds.Select(pid => (subjectId, pid)))
            .ToList();

        foreach (var pid in prerequisiteIds)
        {
            if (HasPathInGraph(proposedEdges, pid, subjectId))
                return Result<IEnumerable<PrerequisiteDto>>.Failure(
                    "La relación de prerequisitos crea un ciclo académico.");
        }

        await _unitOfWork.RemoveAllPrerequisitesForSubjectAsync(subjectId, cancellationToken);
        foreach (var pid in prerequisiteIds)
            await _unitOfWork.AddPrerequisiteAsync(new SubjectPrerequisite(subjectId, pid), cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<IEnumerable<PrerequisiteDto>>.Success(prereqSubjects.Select(MapToPrerequisiteDto));
    }

    // Detección de ciclos en el grafo de prerequisitos.
    // Retorna true si existe un camino dirigido de `from` a `to`.
    // Dirección de arista: SubjectId → PrerequisiteSubjectId (la materia depende del prereq).
    private static bool HasPath(IEnumerable<SubjectPrerequisite> prereqs, Guid from, Guid to)
    {
        var graph = prereqs
            .GroupBy(p => p.SubjectId)
            .ToDictionary(g => g.Key, g => g.Select(p => p.PrerequisiteSubjectId).ToHashSet());
        return DfsReaches(graph, from, to, new HashSet<Guid>());
    }

    private static bool HasPathInGraph(IEnumerable<(Guid SubjectId, Guid PrereqId)> edges, Guid from, Guid to)
    {
        var graph = edges
            .GroupBy(e => e.SubjectId)
            .ToDictionary(g => g.Key, g => g.Select(e => e.PrereqId).ToHashSet());
        return DfsReaches(graph, from, to, new HashSet<Guid>());
    }

    private static bool DfsReaches(Dictionary<Guid, HashSet<Guid>> graph, Guid current, Guid target, HashSet<Guid> visited)
    {
        if (!visited.Add(current)) return false;
        if (!graph.TryGetValue(current, out var neighbors)) return false;
        foreach (var neighbor in neighbors)
        {
            if (neighbor == target) return true;
            if (DfsReaches(graph, neighbor, target, visited)) return true;
        }
        return false;
    }

    // Mappers
    private static SubjectDto MapToDto(Subject subject) =>
        new(subject.Id, subject.Code, subject.Name, subject.Description, subject.Credits,
            subject.SemesterLevel, subject.AppliesToAllCareers,
            subject.Sections.Count(s => s.IsActive),
            subject.Careers);

    private static PrerequisiteDto MapToPrerequisiteDto(Subject subject) =>
        new(subject.Id, subject.Code, subject.Name, subject.Credits, subject.SemesterLevel);
}
