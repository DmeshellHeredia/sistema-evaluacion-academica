using FluentValidation;
using SistemaEvaluacionAcademica.Application.DTOs.Grades;

namespace SistemaEvaluacionAcademica.Application.Validators.Grades;

public class CreateGradeValidator : AbstractValidator<CreateGradeDto>
{
    public CreateGradeValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("El identificador del estudiante es requerido.");

        RuleFor(x => x.SectionId)
            .NotEmpty().WithMessage("El identificador de la sección es requerido.");

        RuleFor(x => x.Value)
            .InclusiveBetween(0, 10).WithMessage("La calificación debe estar entre 0 y 10.")
            .PrecisionScale(4, 2, true).WithMessage("La calificación puede tener máximo 2 decimales.");

        RuleFor(x => x.Period)
            .NotEmpty().WithMessage("El período es requerido.")
            .MaximumLength(20).WithMessage("El período no puede superar 20 caracteres.")
            .Matches(@"^\d{4}-(1|2)$").WithMessage("El formato del período debe ser YYYY-1 o YYYY-2 (ejemplo: 2025-1).");

        RuleFor(x => x.Comments)
            .MaximumLength(500).WithMessage("Los comentarios no pueden superar los 500 caracteres.")
            .When(x => x.Comments is not null);
    }
}
