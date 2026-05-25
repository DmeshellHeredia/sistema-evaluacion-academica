using FluentAssertions;
using SistemaEvaluacionAcademica.Infrastructure.Settings;

namespace SistemaEvaluacionAcademica.UnitTests.Configuration;

public class JwtSettingsValidatorTests
{
    private const string ValidKey = "ClaveSecretaValidaParaTests_32Chars!!";


    [Fact]
    public void Validate_WhenKeyIsNull_ShouldThrow()
    {
        var act = () => JwtSettingsValidator.Validate(null, isProduction: false);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*JwtSettings:SecretKey*");
    }

    [Fact]
    public void Validate_WhenKeyIsEmpty_ShouldThrow()
    {
        var act = () => JwtSettingsValidator.Validate(string.Empty, isProduction: false);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Validate_WhenKeyIsWhitespace_ShouldThrow()
    {
        var act = () => JwtSettingsValidator.Validate("   ", isProduction: false);
        act.Should().Throw<InvalidOperationException>();
    }


    [Fact]
    public void Validate_WhenKeyIsTooShort_ShouldThrow()
    {
        var act = () => JwtSettingsValidator.Validate("short_key", isProduction: false);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*al menos 32 caracteres*");
    }

    [Theory]
    [InlineData("ExactlyThirtyTwoCharactersLong!!")]  // 32 chars
    [InlineData("MoreThanThirtyTwoCharactersLong!!!")]  // >32 chars
    public void Validate_WhenKeyMeetsMinimumLength_ShouldNotThrow(string key)
    {
        var act = () => JwtSettingsValidator.Validate(key, isProduction: false);
        act.Should().NotThrow();
    }


    [Theory]
    [InlineData("CHANGE_ME_USE_ENVIRONMENT_VARIABLE")]
    [InlineData("change_me_use_environment_variable")]  // case-insensitive
    public void Validate_WithLongPlaceholder_InProduction_ShouldThrow(string placeholder)
    {
        // These are ≥32 chars so length check passes; placeholder check fires in production
        var act = () => JwtSettingsValidator.Validate(placeholder, isProduction: true);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*placeholder inseguro*");
    }

    [Fact]
    public void Validate_WithLongPlaceholder_OutsideProduction_ShouldNotThrow()
    {
        // "CHANGE_ME_USE_ENVIRONMENT_VARIABLE" = 34 chars, non-production skips placeholder check
        var act = () => JwtSettingsValidator.Validate("CHANGE_ME_USE_ENVIRONMENT_VARIABLE", isProduction: false);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithShortPlaceholder_OutsideProduction_ShouldThrowLengthError()
    {
        // "CHANGE_ME" = 9 chars — fails length check regardless of environment
        var act = () => JwtSettingsValidator.Validate("CHANGE_ME", isProduction: false);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*al menos 32 caracteres*");
    }


    [Fact]
    public void Validate_WithValidKey_InProduction_ShouldNotThrow()
    {
        var act = () => JwtSettingsValidator.Validate(ValidKey, isProduction: true);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithValidKey_InDevelopment_ShouldNotThrow()
    {
        var act = () => JwtSettingsValidator.Validate(ValidKey, isProduction: false);
        act.Should().NotThrow();
    }
}
