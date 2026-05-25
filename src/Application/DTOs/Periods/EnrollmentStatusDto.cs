namespace SistemaEvaluacionAcademica.Application.DTOs.Periods;

public record EnrollmentStatusDto(
    bool IsOpen,
    string? PeriodName,
    string? PeriodCode,
    DateTime? StartDate,
    DateTime? EndDate
);
