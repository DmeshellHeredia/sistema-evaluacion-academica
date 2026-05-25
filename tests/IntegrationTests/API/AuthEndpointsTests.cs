using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SistemaEvaluacionAcademica.Application.DTOs.Auth;
using SistemaEvaluacionAcademica.Domain.Enums;
using SistemaEvaluacionAcademica.IntegrationTests.Fixtures;

namespace SistemaEvaluacionAcademica.IntegrationTests.API;

[Trait("Category", "Integration")]
public class AuthEndpointsTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthEndpointsTests(CustomWebApplicationFactory factory) : base(factory) { }

    // ── POST /api/auth/login ─────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithValidAdminCredentials_Returns200WithToken()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("admin@academia.com", "Admin123!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = JsonSerializer.Deserialize<LoginResponse>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrWhiteSpace();
        result.Token.Split('.').Should().HaveCount(3); // formato JWT válido
        result.Email.Should().Be("admin@academia.com");
        result.Role.Should().Be("Admin");
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WithValidProfesorCredentials_Returns200()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("prof.garcia@academia.com", "Profesor123!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = JsonSerializer.Deserialize<LoginResponse>(
            await response.Content.ReadAsStringAsync(), JsonOpts);
        result!.Role.Should().Be("Profesor");
    }

    [Fact]
    public async Task Login_WithValidEstudianteCredentials_Returns200()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("juan.perez@academia.com", "Estudiante123!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = JsonSerializer.Deserialize<LoginResponse>(
            await response.Content.ReadAsStringAsync(), JsonOpts);
        result!.Role.Should().Be("Estudiante");
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("admin@academia.com", "WrongPassword!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("nobody@academia.com", "AnyPassword!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithEmptyEmail_Returns422()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("", "ValidPass123!"));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Login_WithEmptyPassword_Returns422()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("valid@email.com", ""));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Theory]
    [InlineData("notanemail", "ValidPass123!")]
    [InlineData("@missinglocal.com", "ValidPass123!")]
    public async Task Login_WithInvalidEmailFormat_Returns422(string email, string password)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, password));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── POST /api/auth/register ──────────────────────────────────────────────

    [Fact]
    public async Task Register_WithoutToken_Returns401()
    {
        var payload  = new RegisterRequest("new@test.com", "Pass123!X", "New", "User", RoleType.Admin, null, null);
        var response = await Client.PostAsJsonAsync("/api/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_AsProfesor_Returns403()
    {
        var client   = CreateAuthenticatedClient("Profesor");
        var payload  = new RegisterRequest("new@test.com", "Pass123!X", "New", "User", RoleType.Admin, null, null);
        var response = await client.PostAsJsonAsync("/api/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Register_AsEstudiante_Returns403()
    {
        var client   = CreateAuthenticatedClient("Estudiante");
        var payload  = new RegisterRequest("new@test.com", "Pass123!X", "New", "User", RoleType.Admin, null, null);
        var response = await client.PostAsJsonAsync("/api/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Register_AsAdmin_WithValidProfesorRequest_Returns201()
    {
        var client      = CreateAuthenticatedClient("Admin");
        var uniqueEmail = $"prof-{Guid.NewGuid()}@test.com";
        var payload     = new RegisterRequest(uniqueEmail, "Valid123!X", "Nuevo", "Profesor", RoleType.Profesor, null, null);

        var response = await client.PostAsJsonAsync("/api/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("userId");
    }

    [Fact]
    public async Task Register_AsAdmin_WithValidEstudianteRequest_Returns201()
    {
        // El email debe derivarse del nombre: nuevo.estudiante@academia.com
        var client  = CreateAuthenticatedClient("Admin");
        var payload = new RegisterRequest(
            "nuevo.estudiante@academia.com", "Valid123!X", "Nuevo", "Estudiante",
            RoleType.Estudiante, Career: "Ingeniería en Sistemas", Semester: 1);

        var response = await client.PostAsJsonAsync("/api/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_AsAdmin_WithDuplicateEmail_Returns400()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var payload  = new RegisterRequest(
            "admin@academia.com", "Valid123!X", "Test", "User", RoleType.Admin, null, null);
        var response = await client.PostAsJsonAsync("/api/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.ToLower().Should().Contain("correo");
    }

    [Fact]
    public async Task Register_AsAdmin_WithWeakPassword_Returns422()
    {
        var client  = CreateAuthenticatedClient("Admin");
        var payload = new RegisterRequest(
            $"test-{Guid.NewGuid()}@test.com", "weak", "Test", "User", RoleType.Admin, null, null);
        var response = await client.PostAsJsonAsync("/api/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Register_AsAdmin_EstudianteWithoutCareer_Returns422()
    {
        var client  = CreateAuthenticatedClient("Admin");
        var payload = new RegisterRequest(
            $"test-{Guid.NewGuid()}@test.com", "Valid123!X", "Test", "User",
            RoleType.Estudiante, Career: null, Semester: null);
        var response = await client.PostAsJsonAsync("/api/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── PUT /api/auth/change-password ────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_WithoutToken_Returns401()
    {
        var response = await Client.PutAsJsonAsync("/api/auth/change-password",
            new { currentPassword = "Admin123!", newPassword = "NewPass456!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_Returns400()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.PutAsJsonAsync("/api/auth/change-password",
            new { currentPassword = "WrongPassword!", newPassword = "NewPass456!" });

        // El endpoint retorna 400 cuando la contraseña actual es incorrecta
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── NameIdentifier inválido → 401 (no 500) ──────────────────────────────

    [Fact]
    public async Task ChangePassword_WithInvalidGuidInNameIdentifier_Returns401()
    {
        // Token firmado correctamente pero con sub = "not-a-guid"
        var token = GenerateTokenWithRawSub("not-a-guid", "Admin");
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.PutAsJsonAsync("/api/auth/change-password",
            new { currentPassword = "any", newPassword = "any" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── pageSize bounds ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(1, 101)]
    public async Task Students_GetAll_WithInvalidPageParams_Returns400(int page, int pageSize)
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync($"/api/students?page={page}&pageSize={pageSize}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 101)]
    public async Task Professors_GetAll_WithInvalidPageParams_Returns400(int page, int pageSize)
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync($"/api/professors?page={page}&pageSize={pageSize}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 101)]
    public async Task Sections_GetAll_WithInvalidPageParams_Returns400(int page, int pageSize)
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync($"/api/sections?page={page}&pageSize={pageSize}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 101)]
    public async Task Subjects_GetAll_WithInvalidPageParams_Returns400(int page, int pageSize)
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync($"/api/subjects?page={page}&pageSize={pageSize}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Helper: token con sub arbitrario ────────────────────────────────────

    private string GenerateTokenWithRawSub(string rawSub, string role)
    {
        var key         = new System.Text.StringBuilder();
        var keyBytes    = System.Text.Encoding.UTF8.GetBytes(CustomWebApplicationFactory.TestJwtSecret);
        var signingKey  = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes);
        var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            signingKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var claims = new System.Security.Claims.Claim[]
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, rawSub),
            new(System.Security.Claims.ClaimTypes.Role, role),
            new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer:             "SistemaEvaluacionAcademica",
            audience:           "SistemaEvaluacionAcademica.Clients",
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}
