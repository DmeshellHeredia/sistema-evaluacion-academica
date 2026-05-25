using SistemaEvaluacionAcademica.Domain.Enums;

namespace SistemaEvaluacionAcademica.Domain.Entities;

public class Activity : BaseEntity
{
    public Guid SectionId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ActivityType Type { get; private set; }
    public DateTime? DueDate { get; private set; }
    public decimal MaxScore { get; private set; }
    public decimal Weight { get; private set; }
    public bool IsPublished { get; private set; }
    public string? ResourceUrl { get; private set; }

    public virtual SubjectSection Section { get; private set; } = null!;
    public virtual ICollection<ActivitySubmission> Submissions { get; private set; } = new List<ActivitySubmission>();

    private Activity() { }

    public Activity(
        Guid sectionId,
        string title,
        string description,
        ActivityType type,
        DateTime? dueDate,
        decimal maxScore,
        decimal weight,
        bool isPublished = true,
        string? resourceUrl = null)
    {
        SectionId = sectionId;
        Title = title;
        Description = description;
        Type = type;
        DueDate = dueDate;
        MaxScore = maxScore;
        Weight = weight;
        IsPublished = isPublished;
        ResourceUrl = resourceUrl;
    }

    public void Update(string title, string description, ActivityType type, DateTime? dueDate, decimal maxScore, decimal weight, bool isPublished, string? resourceUrl)
    {
        Title = title;
        Description = description;
        Type = type;
        DueDate = dueDate;
        MaxScore = maxScore;
        Weight = weight;
        IsPublished = isPublished;
        ResourceUrl = resourceUrl;
    }

    public void Close()
    {
        IsPublished = false;
    }
}
