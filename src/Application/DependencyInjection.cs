using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SistemaEvaluacionAcademica.Application.Interfaces;
using SistemaEvaluacionAcademica.Application.Services;

namespace SistemaEvaluacionAcademica.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IGradeService, GradeService>();
        services.AddScoped<ISubjectService, SubjectService>();
        services.AddScoped<IProfessorService, ProfessorService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<ISectionService, SectionService>();
        services.AddScoped<IAcademicPeriodService, AcademicPeriodService>();
        services.AddScoped<ICourseService, CourseService>();

        // FluentValidation - registra todos los validadores del assembly
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
