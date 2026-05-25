using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SistemaEvaluacionAcademica.Infrastructure.Data;

namespace SistemaEvaluacionAcademica.IntegrationTests.Fixtures;

/// <summary>
/// Clase base para todos los tests de integración.
/// Provee HttpClient autenticado y helpers para acceder a la BD del contenedor.
/// </summary>
[Collection("Integration")]
public abstract class IntegrationTestBase
{
    private const string JwtSecret   = CustomWebApplicationFactory.TestJwtSecret;
    private const string JwtIssuer   = "SistemaEvaluacionAcademica";
    private const string JwtAudience = "SistemaEvaluacionAcademica.Clients";

    protected readonly CustomWebApplicationFactory Factory;

    // Cliente anónimo (sin Authorization header)
    protected readonly HttpClient Client;

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client  = factory.CreateClient();
    }

    // ── Clientes autenticados ────────────────────────────────────────────────

    /// <summary>Crea un HttpClient con token JWT del rol indicado.</summary>
    protected HttpClient CreateAuthenticatedClient(string role, Guid? userId = null)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(role, userId));
        return client;
    }

    // ── Generación de JWT ────────────────────────────────────────────────────

    /// <summary>
    /// Genera un JWT válido usando la misma clave/issuer/audience que el API.
    /// No requiere llamar a /api/auth/login — evita dependencias circulares en tests.
    /// </summary>
    protected string GenerateToken(string role, Guid? userId = null, string email = "test@academia.com")
    {
        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new Claim[]
        {
            new(ClaimTypes.NameIdentifier, (userId ?? Guid.NewGuid()).ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, "Test User"),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer:             JwtIssuer,
            audience:           JwtAudience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ── Acceso a la BD del contenedor ────────────────────────────────────────

    /// <summary>
    /// Ejecuta una acción sobre AppDbContext dentro de un scope de DI.
    /// Útil para preparar datos o verificar estado de la BD en los tests.
    /// </summary>
    protected async Task<TResult> ExecuteInScopeAsync<TResult>(
        Func<AppDbContext, Task<TResult>> action)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(db);
    }

    /// <summary>
    /// Obtiene el ID del primer estudiante sembrado (Juan Pérez — EST-2025-A001).
    /// Filtra por prefijo "EST-" para que los estudiantes creados por tests de
    /// concurrencia (CLR-, SCR-) no desplacen al estudiante sembrado esperado.
    /// </summary>
    protected Task<Guid> GetFirstSeededStudentIdAsync() =>
        ExecuteInScopeAsync(async db =>
            (await db.Students
                .Where(s => s.StudentCode.StartsWith("EST-"))
                .OrderBy(s => s.StudentCode)
                .FirstAsync()).Id);

    /// <summary>
    /// Obtiene el ID de la materia por su código.
    /// </summary>
    protected Task<Guid> GetSubjectIdByCodeAsync(string code) =>
        ExecuteInScopeAsync(async db =>
            (await db.Subjects.FirstAsync(s => s.Code == code)).Id);

    /// <summary>
    /// Obtiene el ID de la sección sembrada (SectionCode == "A") de una materia por su código.
    /// Todos los datos sembrados usan código "A". Los tests crean secciones con prefijos únicos
    /// (SCR-TST-A, CLR-TST-A, EN-TST-A) que nunca coinciden con el código sembrado.
    /// </summary>
    protected Task<Guid> GetSectionIdBySubjectCodeAsync(string subjectCode) =>
        ExecuteInScopeAsync(async db =>
        {
            var subject = await db.Subjects.FirstAsync(s => s.Code == subjectCode);
            var section = await db.SubjectSections
                .FirstAsync(s => s.SubjectId == subject.Id && s.IsActive && s.SectionCode == "A");
            return section.Id;
        });

    /// <summary>
    /// Obtiene el ID de la sección en la que el estudiante está actualmente inscrito para la materia dada.
    /// Más robusto que <see cref="GetSectionIdBySubjectCodeAsync"/> cuando los tests crean secciones adicionales.
    /// </summary>
    protected Task<Guid> GetEnrolledSectionIdAsync(Guid studentId, string subjectCode) =>
        ExecuteInScopeAsync(async db =>
        {
            var subject = await db.Subjects.FirstAsync(s => s.Code == subjectCode);
            var enrollment = await db.SectionEnrollments
                .Where(e => e.StudentId == studentId && e.Section.SubjectId == subject.Id && e.IsActive)
                .OrderBy(e => e.CreatedAt)
                .FirstAsync();
            return enrollment.SectionId;
        });
}
