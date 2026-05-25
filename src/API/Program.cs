using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SistemaEvaluacionAcademica.API.Filters;
using SistemaEvaluacionAcademica.API.Middleware;
using SistemaEvaluacionAcademica.Application;
using SistemaEvaluacionAcademica.Domain.Interfaces;
using SistemaEvaluacionAcademica.Infrastructure;
using SistemaEvaluacionAcademica.Infrastructure.Data;
using SistemaEvaluacionAcademica.Infrastructure.Data.Seeders;
using SistemaEvaluacionAcademica.Infrastructure.Settings;

var builder = WebApplication.CreateBuilder(args);

// Capas de Clean Architecture
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Controladores + serialización JSON
builder.Services.AddControllers(options => options.Filters.Add<RequestIdFilter>())
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Autenticación JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;

// Falla rápido en startup si el secreto falta o es muy corto (< 32 chars en cualquier entorno, placeholder en Production)
JwtSettingsValidator.Validate(jwtSettings.SecretKey, builder.Environment.IsProduction());

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ValidateIssuer   = true,
        ValidIssuer      = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience    = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew        = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            // Skip stamp validation in Testing environment — integration tests generate synthetic JWTs
            var env = context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
            if (env.IsEnvironment("Testing"))
                return;

            var stampClaim = context.Principal?.FindFirst("sec_stamp")?.Value;
            var userIdStr  = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (stampClaim is null || !Guid.TryParse(userIdStr, out var userId))
            {
                context.Fail("Token inválido.");
                return;
            }

            using var scope = context.HttpContext.RequestServices.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var user  = await users.Users.GetByIdAsync(userId, context.HttpContext.RequestAborted);

            if (user is null || user.SecurityStamp.ToString() != stampClaim)
                context.Fail("Sesión invalidada. Inicia sesión de nuevo.");
        },
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync(
                """{"statusCode":401,"message":"Token JWT requerido o inválido."}""");
        },
        OnForbidden = context =>
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync(
                """{"statusCode":403,"message":"No tienes permisos para acceder a este recurso."}""");
        }
    };
});

builder.Services.AddAuthorization();

// Rate limiting — ventana fija por IP en /api/auth/login (5 req/min en producción)
// IOptions permite que PostConfigure sobreescriba el límite en pruebas sin reiniciar la aplicación.
builder.Services.Configure<RateLimitSettings>(
    builder.Configuration.GetSection("RateLimit"));

builder.Services.AddRateLimiter(opts =>
{
    opts.AddPolicy<string>("LoginPolicy", httpContext =>
    {
        var settings = httpContext.RequestServices
            .GetRequiredService<IOptions<RateLimitSettings>>();
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = settings.Value.LoginPermitLimit,
                Window      = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit  = 0,
            });
    });

    opts.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        context.HttpContext.Response.ContentType = "application/json";

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString();

        await context.HttpContext.Response.WriteAsync(
            """{"statusCode":429,"message":"Demasiados intentos de login. Por favor espera un momento."}""",
            cancellationToken);
    };
});

// CORS — orígenes desde configuración
// Development: appsettings.Development.json lista los puertos localhost
// Production:  establecer Cors__AllowedOrigins__0=https://tu-dominio.com vía variable de entorno
// Lista vacía en Production = CORS denegado (falla cerrado)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader();
        else if (builder.Environment.IsDevelopment())
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// Swagger (solo en Development — se sirve en la raíz "/")
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "Sistema de Evaluación Académica",
        Version = "v1",
        Description = """
            API RESTful para gestión académica.

            **Roles disponibles:** Admin, Profesor, Estudiante

            **Flujo de uso:**
            1. POST /api/auth/login → obtener token JWT
            2. Copiar el token y pegarlo en el botón 'Authorize'
            3. Usar los endpoints protegidos
            """,
        Contact = new OpenApiContact
        {
            Name  = "Equipo de Desarrollo",
            Email = "dev@academia.com"
        }
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Pega el token JWT aquí. No incluyas 'Bearer', Swagger lo agrega automáticamente."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// Health checks — GET /health devuelve el estado de conectividad con la BD
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

// Reverse-proxy support (Railway, Render, Azure App Service)
// Permite que UseHttpsRedirection funcione correctamente cuando TLS termina en el proxy.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// Pipeline de middleware (el orden importa)
var app = builder.Build();

// Cabeceras de seguridad en todas las respuestas — debe ejecutarse antes de cualquier middleware que escriba respuestas.
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"]        = "DENY";
    ctx.Response.Headers["Referrer-Policy"]        = "no-referrer";

    // Development: relajada para Swagger UI (usa eval e inline scripts/styles).
    // Non-Development: estricta — solo recursos del mismo origen, sin plugins ni framing.
    ctx.Response.Headers["Content-Security-Policy"] = app.Environment.IsDevelopment()
        ? "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; object-src 'none'; base-uri 'self'"
        : "default-src 'self'; frame-ancestors 'none'; object-src 'none'; base-uri 'self'";

    await next(ctx);
});

app.UseForwardedHeaders();
app.UseMiddleware<RequestIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Academia API v1");
        c.RoutePrefix = string.Empty;
        c.DisplayRequestDuration();
    });
}

app.UseHttpsRedirection();
app.UseCors("Default");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

// Auto-migrar y sembrar en startup (idempotente — seguro correr en cada arranque).
// Aplica migraciones pendientes y siembra datos demo. El seeder no duplica registros.
{
    using var scope = app.Services.CreateScope();
    var db            = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    await MigrateWithRetryAsync(db, startupLogger);
    await DatabaseSeeder.SeedAsync(db);
}

app.Run();

static async Task MigrateWithRetryAsync(AppDbContext db, ILogger logger, int maxAttempts = 5)
{
    // InMemory y otros proveedores no relacionales no soportan migraciones
    if (!db.Database.IsRelational())
        return;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            return;
        }
        catch (Exception ex)
        {
            if (attempt == maxAttempts)
            {
                Console.Error.WriteLine($"""

                    ╔════════════════════════════════════════════════════════════════╗
                    ║  Error de startup: SQL Server no disponible                  ║
                    ╠════════════════════════════════════════════════════════════════╣
                    ║                                                                ║
                    ║  Solución:                                                     ║
                    ║    1. docker compose up -d                                     ║
                    ║    2. docker ps  →  esperar status "healthy" (~20 s)          ║
                    ║    3. dotnet run  (en src/API)                                 ║
                    ║                                                                ║
                    ║  Puerto:  localhost,1444  (SQL Server en Docker)             ║
                    ║  Config:  src/API/appsettings.Development.json               ║
                    ╚════════════════════════════════════════════════════════════════╝

                    Error original: {ex.GetType().Name}: {ex.Message}
                    """);
                throw;
            }

            var delay = attempt * 3;
            logger.LogWarning(
                "[Startup] SQL Server no responde (intento {Attempt}/{Max}). " +
                "Reintentando en {Delay}s... Ejecuta 'docker compose up -d' si no lo has hecho.",
                attempt, maxAttempts, delay);

            await Task.Delay(TimeSpan.FromSeconds(delay));
        }
    }
}

// Expone Program como public para WebApplicationFactory en las pruebas de integración
public partial class Program { }
