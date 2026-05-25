using FluentAssertions;
using SistemaEvaluacionAcademica.Application.DTOs.Grades;
using SistemaEvaluacionAcademica.Application.Validators.Grades;

namespace SistemaEvaluacionAcademica.UnitTests.Validators;

public class CreateGradeValidatorTests
{
    private readonly CreateGradeValidator _validator = new();


    [Theory]
    [InlineData(0)]
    [InlineData(5.5)]
    [InlineData(10)]
    [InlineData(9.99)]
    [InlineData(0.01)]
    public async Task Validate_WithValidGradeValue_ShouldPassValidation(decimal value)
    {
        var dto    = ValidDto() with { Value = value };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.Should().BeTrue();
    }


    [Theory]
    [InlineData(-0.01)]
    [InlineData(10.01)]
    [InlineData(100)]
    [InlineData(-1)]
    public async Task Validate_WithOutOfRangeGradeValue_ShouldFailOnValueProperty(decimal value)
    {
        var dto    = ValidDto() with { Value = value };
        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Value");
    }


    [Theory]
    [InlineData("2024-1")]
    [InlineData("2025-2")]
    [InlineData("2030-1")]
    [InlineData("1999-2")]
    public async Task Validate_WithValidPeriod_ShouldPassValidation(string period)
    {
        var dto    = ValidDto() with { Period = period };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("2024")]
    [InlineData("24-1")]
    [InlineData("2024-3")]
    [InlineData("2024-0")]
    [InlineData("periodo-1")]
    [InlineData("YYYY-1")]
    public async Task Validate_WithInvalidPeriod_ShouldFailValidation(string period)
    {
        var dto    = ValidDto() with { Period = period };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Period");
    }


    [Fact]
    public async Task Validate_WithEmptyStudentId_ShouldFailOnStudentIdProperty()
    {
        var dto    = ValidDto() with { StudentId = Guid.Empty };
        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StudentId");
    }


    [Fact]
    public async Task Validate_WithEmptySectionId_ShouldFailOnSectionIdProperty()
    {
        var dto    = ValidDto() with { SectionId = Guid.Empty };
        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SectionId");
    }


    [Fact]
    public async Task Validate_WithCommentsTooLong_ShouldFailOnCommentsProperty()
    {
        var longComment = new string('A', 501);
        var dto         = ValidDto() with { Comments = longComment };
        var result      = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Comments");
    }

    [Fact]
    public async Task Validate_WithCommentsExactly500Chars_ShouldPass()
    {
        var comment = new string('X', 500);
        var dto     = ValidDto() with { Comments = comment };
        var result  = await _validator.ValidateAsync(dto);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithNullComments_ShouldPass()
    {
        var dto    = ValidDto() with { Comments = null };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.Should().BeTrue();
    }


    [Fact]
    public async Task Validate_WithCompletelyValidDto_ShouldPassAllRules()
    {
        var result = await _validator.ValidateAsync(ValidDto());
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }


    [Fact]
    public async Task Validate_WithMultipleInvalidFields_ShouldReportAllErrors()
    {
        var dto = new CreateGradeDto(Guid.Empty, Guid.Empty, 15m, "bad-period", null);

        var result = await _validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(3); // StudentId, SectionId, Value, Period
    }


    private static CreateGradeDto ValidDto() =>
        new(Guid.NewGuid(), Guid.NewGuid(), 8.5m, "2025-1", null);
}
