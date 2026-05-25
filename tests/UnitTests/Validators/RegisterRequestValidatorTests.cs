using FluentAssertions;
using SistemaEvaluacionAcademica.Application.DTOs.Auth;
using SistemaEvaluacionAcademica.Application.Validators.Auth;
using SistemaEvaluacionAcademica.Domain.Enums;

namespace SistemaEvaluacionAcademica.UnitTests.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();


    [Fact]
    public async Task Validate_WithValidAdminRequest_ShouldPass()
    {
        var result = await _validator.ValidateAsync(ValidAdminRequest());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithValidEstudianteRequest_ShouldPass()
    {
        var result = await _validator.ValidateAsync(ValidEstudianteRequest());
        result.IsValid.Should().BeTrue();
    }


    [Theory]
    [InlineData("nouppercase1")]   // sin mayúscula
    [InlineData("NODIGITSHERE")]   // sin dígito (solo mayúsculas)
    [InlineData("NoDigitsHere!")]  // sin dígito
    [InlineData("Short1!")]        // menos de 8 caracteres
    public async Task Validate_WithWeakPassword_ShouldFailOnPassword(string password)
    {
        var request = ValidAdminRequest() with { Password = password };
        var result  = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Validate_WithEmptyPassword_ShouldFailOnPassword()
    {
        var result = await _validator.ValidateAsync(ValidAdminRequest() with { Password = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }


    [Fact]
    public async Task Validate_WithEmptyFirstName_ShouldFailOnFirstName()
    {
        var result = await _validator.ValidateAsync(ValidAdminRequest() with { FirstName = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FirstName");
    }

    [Fact]
    public async Task Validate_WithEmptyLastName_ShouldFailOnLastName()
    {
        var result = await _validator.ValidateAsync(ValidAdminRequest() with { LastName = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LastName");
    }

    [Fact]
    public async Task Validate_WithFirstNameTooLong_ShouldFailOnFirstName()
    {
        var longName = new string('A', 101);
        var result   = await _validator.ValidateAsync(ValidAdminRequest() with { FirstName = longName });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FirstName");
    }


    [Fact]
    public async Task Validate_EstudianteWithCorrectDerivedEmail_ShouldPass()
    {
        // "Luis"/"Torres" → "luis.torres@academia.com"
        var request = ValidEstudianteRequest();
        var result  = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EstudianteWithManualEmail_ShouldFailOnEmail()
    {
        var request = ValidEstudianteRequest() with { Email = "custom@test.com" };
        var result  = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Validate_EstudianteWithDerivedEmailFromAccentedName_ShouldPass()
    {
        // "María"/"Gómez" → "maria.gomez@academia.com"
        var request = new RegisterRequest(
            "maria.gomez@academia.com", "Maria123!", "María", "Gómez",
            RoleType.Estudiante, "Ingeniería en Sistemas", 2);
        var result = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeTrue();
    }


    [Fact]
    public async Task Validate_EstudianteWithoutCareer_ShouldFailOnCareer()
    {
        var request = ValidEstudianteRequest() with { Career = null };
        var result  = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Career");
    }

    [Fact]
    public async Task Validate_EstudianteWithEmptyCareer_ShouldFailOnCareer()
    {
        var request = ValidEstudianteRequest() with { Career = "" };
        var result  = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Career");
    }

    [Fact]
    public async Task Validate_EstudianteWithoutSemester_ShouldFailOnSemester()
    {
        var request = ValidEstudianteRequest() with { Semester = null };
        var result  = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Semester");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(-1)]
    public async Task Validate_EstudianteWithSemesterOutOfRange_ShouldFailOnSemester(int semester)
    {
        var request = ValidEstudianteRequest() with { Semester = semester };
        var result  = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Semester");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(8)]
    public async Task Validate_EstudianteWithValidSemester_ShouldPass(int semester)
    {
        var request = ValidEstudianteRequest() with { Semester = semester };
        var result  = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeTrue();
    }


    [Theory]
    [InlineData("Ingeniería en Sistemas")]
    [InlineData("Ciberseguridad")]
    [InlineData("Desarrollo de Software")]
    public async Task Validate_EstudianteWithValidCareer_ShouldPass(string career)
    {
        var request = ValidEstudianteRequest() with { Career = career };
        var result  = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Medicina")]
    [InlineData("Administración")]
    [InlineData("sistemas")]
    public async Task Validate_EstudianteWithInvalidCareer_ShouldFailOnCareer(string career)
    {
        var request = ValidEstudianteRequest() with { Career = career };
        var result  = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Career");
    }


    [Fact]
    public async Task Validate_AdminWithoutCareer_ShouldPassBecauseCareerNotRequired()
    {
        var request = ValidAdminRequest() with { Career = null, Semester = null };
        var result  = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ProfesorWithoutCareer_ShouldPass()
    {
        var request = new RegisterRequest(
            "prof@test.com", "Prof123!", "Carlos", "García",
            RoleType.Profesor, null, null);
        var result = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeTrue();
    }


    private static RegisterRequest ValidAdminRequest() =>
        new("admin@academia.com", "Admin123!", "Ana", "Soto", RoleType.Admin, null, null);

    private static RegisterRequest ValidEstudianteRequest() =>
        new("luis.torres@academia.com", "Est12345!", "Luis", "Torres", RoleType.Estudiante, "Ingeniería en Sistemas", 3);
}
