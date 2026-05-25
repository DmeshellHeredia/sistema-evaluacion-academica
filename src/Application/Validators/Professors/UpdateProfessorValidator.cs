using FluentValidation;
using SistemaEvaluacionAcademica.Application.DTOs.Professors;

namespace SistemaEvaluacionAcademica.Application.Validators.Professors;

public class UpdateProfessorValidator : AbstractValidator<UpdateProfessorDto>
{
    public UpdateProfessorValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es requerido.")
            .MaximumLength(100);
    }
}
