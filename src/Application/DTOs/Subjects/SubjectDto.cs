namespace SistemaEvaluacionAcademica.Application.DTOs.Subjects;

public record SubjectDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    int Credits,
    int SemesterLevel,
    bool AppliesToAllCareers,
    int SectionCount,
    IReadOnlyList<string> Careers
);
