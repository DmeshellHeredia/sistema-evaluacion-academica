using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SistemaEvaluacionAcademica.Application.Common;
using SistemaEvaluacionAcademica.Application.DTOs.Courses;
using SistemaEvaluacionAcademica.Application.DTOs.Enrollments;
using SistemaEvaluacionAcademica.Application.DTOs.Sections;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Enums;
using SistemaEvaluacionAcademica.IntegrationTests.Fixtures;

namespace SistemaEvaluacionAcademica.IntegrationTests.API;

/// <summary>
/// Verifica que los endpoints devuelvan 200 (no 500) y una cadena de respaldo cuando el
/// profesor de una sección ha sido eliminado lógicamente, dejando la propiedad de navegación nula en tiempo de ejecución.
/// </summary>
[Trait("Category", "Integration")]
public class NullProfessorTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public NullProfessorTests(CustomWebApplicationFactory factory) : base(factory) { }

    // ── GET /api/sections ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetSections_WhenProfessorDeactivated_Returns200WithFallbackName()
    {
        var (sectionId, sectionCode) = await CreateSectionWithDeactivatedProfessorAsync("NP-SEC-A");

        var adminUserId = await ExecuteInScopeAsync(async db =>
            (await db.Users.FirstAsync(u => u.Email == "admin@academia.com")).Id);
        var client = CreateAuthenticatedClient("Admin", userId: adminUserId);

        var response = await client.GetAsync($"/api/sections?search={sectionCode}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var paged = JsonSerializer.Deserialize<PagedResult<SectionDto>>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        paged.Should().NotBeNull();
        paged!.Items.Should().NotBeEmpty();
        paged.Items.Should().OnlyContain(s => s.ProfessorName == "Sin profesor asignado");
    }

    // ── GET /api/enrollments/schedule/me ─────────────────────────────────────

    [Fact]
    public async Task GetMySchedule_WhenProfessorDeactivated_Returns200WithFallbackName()
    {
        var (sectionId, _) = await CreateSectionWithDeactivatedProfessorAsync("NP-SCH-A");

        // Resolver el estudiante de IS (carlos.ruiz, EST-2025-A002, sem 1) usado como destino de inscripción
        var (studentId, studentUserId) = await ExecuteInScopeAsync(async db =>
        {
            var student = await db.Students.FirstAsync(s => s.StudentCode == "EST-2025-A002");
            return (student.Id, student.UserId);
        });

        // Enroll the student directly in the DB — bypasses enrollment-period gate
        await ExecuteInScopeAsync(async db =>
        {
            var stale = await db.SectionEnrollments
                .Where(e => e.StudentId == studentId && e.SectionId == sectionId)
                .ToListAsync();
            foreach (var e in stale) e.Deactivate();

            await db.SectionEnrollments.AddAsync(new SectionEnrollment(studentId, sectionId));
            await db.SaveChangesAsync();
            return true;
        });

        var client = CreateAuthenticatedClient("Estudiante", userId: studentUserId);
        var response = await client.GetAsync("/api/enrollments/schedule/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var schedule = JsonSerializer.Deserialize<StudentScheduleDto>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        schedule.Should().NotBeNull();
        var enrolledSection = schedule!.Sections.FirstOrDefault(s => s.SectionId == sectionId);
        enrolledSection.Should().NotBeNull("the enrolled section must appear in the schedule");
        enrolledSection!.ProfessorName.Should().Be("Sin profesor asignado");
    }

    // ── GET /api/courses/{sectionId} ─────────────────────────────────────────

    [Fact]
    public async Task GetCourseOverview_WhenProfessorDeactivated_Returns200WithFallbackName()
    {
        var (sectionId, _) = await CreateSectionWithDeactivatedProfessorAsync("NP-OVW-A");

        var client = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync($"/api/courses/{sectionId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = JsonSerializer.Deserialize<CourseOverviewDto>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        dto.Should().NotBeNull();
        dto!.ProfessorName.Should().Be("Sin profesor asignado");
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea un usuario profesor temporal, crea una sección IS-ALG1 con ese profesor,
    /// luego elimina lógicamente al profesor para que la propiedad de navegación sea nula en tiempo de ejecución.
    /// Devuelve el ID de sección y el código de sección.
    /// </summary>
    private Task<(Guid sectionId, string sectionCode)> CreateSectionWithDeactivatedProfessorAsync(
        string sectionCode) =>
        ExecuteInScopeAsync(async db =>
        {
            var profesorRoleId = (await db.Roles.FirstAsync(r => r.Name == "Profesor")).Id;

            var professor = new User(
                $"deact.{Guid.NewGuid():N}@test.com",
                "placeholder-hash",
                "Deactivated", "Professor",
                profesorRoleId);
            await db.Users.AddAsync(professor);
            await db.SaveChangesAsync();

            var subject = await db.Subjects.FirstAsync(s => s.Code == "IS-ALG1");
            var section = new SubjectSection(
                subject.Id, professor.Id, sectionCode,
                DayOfWeekType.Sabado, new TimeOnly(10, 0), new TimeOnly(12, 0),
                "Virtual", 30);
            await db.SubjectSections.AddAsync(section);
            await db.SaveChangesAsync();

            // Eliminar lógicamente al profesor: el filtro global de EF Core lo excluirá ahora
            // de las cargas de propiedades de navegación, dejando section.Professor == null.
            professor.Deactivate();
            await db.SaveChangesAsync();

            return (section.Id, sectionCode);
        });
}
