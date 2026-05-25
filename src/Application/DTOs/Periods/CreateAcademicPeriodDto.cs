namespace SistemaEvaluacionAcademica.Application.DTOs.Periods;

public record CreateAcademicPeriodDto(
    string Name,
    string Code,
    DateTime StartDate,
    DateTime EndDate
);
