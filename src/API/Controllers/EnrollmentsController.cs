using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaEvaluacionAcademica.Application.DTOs.Enrollments;
using SistemaEvaluacionAcademica.Application.Interfaces;
using SistemaEvaluacionAcademica.Domain.Constants;

namespace SistemaEvaluacionAcademica.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class EnrollmentsController : ApiControllerBase
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly IStudentService    _studentService;

    public EnrollmentsController(IEnrollmentService enrollmentService, IStudentService studentService)
    {
        _enrollmentService = enrollmentService;
        _studentService    = studentService;
    }

    /// <summary>Horario del estudiante autenticado. Solo Estudiante.</summary>
    [HttpGet("schedule/me")]
    [Authorize(Roles = Roles.Estudiante)]
    [ProducesResponseType(typeof(StudentScheduleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMySchedule(CancellationToken cancellationToken)
    {
        var student = await _studentService.GetByUserIdAsync(UserId, cancellationToken);
        if (!student.IsSuccess)
            return NotFound(new { student.ErrorMessage });

        var result = await _enrollmentService.GetMyScheduleAsync(student.Data!.Id, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    /// <summary>Catálogo del estudiante autenticado. Solo Estudiante.</summary>
    [HttpGet("catalog/me")]
    [Authorize(Roles = Roles.Estudiante)]
    [ProducesResponseType(typeof(IEnumerable<SubjectCatalogItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyCatalog(CancellationToken cancellationToken)
    {
        var student = await _studentService.GetByUserIdAsync(UserId, cancellationToken);
        if (!student.IsSuccess)
            return NotFound(new { student.ErrorMessage });

        var result = await _enrollmentService.GetCatalogAsync(student.Data!.Id, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    /// <summary>Catálogo por studentId. Solo Admin/Profesor.</summary>
    [HttpGet("catalog/{studentId:guid}")]
    [Authorize(Roles = Roles.AdminOrProfesor)]
    [ProducesResponseType(typeof(IEnumerable<SubjectCatalogItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCatalog(Guid studentId, CancellationToken cancellationToken)
    {
        var result = await _enrollmentService.GetCatalogAsync(studentId, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    /// <summary>Inscribe al estudiante autenticado en una sección. Estudiante solo puede inscribirse a sí mismo.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.AdminOrEstudiante)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Enroll([FromBody] EnrollRequestDto dto, CancellationToken cancellationToken)
    {
        if (UserRole == Roles.Estudiante)
        {
            var student = await _studentService.GetByUserIdAsync(UserId, cancellationToken);
            if (!student.IsSuccess)
                return NotFound(new { student.ErrorMessage });

            if (dto.StudentId != student.Data!.Id)
                return Forbid();
        }

        var result = await _enrollmentService.EnrollAsync(dto, cancellationToken);
        return result.IsSuccess
            ? Ok(new { enrollmentId = result.Data, message = "Inscripción realizada exitosamente." })
            : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    /// <summary>Desinscribe. Estudiante solo puede desinscribirse a sí mismo.</summary>
    [HttpDelete("{studentId:guid}/{sectionId:guid}")]
    [Authorize(Roles = Roles.AdminOrEstudiante)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unenroll(Guid studentId, Guid sectionId, CancellationToken cancellationToken)
    {
        if (UserRole == Roles.Estudiante)
        {
            var student = await _studentService.GetByUserIdAsync(UserId, cancellationToken);
            if (!student.IsSuccess)
                return NotFound(new { student.ErrorMessage });

            if (studentId != student.Data!.Id)
                return Forbid();
        }

        var result = await _enrollmentService.UnenrollAsync(studentId, sectionId, cancellationToken);
        return result.IsSuccess ? NoContent() : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }
}
