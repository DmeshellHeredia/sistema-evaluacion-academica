namespace SistemaEvaluacionAcademica.Domain.Entities;

public class Announcement : BaseEntity
{
    public Guid SectionId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public Guid AuthorId { get; private set; }
    public bool IsPublished { get; private set; }

    public virtual SubjectSection Section { get; private set; } = null!;
    public virtual User Author { get; private set; } = null!;

    private Announcement() { }

    public Announcement(Guid sectionId, Guid authorId, string title, string content, bool isPublished = true)
    {
        SectionId = sectionId;
        AuthorId = authorId;
        Title = title;
        Content = content;
        IsPublished = isPublished;
    }

    public void Update(string title, string content, bool isPublished)
    {
        Title = title;
        Content = content;
        IsPublished = isPublished;
    }
}
