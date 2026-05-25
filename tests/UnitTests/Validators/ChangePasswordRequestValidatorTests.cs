using FluentAssertions;
using SistemaEvaluacionAcademica.Application.DTOs.Auth;
using SistemaEvaluacionAcademica.Application.Validators.Auth;

namespace SistemaEvaluacionAcademica.UnitTests.Validators;

public class ChangePasswordRequestValidatorTests
{
    private readonly ChangePasswordRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidRequest_ShouldPassAllRules()
    {
        var result = await _validator.ValidateAsync(new ChangePasswordRequest("CurrentPass1!", "NewPass123!"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyCurrentPassword_ShouldFail()
    {
        var result = await _validator.ValidateAsync(new ChangePasswordRequest("", "NewPass123!"));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CurrentPassword");
    }

    [Theory]
    [InlineData("a")]
    [InlineData("short1A")]   // 7 chars — one below minimum
    [InlineData("")]
    public async Task Validate_WithNewPasswordTooShort_ShouldFail(string newPassword)
    {
        var result = await _validator.ValidateAsync(new ChangePasswordRequest("CurrentPass1!", newPassword));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }

    [Fact]
    public async Task Validate_WithNewPasswordNoUppercase_ShouldFail()
    {
        var result = await _validator.ValidateAsync(new ChangePasswordRequest("CurrentPass1!", "nouppercase1!"));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }

    [Fact]
    public async Task Validate_WithNewPasswordNoDigit_ShouldFail()
    {
        var result = await _validator.ValidateAsync(new ChangePasswordRequest("CurrentPass1!", "NoDigitsHere!"));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }

    [Theory]
    [InlineData("NewPass1!")]     // exactly 8, meets all rules
    [InlineData("ValidPass123!")] // full complexity
    public async Task Validate_WithValidNewPassword_ShouldPass(string newPassword)
    {
        var result = await _validator.ValidateAsync(new ChangePasswordRequest("CurrentPass1!", newPassword));
        result.IsValid.Should().BeTrue();
    }
}
