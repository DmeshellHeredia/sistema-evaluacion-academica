using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SistemaEvaluacionAcademica.Application.DTOs.Enrollments;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.IntegrationTests.Fixtures;

namespace SistemaEvaluacionAcademica.IntegrationTests.API;

[Trait("Category", "Integration")]
public class CatalogCareerFilterTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public CatalogCareerFilterTests(CustomWebApplicationFactory factory) : base(factory) { }

    // ── Career-specific subjects ──────────────────────────────────────────────

    [Fact]
    public async Task Catalog_ISStudent_SeesOnlyISAndAllCareersSubjects()
    {
        var isStudentId = await ExecuteInScopeAsync(async db =>
            (await db.Students.FirstAsync(s => s.StudentCode == "EST-2025-A001")).Id);

        var client = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync($"/api/enrollments/catalog/{isStudentId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = JsonSerializer.Deserialize<SubjectCatalogItemDto[]>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        items.Should().NotBeNullOrEmpty();
        // Todos los códigos devueltos deben empezar con IS- o estar sembrados como AppliesToAllCareers
        items!.Should().OnlyContain(i =>
            i.Code.StartsWith("IS-") || i.Code.StartsWith("ALL-"),
            "IS student should not see CIB- or DS- subjects");
    }

    [Fact]
    public async Task Catalog_CIBStudent_SeesOnlyCIBAndAllCareersSubjects()
    {
        var cibStudentId = await ExecuteInScopeAsync(async db =>
            (await db.Students.FirstAsync(s => s.StudentCode == "EST-2025-B001")).Id);

        var client = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync($"/api/enrollments/catalog/{cibStudentId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = JsonSerializer.Deserialize<SubjectCatalogItemDto[]>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        items.Should().NotBeNullOrEmpty();
        items!.Should().OnlyContain(i =>
            i.Code.StartsWith("CIB-") || i.Code.StartsWith("ALL-"),
            "CIB student should not see IS- or DS- subjects");
    }

    [Fact]
    public async Task Catalog_ISAndCIBStudents_DoNotShareCareerSpecificSubjects()
    {
        var (isStudentId, cibStudentId) = await ExecuteInScopeAsync(async db =>
        {
            var isS  = (await db.Students.FirstAsync(s => s.StudentCode == "EST-2025-A001")).Id;
            var cibS = (await db.Students.FirstAsync(s => s.StudentCode == "EST-2025-B001")).Id;
            return (isS, cibS);
        });

        var client = CreateAuthenticatedClient("Admin");
        var isResp  = await client.GetAsync($"/api/enrollments/catalog/{isStudentId}");
        var cibResp = await client.GetAsync($"/api/enrollments/catalog/{cibStudentId}");

        var isCodes  = JsonSerializer.Deserialize<SubjectCatalogItemDto[]>(await isResp.Content.ReadAsStringAsync(),  JsonOpts)!
            .Select(i => i.Code).ToHashSet();
        var cibCodes = JsonSerializer.Deserialize<SubjectCatalogItemDto[]>(await cibResp.Content.ReadAsStringAsync(), JsonOpts)!
            .Select(i => i.Code).ToHashSet();

        // Códigos IS: IS-MAT1, IS-PRG1, etc. Códigos CIB: CIB-FUN1, CIB-PRG1, etc.
        // La intersección no debe contener códigos específicos de carrera
        var shared = isCodes.Intersect(cibCodes).ToList();
        shared.Should().NotContain(c => c.StartsWith("IS-"),  "IS- subjects should not appear in CIB catalog");
        shared.Should().NotContain(c => c.StartsWith("CIB-"), "CIB- subjects should not appear in IS catalog");
    }

    // ── Multi-career subjects (AppliesToAllCareers) ───────────────────────────

    [Fact]
    public async Task Catalog_MultiCareerSubject_AppearsForAllStudents()
    {
        // Crear una materia compartida visible para todas las carreras
        var sharedCode = $"ALL-TST-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";

        await ExecuteInScopeAsync(async db =>
        {
            var shared = new Subject(sharedCode, "Materia Compartida", "Aplica a todas las carreras",
                2, Array.Empty<string>(), 1, appliesToAllCareers: true);
            await db.Subjects.AddAsync(shared);
            await db.SaveChangesAsync();
            return true;
        });

        var (isStudentId, cibStudentId, dsStudentId) = await ExecuteInScopeAsync(async db =>
        {
            var isS  = (await db.Students.FirstAsync(s => s.StudentCode == "EST-2025-A001")).Id;
            var cibS = (await db.Students.FirstAsync(s => s.StudentCode == "EST-2025-B001")).Id;
            var dsS  = (await db.Students.FirstAsync(s => s.StudentCode == "EST-2025-C001")).Id;
            return (isS, cibS, dsS);
        });

        var client = CreateAuthenticatedClient("Admin");

        var isItems  = await GetCatalogCodesAsync(client, isStudentId);
        var cibItems = await GetCatalogCodesAsync(client, cibStudentId);
        var dsItems  = await GetCatalogCodesAsync(client, dsStudentId);

        isItems.Should().Contain(sharedCode,  "IS student should see AppliesToAllCareers subjects");
        cibItems.Should().Contain(sharedCode, "CIB student should see AppliesToAllCareers subjects");
        dsItems.Should().Contain(sharedCode,  "DS student should see AppliesToAllCareers subjects");
    }

    [Fact]
    public async Task Catalog_MultiCareerByExplicitList_AppearsForListedCareers()
    {
        // Materia asignada explícitamente a IS y CIB, pero NO a DS
        var multiCode = $"MC-TST-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";

        await ExecuteInScopeAsync(async db =>
        {
            var multi = new Subject(multiCode, "Multi-Carrera Test", "Desc multi",
                3, new[] { "Ingeniería en Sistemas", "Ciberseguridad" }, 1);
            await db.Subjects.AddAsync(multi);
            await db.SaveChangesAsync();
            return true;
        });

        var (isStudentId, cibStudentId, dsStudentId) = await ExecuteInScopeAsync(async db =>
        {
            var isS  = (await db.Students.FirstAsync(s => s.StudentCode == "EST-2025-A001")).Id;
            var cibS = (await db.Students.FirstAsync(s => s.StudentCode == "EST-2025-B001")).Id;
            var dsS  = (await db.Students.FirstAsync(s => s.StudentCode == "EST-2025-C001")).Id;
            return (isS, cibS, dsS);
        });

        var client = CreateAuthenticatedClient("Admin");

        var isItems  = await GetCatalogCodesAsync(client, isStudentId);
        var cibItems = await GetCatalogCodesAsync(client, cibStudentId);
        var dsItems  = await GetCatalogCodesAsync(client, dsStudentId);

        isItems.Should().Contain(multiCode,       "IS is in the explicit career list");
        cibItems.Should().Contain(multiCode,      "CIB is in the explicit career list");
        dsItems.Should().NotContain(multiCode,    "DS is NOT in the explicit career list");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<HashSet<string>> GetCatalogCodesAsync(HttpClient client, Guid studentId)
    {
        var response = await client.GetAsync($"/api/enrollments/catalog/{studentId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = JsonSerializer.Deserialize<SubjectCatalogItemDto[]>(
            await response.Content.ReadAsStringAsync(), JsonOpts);
        return items!.Select(i => i.Code).ToHashSet();
    }
}
