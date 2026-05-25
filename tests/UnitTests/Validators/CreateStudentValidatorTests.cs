using FluentAssertions;
using SistemaEvaluacionAcademica.Application.DTOs.Students;
using SistemaEvaluacionAcademica.Application.Validators.Students;

namespace SistemaEvaluacionAcademica.UnitTests.Validators;

public class CreateStudentValidatorTests
{
    private readonly CreateStudentValidator _validator = new();

    private static CreateStudentDto ValidDto() =>
        new("Pass123!", "Juan", "Pérez", "Ingeniería en Sistemas", 3);

    [Fact]
    public async Task Validate_WithValidDto_ShouldPass()
    {
        var result = await _validator.ValidateAsync(ValidDto());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyPassword_ShouldFail()
    {
        var result = await _validator.ValidateAsync(ValidDto() with { Password = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Validate_WithShortPassword_ShouldFail()
    {
        var result = await _validator.ValidateAsync(ValidDto() with { Password = "short" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Validate_WithEmptyFirstName_ShouldFail()
    {
        var result = await _validator.ValidateAsync(ValidDto() with { FirstName = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FirstName");
    }

    [Fact]
    public async Task Validate_WithEmptyLastName_ShouldFail()
    {
        var result = await _validator.ValidateAsync(ValidDto() with { LastName = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LastName");
    }

    [Fact]
    public async Task Validate_WithEmptyCareer_ShouldFail()
    {
        var result = await _validator.ValidateAsync(ValidDto() with { Career = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Career");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(-1)]
    public async Task Validate_WithSemesterOutOfRange_ShouldFail(int semester)
    {
        var result = await _validator.ValidateAsync(ValidDto() with { Semester = semester });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Semester");
    }

    [Theory]
    [InlineData("Ingeniería en Sistemas")]
    [InlineData("Ciberseguridad")]
    [InlineData("Desarrollo de Software")]
    public async Task Validate_WithValidCareer_ShouldPass(string career)
    {
        var result = await _validator.ValidateAsync(ValidDto() with { Career = career });
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Medicina")]
    [InlineData("Administración")]
    public async Task Validate_WithInvalidCareer_ShouldFail(string career)
    {
        var result = await _validator.ValidateAsync(ValidDto() with { Career = career });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Career");
    }
}
