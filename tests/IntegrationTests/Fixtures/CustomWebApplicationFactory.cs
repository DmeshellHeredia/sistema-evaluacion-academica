using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SistemaEvaluacionAcademica.Infrastructure.Data;
using SistemaEvaluacionAcademica.Infrastructure.Data.Seeders;
using SistemaEvaluacionAcademica.Infrastructure.Settings;
using Testcontainers.MsSql;

namespace SistemaEvaluacionAcademica.IntegrationTests.Fixtures;

/// <summary>
/// WebApplicationFactory con SQL Server en contenedor Docker.
/// Una sola instancia por colección de tests — la BD se crea una vez
/// y persiste durante toda la ejecución de la suite de integración.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // JWT secret compartido entre la factory y los tests
    public const string TestJwtSecret = "TestSecretKeyParaIntegrationTests_MuySegura_2024";

    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Integration@Test123!")
        .Build();

    // ── IAsyncLifetime ───────────────────────────────────────────────────────

    async Task IAsyncLifetime.InitializeAsync()
    {
        // 1. Levantar el contenedor SQL Server
        await _sqlContainer.StartAsync();

        // 2. Acceder a Services dispara ConfigureWebHost, que ya usa la
        //    cadena de conexión del contenedor (el contenedor ya está corriendo).
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 3. Aplicar migraciones y sembrar datos
        await db.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(db);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
    }

    // ── WebApplicationFactory ────────────────────────────────────────────────

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Inyectar configuración de prueba: secreto JWT válido y rate limit alto
            // para que los tests normales no activen el límite de 5 req/min
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = TestJwtSecret,
                ["RateLimit:LoginPermitLimit"] = "1000",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Reemplazar el DbContext real con uno apuntando al contenedor
            var existing = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (existing is not null)
                services.Remove(existing);

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(_sqlContainer.GetConnectionString()));

            // ConfigureAppConfiguration no siempre sobrescribe claves leídas en Program.cs
            // con el modelo de hosting mínimo — usamos PostConfigure para garantizarlo.
            services.PostConfigure<JwtSettings>(opts => opts.SecretKey = TestJwtSecret);
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                opts => opts.TokenValidationParameters.IssuerSigningKey =
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret)));
            services.PostConfigure<RateLimitSettings>(opts => opts.LoginPermitLimit = 10_000);
        });
    }
}
