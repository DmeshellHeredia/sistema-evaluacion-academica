namespace SistemaEvaluacionAcademica.Application.DTOs.Periods;

public record AcademicPeriodDto(
    Guid Id,
    string Name,
    string Code,
    DateTime StartDate,
    DateTime EndDate,
    bool IsEnrollmentOpen,
    DateTime CreatedAt
);
