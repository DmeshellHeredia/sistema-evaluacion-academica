namespace SistemaEvaluacionAcademica.Application.DTOs.Subjects;

public record PrerequisiteDto(Guid SubjectId, string Code, string Name, int Credits, int SemesterLevel);

public record SetPrerequisitesDto(IReadOnlyList<Guid> PrerequisiteIds);
