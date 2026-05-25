using SistemaEvaluacionAcademica.Domain.Enums;

namespace SistemaEvaluacionAcademica.Domain.Entities;

public class ActivitySubmission : BaseEntity
{
    public Guid ActivityId { get; private set; }
    public Guid StudentId { get; private set; }
    public string? Content { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public decimal? Score { get; private set; }
    public string? Feedback { get; private set; }
    public SubmissionStatus Status { get; private set; }

    public virtual Activity Activity { get; private set; } = null!;
    public virtual Student Student { get; private set; } = null!;

    private ActivitySubmission() { }

    public ActivitySubmission(Guid activityId, Guid studentId, string? content = null)
    {
        ActivityId = activityId;
        StudentId = studentId;
        Content = content;
        Status = SubmissionStatus.Pendiente;
    }

    public void Submit(string? content)
    {
        Content = content;
        SubmittedAt = DateTime.UtcNow;
        Status = SubmissionStatus.Entregada;
    }

    public void Grade(decimal score, string? feedback)
    {
        Score = score;
        Feedback = feedback;
        Status = SubmissionStatus.Calificada;
    }

    public void Close()
    {
        Status = SubmissionStatus.Cerrada;
    }
}
