using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SistemaEvaluacionAcademica.Infrastructure.Data;

namespace SistemaEvaluacionAcademica.IntegrationTests.API;

/// <summary>
/// Tests de seguridad para rate limiting y CORS.
/// Usan fábricas ligeras sin TestContainers — InMemory DB es suficiente.
/// </summary>
public class SecurityTests
{
    private const string TestSecret = "SecurityTestsSecretKey_AtLeast32Chars!!";

    private static WebApplicationFactory<Program> BuildFactory(
        Action<IServiceCollection>? configureServices = null)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            // Override JWT secret para que no falle la validación de startup
            builder.ConfigureAppConfiguration((_, cfg) =>
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:SecretKey"] = TestSecret,
                }));
            builder.ConfigureServices(services =>
            {
                // Reemplazar SQL Server con InMemory para no necesitar Docker
                var existing = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (existing is not null)
                    services.Remove(existing);
                services.AddDbContext<AppDbContext>(opts =>
                    opts.UseInMemoryDatabase("SecurityTestsDb"));

                // Configuración adicional específica del test
                configureServices?.Invoke(services);
            });
        });
    }

    private static StringContent LoginBody() =>
        new("""{"email":"test@test.com","password":"Password123!"}""",
            Encoding.UTF8, "application/json");

    // ── Security headers ─────────────────────────────────────────────────────

    [Fact]
    public async Task SecurityHeaders_XContentTypeOptions_IsNoSniff()
    {
        using var factory = BuildFactory();
        var response = await factory.CreateClient().PostAsync("/api/auth/login", LoginBody());
        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
    }

    [Fact]
    public async Task SecurityHeaders_XFrameOptions_IsDeny()
    {
        using var factory = BuildFactory();
        var response = await factory.CreateClient().PostAsync("/api/auth/login", LoginBody());
        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");
    }

    [Fact]
    public async Task SecurityHeaders_ReferrerPolicy_IsNoReferrer()
    {
        using var factory = BuildFactory();
        var response = await factory.CreateClient().PostAsync("/api/auth/login", LoginBody());
        response.Headers.Should().ContainKey("Referrer-Policy");
        response.Headers.GetValues("Referrer-Policy").Should().Contain("no-referrer");
    }

    [Fact]
    public async Task SecurityHeaders_ContentSecurityPolicy_IsPresent()
    {
        using var factory = BuildFactory();
        var response = await factory.CreateClient().PostAsync("/api/auth/login", LoginBody());
        response.Headers.Should().ContainKey("Content-Security-Policy");
    }

    [Fact]
    public async Task SecurityHeaders_ContentSecurityPolicy_BlocksFraming()
    {
        using var factory = BuildFactory();
        var response = await factory.CreateClient().PostAsync("/api/auth/login", LoginBody());
        var csp = response.Headers.GetValues("Content-Security-Policy").First();
        csp.Should().Contain("frame-ancestors 'none'");
    }

    [Fact]
    public async Task SecurityHeaders_ContentSecurityPolicy_BlocksPlugins()
    {
        using var factory = BuildFactory();
        var response = await factory.CreateClient().PostAsync("/api/auth/login", LoginBody());
        var csp = response.Headers.GetValues("Content-Security-Policy").First();
        csp.Should().Contain("object-src 'none'");
    }

    [Fact]
    public async Task SecurityHeaders_ContentSecurityPolicy_RestrictsBaseUri()
    {
        using var factory = BuildFactory();
        var response = await factory.CreateClient().PostAsync("/api/auth/login", LoginBody());
        var csp = response.Headers.GetValues("Content-Security-Policy").First();
        csp.Should().Contain("base-uri 'self'");
    }

    // ── Rate Limiting ────────────────────────────────────────────────────────

    /// <summary>
    /// Envía 6 requests contra el límite por defecto de 5 por minuto.
    /// Los primeros 5 pasan (devuelven 401 — usuario inexistente en InMemory DB).
    /// El sexto es rechazado por el rate limiter.
    /// </summary>
    [Fact]
    public async Task Login_WhenRateLimitExceeded_Returns429()
    {
        using var factory = BuildFactory();
        var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
            await client.PostAsync("/api/auth/login", LoginBody());

        var response = await client.PostAsync("/api/auth/login", LoginBody());

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Login_WhenRateLimitExceeded_ResponseIncludesRetryAfterHeader()
    {
        using var factory = BuildFactory();
        var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
            await client.PostAsync("/api/auth/login", LoginBody());

        var response = await client.PostAsync("/api/auth/login", LoginBody());

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        response.Headers.Should().ContainKey("Retry-After");
    }

    // ── CORS ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sobreescribe la política CORS "Default" vía ConfigureServices (después de
    /// que Program.cs la registra) para que apunte a un origen específico.
    /// </summary>
    [Fact]
    public async Task Cors_WithAllowedOrigin_ReturnsAccessControlHeader()
    {
        using var factory = BuildFactory(services =>
            services.AddCors(opts =>
                opts.AddPolicy("Default", policy =>
                    policy.WithOrigins("https://allowed-domain.com")
                          .AllowAnyMethod()
                          .AllowAnyHeader())));

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login");
        request.Headers.Add("Origin", "https://allowed-domain.com");
        request.Content = LoginBody();

        var response = await factory.CreateClient().SendAsync(request);

        response.Headers.Should().ContainKey("Access-Control-Allow-Origin");
        response.Headers.GetValues("Access-Control-Allow-Origin")
            .Should().Contain("https://allowed-domain.com");
    }

    [Fact]
    public async Task Cors_WithUnknownOrigin_DoesNotReturnAccessControlHeader()
    {
        using var factory = BuildFactory(services =>
            services.AddCors(opts =>
                opts.AddPolicy("Default", policy =>
                    policy.WithOrigins("https://allowed-domain.com")
                          .AllowAnyMethod()
                          .AllowAnyHeader())));

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login");
        request.Headers.Add("Origin", "https://evil-domain.com");
        request.Content = LoginBody();

        var response = await factory.CreateClient().SendAsync(request);

        response.Headers.Should().NotContainKey("Access-Control-Allow-Origin");
    }

    // ── Per-IP Rate Limiting ─────────────────────────────────────────────────

    /// <summary>
    /// Middleware inyectado vía IStartupFilter que lee X-Test-Remote-IP y establece
    /// HttpContext.Connection.RemoteIpAddress — permite pruebas de partición por IP
    /// sin modificar el código de producción.
    /// </summary>
    private sealed class RemoteIpOverrideStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            app =>
            {
                app.Use(async (ctx, nx) =>
                {
                    if (ctx.Request.Headers.TryGetValue("X-Test-Remote-IP", out var ipStr) &&
                        IPAddress.TryParse(ipStr.ToString(), out var addr))
                    {
                        ctx.Connection.RemoteIpAddress = addr;
                    }
                    await nx(ctx);
                });
                next(app);
            };
    }

    private static WebApplicationFactory<Program> BuildFactoryWithIpSimulation() =>
        BuildFactory(services =>
            services.AddSingleton<IStartupFilter, RemoteIpOverrideStartupFilter>());

    /// <summary>
    /// Verifica el aislamiento por IP: agotar la ventana de IP_A (429) NO debe bloquear a IP_B.
    /// </summary>
    [Fact]
    public async Task Login_WhenDifferentIpExceedsLimit_OtherIpIsNotBlocked()
    {
        using var factory = BuildFactoryWithIpSimulation();
        var client = factory.CreateClient();

        // Agotar la ventana de 5 peticiones para IP_A
        for (var i = 0; i < 5; i++)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login");
            req.Headers.Add("X-Test-Remote-IP", "10.0.0.1");
            req.Content = LoginBody();
            await client.SendAsync(req);
        }

        // La 6ª petición de IP_A debe ser bloqueada
        var blockedReq = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login");
        blockedReq.Headers.Add("X-Test-Remote-IP", "10.0.0.1");
        blockedReq.Content = LoginBody();
        var blockedRes = await client.SendAsync(blockedReq);
        blockedRes.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        // La primera petición de IP_B NO debe ser bloqueada
        var ipBReq = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login");
        ipBReq.Headers.Add("X-Test-Remote-IP", "10.0.0.2");
        ipBReq.Content = LoginBody();
        var ipBRes = await client.SendAsync(ipBReq);
        ipBRes.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
    }

    /// <summary>
    /// Verifica que el endpoint de login es accesible antes de alcanzar el límite de tasa
    /// (devuelve 401 por credenciales inválidas, no 429).
    /// </summary>
    [Fact]
    public async Task Login_BeforeRateLimitIsReached_RequestIsProcessedNormally()
    {
        using var factory = BuildFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/auth/login", LoginBody());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
