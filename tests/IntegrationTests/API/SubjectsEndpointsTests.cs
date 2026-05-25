using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SistemaEvaluacionAcademica.Application.Common;
using SistemaEvaluacionAcademica.Application.DTOs.Subjects;
using SistemaEvaluacionAcademica.IntegrationTests.Fixtures;

namespace SistemaEvaluacionAcademica.IntegrationTests.API;

[Trait("Category", "Integration")]
public class SubjectsEndpointsTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SubjectsEndpointsTests(CustomWebApplicationFactory factory) : base(factory) { }

    // ── GET /api/subjects ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync("/api/subjects");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_AsAdmin_ReturnsSeededSubjects()
    {
        var adminUserId = await ExecuteInScopeAsync(async db =>
            (await db.Users.FirstAsync(u => u.Email == "admin@academia.com")).Id);
        var client = CreateAuthenticatedClient("Admin", userId: adminUserId);

        var response = await client.GetAsync("/api/subjects?page=1&pageSize=50");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var paged = JsonSerializer.Deserialize<PagedResult<SubjectDto>>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        paged.Should().NotBeNull();
        paged!.Items.Should().NotBeEmpty();
        paged.Items.Should().Contain(s => s.Code == "IS-MAT1");
    }

    // ── Pagination ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Pagination_ReturnsTotalCountAndCorrectPage()
    {
        var adminUserId = await ExecuteInScopeAsync(async db =>
            (await db.Users.FirstAsync(u => u.Email == "admin@academia.com")).Id);
        var client = CreateAuthenticatedClient("Admin", userId: adminUserId);

        var response = await client.GetAsync("/api/subjects?page=1&pageSize=5");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var paged = JsonSerializer.Deserialize<PagedResult<SubjectDto>>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        paged.Should().NotBeNull();
        paged!.Page.Should().Be(1);
        paged.PageSize.Should().Be(5);
        paged.Items.Should().HaveCount(5);
        paged.TotalCount.Should().BeGreaterThan(5);
        paged.TotalPages.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task GetAll_SecondPage_ReturnsDifferentItems()
    {
        var adminUserId = await ExecuteInScopeAsync(async db =>
            (await db.Users.FirstAsync(u => u.Email == "admin@academia.com")).Id);
        var client = CreateAuthenticatedClient("Admin", userId: adminUserId);

        var resp1 = await client.GetAsync("/api/subjects?page=1&pageSize=5");
        var resp2 = await client.GetAsync("/api/subjects?page=2&pageSize=5");

        var page1 = JsonSerializer.Deserialize<PagedResult<SubjectDto>>(
            await resp1.Content.ReadAsStringAsync(), JsonOpts);
        var page2 = JsonSerializer.Deserialize<PagedResult<SubjectDto>>(
            await resp2.Content.ReadAsStringAsync(), JsonOpts);

        var ids1 = page1!.Items.Select(s => s.Id).ToHashSet();
        var ids2 = page2!.Items.Select(s => s.Id).ToHashSet();

        ids1.Intersect(ids2).Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_SearchFilter_ReturnsOnlyMatchingItems()
    {
        var adminUserId = await ExecuteInScopeAsync(async db =>
            (await db.Users.FirstAsync(u => u.Email == "admin@academia.com")).Id);
        var client = CreateAuthenticatedClient("Admin", userId: adminUserId);

        var response = await client.GetAsync("/api/subjects?search=MAT1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var paged = JsonSerializer.Deserialize<PagedResult<SubjectDto>>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        paged!.Items.Should().NotBeEmpty();
        paged.Items.Should().OnlyContain(s => s.Code.Contains("MAT1") || s.Name.Contains("MAT1") || s.Description.Contains("MAT1"));
    }

    [Fact]
    public async Task GetAll_SearchNoMatch_ReturnsEmptyItems()
    {
        var adminUserId = await ExecuteInScopeAsync(async db =>
            (await db.Users.FirstAsync(u => u.Email == "admin@academia.com")).Id);
        var client = CreateAuthenticatedClient("Admin", userId: adminUserId);

        var response = await client.GetAsync("/api/subjects?search=XXXXXNOTFOUND");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var paged = JsonSerializer.Deserialize<PagedResult<SubjectDto>>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        paged!.Items.Should().BeEmpty();
        paged.TotalCount.Should().Be(0);
    }

    // ── Prerequisites: create → set → reload ─────────────────────────────────

    [Fact]
    public async Task SetPrerequisites_AsAdmin_PersistsAndLoadsCorrectly()
    {
        var adminUserId = await ExecuteInScopeAsync(async db =>
            (await db.Users.FirstAsync(u => u.Email == "admin@academia.com")).Id);
        var client = CreateAuthenticatedClient("Admin", userId: adminUserId);

        // Get two seeded subjects: IS-MAT2 (requires IS-MAT1 per seed)
        var mat1Id = await GetSubjectIdByCodeAsync("IS-MAT1");
        var mat2Id = await GetSubjectIdByCodeAsync("IS-MAT2");

        // Set IS-MAT2's prerequisites to [IS-MAT1]
        var setBody = new { prerequisiteIds = new[] { mat1Id } };
        var setResp = await client.PutAsJsonAsync($"/api/subjects/{mat2Id}/prerequisites", setBody);
        setResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Reload prerequisites for IS-MAT2
        var getResp = await client.GetAsync($"/api/subjects/{mat2Id}/prerequisites");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var prereqs = JsonSerializer.Deserialize<PrerequisiteDto[]>(
            await getResp.Content.ReadAsStringAsync(), JsonOpts);

        prereqs.Should().NotBeNullOrEmpty();
        prereqs!.Should().Contain(p => p.SubjectId == mat1Id);
    }

    [Fact]
    public async Task SetPrerequisites_CycleDetected_Returns400()
    {
        var adminUserId = await ExecuteInScopeAsync(async db =>
            (await db.Users.FirstAsync(u => u.Email == "admin@academia.com")).Id);
        var client = CreateAuthenticatedClient("Admin", userId: adminUserId);

        var mat1Id = await GetSubjectIdByCodeAsync("IS-MAT1");
        var mat2Id = await GetSubjectIdByCodeAsync("IS-MAT2");

        // IS-MAT2 requires IS-MAT1 → trying to set IS-MAT1 to require IS-MAT2 creates cycle
        var setMat2 = new { prerequisiteIds = new[] { mat1Id } };
        await client.PutAsJsonAsync($"/api/subjects/{mat2Id}/prerequisites", setMat2);

        // Now try to add IS-MAT2 as prereq of IS-MAT1 → cycle
        var cycleBody = new { prerequisiteIds = new[] { mat2Id } };
        var cycleResp = await client.PutAsJsonAsync($"/api/subjects/{mat1Id}/prerequisites", cycleBody);
        cycleResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await cycleResp.Content.ReadAsStringAsync();
        body.Should().Contain("ciclo académico");
    }

    [Fact]
    public async Task SetPrerequisites_ClearAll_Returns200WithEmptyList()
    {
        var adminUserId = await ExecuteInScopeAsync(async db =>
            (await db.Users.FirstAsync(u => u.Email == "admin@academia.com")).Id);
        var client = CreateAuthenticatedClient("Admin", userId: adminUserId);

        var mat1Id = await GetSubjectIdByCodeAsync("IS-MAT1");

        // Clear all prerequisites for IS-MAT1
        var clearBody = new { prerequisiteIds = Array.Empty<Guid>() };
        var clearResp = await client.PutAsJsonAsync($"/api/subjects/{mat1Id}/prerequisites", clearBody);
        clearResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResp = await client.GetAsync($"/api/subjects/{mat1Id}/prerequisites");
        var prereqs = JsonSerializer.Deserialize<PrerequisiteDto[]>(
            await getResp.Content.ReadAsStringAsync(), JsonOpts);
        prereqs.Should().BeEmpty();
    }

    [Fact]
    public async Task SetPrerequisites_AsEstudiante_Returns403()
    {
        var mat1Id = await GetSubjectIdByCodeAsync("IS-MAT1");
        var client = CreateAuthenticatedClient("Estudiante");
        var body = new { prerequisiteIds = Array.Empty<Guid>() };
        var response = await client.PutAsJsonAsync($"/api/subjects/{mat1Id}/prerequisites", body);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── AddPrerequisite — ciclo y casos edge ─────────────────────────────────

    [Fact]
    public async Task AddPrerequisite_ValidDirect_Returns201()
    {
        var client = CreateAuthenticatedClient("Admin");
        var mat2Id = await GetSubjectIdByCodeAsync("IS-MAT2");
        var mat1Id = await GetSubjectIdByCodeAsync("IS-MAT1");

        // Asegurar que IS-MAT2 no tenga prerequisitos primero
        await client.PutAsJsonAsync($"/api/subjects/{mat2Id}/prerequisites", new { prerequisiteIds = Array.Empty<Guid>() });

        var response = await client.PostAsync($"/api/subjects/{mat2Id}/prerequisites/{mat1Id}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task AddPrerequisite_SelfReference_Returns400()
    {
        var client = CreateAuthenticatedClient("Admin");
        var mat1Id = await GetSubjectIdByCodeAsync("IS-MAT1");

        var response = await client.PostAsync($"/api/subjects/{mat1Id}/prerequisites/{mat1Id}", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("sí misma");
    }

    [Fact]
    public async Task AddPrerequisite_DirectCycle_Returns400WithCycleMessage()
    {
        var client = CreateAuthenticatedClient("Admin");
        var mat1Id = await GetSubjectIdByCodeAsync("IS-MAT1");
        var mat2Id = await GetSubjectIdByCodeAsync("IS-MAT2");

        // Estado: IS-MAT2 → IS-MAT1. Intentar IS-MAT1 → IS-MAT2 crea A ↔ B.
        await client.PutAsJsonAsync($"/api/subjects/{mat2Id}/prerequisites", new { prerequisiteIds = new[] { mat1Id } });
        await client.PutAsJsonAsync($"/api/subjects/{mat1Id}/prerequisites", new { prerequisiteIds = Array.Empty<Guid>() });

        var response = await client.PostAsync($"/api/subjects/{mat1Id}/prerequisites/{mat2Id}", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("ciclo académico");
    }

    [Fact]
    public async Task AddPrerequisite_IndirectCycle_Returns400WithCycleMessage()
    {
        var client = CreateAuthenticatedClient("Admin");
        // Usar IS-MAT1, IS-MAT2, IS-BD1 para el ciclo A→B→C→A
        var mat1Id = await GetSubjectIdByCodeAsync("IS-MAT1");
        var mat2Id = await GetSubjectIdByCodeAsync("IS-MAT2");
        var bd1Id  = await GetSubjectIdByCodeAsync("IS-BD1");

        // Limpiar prerequisitos de los tres para partir de estado limpio
        await client.PutAsJsonAsync($"/api/subjects/{mat1Id}/prerequisites", new { prerequisiteIds = Array.Empty<Guid>() });
        await client.PutAsJsonAsync($"/api/subjects/{mat2Id}/prerequisites", new { prerequisiteIds = Array.Empty<Guid>() });
        await client.PutAsJsonAsync($"/api/subjects/{bd1Id}/prerequisites", new { prerequisiteIds = Array.Empty<Guid>() });

        // Construir cadena: IS-MAT2 → IS-MAT1, IS-BD1 → IS-MAT2
        await client.PostAsync($"/api/subjects/{mat2Id}/prerequisites/{mat1Id}", null);
        await client.PostAsync($"/api/subjects/{bd1Id}/prerequisites/{mat2Id}", null);

        // Intentar IS-MAT1 → IS-BD1: crearía IS-MAT1 → IS-BD1 → IS-MAT2 → IS-MAT1
        var response = await client.PostAsync($"/api/subjects/{mat1Id}/prerequisites/{bd1Id}", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("ciclo académico");
    }

    // ── Subjects include Careers field ────────────────────────────────────────

    [Fact]
    public async Task GetAll_SubjectDto_IncludesCareersField()
    {
        var adminUserId = await ExecuteInScopeAsync(async db =>
            (await db.Users.FirstAsync(u => u.Email == "admin@academia.com")).Id);
        var client = CreateAuthenticatedClient("Admin", userId: adminUserId);

        var response = await client.GetAsync("/api/subjects?page=1&pageSize=50");
        var paged = JsonSerializer.Deserialize<PagedResult<SubjectDto>>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        paged!.Items.Should().NotBeEmpty();
        // Every subject should have a non-null Careers list
        paged.Items.Should().AllSatisfy(s => s.Careers.Should().NotBeNull());
    }

    // ── Multi-career: update sends careers ───────────────────────────────────

    [Fact]
    public async Task UpdateSubject_WithMultipleCareers_PersistsCareersCorrectly()
    {
        var adminUserId = await ExecuteInScopeAsync(async db =>
            (await db.Users.FirstAsync(u => u.Email == "admin@academia.com")).Id);
        var client = CreateAuthenticatedClient("Admin", userId: adminUserId);
        var mat1Id = await GetSubjectIdByCodeAsync("IS-MAT1");

        // Get current subject
        var getResp = await client.GetAsync($"/api/subjects/{mat1Id}");
        var current = JsonSerializer.Deserialize<SubjectDto>(
            await getResp.Content.ReadAsStringAsync(), JsonOpts);

        // Update with two careers
        var updateBody = new
        {
            name = current!.Name,
            description = current.Description,
            credits = current.Credits,
            career = "Ingeniería en Sistemas",
            semesterLevel = current.SemesterLevel,
            appliesToAllCareers = false,
            careers = new[] { "Ingeniería en Sistemas", "Desarrollo de Software" },
        };
        var updateResp = await client.PutAsJsonAsync($"/api/subjects/{mat1Id}", updateBody);
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = JsonSerializer.Deserialize<SubjectDto>(
            await updateResp.Content.ReadAsStringAsync(), JsonOpts);

        updated!.Careers.Should().Contain("Ingeniería en Sistemas");
        updated.Careers.Should().Contain("Desarrollo de Software");

        // Restore to single career
        var restoreBody = new
        {
            name = current.Name,
            description = current.Description,
            credits = current.Credits,
            career = "Ingeniería en Sistemas",
            semesterLevel = current.SemesterLevel,
            appliesToAllCareers = false,
            careers = new[] { "Ingeniería en Sistemas" },
        };
        await client.PutAsJsonAsync($"/api/subjects/{mat1Id}", restoreBody);
    }

}
