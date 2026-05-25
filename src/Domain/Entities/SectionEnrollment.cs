namespace SistemaEvaluacionAcademica.Domain.Entities;

public class SectionEnrollment : BaseEntity
{
    public Guid StudentId { get; private set; }
    public Guid SectionId { get; private set; }
    public DateTime EnrollmentDate { get; private set; }

    public virtual Student Student { get; private set; } = null!;
    public virtual SubjectSection Section { get; private set; } = null!;

    private SectionEnrollment() { }

    public SectionEnrollment(Guid studentId, Guid sectionId)
    {
        StudentId = studentId;
        SectionId = sectionId;
        EnrollmentDate = DateTime.UtcNow;
    }
}
