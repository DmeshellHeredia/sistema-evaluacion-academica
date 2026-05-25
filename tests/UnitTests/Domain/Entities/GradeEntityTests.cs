using FluentAssertions;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Enums;
using SistemaEvaluacionAcademica.Domain.Exceptions;
using SistemaEvaluacionAcademica.UnitTests.Helpers;

namespace SistemaEvaluacionAcademica.UnitTests.Domain.Entities;

public class GradeEntityTests
{

    [Theory]
    [InlineData(10.0, GradeCategory.Excelente)]
    [InlineData(9.0,  GradeCategory.Excelente)]
    [InlineData(8.99, GradeCategory.Buena)]
    [InlineData(7.0,  GradeCategory.Buena)]
    [InlineData(6.99, GradeCategory.PorMejorar)]
    [InlineData(0.0,  GradeCategory.PorMejorar)]
    public void Category_ShouldBeComputedCorrectly(decimal value, GradeCategory expected)
    {
        var grade = TestDataBuilder.CreateGrade(value: value);
        grade.Category.Should().Be(expected);
    }

    [Theory]
    [InlineData(9.5,  "Excelente")]
    [InlineData(7.5,  "Buena")]
    [InlineData(4.0,  "Por mejorar")]
    public void CategoryDescription_ShouldReturnSpanishLabel(decimal value, string expected)
    {
        var grade = TestDataBuilder.CreateGrade(value: value);
        grade.CategoryDescription.Should().Be(expected);
    }


    [Fact]
    public void Category_AtExactly9Point0_ShouldBeExcelente()
    {
        var grade = TestDataBuilder.CreateGrade(value: 9.0m);
        grade.Category.Should().Be(GradeCategory.Excelente);
    }

    [Fact]
    public void Category_AtExactly7Point0_ShouldBeBuena()
    {
        var grade = TestDataBuilder.CreateGrade(value: 7.0m);
        grade.Category.Should().Be(GradeCategory.Buena);
    }

    [Fact]
    public void Category_JustBelow7Point0_ShouldBePorMejorar()
    {
        var grade = TestDataBuilder.CreateGrade(value: 6.99m);
        grade.Category.Should().Be(GradeCategory.PorMejorar);
    }


    [Theory]
    [InlineData(10.01)]
    [InlineData(11.0)]
    [InlineData(100.0)]
    [InlineData(-0.01)]
    [InlineData(-1.0)]
    public void Constructor_WithOutOfRangeValue_ShouldThrowDomainException(decimal invalidValue)
    {
        var act = () => TestDataBuilder.CreateGrade(value: invalidValue);
        act.Should().Throw<DomainException>()
            .WithMessage("*calificación*");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(5.5)]
    [InlineData(10.0)]
    [InlineData(9.99)]
    public void Constructor_WithValidValue_ShouldNotThrow(decimal validValue)
    {
        var act = () => TestDataBuilder.CreateGrade(value: validValue);
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var studentId     = Guid.NewGuid();
        var subjectId     = Guid.NewGuid();
        var gradedById    = Guid.NewGuid();
        var period        = "2025-1";
        var comments      = "Buen desempeño";

        var grade = new Grade(studentId, subjectId, null, 8.5m, period, gradedById, comments);

        grade.StudentId.Should().Be(studentId);
        grade.SubjectId.Should().Be(subjectId);
        grade.Value.Should().Be(8.5m);
        grade.Period.Should().Be(period);
        grade.GradedByUserId.Should().Be(gradedById);
        grade.Comments.Should().Be(comments);
        grade.Id.Should().NotBeEmpty();
        grade.IsActive.Should().BeTrue();
        grade.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Constructor_WhenCommentsIsNull_ShouldBeNull()
    {
        var grade = TestDataBuilder.CreateGrade(comments: null);
        grade.Comments.Should().BeNull();
    }


    [Fact]
    public void Update_WithValidValue_ShouldChangeValueAndComments()
    {
        var grade = TestDataBuilder.CreateGrade(value: 7.0m);
        var newComments = "Actualizado por corrección";

        grade.Update(9.5m, newComments);

        grade.Value.Should().Be(9.5m);
        grade.Comments.Should().Be(newComments);
    }

    [Fact]
    public void Update_WithNullComments_ShouldSetCommentsToNull()
    {
        var grade = TestDataBuilder.CreateGrade(value: 8.0m, comments: "Comentario inicial");

        grade.Update(8.0m, null);

        grade.Comments.Should().BeNull();
    }

    [Fact]
    public void Update_WithInvalidValue_ShouldThrowDomainException()
    {
        var grade = TestDataBuilder.CreateGrade(value: 8.0m);

        var act = () => grade.Update(11.0m, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Update_ShouldRecalculateCategory()
    {
        var grade = TestDataBuilder.CreateGrade(value: 5.0m);
        grade.Category.Should().Be(GradeCategory.PorMejorar);

        grade.Update(9.5m, null);

        grade.Category.Should().Be(GradeCategory.Excelente);
    }


    [Fact]
    public void NewGrade_ShouldHaveIsActiveTrue()
    {
        var grade = TestDataBuilder.CreateGrade();
        grade.IsActive.Should().BeTrue();
    }

    [Fact]
    public void NewGrade_UpdatedAt_ShouldBeNull()
    {
        var grade = TestDataBuilder.CreateGrade();
        grade.UpdatedAt.Should().BeNull();
    }
}
