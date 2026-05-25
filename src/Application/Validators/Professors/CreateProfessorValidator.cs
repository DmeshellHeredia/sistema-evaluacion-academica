using FluentValidation;
using SistemaEvaluacionAcademica.Application.DTOs.Professors;

namespace SistemaEvaluacionAcademica.Application.Validators.Professors;

public class CreateProfessorValidator : AbstractValidator<CreateProfessorDto>
{
    public CreateProfessorValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo es requerido.")
            .EmailAddress().WithMessage("El formato del correo es inválido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es requerida.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es requerido.")
            .MaximumLength(100);
    }
}
