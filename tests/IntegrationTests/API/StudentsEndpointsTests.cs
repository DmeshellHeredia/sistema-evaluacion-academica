using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SistemaEvaluacionAcademica.Application.DTOs.Grades;
using SistemaEvaluacionAcademica.Application.DTOs.Students;
using SistemaEvaluacionAcademica.IntegrationTests.Fixtures;

namespace SistemaEvaluacionAcademica.IntegrationTests.API;

[Trait("Category", "Integration")]
public class StudentsEndpointsTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public StudentsEndpointsTests(CustomWebApplicationFactory factory) : base(factory) { }

    // ── GET /api/students ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync("/api/students");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_AsEstudiante_Returns403()
    {
        var client   = CreateAuthenticatedClient("Estudiante");
        var response = await client.GetAsync("/api/students");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_AsAdmin_Returns200WithPagedList()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync("/api/students?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.TryGetProperty("items", out var items).Should().BeTrue();
        doc.RootElement.TryGetProperty("totalCount", out _).Should().BeTrue();
        items.GetArrayLength().Should().BeGreaterThan(0); // 3 estudiantes sembrados
    }

    [Fact]
    public async Task GetAll_AsProfesor_Returns403()
    {
        var client   = CreateAuthenticatedClient("Profesor");
        var response = await client.GetAsync("/api/students");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_SecondPage_Returns200WithCorrectPagination()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync("/api/students?page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.TryGetProperty("items", out var items).Should().BeTrue();
        items.GetArrayLength().Should().Be(2); // pageSize=2 → exactamente 2 items
        doc.RootElement.GetProperty("totalPages").GetInt32().Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetAll_WithSearchByName_ReturnsMatchingStudents()
    {
        var client   = CreateAuthenticatedClient("Admin");
        // "juan" matches the seeded student juan.perez@academia.com
        var response = await client.GetAsync("/api/students?search=juan");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        var items = doc.RootElement.GetProperty("items");
        items.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAll_WithSearch_FindsStudentOutsideFirstPage()
    {
        var adminClient = CreateAuthenticatedClient("Admin");

        // Use unique first name so derived email won't conflict between test runs
        var suffix    = Guid.NewGuid().ToString("N")[..8]; // 8 lowercase hex chars
        var firstName = $"Busca{suffix}";
        var dto = new CreateStudentDto(
            "Valid123!X", firstName, "Alumno", "Ingeniería en Sistemas", 1);

        var createRes = await adminClient.PostAsJsonAsync("/api/students", dto);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var searchRes = await adminClient.GetAsync($"/api/students?page=1&pageSize=1&search={firstName}");

        searchRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await searchRes.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.GetProperty("totalCount").GetInt32().Should().Be(1);
        doc.RootElement.GetProperty("items")[0]
            .GetProperty("career").GetString().Should().Be("Ingeniería en Sistemas");
    }

    [Fact]
    public async Task GetAll_WithSearch_StudentOnPage2IsFoundOnPage1OfResults()
    {
        var adminClient = CreateAuthenticatedClient("Admin");

        // Verify there are 2+ students so a second page exists with pageSize=1
        var baseRes = await adminClient.GetAsync("/api/students?page=1&pageSize=1");
        baseRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var baseBody = await baseRes.Content.ReadAsStringAsync();
        using var baseDoc = JsonDocument.Parse(baseBody);
        baseDoc.RootElement.GetProperty("totalPages").GetInt32().Should().BeGreaterThan(1,
            because: "seeder inserts 3 students, so pageSize=1 yields >1 pages");

        // Get the student code of whoever lives on page 2
        var page2Res  = await adminClient.GetAsync("/api/students?page=2&pageSize=1");
        var page2Body = await page2Res.Content.ReadAsStringAsync();
        using var page2Doc = JsonDocument.Parse(page2Body);
        var studentCode = page2Doc.RootElement.GetProperty("items")[0]
            .GetProperty("studentCode").GetString()!;

        // Search by that code with page=1 — server must search across all students
        var searchRes = await adminClient.GetAsync(
            $"/api/students?page=1&pageSize=1&search={studentCode}");

        searchRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchBody = await searchRes.Content.ReadAsStringAsync();
        using var searchDoc = JsonDocument.Parse(searchBody);

        searchDoc.RootElement.GetProperty("totalCount").GetInt32().Should().Be(1);
        searchDoc.RootElement.GetProperty("items")[0]
            .GetProperty("studentCode").GetString().Should().Be(studentCode);
    }

    [Fact]
    public async Task GetAll_WithSearchNoMatch_ReturnsEmptyItems()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync("/api/students?search=xqznotexists99999");

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
        var client = CreateAuthenticatedClient("Admin");

        var page0  = await client.GetAsync("/api/students?page=0&pageSize=10");
        var page1  = await client.GetAsync("/api/students?page=1&pageSize=10");

        page0.StatusCode.Should().Be(HttpStatusCode.OK);

        var body0 = await page0.Content.ReadAsStringAsync();
        var body1 = await page1.Content.ReadAsStringAsync();
        using var doc0 = JsonDocument.Parse(body0);
        using var doc1 = JsonDocument.Parse(body1);

        // Both must return the same page (page=1 items)
        doc0.RootElement.GetProperty("page").GetInt32().Should().Be(1);
        doc0.RootElement.GetProperty("items").GetArrayLength()
            .Should().Be(doc1.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task GetAll_WithNegativePage_ClampsToPage1()
    {
        var client = CreateAuthenticatedClient("Admin");

        var negPage = await client.GetAsync("/api/students?page=-99&pageSize=10");
        negPage.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await negPage.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.GetProperty("page").GetInt32().Should().Be(1);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAll_WithPageSizeExceedingMax_ClampsTo100()
    {
        var client = CreateAuthenticatedClient("Admin");

        var response = await client.GetAsync("/api/students?page=1&pageSize=9999");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.GetProperty("pageSize").GetInt32().Should().Be(100);
    }

    [Fact]
    public async Task GetAll_WithPageSize0_ClampsTo1AndReturnsItems()
    {
        var client = CreateAuthenticatedClient("Admin");

        var response = await client.GetAsync("/api/students?page=1&pageSize=0");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.GetProperty("pageSize").GetInt32().Should().Be(1);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().Be(1);
    }

    // ── GET /api/students/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task GetById_WithNonExistentId_Returns404()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync($"/api/students/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_WithValidSeededStudent_Returns200WithData()
    {
        var studentId = await GetFirstSeededStudentIdAsync();

        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync($"/api/students/{studentId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = JsonSerializer.Deserialize<StudentDto>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        result.Should().NotBeNull();
        result!.Id.Should().Be(studentId);
        result.StudentCode.Should().StartWith("EST-");
        result.Career.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetById_AsProfesor_Returns200()
    {
        var studentId = await GetFirstSeededStudentIdAsync();
        var client    = CreateAuthenticatedClient("Profesor");
        var response  = await client.GetAsync($"/api/students/{studentId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── POST /api/students ───────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        var dto      = new CreateStudentDto("Valid123!", "Test", "User", "Ciberseguridad", 1);
        var response = await Client.PostAsJsonAsync("/api/students", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_AsProfesor_Returns403()
    {
        var client   = CreateAuthenticatedClient("Profesor");
        var dto      = new CreateStudentDto("Valid123!", "Test", "User", "Ciberseguridad", 1);
        var response = await client.PostAsJsonAsync("/api/students", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_AsAdmin_WithValidData_Returns201WithStudentDto()
    {
        var adminClient = CreateAuthenticatedClient("Admin");
        var suffix      = Guid.NewGuid().ToString("N")[..8];
        var dto    = new CreateStudentDto(
            "Valid123!X",
            $"Nuevo{suffix}",
            "Estudiante",
            "Ingeniería en Sistemas",
            3);

        var response = await adminClient.PostAsJsonAsync("/api/students", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = JsonSerializer.Deserialize<StudentDto>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        result.Should().NotBeNull();
        result!.Career.Should().Be("Ingeniería en Sistemas");
        result.Semester.Should().Be(3);
        result.StudentCode.Should().MatchRegex(@"EST-\d{4}-[0-9A-F]{6}");
    }

    [Fact]
    public async Task Create_AsAdmin_WithNameThatMatchesSeededStudent_Returns400()
    {
        // "Juan"/"Pérez" → derived email "juan.perez@academia.com" already exists in seeder
        var client = CreateAuthenticatedClient("Admin");
        var dto = new CreateStudentDto("Valid123!X", "Juan", "Pérez", "Ingeniería en Sistemas", 1);

        var response = await client.PostAsJsonAsync("/api/students", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_AsAdmin_EmailIsAutomaticallyDerived()
    {
        // Backend should derive email — verify the response email matches the pattern
        var adminClient = CreateAuthenticatedClient("Admin");
        var suffix      = Guid.NewGuid().ToString("N")[..8];
        var firstName   = $"Test{suffix}";
        var lastName    = "Alumno";
        var dto         = new CreateStudentDto("Valid123!X", firstName, lastName, "Desarrollo de Software", 1);

        var response = await adminClient.PostAsJsonAsync("/api/students", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = JsonSerializer.Deserialize<StudentDto>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        var expectedEmail = $"test{suffix}.alumno@academia.com";
        result!.Email.Should().Be(expectedEmail);
    }

    // ── PUT /api/students/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task Update_AsAdmin_WithValidData_Returns200()
    {
        var studentId = await GetFirstSeededStudentIdAsync();
        var client    = CreateAuthenticatedClient("Admin");
        var dto       = new UpdateStudentDto("Ciberseguridad", 4);

        var response = await client.PutAsJsonAsync($"/api/students/{studentId}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = JsonSerializer.Deserialize<StudentDto>(
            await response.Content.ReadAsStringAsync(), JsonOpts);
        result!.Career.Should().Be("Ciberseguridad");
        result.Semester.Should().Be(4);
    }

    [Fact]
    public async Task Update_WithNonExistentId_Returns404()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.PutAsJsonAsync(
            $"/api/students/{Guid.NewGuid()}", new UpdateStudentDto("Ingeniería en Sistemas", 1));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /api/students/{id} ────────────────────────────────────────────

    [Fact]
    public async Task Delete_WithNonExistentId_Returns404()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.DeleteAsync($"/api/students/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_AsProfesor_Returns403()
    {
        var studentId = await GetFirstSeededStudentIdAsync();

        var client   = CreateAuthenticatedClient("Profesor");
        var response = await client.DeleteAsync($"/api/students/{studentId}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_AsAdmin_WithValidId_Returns204AndStudentIsInactive()
    {
        var adminClient = CreateAuthenticatedClient("Admin");
        var suffix      = Guid.NewGuid().ToString("N")[..8];
        var createDto   = new CreateStudentDto("Valid123!X", $"Para{suffix}", "Borrar", "IT", 1);

        var createResponse = await adminClient.PostAsJsonAsync("/api/students", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = JsonSerializer.Deserialize<StudentDto>(
            await createResponse.Content.ReadAsStringAsync(), JsonOpts);

        // Borrar (soft delete)
        var deleteResponse = await adminClient.DeleteAsync($"/api/students/{created!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verificar que el estudiante está inactivo en la BD
        var isInactive = await ExecuteInScopeAsync(async db =>
        {
            var student = await db.Students.IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == created.Id);
            return student?.IsActive == false;
        });
        isInactive.Should().BeTrue();
    }

    // ── GET /api/students/{id}/grade-report ──────────────────────────────────

    [Fact]
    public async Task GetGradeReport_AsAdmin_Returns200WithReport()
    {
        var studentId = await GetFirstSeededStudentIdAsync();
        var client    = CreateAuthenticatedClient("Admin");
        var response  = await client.GetAsync($"/api/students/{studentId}/grade-report");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var report = JsonSerializer.Deserialize<StudentGradeReportDto>(
            await response.Content.ReadAsStringAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        report.Should().NotBeNull();
        report!.StudentCode.Should().NotBeNullOrWhiteSpace();
        report.OverallAverage.Should().BeGreaterThanOrEqualTo(0);
        report.SubjectGrades.Should().NotBeEmpty(); // Juan Pérez tiene 3 calificaciones sembradas
    }

    [Fact]
    public async Task GetGradeReport_WithNonExistentId_Returns404()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync($"/api/students/{Guid.NewGuid()}/grade-report");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
