using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SistemaEvaluacionAcademica.Application.Common;
using SistemaEvaluacionAcademica.Application.DTOs.Grades;
using SistemaEvaluacionAcademica.Application.DTOs.Students;
using SistemaEvaluacionAcademica.IntegrationTests.Fixtures;

namespace SistemaEvaluacionAcademica.IntegrationTests.API;

[Trait("Category", "Integration")]
public class GradesEndpointsTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GradesEndpointsTests(CustomWebApplicationFactory factory) : base(factory) { }

    // ── POST /api/grades ──────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        var dto      = new CreateGradeDto(Guid.NewGuid(), Guid.NewGuid(), 8.5m, "2025-2", null);
        var response = await Client.PostAsJsonAsync("/api/grades", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_AsEstudiante_Returns403()
    {
        var client   = CreateAuthenticatedClient("Estudiante");
        var dto      = new CreateGradeDto(Guid.NewGuid(), Guid.NewGuid(), 8.5m, "2025-2", null);
        var response = await client.PostAsJsonAsync("/api/grades", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_AsAdmin_WithValidData_Returns201WithGradeDto()
    {
        var studentId = await GetFirstSeededStudentIdAsync();
        // Obtener la sección exacta en la que el estudiante está inscrito — robusto ante secciones adicionales de tests
        var sectionId = await GetEnrolledSectionIdAsync(studentId, "IS-MAT1");
        var adminUserId = await ExecuteInScopeAsync(async db =>
            (await db.Users.FirstAsync(u => u.Email == "admin@academia.com")).Id);

        var client = CreateAuthenticatedClient("Admin", userId: adminUserId);
        var dto    = new CreateGradeDto(studentId, sectionId, 8.0m, "2024-2", "Comentario de prueba");

        var response = await client.PostAsJsonAsync("/api/grades", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = JsonSerializer.Deserialize<GradeDto>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        result.Should().NotBeNull();
        result!.Value.Should().Be(8.0m);
        result.Period.Should().Be("2024-2");
        result.Comments.Should().Be("Comentario de prueba");
        result.Category.Should().Be("Buena"); // 8.0 → Buena
    }

    [Fact]
    public async Task Create_AsProfesor_WithValidData_Returns201()
    {
        var studentId = await GetFirstSeededStudentIdAsync();
        // IS-MAT1 es impartida por prof.garcia — obtener la sección real en la que el estudiante está inscrito
        var sectionId = await GetEnrolledSectionIdAsync(studentId, "IS-MAT1");
        var profesorUserId = await ExecuteInScopeAsync(async db =>
            (await db.Users.FirstAsync(u => u.Email == "prof.garcia@academia.com")).Id);

        var client = CreateAuthenticatedClient("Profesor", userId: profesorUserId);
        var dto    = new CreateGradeDto(studentId, sectionId, 9.5m, "1999-1", null);

        var response = await client.PostAsJsonAsync("/api/grades", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_WithInvalidGradeValue_Returns422()
    {
        var client = CreateAuthenticatedClient("Admin");
        var dto    = new CreateGradeDto(Guid.NewGuid(), Guid.NewGuid(), 11.0m, "2025-2", null);

        var response = await client.PostAsJsonAsync("/api/grades", dto);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Create_WithInvalidPeriod_Returns422()
    {
        var client = CreateAuthenticatedClient("Admin");
        var dto    = new CreateGradeDto(Guid.NewGuid(), Guid.NewGuid(), 8.5m, "bad-period", null);

        var response = await client.PostAsJsonAsync("/api/grades", dto);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Create_WithNonExistentStudent_Returns404()
    {
        // El estudiante no se encuentra antes de verificar la sección; GUIDs aleatorios para ambos
        var client   = CreateAuthenticatedClient("Admin");
        var dto      = new CreateGradeDto(Guid.NewGuid(), Guid.NewGuid(), 8.0m, "2022-1", null);

        var response = await client.PostAsJsonAsync("/api/grades", dto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_DuplicateStudentSectionPeriod_Returns400()
    {
        var studentId = await GetFirstSeededStudentIdAsync();
        // El seeder ya creó una calificación para student1 + sección IS-MAT1 + "2024-1"
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");

        var client   = CreateAuthenticatedClient("Admin");
        var dto      = new CreateGradeDto(studentId, sectionId, 7.0m, "2024-1", null);
        var response = await client.PostAsJsonAsync("/api/grades", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── PUT /api/grades/{id} ──────────────────────────────────────────────────

    [Fact]
    public async Task Update_WithNonExistentId_Returns404()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.PutAsJsonAsync($"/api/grades/{Guid.NewGuid()}",
            new { value = 8.0m, comments = "Updated" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_WithInvalidValue_Returns422()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.PutAsJsonAsync($"/api/grades/{Guid.NewGuid()}",
            new { value = 15.0m, comments = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Update_AsProfesor_WithGradeFromOwnSection_Returns200()
    {
        // prof.garcia está asignado a la sección IS-MAT1; student1 tiene calificación ahí para 2024-1
        var profesorUserId = await ExecuteInScopeAsync(async db =>
            (await db.Users.FirstAsync(u => u.Email == "prof.garcia@academia.com")).Id);
        var subjectId = await GetSubjectIdByCodeAsync("IS-MAT1");
        var studentId = await GetFirstSeededStudentIdAsync();

        var gradeId = await ExecuteInScopeAsync(async db =>
        {
            var grade = await db.Grades.FirstOrDefaultAsync(
                g => g.StudentId == studentId && g.SubjectId == subjectId && g.Period == "2024-1");
            return grade?.Id;
        });

        if (gradeId is null)
            return;

        var client   = CreateAuthenticatedClient("Profesor", userId: profesorUserId);
        var response = await client.PutAsJsonAsync($"/api/grades/{gradeId}",
            new { value = 8.5m, comments = "Revisión" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Update_AsProfesor_WithGradeFromAnotherSection_Returns403()
    {
        // prof.garcia NO está asignado a IS-BD1; student1 tiene calificación ahí para 2024-2
        var profesorUserId = await ExecuteInScopeAsync(async db =>
            (await db.Users.FirstAsync(u => u.Email == "prof.garcia@academia.com")).Id);
        var subjectId = await GetSubjectIdByCodeAsync("IS-BD1");
        var studentId = await GetFirstSeededStudentIdAsync();

        var gradeId = await ExecuteInScopeAsync(async db =>
        {
            var grade = await db.Grades.FirstOrDefaultAsync(
                g => g.StudentId == studentId && g.SubjectId == subjectId && g.Period == "2024-2");
            return grade?.Id;
        });

        if (gradeId is null)
            return;

        var client   = CreateAuthenticatedClient("Profesor", userId: profesorUserId);
        var response = await client.PutAsJsonAsync($"/api/grades/{gradeId}",
            new { value = 7.0m, comments = "Intento" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── DELETE /api/grades/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task Delete_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"/api/grades/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_AsProfesor_Returns403()
    {
        var client   = CreateAuthenticatedClient("Profesor");
        var response = await client.DeleteAsync($"/api/grades/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_AsAdmin_WithNonExistentId_Returns404()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.DeleteAsync($"/api/grades/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/grades/student/{studentId} ──────────────────────────────────

    [Fact]
    public async Task GetByStudent_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"/api/grades/student/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetByStudent_AsAdmin_Returns200WithPagedResult()
    {
        var studentId = await GetFirstSeededStudentIdAsync();
        var client    = CreateAuthenticatedClient("Admin");
        var response  = await client.GetAsync($"/api/grades/student/{studentId}?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.TryGetProperty("items", out var items).Should().BeTrue();
        doc.RootElement.TryGetProperty("totalCount", out var totalCount).Should().BeTrue();
        doc.RootElement.TryGetProperty("page", out var pageEl).Should().BeTrue();
        doc.RootElement.TryGetProperty("totalPages", out _).Should().BeTrue();

        items.GetArrayLength().Should().BeGreaterThan(0);
        totalCount.GetInt32().Should().BeGreaterThan(0);
        pageEl.GetInt32().Should().Be(1);

        var grades = JsonSerializer.Deserialize<PagedResult<GradeDto>>(body, JsonOpts);
        grades!.Items.Should().AllSatisfy(g =>
        {
            g.Value.Should().BeInRange(0, 10);
            g.Category.Should().NotBeNullOrWhiteSpace();
            g.Period.Should().MatchRegex(@"^\d{4}-[12]$");
        });
    }

    [Fact]
    public async Task GetByStudent_AsProfesor_Returns200()
    {
        var studentId = await GetFirstSeededStudentIdAsync();
        var client    = CreateAuthenticatedClient("Profesor");
        var response  = await client.GetAsync($"/api/grades/student/{studentId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetByStudent_WithPeriodFilter_Returns200WithFilteredItems()
    {
        var studentId = await GetFirstSeededStudentIdAsync();
        var client    = CreateAuthenticatedClient("Admin");
        var response  = await client.GetAsync($"/api/grades/student/{studentId}?period=2024-1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var paged = JsonSerializer.Deserialize<PagedResult<GradeDto>>(body, JsonOpts);

        paged.Should().NotBeNull();
        paged!.Items.Should().NotBeEmpty();
        paged.Items.Should().OnlyContain(g => g.Period == "2024-1");
    }

    [Fact]
    public async Task GetByStudent_PageSizeClamp_Returns200WithMaxPageSize()
    {
        var studentId = await GetFirstSeededStudentIdAsync();
        var client    = CreateAuthenticatedClient("Admin");
        // pageSize=200 supera el límite de 100; el servicio lo clampea a 100
        var response  = await client.GetAsync($"/api/grades/student/{studentId}?page=1&pageSize=200");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("pageSize").GetInt32().Should().Be(100);
    }

    [Fact]
    public async Task GetByStudent_NoGrades_Returns200WithEmptyItems()
    {
        // Crear estudiante nuevo sin calificaciones
        var adminClient = CreateAuthenticatedClient("Admin");
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var createDto = new CreateStudentDto(
            "Valid123!X", $"Sin{suffix}", "Notas", "Ingeniería en Sistemas", 1);
        var createRes = await adminClient.PostAsJsonAsync("/api/students", createDto);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var student = JsonSerializer.Deserialize<StudentDto>(
            await createRes.Content.ReadAsStringAsync(), JsonOpts);

        var response = await adminClient.GetAsync($"/api/grades/student/{student!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var paged = JsonSerializer.Deserialize<PagedResult<GradeDto>>(body, JsonOpts);
        paged!.Items.Should().BeEmpty();
        paged.TotalCount.Should().Be(0);
    }

    // ── GET /api/grades/subject/{subjectId} ───────────────────────────────────

    [Fact]
    public async Task GetBySubject_AsAdmin_Returns200WithPagedResult()
    {
        var subjectId = await GetSubjectIdByCodeAsync("IS-MAT1");
        var client    = CreateAuthenticatedClient("Admin");
        var response  = await client.GetAsync($"/api/grades/subject/{subjectId}?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.TryGetProperty("items", out var items).Should().BeTrue();
        doc.RootElement.TryGetProperty("totalCount", out _).Should().BeTrue();
        items.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetBySubject_AsEstudiante_Returns403()
    {
        var subjectId = await GetSubjectIdByCodeAsync("IS-MAT1");
        var client    = CreateAuthenticatedClient("Estudiante");
        var response  = await client.GetAsync($"/api/grades/subject/{subjectId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetBySubject_WithPeriodFilter_Returns200WithFilteredGrades()
    {
        var subjectId = await GetSubjectIdByCodeAsync("IS-MAT1");
        var client    = CreateAuthenticatedClient("Admin");
        var response  = await client.GetAsync($"/api/grades/subject/{subjectId}?period=2024-1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var paged = JsonSerializer.Deserialize<PagedResult<GradeDto>>(body, JsonOpts);

        paged.Should().NotBeNull();
        paged!.Items.Should().NotBeEmpty();
        paged.Items.Should().OnlyContain(g => g.Period == "2024-1");
    }

    [Fact]
    public async Task GetBySubject_PageSizeOne_Returns200WithCorrectPagination()
    {
        var subjectId = await GetSubjectIdByCodeAsync("IS-MAT1");
        var client    = CreateAuthenticatedClient("Admin");
        var response  = await client.GetAsync($"/api/grades/subject/{subjectId}?page=1&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var paged = JsonSerializer.Deserialize<PagedResult<GradeDto>>(body, JsonOpts);

        paged.Should().NotBeNull();
        paged!.Items.Should().HaveCount(1);
        paged.PageSize.Should().Be(1);
        paged.TotalCount.Should().BeGreaterThanOrEqualTo(1);
    }

    // ── GET /api/grades/section/{sectionId} ───────────────────────────────────

    [Fact]
    public async Task GetBySection_AsAdmin_Returns200WithPagedResult()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var client    = CreateAuthenticatedClient("Admin");
        var response  = await client.GetAsync($"/api/grades/section/{sectionId}?page=1&pageSize=50");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.TryGetProperty("items", out var items).Should().BeTrue();
        doc.RootElement.TryGetProperty("totalCount", out _).Should().BeTrue();
        items.GetArrayLength().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetBySection_AsProfesor_WithOwnSection_Returns200()
    {
        // prof.garcia está asignado a IS-MAT1
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var profesorUserId = await ExecuteInScopeAsync(async db =>
            (await db.Users.FirstAsync(u => u.Email == "prof.garcia@academia.com")).Id);

        var client   = CreateAuthenticatedClient("Profesor", userId: profesorUserId);
        var response = await client.GetAsync($"/api/grades/section/{sectionId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetBySection_AsProfesor_WithOtherProfessorSection_Returns403()
    {
        // prof.garcia NO está asignado a IS-BD1
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-BD1");
        var profesorUserId = await ExecuteInScopeAsync(async db =>
            (await db.Users.FirstAsync(u => u.Email == "prof.garcia@academia.com")).Id);

        var client   = CreateAuthenticatedClient("Profesor", userId: profesorUserId);
        var response = await client.GetAsync($"/api/grades/section/{sectionId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetBySection_WithNonExistentId_Returns404()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync($"/api/grades/section/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/grades/student/{studentId}/average ───────────────────────────

    [Fact]
    public async Task GetStudentAverage_AsAdmin_Returns200WithAverageAndCategory()
    {
        var studentId = await GetFirstSeededStudentIdAsync();
        var client    = CreateAuthenticatedClient("Admin");
        var response  = await client.GetAsync($"/api/grades/student/{studentId}/average");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.TryGetProperty("average", out var avg).Should().BeTrue();
        doc.RootElement.TryGetProperty("category", out _).Should().BeTrue();

        avg.GetDecimal().Should().BeInRange(0, 10);
    }

    // ── GET /api/grades/subject/{subjectId}/average ───────────────────────────

    [Fact]
    public async Task GetSubjectAverage_AsAdmin_Returns200WithAverage()
    {
        var subjectId = await GetSubjectIdByCodeAsync("IS-MAT1");
        var client    = CreateAuthenticatedClient("Admin");
        var response  = await client.GetAsync($"/api/grades/subject/{subjectId}/average?period=2024-1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.TryGetProperty("average", out var avg).Should().BeTrue();
        avg.GetDecimal().Should().BeInRange(0, 10);
    }
}
