using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SistemaEvaluacionAcademica.Application.DTOs.Courses;
using SistemaEvaluacionAcademica.IntegrationTests.Fixtures;

namespace SistemaEvaluacionAcademica.IntegrationTests.API;

/// <summary>
/// Tests de integración HTTP para /api/courses.
/// Sección de referencia: IS-MAT1 (prof.garcia dueño, prof.lopez ajeno).
/// Estudiante inscrito: juan.perez. Estudiante ajeno: sofia.mendez.
/// </summary>
[Trait("Category", "Integration")]
public class CoursesEndpointsTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public CoursesEndpointsTests(CustomWebApplicationFactory factory) : base(factory) { }

    // ── GET /api/courses/{sectionId} — Overview ──────────────────────────────

    [Fact]
    public async Task GetOverview_WithoutToken_Returns401()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var response = await Client.GetAsync($"/api/courses/{sectionId}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOverview_AsAdmin_Returns200()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var client = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync($"/api/courses/{sectionId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOverview_AsAdmin_ReturnsCorrectPayload()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var client = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync($"/api/courses/{sectionId}");

        var dto = JsonSerializer.Deserialize<CourseOverviewDto>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        dto.Should().NotBeNull();
        dto!.SectionId.Should().Be(sectionId);
        dto.SubjectCode.Should().Be("IS-MAT1");
        dto.SubjectName.Should().Be("Matemáticas I");
        dto.ProfessorName.Should().Contain("García");
        dto.Capacity.Should().Be(30);
        dto.EnrolledCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetOverview_AsProfesorOwnSection_Returns200()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var profGarciaId = await GetUserIdByEmailAsync("prof.garcia@academia.com");
        var client = CreateAuthenticatedClient("Profesor", userId: profGarciaId);

        var response = await client.GetAsync($"/api/courses/{sectionId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOverview_AsProfesorAlienSection_Returns403()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var profLopezId = await GetUserIdByEmailAsync("prof.lopez@academia.com");
        var client = CreateAuthenticatedClient("Profesor", userId: profLopezId);

        var response = await client.GetAsync($"/api/courses/{sectionId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetOverview_AsEstudianteEnrolledInSection_Returns200()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var juanId = await GetUserIdByEmailAsync("juan.perez@academia.com");
        var client = CreateAuthenticatedClient("Estudiante", userId: juanId);

        var response = await client.GetAsync($"/api/courses/{sectionId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOverview_AsEstudianteNotEnrolledInSection_Returns403()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var sofiaId = await GetUserIdByEmailAsync("sofia.mendez@academia.com");
        var client = CreateAuthenticatedClient("Estudiante", userId: sofiaId);

        var response = await client.GetAsync($"/api/courses/{sectionId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetOverview_NonExistentSection_Returns404()
    {
        var client = CreateAuthenticatedClient("Admin");
        var response = await client.GetAsync($"/api/courses/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/courses/{sectionId}/participants ────────────────────────────

    [Fact]
    public async Task GetParticipants_AsAdmin_Returns200WithStudents()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var client = CreateAuthenticatedClient("Admin");

        var response = await client.GetAsync($"/api/courses/{sectionId}/participants");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var participants = JsonSerializer.Deserialize<ParticipantDto[]>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        participants.Should().NotBeNullOrEmpty();
        participants!.Should().Contain(p => p.Email == "juan.perez@academia.com");
        participants.Should().Contain(p => p.Email == "carlos.ruiz@academia.com");
    }

    [Fact]
    public async Task GetParticipants_AsProfesorOwnSection_Returns200()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var profGarciaId = await GetUserIdByEmailAsync("prof.garcia@academia.com");
        var client = CreateAuthenticatedClient("Profesor", userId: profGarciaId);

        var response = await client.GetAsync($"/api/courses/{sectionId}/participants");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetParticipants_AsProfesorAlienSection_Returns403()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var profLopezId = await GetUserIdByEmailAsync("prof.lopez@academia.com");
        var client = CreateAuthenticatedClient("Profesor", userId: profLopezId);

        var response = await client.GetAsync($"/api/courses/{sectionId}/participants");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetParticipants_AsEstudianteEnrolled_Returns200()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var juanId = await GetUserIdByEmailAsync("juan.perez@academia.com");
        var client = CreateAuthenticatedClient("Estudiante", userId: juanId);

        var response = await client.GetAsync($"/api/courses/{sectionId}/participants");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetParticipants_AsEstudianteNotEnrolled_Returns403()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var sofiaId = await GetUserIdByEmailAsync("sofia.mendez@academia.com");
        var client = CreateAuthenticatedClient("Estudiante", userId: sofiaId);

        var response = await client.GetAsync($"/api/courses/{sectionId}/participants");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/courses/{sectionId}/activities ─────────────────────────────

    [Fact]
    public async Task CreateActivity_AsEstudiante_Returns403()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var client = CreateAuthenticatedClient("Estudiante");
        var dto = new CreateActivityDto("Tarea", "Desc", "Tarea", null, 10m, 0.2m);

        var response = await client.PostAsJsonAsync($"/api/courses/{sectionId}/activities", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateActivity_AsProfesorOwnSection_Returns201WithPayload()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var profGarciaId = await GetUserIdByEmailAsync("prof.garcia@academia.com");
        var client = CreateAuthenticatedClient("Profesor", userId: profGarciaId);
        var dto = new CreateActivityDto("Tarea de integración", "Descripción de prueba", "Tarea", null, 10m, 0.2m);

        var response = await client.PostAsJsonAsync($"/api/courses/{sectionId}/activities", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = JsonSerializer.Deserialize<ActivityDto>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        created.Should().NotBeNull();
        created!.Title.Should().Be("Tarea de integración");
        created.SectionId.Should().Be(sectionId);
        created.MaxScore.Should().Be(10m);
        created.Weight.Should().Be(0.2m);
    }

    [Fact]
    public async Task CreateActivity_AsProfesorAlienSection_Returns403()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var profLopezId = await GetUserIdByEmailAsync("prof.lopez@academia.com");
        var client = CreateAuthenticatedClient("Profesor", userId: profLopezId);
        var dto = new CreateActivityDto("Tarea invasora", "Desc", "Tarea", null, 10m, 0.2m);

        var response = await client.PostAsJsonAsync($"/api/courses/{sectionId}/activities", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET /api/courses/{sectionId}/activities ──────────────────────────────

    [Fact]
    public async Task GetActivities_AsProfesorOwnSection_Returns200()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var profGarciaId = await GetUserIdByEmailAsync("prof.garcia@academia.com");

        // Crear una actividad primero para que la lista no esté vacía
        var profClient = CreateAuthenticatedClient("Profesor", userId: profGarciaId);
        await profClient.PostAsJsonAsync($"/api/courses/{sectionId}/activities",
            new CreateActivityDto("Actividad GET test", "Desc", "Tarea", null, 10m, 0.1m));

        var response = await profClient.GetAsync($"/api/courses/{sectionId}/activities");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var activities = JsonSerializer.Deserialize<ActivityDto[]>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        activities.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetActivities_AsEstudianteEnrolled_Returns200()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var juanId = await GetUserIdByEmailAsync("juan.perez@academia.com");
        var client = CreateAuthenticatedClient("Estudiante", userId: juanId);

        var response = await client.GetAsync($"/api/courses/{sectionId}/activities");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetActivities_AsEstudianteNotEnrolled_Returns403()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var sofiaId = await GetUserIdByEmailAsync("sofia.mendez@academia.com");
        var client = CreateAuthenticatedClient("Estudiante", userId: sofiaId);

        var response = await client.GetAsync($"/api/courses/{sectionId}/activities");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── PUT + DELETE /api/courses/activities/{id} ────────────────────────────

    [Fact]
    public async Task UpdateActivity_AsProfesorOwnSection_Returns200()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var profGarciaId = await GetUserIdByEmailAsync("prof.garcia@academia.com");
        var client = CreateAuthenticatedClient("Profesor", userId: profGarciaId);

        // Crear
        var createResp = await client.PostAsJsonAsync($"/api/courses/{sectionId}/activities",
            new CreateActivityDto("Actividad para editar", "Original", "Tarea", null, 10m, 0.3m));
        var created = JsonSerializer.Deserialize<ActivityDto>(
            await createResp.Content.ReadAsStringAsync(), JsonOpts)!;

        // Actualizar
        var updateDto = new UpdateActivityDto("Actividad editada", "Actualizada", "Evaluacion", null, 20m, 0.4m, true, null);
        var updateResp = await client.PutAsJsonAsync($"/api/courses/activities/{created.Id}", updateDto);

        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = JsonSerializer.Deserialize<ActivityDto>(
            await updateResp.Content.ReadAsStringAsync(), JsonOpts)!;
        updated.Title.Should().Be("Actividad editada");
        updated.MaxScore.Should().Be(20m);
    }

    [Fact]
    public async Task UpdateActivity_AsProfesorAlienSection_Returns403()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var profGarciaId = await GetUserIdByEmailAsync("prof.garcia@academia.com");
        var profLopezId = await GetUserIdByEmailAsync("prof.lopez@academia.com");

        // Crear con el profesor dueño
        var ownerClient = CreateAuthenticatedClient("Profesor", userId: profGarciaId);
        var createResp = await ownerClient.PostAsJsonAsync($"/api/courses/{sectionId}/activities",
            new CreateActivityDto("Actividad ajena", "Desc", "Tarea", null, 10m, 0.1m));
        var created = JsonSerializer.Deserialize<ActivityDto>(
            await createResp.Content.ReadAsStringAsync(), JsonOpts)!;

        // Intentar actualizar con profesor ajeno
        var alienClient = CreateAuthenticatedClient("Profesor", userId: profLopezId);
        var updateDto = new UpdateActivityDto("Hackeo", "Desc", "Tarea", null, 10m, 0.1m, true, null);
        var updateResp = await alienClient.PutAsJsonAsync($"/api/courses/activities/{created.Id}", updateDto);

        updateResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteActivity_AsProfesorOwnSection_Returns204()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var profGarciaId = await GetUserIdByEmailAsync("prof.garcia@academia.com");
        var client = CreateAuthenticatedClient("Profesor", userId: profGarciaId);

        // Crear
        var createResp = await client.PostAsJsonAsync($"/api/courses/{sectionId}/activities",
            new CreateActivityDto("Actividad para eliminar", "Desc", "Tarea", null, 10m, 0.1m));
        var created = JsonSerializer.Deserialize<ActivityDto>(
            await createResp.Content.ReadAsStringAsync(), JsonOpts)!;

        // Eliminar
        var deleteResp = await client.DeleteAsync($"/api/courses/activities/{created.Id}");

        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteActivity_AsProfesorAlienSection_Returns403()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var profGarciaId = await GetUserIdByEmailAsync("prof.garcia@academia.com");
        var profLopezId = await GetUserIdByEmailAsync("prof.lopez@academia.com");

        // Crear con el profesor dueño
        var ownerClient = CreateAuthenticatedClient("Profesor", userId: profGarciaId);
        var createResp = await ownerClient.PostAsJsonAsync($"/api/courses/{sectionId}/activities",
            new CreateActivityDto("Actividad a borrar ajena", "Desc", "Tarea", null, 5m, 0.1m));
        var created = JsonSerializer.Deserialize<ActivityDto>(
            await createResp.Content.ReadAsStringAsync(), JsonOpts)!;

        // Intentar eliminar con profesor ajeno
        var alienClient = CreateAuthenticatedClient("Profesor", userId: profLopezId);
        var deleteResp = await alienClient.DeleteAsync($"/api/courses/activities/{created.Id}");

        deleteResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/courses/{sectionId}/announcements ──────────────────────────

    [Fact]
    public async Task CreateAnnouncement_AsEstudiante_Returns403()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var client = CreateAuthenticatedClient("Estudiante");
        var dto = new CreateAnnouncementDto("Aviso", "Contenido");

        var response = await client.PostAsJsonAsync($"/api/courses/{sectionId}/announcements", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateAnnouncement_AsProfesorOwnSection_Returns201WithPayload()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var profGarciaId = await GetUserIdByEmailAsync("prof.garcia@academia.com");
        var client = CreateAuthenticatedClient("Profesor", userId: profGarciaId);
        var dto = new CreateAnnouncementDto("Aviso de integración", "Contenido del aviso de prueba");

        var response = await client.PostAsJsonAsync($"/api/courses/{sectionId}/announcements", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = JsonSerializer.Deserialize<AnnouncementDto>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        created.Should().NotBeNull();
        created!.Title.Should().Be("Aviso de integración");
        created.SectionId.Should().Be(sectionId);
        created.AuthorId.Should().Be(profGarciaId);
    }

    [Fact]
    public async Task CreateAnnouncement_AsProfesorAlienSection_Returns403()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var profLopezId = await GetUserIdByEmailAsync("prof.lopez@academia.com");
        var client = CreateAuthenticatedClient("Profesor", userId: profLopezId);
        var dto = new CreateAnnouncementDto("Aviso invasor", "Contenido");

        var response = await client.PostAsJsonAsync($"/api/courses/{sectionId}/announcements", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET /api/courses/{sectionId}/announcements ───────────────────────────

    [Fact]
    public async Task GetAnnouncements_AsEstudianteEnrolled_Returns200()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var juanId = await GetUserIdByEmailAsync("juan.perez@academia.com");
        var client = CreateAuthenticatedClient("Estudiante", userId: juanId);

        var response = await client.GetAsync($"/api/courses/{sectionId}/announcements");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAnnouncements_AsEstudianteNotEnrolled_Returns403()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var sofiaId = await GetUserIdByEmailAsync("sofia.mendez@academia.com");
        var client = CreateAuthenticatedClient("Estudiante", userId: sofiaId);

        var response = await client.GetAsync($"/api/courses/{sectionId}/announcements");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAnnouncements_CreatedAnnouncementAppearsInList()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var profGarciaId = await GetUserIdByEmailAsync("prof.garcia@academia.com");
        var profClient = CreateAuthenticatedClient("Profesor", userId: profGarciaId);

        // Crear
        var uniqueTitle = $"Aviso list-test {Guid.NewGuid():N}";
        await profClient.PostAsJsonAsync($"/api/courses/{sectionId}/announcements",
            new CreateAnnouncementDto(uniqueTitle, "Cuerpo del aviso"));

        // Listar como estudiante inscrito
        var juanId = await GetUserIdByEmailAsync("juan.perez@academia.com");
        var studentClient = CreateAuthenticatedClient("Estudiante", userId: juanId);
        var listResp = await studentClient.GetAsync($"/api/courses/{sectionId}/announcements");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var announcements = JsonSerializer.Deserialize<AnnouncementDto[]>(
            await listResp.Content.ReadAsStringAsync(), JsonOpts);

        announcements.Should().Contain(a => a.Title == uniqueTitle);
    }

    // ── PUT + DELETE /api/courses/announcements/{id} ─────────────────────────

    [Fact]
    public async Task UpdateAnnouncement_AsProfesorOwnSection_Returns200()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var profGarciaId = await GetUserIdByEmailAsync("prof.garcia@academia.com");
        var client = CreateAuthenticatedClient("Profesor", userId: profGarciaId);

        // Crear
        var createResp = await client.PostAsJsonAsync($"/api/courses/{sectionId}/announcements",
            new CreateAnnouncementDto("Aviso a editar", "Original"));
        var created = JsonSerializer.Deserialize<AnnouncementDto>(
            await createResp.Content.ReadAsStringAsync(), JsonOpts)!;

        // Actualizar
        var updateResp = await client.PutAsJsonAsync($"/api/courses/announcements/{created.Id}",
            new UpdateAnnouncementDto("Aviso editado", "Actualizado", true));

        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = JsonSerializer.Deserialize<AnnouncementDto>(
            await updateResp.Content.ReadAsStringAsync(), JsonOpts)!;
        updated.Title.Should().Be("Aviso editado");
    }

    [Fact]
    public async Task UpdateAnnouncement_AsProfesorAlienSection_Returns403()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var profGarciaId = await GetUserIdByEmailAsync("prof.garcia@academia.com");
        var profLopezId = await GetUserIdByEmailAsync("prof.lopez@academia.com");

        var ownerClient = CreateAuthenticatedClient("Profesor", userId: profGarciaId);
        var createResp = await ownerClient.PostAsJsonAsync($"/api/courses/{sectionId}/announcements",
            new CreateAnnouncementDto("Aviso ajeno", "Desc"));
        var created = JsonSerializer.Deserialize<AnnouncementDto>(
            await createResp.Content.ReadAsStringAsync(), JsonOpts)!;

        var alienClient = CreateAuthenticatedClient("Profesor", userId: profLopezId);
        var updateResp = await alienClient.PutAsJsonAsync($"/api/courses/announcements/{created.Id}",
            new UpdateAnnouncementDto("Hackeo", "Desc", true));

        updateResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteAnnouncement_AsProfesorOwnSection_Returns204()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var profGarciaId = await GetUserIdByEmailAsync("prof.garcia@academia.com");
        var client = CreateAuthenticatedClient("Profesor", userId: profGarciaId);

        // Crear
        var createResp = await client.PostAsJsonAsync($"/api/courses/{sectionId}/announcements",
            new CreateAnnouncementDto("Aviso a eliminar", "Desc"));
        var created = JsonSerializer.Deserialize<AnnouncementDto>(
            await createResp.Content.ReadAsStringAsync(), JsonOpts)!;

        // Eliminar
        var deleteResp = await client.DeleteAsync($"/api/courses/announcements/{created.Id}");

        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── Submissions — GET /api/courses/activities/{id}/submissions ────────────

    [Fact]
    public async Task GetSubmissions_AsProfesorOwnSection_Returns200()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var profGarciaId = await GetUserIdByEmailAsync("prof.garcia@academia.com");
        var profClient = CreateAuthenticatedClient("Profesor", userId: profGarciaId);

        // Crear actividad
        var actResp = await profClient.PostAsJsonAsync($"/api/courses/{sectionId}/activities",
            new CreateActivityDto("Actividad submissions test", "Desc", "Tarea", null, 10m, 0.1m));
        var activity = JsonSerializer.Deserialize<ActivityDto>(
            await actResp.Content.ReadAsStringAsync(), JsonOpts)!;

        // Obtener entregas (inicialmente vacío, pero 200 OK)
        var resp = await profClient.GetAsync($"/api/courses/activities/{activity.Id}/submissions");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var submissions = JsonSerializer.Deserialize<SubmissionDto[]>(
            await resp.Content.ReadAsStringAsync(), JsonOpts);
        submissions.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSubmissions_AsProfesorAlienSection_Returns403()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var profGarciaId = await GetUserIdByEmailAsync("prof.garcia@academia.com");
        var profLopezId = await GetUserIdByEmailAsync("prof.lopez@academia.com");

        // El profesor dueño crea la actividad
        var ownerClient = CreateAuthenticatedClient("Profesor", userId: profGarciaId);
        var actResp = await ownerClient.PostAsJsonAsync($"/api/courses/{sectionId}/activities",
            new CreateActivityDto("Actividad submissions ajena", "Desc", "Tarea", null, 10m, 0.1m));
        var activity = JsonSerializer.Deserialize<ActivityDto>(
            await actResp.Content.ReadAsStringAsync(), JsonOpts)!;

        // El profesor ajeno intenta leer las entregas
        var alienClient = CreateAuthenticatedClient("Profesor", userId: profLopezId);
        var resp = await alienClient.GetAsync($"/api/courses/activities/{activity.Id}/submissions");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Submissions — POST submit + GET my-submissions ───────────────────────

    [Fact]
    public async Task Submit_AsEstudianteEnrolled_Returns200()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var profGarciaId = await GetUserIdByEmailAsync("prof.garcia@academia.com");
        var juanId = await GetUserIdByEmailAsync("juan.perez@academia.com");

        // Crear actividad (publicada)
        var profClient = CreateAuthenticatedClient("Profesor", userId: profGarciaId);
        var actResp = await profClient.PostAsJsonAsync($"/api/courses/{sectionId}/activities",
            new CreateActivityDto("Actividad para submit", "Desc", "Tarea",
                DateTime.UtcNow.AddDays(7), 10m, 0.2m, IsPublished: true));
        var activity = JsonSerializer.Deserialize<ActivityDto>(
            await actResp.Content.ReadAsStringAsync(), JsonOpts)!;

        // El estudiante entrega
        var studentClient = CreateAuthenticatedClient("Estudiante", userId: juanId);
        var submitResp = await studentClient.PostAsJsonAsync(
            $"/api/courses/activities/{activity.Id}/submit",
            new SubmitDto("Mi respuesta de integración"));

        submitResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Submit_AsEstudianteNotEnrolled_Returns403()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var profGarciaId = await GetUserIdByEmailAsync("prof.garcia@academia.com");
        var sofiaId = await GetUserIdByEmailAsync("sofia.mendez@academia.com");

        // Crear actividad
        var profClient = CreateAuthenticatedClient("Profesor", userId: profGarciaId);
        var actResp = await profClient.PostAsJsonAsync($"/api/courses/{sectionId}/activities",
            new CreateActivityDto("Actividad ajena submit", "Desc", "Tarea", null, 10m, 0.1m));
        var activity = JsonSerializer.Deserialize<ActivityDto>(
            await actResp.Content.ReadAsStringAsync(), JsonOpts)!;

        // Estudiante no inscrito intenta entregar
        var sofiaClient = CreateAuthenticatedClient("Estudiante", userId: sofiaId);
        var submitResp = await sofiaClient.PostAsJsonAsync(
            $"/api/courses/activities/{activity.Id}/submit",
            new SubmitDto("Intento de submit sin inscripción"));

        submitResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetMySubmissions_AsEstudianteEnrolled_Returns200()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var juanId = await GetUserIdByEmailAsync("juan.perez@academia.com");
        var client = CreateAuthenticatedClient("Estudiante", userId: juanId);

        var response = await client.GetAsync($"/api/courses/{sectionId}/my-submissions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var submissions = JsonSerializer.Deserialize<StudentSubmissionDto[]>(
            await response.Content.ReadAsStringAsync(), JsonOpts);
        submissions.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMySubmissions_AsEstudianteNotEnrolled_Returns403()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var sofiaId = await GetUserIdByEmailAsync("sofia.mendez@academia.com");
        // sofia.mendez tiene rol Estudiante pero es rechazada por verificación de inscripción;
        // el rol pasa [Authorize], pero el servicio obliga la inscripción
        var client = CreateAuthenticatedClient("Estudiante", userId: sofiaId);

        var response = await client.GetAsync($"/api/courses/{sectionId}/my-submissions");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Grade submission ─────────────────────────────────────────────────────

    [Fact]
    public async Task GradeSubmission_AsProfesorOwnSection_Returns200()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var profGarciaId = await GetUserIdByEmailAsync("prof.garcia@academia.com");
        var juanId = await GetUserIdByEmailAsync("juan.perez@academia.com");

        var profClient = CreateAuthenticatedClient("Profesor", userId: profGarciaId);
        var studentClient = CreateAuthenticatedClient("Estudiante", userId: juanId);

        // Crear actividad
        var actResp = await profClient.PostAsJsonAsync($"/api/courses/{sectionId}/activities",
            new CreateActivityDto("Actividad a calificar", "Desc", "Tarea",
                DateTime.UtcNow.AddDays(7), 10m, 0.3m, IsPublished: true));
        var activity = JsonSerializer.Deserialize<ActivityDto>(
            await actResp.Content.ReadAsStringAsync(), JsonOpts)!;

        // El estudiante entrega
        await studentClient.PostAsJsonAsync($"/api/courses/activities/{activity.Id}/submit",
            new SubmitDto("Respuesta para calificar"));

        // Obtener entregas para encontrar el ID de la entrega
        var subsResp = await profClient.GetAsync($"/api/courses/activities/{activity.Id}/submissions");
        var submissions = JsonSerializer.Deserialize<SubmissionDto[]>(
            await subsResp.Content.ReadAsStringAsync(), JsonOpts)!;
        submissions.Should().NotBeEmpty();
        var submissionId = submissions.First().Id;

        // Calificar
        var gradeResp = await profClient.PatchAsJsonAsync($"/api/courses/submissions/{submissionId}/grade",
            new GradeSubmissionDto(8.5m, "Excelente trabajo"));

        gradeResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var graded = JsonSerializer.Deserialize<SubmissionDto>(
            await gradeResp.Content.ReadAsStringAsync(), JsonOpts)!;
        graded.Score.Should().Be(8.5m);
        graded.Feedback.Should().Be("Excelente trabajo");
    }

    [Fact]
    public async Task GradeSubmission_AsProfesorAlienSection_Returns403()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var profGarciaId = await GetUserIdByEmailAsync("prof.garcia@academia.com");
        var profLopezId = await GetUserIdByEmailAsync("prof.lopez@academia.com");
        var juanId = await GetUserIdByEmailAsync("juan.perez@academia.com");

        var ownerClient = CreateAuthenticatedClient("Profesor", userId: profGarciaId);
        var alienClient = CreateAuthenticatedClient("Profesor", userId: profLopezId);
        var studentClient = CreateAuthenticatedClient("Estudiante", userId: juanId);

        // El profesor dueño crea la actividad
        var actResp = await ownerClient.PostAsJsonAsync($"/api/courses/{sectionId}/activities",
            new CreateActivityDto("Actividad ajena calificación", "Desc", "Tarea",
                DateTime.UtcNow.AddDays(7), 10m, 0.1m, IsPublished: true));
        var activity = JsonSerializer.Deserialize<ActivityDto>(
            await actResp.Content.ReadAsStringAsync(), JsonOpts)!;

        // El estudiante entrega
        await studentClient.PostAsJsonAsync($"/api/courses/activities/{activity.Id}/submit",
            new SubmitDto("Respuesta"));

        // El profesor dueño lee las entregas
        var subsResp = await ownerClient.GetAsync($"/api/courses/activities/{activity.Id}/submissions");
        var subs = JsonSerializer.Deserialize<SubmissionDto[]>(
            await subsResp.Content.ReadAsStringAsync(), JsonOpts)!;
        var submissionId = subs.First().Id;

        // El profesor ajeno intenta calificar
        var gradeResp = await alienClient.PatchAsJsonAsync($"/api/courses/submissions/{submissionId}/grade",
            new GradeSubmissionDto(5m, "Invasión"));

        gradeResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET /api/courses/{sectionId}/grade-suggestions ───────────────────────

    [Fact]
    public async Task GetGradeSuggestions_AsProfesorOwnSection_Returns200WithList()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var profGarciaId = await GetUserIdByEmailAsync("prof.garcia@academia.com");
        var client = CreateAuthenticatedClient("Profesor", userId: profGarciaId);

        var response = await client.GetAsync($"/api/courses/{sectionId}/grade-suggestions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var suggestions = JsonSerializer.Deserialize<StudentGradeSuggestionDto[]>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        suggestions.Should().NotBeNull();
        suggestions!.Should().AllSatisfy(s => s.StudentId.Should().NotBeEmpty());
    }

    [Fact]
    public async Task GetGradeSuggestions_AsAdmin_Returns200()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var client = CreateAuthenticatedClient("Admin");

        var response = await client.GetAsync($"/api/courses/{sectionId}/grade-suggestions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetGradeSuggestions_AsProfesorAlienSection_Returns403()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var profLopezId = await GetUserIdByEmailAsync("prof.lopez@academia.com");
        var client = CreateAuthenticatedClient("Profesor", userId: profLopezId);

        var response = await client.GetAsync($"/api/courses/{sectionId}/grade-suggestions");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetGradeSuggestions_AsEstudiante_Returns403()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var juanId = await GetUserIdByEmailAsync("juan.perez@academia.com");
        var client = CreateAuthenticatedClient("Estudiante", userId: juanId);

        var response = await client.GetAsync($"/api/courses/{sectionId}/grade-suggestions");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET /api/courses/{sectionId}/my-grade-suggestion ─────────────────────

    [Fact]
    public async Task GetMyGradeSuggestion_AsEstudianteEnrolled_Returns200()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var juanId = await GetUserIdByEmailAsync("juan.perez@academia.com");
        var client = CreateAuthenticatedClient("Estudiante", userId: juanId);

        var response = await client.GetAsync($"/api/courses/{sectionId}/my-grade-suggestion");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = JsonSerializer.Deserialize<MySuggestionDto>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        dto.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMyGradeSuggestion_AsEstudianteNotEnrolled_Returns403()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var sofiaId = await GetUserIdByEmailAsync("sofia.mendez@academia.com");
        var client = CreateAuthenticatedClient("Estudiante", userId: sofiaId);

        var response = await client.GetAsync($"/api/courses/{sectionId}/my-grade-suggestion");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetMyGradeSuggestion_AsProfesor_Returns403()
    {
        var sectionId = await GetSectionIdBySubjectCodeAsync("IS-MAT1");
        var profGarciaId = await GetUserIdByEmailAsync("prof.garcia@academia.com");
        var client = CreateAuthenticatedClient("Profesor", userId: profGarciaId);

        var response = await client.GetAsync($"/api/courses/{sectionId}/my-grade-suggestion");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Task<Guid> GetUserIdByEmailAsync(string email) =>
        ExecuteInScopeAsync(async db =>
            (await db.Users.FirstAsync(u => u.Email == email)).Id);
}
