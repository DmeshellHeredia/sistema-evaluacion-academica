namespace SistemaEvaluacionAcademica.Domain.Entities;

public class SubjectPrerequisite : BaseEntity
{
    public Guid SubjectId { get; private set; }
    public Guid PrerequisiteSubjectId { get; private set; }

    public virtual Subject Subject { get; private set; } = null!;
    public virtual Subject PrerequisiteSubject { get; private set; } = null!;

    private SubjectPrerequisite() { }

    public SubjectPrerequisite(Guid subjectId, Guid prerequisiteSubjectId)
    {
        SubjectId = subjectId;
        PrerequisiteSubjectId = prerequisiteSubjectId;
    }
}
