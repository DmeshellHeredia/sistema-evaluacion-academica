using FluentValidation;
using SistemaEvaluacionAcademica.Application.DTOs.Students;
using SistemaEvaluacionAcademica.Domain.Enums;

namespace SistemaEvaluacionAcademica.Application.Validators.Students;

public class CreateStudentValidator : AbstractValidator<CreateStudentDto>
{
    public CreateStudentValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es requerida.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es requerido.")
            .MaximumLength(100);

        RuleFor(x => x.Career)
            .NotEmpty().WithMessage("La carrera es requerida.")
            .Must(CareerTypes.IsValid)
            .WithMessage($"Carrera inválida. Opciones: {string.Join(", ", CareerTypes.All)}.");

        RuleFor(x => x.Semester)
            .InclusiveBetween(1, 8).WithMessage("El semestre debe estar entre 1 y 8.");
    }
}
