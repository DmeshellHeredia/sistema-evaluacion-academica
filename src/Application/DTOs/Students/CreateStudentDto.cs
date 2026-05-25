namespace SistemaEvaluacionAcademica.Application.DTOs.Students;

public record CreateStudentDto(
    string Password,
    string FirstName,
    string LastName,
    string Career,
    int Semester
);
