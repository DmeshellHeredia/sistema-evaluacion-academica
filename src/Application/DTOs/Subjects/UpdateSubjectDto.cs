namespace SistemaEvaluacionAcademica.Application.DTOs.Subjects;

public record UpdateSubjectDto(
    string Name,
    string Description,
    int Credits,
    int SemesterLevel,
    bool AppliesToAllCareers = false,
    IReadOnlyList<string>? Careers = null
);
