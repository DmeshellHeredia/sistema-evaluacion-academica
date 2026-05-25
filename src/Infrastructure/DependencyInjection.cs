using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SistemaEvaluacionAcademica.Application.Interfaces;
using SistemaEvaluacionAcademica.Application.Settings;
using SistemaEvaluacionAcademica.Domain.Interfaces;
using SistemaEvaluacionAcademica.Infrastructure.Data;
using SistemaEvaluacionAcademica.Infrastructure.Repositories;
using SistemaEvaluacionAcademica.Infrastructure.Services;
using SistemaEvaluacionAcademica.Infrastructure.Settings;

namespace SistemaEvaluacionAcademica.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Base de datos
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
            ));

        // Repositorios
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<ISubjectRepository, SubjectRepository>();
        services.AddScoped<IGradeRepository, GradeRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();

        // Unidad de trabajo
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Configuración académica
        services.Configure<AcademicSettings>(configuration.GetSection("AcademicSettings"));

        // JWT
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.AddScoped<IJwtService, JwtService>();

        // Hashing de contraseñas
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();

        return services;
    }
}
