using SistemaEvaluacionAcademica.Domain.Enums;

namespace SistemaEvaluacionAcademica.Domain.Entities;

public class SubjectSection : BaseEntity
{
    public Guid SubjectId { get; private set; }
    public Guid ProfessorId { get; private set; }
    public string SectionCode { get; private set; } = string.Empty;
    public DayOfWeekType DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public string Modality { get; private set; } = string.Empty;
    public int Capacity { get; private set; }
    public string Classroom { get; private set; } = string.Empty;

    public virtual Subject Subject { get; private set; } = null!;
    public virtual User Professor { get; private set; } = null!;
    public virtual ICollection<SectionEnrollment> Enrollments { get; private set; } = new List<SectionEnrollment>();
    public virtual ICollection<Grade> Grades { get; private set; } = new List<Grade>();

    private SubjectSection() { }

    public SubjectSection(
        Guid subjectId,
        Guid professorId,
        string sectionCode,
        DayOfWeekType dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        string modality,
        int capacity,
        string classroom = "")
    {
        SubjectId = subjectId;
        ProfessorId = professorId;
        SectionCode = sectionCode;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        Modality = modality;
        Capacity = capacity;
        Classroom = classroom;
    }

    public void Update(DayOfWeekType dayOfWeek, TimeOnly startTime, TimeOnly endTime, string modality, int capacity, string classroom)
    {
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        Modality = modality;
        Capacity = capacity;
        Classroom = classroom;
    }

    public bool OverlapsWith(SubjectSection other)
    {
        if (DayOfWeek != other.DayOfWeek)
            return false;
        return StartTime < other.EndTime && EndTime > other.StartTime;
    }

    public int ActiveEnrollmentCount => Enrollments.Count(e => e.IsActive);
    public bool IsFull => ActiveEnrollmentCount >= Capacity;
}
