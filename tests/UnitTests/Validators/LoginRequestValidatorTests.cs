using FluentAssertions;
using SistemaEvaluacionAcademica.Application.DTOs.Auth;
using SistemaEvaluacionAcademica.Application.Validators.Auth;

namespace SistemaEvaluacionAcademica.UnitTests.Validators;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidRequest_ShouldPassAllRules()
    {
        var result = await _validator.ValidateAsync(ValidRequest());
        result.IsValid.Should().BeTrue();
    }


    [Fact]
    public async Task Validate_WithEmptyEmail_ShouldFailOnEmail()
    {
        var result = await _validator.ValidateAsync(ValidRequest() with { Email = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@missinglocal.com")]
    [InlineData("missingdomain@")]
    [InlineData("plainaddress")]
    public async Task Validate_WithInvalidEmailFormat_ShouldFailOnEmail(string email)
    {
        var result = await _validator.ValidateAsync(ValidRequest() with { Email = email });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }


    [Fact]
    public async Task Validate_WithEmptyPassword_ShouldFailOnPassword()
    {
        var result = await _validator.ValidateAsync(ValidRequest() with { Password = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Theory]
    [InlineData("12345")]   // 5 chars (min is 6)
    [InlineData("abc")]
    public async Task Validate_WithPasswordTooShort_ShouldFailOnPassword(string password)
    {
        var result = await _validator.ValidateAsync(ValidRequest() with { Password = password });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Theory]
    [InlineData("123456")]        // exactly 6
    [InlineData("ValidPass123!")] // full complexity
    public async Task Validate_WithValidPassword_ShouldPass(string password)
    {
        var result = await _validator.ValidateAsync(ValidRequest() with { Password = password });
        result.IsValid.Should().BeTrue();
    }


    private static LoginRequest ValidRequest() =>
        new("admin@academia.com", "Admin123!");
}
