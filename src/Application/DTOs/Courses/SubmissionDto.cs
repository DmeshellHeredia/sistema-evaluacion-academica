namespace SistemaEvaluacionAcademica.Application.DTOs.Courses;

public record SubmissionDto(
    Guid Id,
    Guid ActivityId,
    string ActivityTitle,
    Guid StudentId,
    string StudentName,
    string StudentCode,
    string? Content,
    DateTime? SubmittedAt,
    decimal? Score,
    decimal MaxScore,
    string? Feedback,
    string Status,
    DateTime CreatedAt
);

public record SubmitDto(
    string? Content
);

public record GradeSubmissionDto(
    decimal Score,
    string? Feedback
);

public record StudentSubmissionDto(
    Guid ActivityId,
    string ActivityTitle,
    string ActivityType,
    DateTime? DueDate,
    decimal MaxScore,
    decimal Weight,
    Guid? SubmissionId,
    string? Content,
    DateTime? SubmittedAt,
    decimal? Score,
    string? Feedback,
    string Status
);
