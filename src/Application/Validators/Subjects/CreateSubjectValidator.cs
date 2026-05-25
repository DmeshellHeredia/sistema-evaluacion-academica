using FluentValidation;
using SistemaEvaluacionAcademica.Application.DTOs.Subjects;
using SistemaEvaluacionAcademica.Domain.Enums;

namespace SistemaEvaluacionAcademica.Application.Validators.Subjects;

public class CreateSubjectValidator : AbstractValidator<CreateSubjectDto>
{
    public CreateSubjectValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("El código de la materia es requerido.")
            .MaximumLength(20).WithMessage("El código no puede superar 20 caracteres.")
            .Matches(@"^[A-Z][A-Z0-9\-]{1,18}[A-Z0-9]$")
            .WithMessage("El código debe contener solo letras mayúsculas, dígitos y guiones, y tener al menos 3 caracteres (ejemplo: MAT101, IS-MAT1).");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la materia es requerido.")
            .MinimumLength(3).WithMessage("El nombre debe tener al menos 3 caracteres.")
            .MaximumLength(200).WithMessage("El nombre no puede superar 200 caracteres.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción es requerida.")
            .MaximumLength(1000).WithMessage("La descripción no puede superar 1000 caracteres.");

        RuleFor(x => x.Credits)
            .InclusiveBetween(1, 10).WithMessage("Los créditos deben estar entre 1 y 10.");

        RuleFor(x => x.Careers)
            .Must(careers => careers != null && careers.Count > 0)
            .WithMessage("Debe especificar al menos una carrera.")
            .When(x => !x.AppliesToAllCareers);

        RuleForEach(x => x.Careers)
            .Must(CareerTypes.IsValid)
            .WithMessage($"Carrera inválida. Opciones: {string.Join(", ", CareerTypes.All)}.")
            .When(x => x.Careers is { Count: > 0 });

        RuleFor(x => x.SemesterLevel)
            .InclusiveBetween(1, 8).WithMessage("El nivel de semestre debe estar entre 1 y 8.");
    }
}
