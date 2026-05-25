using FluentValidation;

namespace SistemaEvaluacionAcademica.Application.Validators.Grades;

public record UpdateGradeRequest(decimal Value, string? Comments);

public class UpdateGradeValidator : AbstractValidator<UpdateGradeRequest>
{
    public UpdateGradeValidator()
    {
        RuleFor(x => x.Value)
            .InclusiveBetween(0, 10).WithMessage("La calificación debe estar entre 0 y 10.")
            .PrecisionScale(4, 2, true).WithMessage("La calificación puede tener máximo 2 decimales.");

        RuleFor(x => x.Comments)
            .MaximumLength(500).WithMessage("Los comentarios no pueden superar 500 caracteres.")
            .When(x => x.Comments is not null);
    }
}
