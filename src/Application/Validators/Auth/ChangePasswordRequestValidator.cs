using FluentValidation;
using SistemaEvaluacionAcademica.Application.DTOs.Auth;

namespace SistemaEvaluacionAcademica.Application.Validators.Auth;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("La contraseña actual es requerida.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("La contraseña nueva es requerida.")
            .MinimumLength(8).WithMessage("La contraseña nueva debe tener al menos 8 caracteres.")
            .Matches(@"[A-Z]").WithMessage("La contraseña nueva debe contener al menos una letra mayúscula.")
            .Matches(@"[0-9]").WithMessage("La contraseña nueva debe contener al menos un número.");
    }
}
