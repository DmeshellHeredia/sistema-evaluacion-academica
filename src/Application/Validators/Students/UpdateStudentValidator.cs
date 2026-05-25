using FluentValidation;
using SistemaEvaluacionAcademica.Application.DTOs.Students;
using SistemaEvaluacionAcademica.Domain.Enums;

namespace SistemaEvaluacionAcademica.Application.Validators.Students;

public class UpdateStudentValidator : AbstractValidator<UpdateStudentDto>
{
    public UpdateStudentValidator()
    {
        RuleFor(x => x.Career)
            .NotEmpty().WithMessage("La carrera es requerida.")
            .Must(CareerTypes.IsValid)
            .WithMessage($"Carrera inválida. Opciones: {string.Join(", ", CareerTypes.All)}.");

        RuleFor(x => x.Semester)
            .InclusiveBetween(1, 8).WithMessage("El semestre debe estar entre 1 y 8.");
    }
}
