using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SistemaEvaluacionAcademica.Application.Common;
using SistemaEvaluacionAcademica.Application.DTOs.Sections;
using SistemaEvaluacionAcademica.IntegrationTests.Fixtures;

namespace SistemaEvaluacionAcademica.IntegrationTests.API;

[Trait("Category", "Integration")]
public class SectionsEndpointsTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public SectionsEndpointsTests(CustomWebApplicationFactory factory) : base(factory) { }

    // ── GET /api/sections ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync("/api/sections");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_AsProfesor_Returns403()
    {
        var client = CreateAuthenticatedClient("Profesor");
        var response = await client.GetAsync("/api/sections");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_AsAdmin_ReturnsPagedResult()
    {
        var adminUserId = await ExecuteInScopeAsync(async db =>
            (await db.Users.FirstAsync(u => u.Email == "admin@academia.com")).Id);
        var client = CreateAuthenticatedClient("Admin", userId: adminUserId);

        var response = await client.GetAsync("/api/sections?page=1&pageSize=50");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var paged = JsonSerializer.Deserialize<PagedResult<SectionDto>>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        paged.Should().NotBeNull();
        paged!.Items.Should().NotBeEmpty();
        paged.Page.Should().Be(1);
        paged.PageSize.Should().Be(50);
        paged.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAll_Pagination_SmallPageSize_ReturnsCorrectSlice()
    {
        var adminUserId = await ExecuteInScopeAsync(async db =>
            (await db.Users.FirstAsync(u => u.Email == "admin@academia.com")).Id);
        var client = CreateAuthenticatedClient("Admin", userId: adminUserId);

        var resp1 = await client.GetAsync("/api/sections?page=1&pageSize=3");
        resp1.StatusCode.Should().Be(HttpStatusCode.OK);

        var page1 = JsonSerializer.Deserialize<PagedResult<SectionDto>>(
            await resp1.Content.ReadAsStringAsync(), JsonOpts);

        page1!.Items.Should().HaveCount(3);
        page1.TotalCount.Should().BeGreaterThan(3);
        page1.TotalPages.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task GetAll_SecondPage_ReturnsDifferentItems()
    {
        var adminUserId = await ExecuteInScopeAsync(async db =>
            (await db.Users.FirstAsync(u => u.Email == "admin@academia.com")).Id);
        var client = CreateAuthenticatedClient("Admin", userId: adminUserId);

        var resp1 = await client.GetAsync("/api/sections?page=1&pageSize=3");
        var resp2 = await client.GetAsync("/api/sections?page=2&pageSize=3");

        var page1 = JsonSerializer.Deserialize<PagedResult<SectionDto>>(
            await resp1.Content.ReadAsStringAsync(), JsonOpts);
        var page2 = JsonSerializer.Deserialize<PagedResult<SectionDto>>(
            await resp2.Content.ReadAsStringAsync(), JsonOpts);

        var ids1 = page1!.Items.Select(s => s.Id).ToHashSet();
        var ids2 = page2!.Items.Select(s => s.Id).ToHashSet();

        ids1.Intersect(ids2).Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_SearchBySubjectCode_ReturnsOnlyMatches()
    {
        var adminUserId = await ExecuteInScopeAsync(async db =>
            (await db.Users.FirstAsync(u => u.Email == "admin@academia.com")).Id);
        var client = CreateAuthenticatedClient("Admin", userId: adminUserId);

        var response = await client.GetAsync("/api/sections?search=IS-MAT1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var paged = JsonSerializer.Deserialize<PagedResult<SectionDto>>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        paged!.Items.Should().OnlyContain(s =>
            s.SubjectCode.Contains("IS-MAT1") ||
            s.SubjectName.Contains("IS-MAT1") ||
            s.SectionCode.Contains("IS-MAT1"));
    }

    [Fact]
    public async Task GetAll_SearchNoMatch_ReturnsEmptyItems()
    {
        var adminUserId = await ExecuteInScopeAsync(async db =>
            (await db.Users.FirstAsync(u => u.Email == "admin@academia.com")).Id);
        var client = CreateAuthenticatedClient("Admin", userId: adminUserId);

        var response = await client.GetAsync("/api/sections?search=XXXXXXNOTFOUND");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var paged = JsonSerializer.Deserialize<PagedResult<SectionDto>>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        paged!.Items.Should().BeEmpty();
        paged.TotalCount.Should().Be(0);
    }

    // ── GET /api/sections/my ─────────────────────────────────────────────────

    [Fact]
    public async Task GetMine_AsAdmin_ReturnsAllSections()
    {
        var adminUserId = await ExecuteInScopeAsync(async db =>
            (await db.Users.FirstAsync(u => u.Email == "admin@academia.com")).Id);
        var client = CreateAuthenticatedClient("Admin", userId: adminUserId);

        var response = await client.GetAsync("/api/sections/my");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var sections = JsonSerializer.Deserialize<SectionDto[]>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        sections.Should().NotBeNullOrEmpty();
    }
}
