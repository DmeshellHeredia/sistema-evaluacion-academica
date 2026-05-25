namespace SistemaEvaluacionAcademica.Domain.Entities;

public class Subject : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int Credits { get; private set; }
    public int SemesterLevel { get; private set; }
    public bool AppliesToAllCareers { get; private set; }
    public IReadOnlyList<string> Careers { get; private set; } = Array.Empty<string>();

    public virtual ICollection<Grade> Grades { get; private set; } = new List<Grade>();
    public virtual ICollection<SubjectSection> Sections { get; private set; } = new List<SubjectSection>();
    public virtual ICollection<SubjectPrerequisite> Prerequisites { get; private set; } = new List<SubjectPrerequisite>();

    private Subject() { }

    public Subject(string code, string name, string description, int credits, string career, int semesterLevel, bool appliesToAllCareers = false)
        : this(code, name, description, credits,
               appliesToAllCareers || string.IsNullOrEmpty(career) ? Array.Empty<string>() : new[] { career },
               semesterLevel, appliesToAllCareers) { }

    public Subject(string code, string name, string description, int credits, IReadOnlyList<string> careers, int semesterLevel, bool appliesToAllCareers = false)
    {
        Code = code;
        Name = name;
        Description = description;
        Credits = credits;
        AppliesToAllCareers = appliesToAllCareers;
        Careers = appliesToAllCareers ? Array.Empty<string>() : careers;
        SemesterLevel = semesterLevel;
    }

    public void Update(string name, string description, int credits, IReadOnlyList<string> careers, bool appliesToAllCareers, int semesterLevel)
    {
        Name = name;
        Description = description;
        Credits = credits;
        AppliesToAllCareers = appliesToAllCareers;
        Careers = appliesToAllCareers ? Array.Empty<string>() : careers;
        SemesterLevel = semesterLevel;
    }

    public bool AppliesToCareer(string career) =>
        AppliesToAllCareers ||
        Careers.Any(c => string.Equals(c, career, StringComparison.OrdinalIgnoreCase));
}
