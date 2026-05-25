using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Options;
using SistemaEvaluacionAcademica.Domain.Enums;
using SistemaEvaluacionAcademica.Infrastructure.Services;
using SistemaEvaluacionAcademica.Infrastructure.Settings;
using SistemaEvaluacionAcademica.UnitTests.Helpers;

namespace SistemaEvaluacionAcademica.UnitTests.Services;

/// <summary>
/// Tests unitarios de JwtService (Infrastructure layer).
/// No requiere BD ni mocks — prueba la criptografía JWT directamente.
/// </summary>
public class JwtServiceTests
{
    private static readonly JwtSettings TestSettings = new()
    {
        // Clave de 512 bits para cumplir el mínimo de HmacSha256
        SecretKey         = "super-secret-key-for-testing-only-must-be-long-enough-512bits!",
        Issuer            = "https://academia.test",
        Audience          = "academia-client-test",
        ExpirationMinutes = 60
    };

    private readonly JwtService _sut = new(Options.Create(TestSettings));


    [Fact]
    public void GenerateToken_ShouldReturnNonEmptyString()
    {
        var user  = TestDataBuilder.CreateAdminUser();
        var token = _sut.GenerateToken(user);

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateToken_ShouldReturnValidJwtFormat()
    {
        var user  = TestDataBuilder.CreateAdminUser();
        var token = _sut.GenerateToken(user);

        // JWT = header.payload.signature (tres secciones separadas por '.')
        token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void GenerateToken_ShouldIncludeUserIdInClaims()
    {
        var user      = TestDataBuilder.CreateAdminUser();
        var token     = _sut.GenerateToken(user);
        var principal = ParseToken(token);

        var nameIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? principal.FindFirst("nameid")?.Value;

        nameIdClaim.Should().Be(user.Id.ToString());
    }

    [Fact]
    public void GenerateToken_ShouldIncludeEmailClaim()
    {
        var user      = TestDataBuilder.CreateAdminUser();
        var token     = _sut.GenerateToken(user);
        var principal = ParseToken(token);

        var emailClaim = principal.FindFirst(ClaimTypes.Email)?.Value
                      ?? principal.FindFirst("email")?.Value;

        emailClaim.Should().Be(user.Email);
    }

    [Fact]
    public void GenerateToken_ShouldIncludeRoleNameClaim()
    {
        var user      = TestDataBuilder.CreateProfesorUser();
        var token     = _sut.GenerateToken(user);
        var principal = ParseToken(token);

        var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value
                     ?? principal.FindFirst("role")?.Value;

        roleClaim.Should().Be("Profesor");
    }

    [Fact]
    public void GenerateToken_ShouldIncludeRoleTypeCustomClaim()
    {
        var user      = TestDataBuilder.CreateEstudianteUser();
        var token     = _sut.GenerateToken(user);
        var principal = ParseToken(token);

        var roleTypeClaim = principal.FindFirst("roleType")?.Value;

        roleTypeClaim.Should().Be(RoleType.Estudiante.ToString());
    }

    [Fact]
    public void GenerateToken_ShouldIncludeFullNameClaim()
    {
        var user      = TestDataBuilder.CreateAdminUser(firstName: "Juan", lastName: "Pérez");
        var token     = _sut.GenerateToken(user);
        var principal = ParseToken(token);

        var nameClaim = principal.FindFirst(ClaimTypes.Name)?.Value
                     ?? principal.FindFirst("name")?.Value;

        nameClaim.Should().Be("Juan Pérez");
    }

    [Fact]
    public void GenerateToken_ShouldHaveJtiClaim()
    {
        var user  = TestDataBuilder.CreateAdminUser();
        var token = _sut.GenerateToken(user);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        parsed.Id.Should().NotBeNullOrEmpty();
        Guid.TryParse(parsed.Id, out _).Should().BeTrue();
    }

    [Fact]
    public void GenerateToken_TwoCallsForSameUser_ShouldProduceDifferentJti()
    {
        var user   = TestDataBuilder.CreateAdminUser();
        var token1 = _sut.GenerateToken(user);
        var token2 = _sut.GenerateToken(user);

        var jti1 = new JwtSecurityTokenHandler().ReadJwtToken(token1).Id;
        var jti2 = new JwtSecurityTokenHandler().ReadJwtToken(token2).Id;

        jti1.Should().NotBe(jti2);
    }

    [Fact]
    public void GenerateToken_ShouldIncludeSecurityStampClaim()
    {
        var user   = TestDataBuilder.CreateAdminUser();
        var token  = _sut.GenerateToken(user);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var secStampClaim = parsed.Claims.FirstOrDefault(c => c.Type == "sec_stamp");

        secStampClaim.Should().NotBeNull();
        Guid.TryParse(secStampClaim!.Value, out _).Should().BeTrue();
        secStampClaim.Value.Should().Be(user.SecurityStamp.ToString());
    }

    [Fact]
    public void GenerateToken_AfterPasswordChange_ShouldEmbedRotatedStamp()
    {
        // Proves end-to-end: password change → new stamp in entity → new stamp in token.
        // OnTokenValidated rejects the old token because its "sec_stamp" claim no longer
        // matches the value stored in the database.
        var user = TestDataBuilder.CreateAdminUser();

        var tokenBefore     = _sut.GenerateToken(user);
        var stampInBefore   = new JwtSecurityTokenHandler().ReadJwtToken(tokenBefore)
            .Claims.First(c => c.Type == "sec_stamp").Value;

        user.UpdatePassword("newPasswordHash");

        var tokenAfter    = _sut.GenerateToken(user);
        var stampInAfter  = new JwtSecurityTokenHandler().ReadJwtToken(tokenAfter)
            .Claims.First(c => c.Type == "sec_stamp").Value;

        stampInAfter.Should().NotBe(stampInBefore);
        stampInAfter.Should().Be(user.SecurityStamp.ToString());
    }


    [Fact]
    public void ValidateToken_WithValidToken_ShouldReturnClaimsPrincipal()
    {
        var user      = TestDataBuilder.CreateAdminUser();
        var token     = _sut.GenerateToken(user);
        var principal = _sut.ValidateToken(token);

        principal.Should().NotBeNull();
    }

    [Fact]
    public void ValidateToken_WithTamperedSignature_ShouldReturnNull()
    {
        var user  = TestDataBuilder.CreateAdminUser();
        var token = _sut.GenerateToken(user);

        // Altera un carácter en el centro de la firma (no el último, cuyos 2 bits
        // bajos son padding ignorado en base64url para firmas de 32 bytes).
        var sigStart = token.LastIndexOf('.') + 1;
        var midSig   = sigStart + (token.Length - sigStart) / 2;
        var chars    = token.ToCharArray();
        chars[midSig] = chars[midSig] == 'a' ? 'b' : 'a';
        var tampered = new string(chars);

        var principal = _sut.ValidateToken(tampered);
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithExpiredToken_ShouldReturnNull()
    {
        var expiredSettings = new JwtSettings
        {
            SecretKey         = TestSettings.SecretKey,
            Issuer            = TestSettings.Issuer,
            Audience          = TestSettings.Audience,
            ExpirationMinutes = -1   // expirado hace 1 minuto
        };
        var expiredSut = new JwtService(Options.Create(expiredSettings));

        var user  = TestDataBuilder.CreateAdminUser();
        var token = expiredSut.GenerateToken(user);

        var principal = _sut.ValidateToken(token);
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithWrongIssuer_ShouldReturnNull()
    {
        // Token generado con issuer diferente
        var wrongIssuerSettings = new JwtSettings
        {
            SecretKey         = TestSettings.SecretKey,
            Issuer            = "https://attacker.com",
            Audience          = TestSettings.Audience,
            ExpirationMinutes = 60
        };
        var wrongSut = new JwtService(Options.Create(wrongIssuerSettings));

        var user  = TestDataBuilder.CreateAdminUser();
        var token = wrongSut.GenerateToken(user);

        // Validar con el servicio correcto (issuer no coincide)
        var principal = _sut.ValidateToken(token);
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithWrongAudience_ShouldReturnNull()
    {
        var wrongAudienceSettings = new JwtSettings
        {
            SecretKey         = TestSettings.SecretKey,
            Issuer            = TestSettings.Issuer,
            Audience          = "wrong-audience",
            ExpirationMinutes = 60
        };
        var wrongSut = new JwtService(Options.Create(wrongAudienceSettings));

        var user  = TestDataBuilder.CreateAdminUser();
        var token = wrongSut.GenerateToken(user);

        var principal = _sut.ValidateToken(token);
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithCompletelyInvalidString_ShouldReturnNull()
    {
        _sut.ValidateToken("not.a.jwt").Should().BeNull();
        _sut.ValidateToken("").Should().BeNull();
        _sut.ValidateToken("random-garbage").Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithGarbageInput_DoesNotThrow()
    {
        // Verifica que la capa de excepción captura solo SecurityTokenException/ArgumentException
        // y no relanza excepciones inesperadas que causarían 500.
        var act = () => _sut.ValidateToken("completamente.invalido.xxx");
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateToken_WithEmptyString_DoesNotThrow()
    {
        var act = () => _sut.ValidateToken("");
        act.Should().NotThrow();
    }


    [Fact]
    public void GetTokenExpiration_ShouldReturnFutureDateTime()
    {
        var expiration = _sut.GetTokenExpiration();
        expiration.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void GetTokenExpiration_ShouldRespectExpirationMinutesSetting()
    {
        var expiration = _sut.GetTokenExpiration();
        expiration.Should().BeCloseTo(
            DateTime.UtcNow.AddMinutes(TestSettings.ExpirationMinutes),
            TimeSpan.FromSeconds(10));
    }


    private static ClaimsPrincipal ParseToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt     = handler.ReadJwtToken(token);

        var identity  = new ClaimsIdentity(jwt.Claims, "Bearer");
        return new ClaimsPrincipal(identity);
    }
}
