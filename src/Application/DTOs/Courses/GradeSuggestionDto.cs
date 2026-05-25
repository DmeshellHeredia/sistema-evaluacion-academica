namespace SistemaEvaluacionAcademica.Application.DTOs.Courses;

/// <summary>
/// Vista por alumno que el profesor recibe al consultar sugerencias de notas para su sección.
/// SuggestedScore es 0-10 calculado desde actividades ponderadas; null si ninguna está calificada.
/// OfficialGrade es la nota académica registrada en el expediente; null si aún no fue asignada.
/// </summary>
public record StudentGradeSuggestionDto(
    Guid StudentId,
    string StudentCode,
    string StudentName,
    decimal? SuggestedScore,
    decimal? OfficialGrade,
    int CompletedActivities,
    int TotalActivities,
    decimal TotalWeight
);

/// <summary>
/// Vista personal del estudiante. Mismos campos pero sin datos de otros alumnos.
/// </summary>
public record MySuggestionDto(
    decimal? SuggestedScore,
    decimal? OfficialGrade,
    int CompletedActivities,
    int TotalActivities,
    decimal TotalWeight
);
