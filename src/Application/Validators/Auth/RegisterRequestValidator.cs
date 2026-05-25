using FluentValidation;
using SistemaEvaluacionAcademica.Application.Common;
using SistemaEvaluacionAcademica.Application.DTOs.Auth;
using SistemaEvaluacionAcademica.Domain.Enums;

namespace SistemaEvaluacionAcademica.Application.Validators.Auth;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es requerido.")
            .EmailAddress().WithMessage("El formato del correo es inválido.")
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es requerida.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .Matches(@"[A-Z]").WithMessage("Debe contener al menos una letra mayúscula.")
            .Matches(@"[0-9]").WithMessage("Debe contener al menos un número.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es requerido.")
            .MaximumLength(100);

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("El rol especificado no es válido.");

        When(x => x.Role == RoleType.Estudiante, () =>
        {
            RuleFor(x => x.Career)
                .NotEmpty().WithMessage("La carrera es requerida para estudiantes.")
                .Must(c => c != null && CareerTypes.IsValid(c))
                .WithMessage($"Carrera inválida. Opciones: {string.Join(", ", CareerTypes.All)}.");

            RuleFor(x => x.Semester)
                .NotNull().WithMessage("El semestre es requerido para estudiantes.")
                .InclusiveBetween(1, 8).WithMessage("El semestre debe estar entre 1 y 8.");

            RuleFor(x => x.Email)
                .Must((req, email) =>
                    !string.IsNullOrWhiteSpace(req.FirstName) &&
                    !string.IsNullOrWhiteSpace(req.LastName) &&
                    email == EmailHelper.DeriveStudentEmail(req.FirstName, req.LastName))
                .WithMessage("El correo del estudiante debe derivarse del nombre y apellido ([nombre].[apellido]@academia.com).");
        });
    }
}
