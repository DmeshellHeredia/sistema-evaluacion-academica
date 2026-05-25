namespace SistemaEvaluacionAcademica.Application.DTOs.Courses;

public record ActivityDto(
    Guid Id,
    Guid SectionId,
    string Title,
    string Description,
    string Type,
    DateTime? DueDate,
    decimal MaxScore,
    decimal Weight,
    bool IsPublished,
    string? ResourceUrl,
    int TotalSubmissions,
    int GradedSubmissions,
    DateTime CreatedAt
);

public record CreateActivityDto(
    string Title,
    string Description,
    string Type,
    DateTime? DueDate,
    decimal MaxScore,
    decimal Weight,
    bool IsPublished = true,
    string? ResourceUrl = null
);

public record UpdateActivityDto(
    string Title,
    string Description,
    string Type,
    DateTime? DueDate,
    decimal MaxScore,
    decimal Weight,
    bool IsPublished,
    string? ResourceUrl
);
