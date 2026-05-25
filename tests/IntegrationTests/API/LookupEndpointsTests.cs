using System.Net;
using System.Text.Json;
using FluentAssertions;
using SistemaEvaluacionAcademica.IntegrationTests.Fixtures;

namespace SistemaEvaluacionAcademica.IntegrationTests.API;

[Trait("Category", "Integration")]
public class LookupEndpointsTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public LookupEndpointsTests(CustomWebApplicationFactory factory) : base(factory) { }

    // ── GET /api/subjects/lookup — auth ──────────────────────────────────────

    [Fact]
    public async Task SubjectsLookup_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync("/api/subjects/lookup");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SubjectsLookup_AsEstudiante_Returns403()
    {
        var client   = CreateAuthenticatedClient("Estudiante");
        var response = await client.GetAsync("/api/subjects/lookup");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET /api/subjects/lookup — payload ───────────────────────────────────

    [Fact]
    public async Task SubjectsLookup_AsAdmin_ReturnsLightweightPayload()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync("/api/subjects/lookup");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        // Must be an array, not a paged object
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);

        var first = doc.RootElement[0];
        first.TryGetProperty("id",            out _).Should().BeTrue();
        first.TryGetProperty("code",          out _).Should().BeTrue();
        first.TryGetProperty("name",          out _).Should().BeTrue();
        first.TryGetProperty("semesterLevel", out _).Should().BeTrue();

        // Must NOT contain heavy fields
        first.TryGetProperty("description",       out _).Should().BeFalse();
        first.TryGetProperty("credits",           out _).Should().BeFalse();
        first.TryGetProperty("sectionCount",      out _).Should().BeFalse();
        first.TryGetProperty("appliesToAllCareers", out _).Should().BeFalse();
    }

    [Fact]
    public async Task SubjectsLookup_AsProfesor_Returns200()
    {
        var client   = CreateAuthenticatedClient("Profesor");
        var response = await client.GetAsync("/api/subjects/lookup");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SubjectsLookup_ReturnsAllSeededSubjects()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync("/api/subjects/lookup?limit=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        // Seeder inserts 27 subjects (other tests may add more)
        doc.RootElement.GetArrayLength().Should().BeGreaterThanOrEqualTo(27);
    }

    [Fact]
    public async Task SubjectsLookup_WithSearchByCode_ReturnsMatchingItems()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync("/api/subjects/lookup?search=IS-MAT");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        var items = doc.RootElement;
        items.GetArrayLength().Should().BeGreaterThan(0);
        items[0].GetProperty("code").GetString().Should().StartWith("IS-MAT");
    }

    [Fact]
    public async Task SubjectsLookup_WithSearchByName_ReturnsMatchingItems()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync("/api/subjects/lookup?search=programaci");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SubjectsLookup_WithSearchNoMatch_ReturnsEmptyArray()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync("/api/subjects/lookup?search=xqznotexists99999");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task SubjectsLookup_WithLimit1_ReturnsExactly1Item()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync("/api/subjects/lookup?limit=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task SubjectsLookup_LimitClampsAt100()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync("/api/subjects/lookup?limit=9999");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        // 27 subjects seeded; clamped to 100 still returns all of them
        doc.RootElement.GetArrayLength().Should().BeGreaterThanOrEqualTo(27);
    }

    // ── GET /api/sections/lookup — auth ──────────────────────────────────────

    [Fact]
    public async Task SectionsLookup_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync("/api/sections/lookup");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SectionsLookup_AsEstudiante_Returns403()
    {
        var client   = CreateAuthenticatedClient("Estudiante");
        var response = await client.GetAsync("/api/sections/lookup");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET /api/sections/lookup — payload ───────────────────────────────────

    [Fact]
    public async Task SectionsLookup_AsAdmin_ReturnsLightweightPayload()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync("/api/sections/lookup");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);

        var first = doc.RootElement[0];
        first.TryGetProperty("id",          out _).Should().BeTrue();
        first.TryGetProperty("subjectCode", out _).Should().BeTrue();
        first.TryGetProperty("subjectName", out _).Should().BeTrue();
        first.TryGetProperty("sectionCode", out _).Should().BeTrue();
        first.TryGetProperty("dayOfWeek",   out _).Should().BeTrue();
        first.TryGetProperty("startTime",   out _).Should().BeTrue();
        first.TryGetProperty("endTime",     out _).Should().BeTrue();

        // Must NOT contain heavy fields
        first.TryGetProperty("capacity",      out _).Should().BeFalse();
        first.TryGetProperty("enrolledCount", out _).Should().BeFalse();
        first.TryGetProperty("professorName", out _).Should().BeFalse();
        first.TryGetProperty("modality",      out _).Should().BeFalse();
    }

    [Fact]
    public async Task SectionsLookup_AsProfesor_Returns200()
    {
        var client   = CreateAuthenticatedClient("Profesor");
        var response = await client.GetAsync("/api/sections/lookup");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SectionsLookup_ReturnsAllSeededSections()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync("/api/sections/lookup?limit=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        // Seeder inserts 27 sections (other tests may add more)
        doc.RootElement.GetArrayLength().Should().BeGreaterThanOrEqualTo(27);
    }

    [Fact]
    public async Task SectionsLookup_WithSearchBySubjectCode_ReturnsMatchingItems()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync("/api/sections/lookup?search=IS-MAT");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.GetArrayLength().Should().BeGreaterThan(0);
        doc.RootElement[0].GetProperty("subjectCode").GetString().Should().StartWith("IS-MAT");
    }

    [Fact]
    public async Task SectionsLookup_WithSearchNoMatch_ReturnsEmptyArray()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync("/api/sections/lookup?search=xqznotexists99999");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task SectionsLookup_WithLimit2_ReturnsExactly2Items()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync("/api/sections/lookup?limit=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task SectionsLookup_StartAndEndTimeAreFormattedAsHHmm()
    {
        var client   = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync("/api/sections/lookup?limit=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        var startTime = doc.RootElement[0].GetProperty("startTime").GetString()!;
        var endTime   = doc.RootElement[0].GetProperty("endTime").GetString()!;

        startTime.Should().MatchRegex(@"^\d{2}:\d{2}$");
        endTime.Should().MatchRegex(@"^\d{2}:\d{2}$");
    }
}
