namespace SistemaEvaluacionAcademica.Application.DTOs.Grades;

public record StudentGradeReportDto(
    Guid StudentId,
    string StudentCode,
    string StudentFullName,
    string Career,
    int Semester,
    decimal? OverallAverage,
    string AverageCategory,
    IEnumerable<SubjectGradeDto> SubjectGrades
);

public record SubjectGradeDto(
    string SubjectCode,
    string SubjectName,
    decimal Grade,
    string Category,
    string Period,
    string? Comments
);
