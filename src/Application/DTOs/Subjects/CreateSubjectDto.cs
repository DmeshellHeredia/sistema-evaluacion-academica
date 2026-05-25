namespace SistemaEvaluacionAcademica.Application.DTOs.Subjects;

public record CreateSubjectDto(
    string Code,
    string Name,
    string Description,
    int Credits,
    int SemesterLevel,
    bool AppliesToAllCareers = false,
    IReadOnlyList<string>? Careers = null
);
