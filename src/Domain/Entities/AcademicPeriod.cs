namespace SistemaEvaluacionAcademica.Domain.Entities;

public class AcademicPeriod : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public bool IsEnrollmentOpen { get; private set; }

    private AcademicPeriod() { }

    public AcademicPeriod(string name, string code, DateTime startDate, DateTime endDate)
    {
        Name = name;
        Code = code;
        StartDate = startDate;
        EndDate = endDate;
        IsEnrollmentOpen = false;
    }

    public void OpenEnrollment()
    {
        IsEnrollmentOpen = true;
    }

    public void CloseEnrollment()
    {
        IsEnrollmentOpen = false;
    }

    public void Update(string name, string code, DateTime startDate, DateTime endDate)
    {
        Name = name;
        Code = code;
        StartDate = startDate;
        EndDate = endDate;
    }
}
