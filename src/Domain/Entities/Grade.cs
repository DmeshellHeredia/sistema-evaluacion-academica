using SistemaEvaluacionAcademica.Domain.Enums;
using SistemaEvaluacionAcademica.Domain.Exceptions;

namespace SistemaEvaluacionAcademica.Domain.Entities;

public class Grade : BaseEntity
{
    public Guid StudentId { get; private set; }
    public Guid SubjectId { get; private set; }
    public Guid? SectionId { get; private set; }
    public decimal Value { get; private set; }
    public string Period { get; private set; } = string.Empty;
    public string? Comments { get; private set; }
    public Guid GradedByUserId { get; private set; }

    public virtual Student Student { get; private set; } = null!;
    public virtual Subject Subject { get; private set; } = null!;
    public virtual SubjectSection? Section { get; private set; }
    public virtual User GradedByUser { get; private set; } = null!;

    private Grade() { }

    public Grade(Guid studentId, Guid subjectId, Guid? sectionId, decimal value, string period, Guid gradedByUserId, string? comments = null)
    {
        ValidateValue(value);
        StudentId = studentId;
        SubjectId = subjectId;
        SectionId = sectionId;
        Value = value;
        Period = period;
        GradedByUserId = gradedByUserId;
        Comments = comments;
    }

    public GradeCategory Category => Value switch
    {
        >= 9.0m => GradeCategory.Excelente,
        >= 7.0m => GradeCategory.Buena,
        _ => GradeCategory.PorMejorar
    };

    public string CategoryDescription => CategoryFor(Value);

    public static string CategoryFor(decimal value) => value switch
    {
        >= 9.0m => "Excelente",
        >= 7.0m => "Buena",
        _ => "Por mejorar"
    };

    public void Update(decimal newValue, string? comments)
    {
        ValidateValue(newValue);
        Value = newValue;
        Comments = comments;
    }

    private static void ValidateValue(decimal value)
    {
        if (value < 0 || value > 10)
            throw new DomainException("La calificación debe estar entre 0 y 10.");
    }
}
