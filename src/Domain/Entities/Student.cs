namespace SistemaEvaluacionAcademica.Domain.Entities;

public class Student : BaseEntity
{
    public string StudentCode { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public string Career { get; private set; } = string.Empty;
    public int Semester { get; private set; }

    public virtual User User { get; private set; } = null!;
    public virtual ICollection<Grade> Grades { get; private set; } = new List<Grade>();
    public virtual ICollection<SectionEnrollment> SectionEnrollments { get; private set; } = new List<SectionEnrollment>();

    private Student() { }

    public Student(string studentCode, Guid userId, string career, int semester)
    {
        StudentCode = studentCode;
        UserId = userId;
        Career = career;
        Semester = semester;
    }

    public void Update(string career, int semester)
    {
        Career = career;
        Semester = semester;
    }

    public decimal? CalculateOverallAverage()
    {
        var active = Grades.Where(g => g.IsActive).ToList();
        if (active.Count == 0) return null;
        return Math.Round(active.Average(g => g.Value), 2);
    }

    public static string GenerateCode()
    {
        var year  = DateTime.UtcNow.Year;
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(3);
        return $"EST-{year}-{Convert.ToHexString(bytes)}";
    }
}
