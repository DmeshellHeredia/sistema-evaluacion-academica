using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using SistemaEvaluacionAcademica.IntegrationTests.Fixtures;

namespace SistemaEvaluacionAcademica.IntegrationTests.API;

[Trait("Category", "Integration")]
public class CorrelationIdTests : IntegrationTestBase
{
    public CorrelationIdTests(CustomWebApplicationFactory factory) : base(factory) { }

    // ── Encabezado presente en cada respuesta ───────────────────────────────

    [Fact]
    public async Task SuccessfulResponse_HasXRequestIdHeader()
    {
        var response = await Client.GetAsync("/health");

        response.Headers.Contains("X-Request-ID").Should().BeTrue();
        response.Headers.GetValues("X-Request-ID").First().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task UnauthorizedResponse_HasXRequestIdHeader()
    {
        var response = await Client.GetAsync("/api/professors");

        response.Headers.Contains("X-Request-ID").Should().BeTrue();
    }

    // ── El ID proporcionado por el cliente se propaga ───────────────────────

    [Fact]
    public async Task ClientSuppliedRequestId_IsEchoedBackOnSuccess()
    {
        var clientId = Guid.NewGuid().ToString("N");

        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Request-ID", clientId);

        var response = await Client.SendAsync(request);

        response.Headers.GetValues("X-Request-ID").First().Should().Be(clientId);
    }

    [Fact]
    public async Task ClientSuppliedRequestId_IsEchoedBackOnError()
    {
        var clientId = Guid.NewGuid().ToString("N");
        var client   = CreateAuthenticatedClient("Admin");

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/professors/{Guid.NewGuid()}");
        request.Headers.Add("X-Request-ID", clientId);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken("Admin"));

        var response = await client.SendAsync(request);

        response.Headers.GetValues("X-Request-ID").First().Should().Be(clientId);
    }

    // ── El encabezado de respuesta coincide con el cuerpo del error JSON ────

    [Fact]
    public async Task ErrorBody_RequestIdMatchesResponseHeader()
    {
        var clientId = Guid.NewGuid().ToString("N");
        var client   = CreateAuthenticatedClient("Admin");

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/professors/{Guid.NewGuid()}");
        request.Headers.Add("X-Request-ID", clientId);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken("Admin"));

        var response = await client.SendAsync(request);

        var headerValue = response.Headers.GetValues("X-Request-ID").First();
        headerValue.Should().Be(clientId);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.TryGetProperty("requestId", out var requestIdProp).Should().BeTrue();
        requestIdProp.GetString().Should().Be(clientId);
    }

    // ── Sin ID del cliente — el servidor genera uno ─────────────────────────

    [Fact]
    public async Task NoClientRequestId_ServerGeneratesOne()
    {
        var response1 = await Client.GetAsync("/health");
        var response2 = await Client.GetAsync("/health");

        var id1 = response1.Headers.GetValues("X-Request-ID").First();
        var id2 = response2.Headers.GetValues("X-Request-ID").First();

        id1.Should().NotBeNullOrWhiteSpace();
        id2.Should().NotBeNullOrWhiteSpace();
        id1.Should().NotBe(id2);
    }
}
