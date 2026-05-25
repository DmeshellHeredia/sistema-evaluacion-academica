namespace SistemaEvaluacionAcademica.Application.DTOs.Periods;

public record UpdateAcademicPeriodDto(
    string Name,
    string Code,
    DateTime StartDate,
    DateTime EndDate
);
