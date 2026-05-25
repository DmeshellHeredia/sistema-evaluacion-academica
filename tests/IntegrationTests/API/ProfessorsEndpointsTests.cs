using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SistemaEvaluacionAcademica.Application.DTOs.Professors;
using SistemaEvaluacionAcademica.IntegrationTests.Fixtures;

namespace SistemaEvaluacionAcademica.IntegrationTests.API;

[Trait("Category", "Integration")]
public class ProfessorsEndpointsTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ProfessorsEndpointsTests(CustomWebApplicationFactory factory) : base(factory) { }

    // ── GET /api/professors — auth ───────────────────────────────────────────

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync("/api/professors");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_AsEstudiante_Returns403()
    {
        var client   = CreateAuthenticatedClient("Estudiante");
        var response = await client.GetAsync("/api/professors");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_AsProfesor_Returns403()
    {
        var client   = CreateAuthenticatedClient("Profesor");
        var response = await client.GetAsync("/api/professors");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET /api/professors — pagination ─────────────────────────────────────

    [Fact]
    public async Task GetAll_AsAdmin_Returns200WithPagedResult()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync("/api/professors?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.TryGetProperty("items",      out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("totalCount", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("page",       out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("pageSize",   out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("totalPages", out _).Should().BeTrue();

        // Seeder inserts 3 professors (other tests may add more)
        doc.RootElement.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(3);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task GetAll_Page1WithPageSize2_Returns2Items()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync("/api/professors?page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.GetProperty("items").GetArrayLength().Should().Be(2);
        doc.RootElement.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(3);
        doc.RootElement.GetProperty("totalPages").GetInt32().Should().BeGreaterThanOrEqualTo(2);
        doc.RootElement.GetProperty("page").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetAll_Page2WithPageSize1_Returns1Item()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync("/api/professors?page=2&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        // pageSize=1 always yields exactly 1 item on page 2 (>= 3 professors seeded)
        doc.RootElement.GetProperty("items").GetArrayLength().Should().Be(1);
        doc.RootElement.GetProperty("page").GetInt32().Should().Be(2);
    }

    // ── GET /api/professors — search ─────────────────────────────────────────

    [Fact]
    public async Task GetAll_WithSearchByLastName_ReturnsMatchingProfessors()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync("/api/professors?search=garcia");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        var items = doc.RootElement.GetProperty("items");
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("email").GetString().Should().Contain("garcia");
    }

    [Fact]
    public async Task GetAll_WithSearchByEmail_ReturnsMatchingProfessors()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync("/api/professors?search=lopez");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.GetProperty("totalCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetAll_WithSearchNoMatch_ReturnsEmptyItems()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync("/api/professors?search=xqznotexists99999");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.GetProperty("items").GetArrayLength().Should().Be(0);
        doc.RootElement.GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    // ── Pagination edge cases — page/pageSize clamping ───────────────────────

    [Fact]
    public async Task GetAll_WithPage0_ClampsToPage1()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync("/api/professors?page=0&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.GetProperty("page").GetInt32().Should().Be(1);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAll_WithPageSizeExceedingMax_ClampsTo100()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync("/api/professors?page=1&pageSize=9999");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.GetProperty("pageSize").GetInt32().Should().Be(100);
    }

    [Fact]
    public async Task GetAll_WithPageSize0_ClampsTo1()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync("/api/professors?page=1&pageSize=0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.GetProperty("pageSize").GetInt32().Should().Be(1);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().Be(1);
    }

    // ── GET /api/professors/{id} ─────────────────────────────────────────────

    [Fact]
    public async Task GetById_WithNonExistentId_Returns404()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync($"/api/professors/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_AsAdmin_WithValidId_Returns200WithSectionCount()
    {
        var professorId = await GetFirstSeededProfessorIdAsync();

        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync($"/api/professors/{professorId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = JsonSerializer.Deserialize<ProfessorDto>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        result.Should().NotBeNull();
        result!.Id.Should().Be(professorId);
        result.SectionCount.Should().BeGreaterThanOrEqualTo(0);
    }

    // ── POST /api/professors ─────────────────────────────────────────────────

    [Fact]
    public async Task Create_AsAdmin_WithValidData_Returns201()
    {
        var adminClient = CreateAuthenticatedClient("Admin");
        var suffix      = Guid.NewGuid().ToString("N")[..8];
        var dto = new CreateProfessorDto(
            $"nuevo.prof{suffix}@academia.com",
            "Password123!",
            $"Nuevo{suffix}",
            "Docente");

        var response = await adminClient.PostAsJsonAsync("/api/professors", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = JsonSerializer.Deserialize<ProfessorDto>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        result.Should().NotBeNull();
        result!.Email.Should().Contain(suffix);
        result.SectionCount.Should().Be(0);
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_Returns400()
    {
        var adminClient = CreateAuthenticatedClient("Admin");
        var dto = new CreateProfessorDto(
            "prof.garcia@academia.com", "Password123!", "Ana", "García");

        var response = await adminClient.PostAsJsonAsync("/api/professors", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        var dto = new CreateProfessorDto("new@academia.com", "Password123!", "Test", "Prof");
        var response = await Client.PostAsJsonAsync("/api/professors", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── PUT /api/professors/{id} ─────────────────────────────────────────────

    [Fact]
    public async Task Update_AsAdmin_WithValidData_Returns200()
    {
        var professorId = await GetFirstSeededProfessorIdAsync();
        var client      = CreateAuthenticatedClient("Admin");
        var dto         = new UpdateProfessorDto("Ana Actualizada", "García Modificada");

        var response = await client.PutAsJsonAsync($"/api/professors/{professorId}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = JsonSerializer.Deserialize<ProfessorDto>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        result!.FirstName.Should().Be("Ana Actualizada");
        result.LastName.Should().Be("García Modificada");
    }

    [Fact]
    public async Task Update_WithNonExistentId_Returns404()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.PutAsJsonAsync(
            $"/api/professors/{Guid.NewGuid()}", new UpdateProfessorDto("X", "Y"));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /api/professors/{id} ──────────────────────────────────────────

    [Fact]
    public async Task Delete_WithNonExistentId_Returns404()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.DeleteAsync($"/api/professors/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_AsAdmin_WithCreatedProfessor_Returns204()
    {
        var adminClient = CreateAuthenticatedClient("Admin");
        var suffix      = Guid.NewGuid().ToString("N")[..8];
        var createDto   = new CreateProfessorDto(
            $"tobedeleted{suffix}@academia.com", "Password123!", "Para", "Borrar");

        var createResponse = await adminClient.PostAsJsonAsync("/api/professors", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = JsonSerializer.Deserialize<ProfessorDto>(
            await createResponse.Content.ReadAsStringAsync(), JsonOpts);

        var deleteResponse = await adminClient.DeleteAsync($"/api/professors/{created!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Task<Guid> GetFirstSeededProfessorIdAsync() =>
        ExecuteInScopeAsync(async db =>
        {
            var role = await db.Roles.FirstAsync(r => r.Name == "Profesor");
            var user = await db.Users
                .Where(u => u.RoleId == role.Id && u.IsActive)
                .OrderBy(u => u.LastName)
                .FirstAsync();
            return user.Id;
        });
}
