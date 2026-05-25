using FluentValidation;
using SistemaEvaluacionAcademica.Application.DTOs.Auth;

namespace SistemaEvaluacionAcademica.Application.Validators.Auth;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es requerido.")
            .EmailAddress().WithMessage("El formato del correo electrónico es inválido.")
            .MaximumLength(256).WithMessage("El correo no puede superar los 256 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es requerida.")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.");
    }
}
