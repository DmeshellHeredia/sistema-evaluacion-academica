namespace SistemaEvaluacionAcademica.Application.DTOs.Courses;

public record AnnouncementDto(
    Guid Id,
    Guid SectionId,
    string Title,
    string Content,
    Guid AuthorId,
    string AuthorName,
    bool IsPublished,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateAnnouncementDto(
    string Title,
    string Content,
    bool IsPublished = true
);

public record UpdateAnnouncementDto(
    string Title,
    string Content,
    bool IsPublished
);
