using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaEvaluacionAcademica.Application.DTOs.Courses;
using SistemaEvaluacionAcademica.Application.Interfaces;
using SistemaEvaluacionAcademica.Domain.Constants;

namespace SistemaEvaluacionAcademica.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class CoursesController : ApiControllerBase
{
    private readonly ICourseService _courses;

    public CoursesController(ICourseService courses) => _courses = courses;

    // Overview

    [HttpGet("{sectionId:guid}")]
    public async Task<IActionResult> GetOverview(Guid sectionId, CancellationToken ct)
    {
        var result = await _courses.GetOverviewAsync(sectionId, UserId, UserRole, ct);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    // Participants

    [HttpGet("{sectionId:guid}/participants")]
    public async Task<IActionResult> GetParticipants(Guid sectionId, CancellationToken ct)
    {
        var result = await _courses.GetParticipantsAsync(sectionId, UserId, UserRole, ct);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    // Announcements

    [HttpGet("{sectionId:guid}/announcements")]
    public async Task<IActionResult> GetAnnouncements(Guid sectionId, CancellationToken ct)
    {
        var result = await _courses.GetAnnouncementsAsync(sectionId, UserId, UserRole, ct);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    [HttpPost("{sectionId:guid}/announcements")]
    [Authorize(Roles = Roles.AdminOrProfesor)]
    public async Task<IActionResult> CreateAnnouncement(Guid sectionId, [FromBody] CreateAnnouncementDto dto, CancellationToken ct)
    {
        var result = await _courses.CreateAnnouncementAsync(sectionId, dto, UserId, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetAnnouncements), new { sectionId }, result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    [HttpPut("announcements/{id:guid}")]
    [Authorize(Roles = Roles.AdminOrProfesor)]
    public async Task<IActionResult> UpdateAnnouncement(Guid id, [FromBody] UpdateAnnouncementDto dto, CancellationToken ct)
    {
        var result = await _courses.UpdateAnnouncementAsync(id, dto, UserId, ct);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    [HttpDelete("announcements/{id:guid}")]
    [Authorize(Roles = Roles.AdminOrProfesor)]
    public async Task<IActionResult> DeleteAnnouncement(Guid id, CancellationToken ct)
    {
        var result = await _courses.DeleteAnnouncementAsync(id, UserId, ct);
        return result.IsSuccess ? NoContent() : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    // Activities

    [HttpGet("{sectionId:guid}/activities")]
    public async Task<IActionResult> GetActivities(Guid sectionId, CancellationToken ct)
    {
        var result = await _courses.GetActivitiesAsync(sectionId, UserId, UserRole, ct);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    [HttpGet("activities/{activityId:guid}")]
    public async Task<IActionResult> GetActivity(Guid activityId, CancellationToken ct)
    {
        var result = await _courses.GetActivityAsync(activityId, UserId, UserRole, ct);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    [HttpPost("{sectionId:guid}/activities")]
    [Authorize(Roles = Roles.AdminOrProfesor)]
    public async Task<IActionResult> CreateActivity(Guid sectionId, [FromBody] CreateActivityDto dto, CancellationToken ct)
    {
        var result = await _courses.CreateActivityAsync(sectionId, dto, UserId, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetActivities), new { sectionId }, result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    [HttpPut("activities/{activityId:guid}")]
    [Authorize(Roles = Roles.AdminOrProfesor)]
    public async Task<IActionResult> UpdateActivity(Guid activityId, [FromBody] UpdateActivityDto dto, CancellationToken ct)
    {
        var result = await _courses.UpdateActivityAsync(activityId, dto, UserId, ct);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    [HttpDelete("activities/{activityId:guid}")]
    [Authorize(Roles = Roles.AdminOrProfesor)]
    public async Task<IActionResult> DeleteActivity(Guid activityId, CancellationToken ct)
    {
        var result = await _courses.DeleteActivityAsync(activityId, UserId, ct);
        return result.IsSuccess ? NoContent() : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    // Submissions — professor

    [HttpGet("activities/{activityId:guid}/submissions")]
    [Authorize(Roles = Roles.AdminOrProfesor)]
    public async Task<IActionResult> GetSubmissions(Guid activityId, CancellationToken ct)
    {
        var result = await _courses.GetSubmissionsForActivityAsync(activityId, UserId, ct);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    [HttpPatch("submissions/{submissionId:guid}/grade")]
    [Authorize(Roles = Roles.AdminOrProfesor)]
    public async Task<IActionResult> GradeSubmission(Guid submissionId, [FromBody] GradeSubmissionDto dto, CancellationToken ct)
    {
        var result = await _courses.GradeSubmissionAsync(submissionId, dto, UserId, ct);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    // Submissions — student

    [HttpGet("{sectionId:guid}/my-submissions")]
    [Authorize(Roles = Roles.Estudiante)]
    public async Task<IActionResult> GetMySubmissions(Guid sectionId, CancellationToken ct)
    {
        var result = await _courses.GetMySubmissionsAsync(sectionId, UserId, ct);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    [HttpPost("activities/{activityId:guid}/submit")]
    [Authorize(Roles = Roles.Estudiante)]
    public async Task<IActionResult> Submit(Guid activityId, [FromBody] SubmitDto dto, CancellationToken ct)
    {
        var result = await _courses.SubmitAsync(activityId, dto, UserId, ct);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    // Grade suggestions — puente LMS ↔ académico

    /// <summary>Nota sugerida por actividades vs nota oficial para todos los alumnos. Admin/Profesor.</summary>
    [HttpGet("{sectionId:guid}/grade-suggestions")]
    [Authorize(Roles = Roles.AdminOrProfesor)]
    [ProducesResponseType(typeof(IEnumerable<Application.DTOs.Courses.StudentGradeSuggestionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGradeSuggestions(Guid sectionId, CancellationToken ct)
    {
        var result = await _courses.GetGradeSuggestionsAsync(sectionId, UserId, UserRole, ct);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    /// <summary>Nota sugerida y nota oficial del estudiante autenticado. Solo Estudiante.</summary>
    [HttpGet("{sectionId:guid}/my-grade-suggestion")]
    [Authorize(Roles = Roles.Estudiante)]
    [ProducesResponseType(typeof(Application.DTOs.Courses.MySuggestionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyGradeSuggestion(Guid sectionId, CancellationToken ct)
    {
        var result = await _courses.GetMyGradeSuggestionAsync(sectionId, UserId, ct);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }
}
