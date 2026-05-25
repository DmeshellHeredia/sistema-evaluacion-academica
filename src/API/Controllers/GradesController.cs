using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaEvaluacionAcademica.Application.DTOs.Grades;
using SistemaEvaluacionAcademica.Application.Interfaces;
using SistemaEvaluacionAcademica.Application.Validators.Grades;
using SistemaEvaluacionAcademica.Domain.Constants;
using SistemaEvaluacionAcademica.Domain.Entities;

namespace SistemaEvaluacionAcademica.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class GradesController : ApiControllerBase
{
    private readonly IGradeService _gradeService;
    private readonly IStudentService _studentService;
    private readonly IValidator<CreateGradeDto> _createValidator;
    private readonly IValidator<UpdateGradeRequest> _updateValidator;

    public GradesController(
        IGradeService gradeService,
        IStudentService studentService,
        IValidator<CreateGradeDto> createValidator,
        IValidator<UpdateGradeRequest> updateValidator)
    {
        _gradeService    = gradeService;
        _studentService  = studentService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Registra una calificación para un estudiante en una materia.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.AdminOrProfesor)]
    [ProducesResponseType(typeof(GradeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateGradeDto dto, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return UnprocessableEntity(validation.Errors.Select(e => e.ErrorMessage));

        var result = await _gradeService.CreateAsync(dto, UserId, UserRole, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetByStudent), new { studentId = dto.StudentId }, result.Data)
            : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    /// <summary>Actualiza el valor y/o comentario de una calificación existente.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.AdminOrProfesor)]
    [ProducesResponseType(typeof(GradeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGradeRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return UnprocessableEntity(validation.Errors.Select(e => e.ErrorMessage));

        var result = await _gradeService.UpdateAsync(id, request.Value, request.Comments, UserId, UserRole, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    /// <summary>Elimina (soft delete) una calificación. Solo Admin.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _gradeService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    /// <summary>Calificaciones de un estudiante paginadas. Estudiante solo ve las suyas.</summary>
    [HttpGet("student/{studentId:guid}")]
    [Authorize(Roles = Roles.AdminOrProfesorOrEstudiante)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetByStudent(
        Guid studentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? period = null,
        CancellationToken cancellationToken = default)
    {
        if (UserRole == Roles.Estudiante)
        {
            var own = await _studentService.GetByUserIdAsync(UserId, cancellationToken);
            if (!own.IsSuccess || own.Data!.Id != studentId)
                return Forbid();
        }

        var result = await _gradeService.GetByStudentAsync(studentId, page, pageSize, period, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Data)
            : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    /// <summary>Obtiene calificaciones de una sección paginadas. Profesor solo ve sus secciones.</summary>
    [HttpGet("section/{sectionId:guid}")]
    [Authorize(Roles = Roles.AdminOrProfesor)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySection(
        Guid sectionId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _gradeService.GetBySectionAsync(sectionId, page, pageSize, UserId, UserRole, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    /// <summary>
    /// Obtiene calificaciones de una materia paginadas. Acepta filtro opcional de período (ej: 2025-1).
    /// </summary>
    [HttpGet("subject/{subjectId:guid}")]
    [Authorize(Roles = Roles.AdminOrProfesor)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBySubject(
        Guid subjectId,
        [FromQuery] string? period,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _gradeService.GetBySubjectAsync(subjectId, period, page, pageSize, UserId, UserRole, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    /// <summary>Obtiene el promedio general del estudiante con su categoría automática.</summary>
    [HttpGet("student/{studentId:guid}/average")]
    [Authorize(Roles = Roles.AdminOrProfesorOrEstudiante)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStudentAverage(Guid studentId, CancellationToken cancellationToken)
    {
        if (UserRole == Roles.Estudiante)
        {
            var own = await _studentService.GetByUserIdAsync(UserId, cancellationToken);
            if (!own.IsSuccess || own.Data!.Id != studentId)
                return Forbid();
        }

        var result = await _gradeService.GetStudentAverageAsync(studentId, cancellationToken);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { result.ErrorMessage });

        var average = result.Data;
        return Ok(new
        {
            StudentId = studentId,
            Average   = average,
            Category  = average.HasValue ? Grade.CategoryFor(average.Value) : null,
            HasGrades = average.HasValue
        });
    }

    /// <summary>Obtiene el promedio de una materia en un período específico.</summary>
    [HttpGet("subject/{subjectId:guid}/average")]
    [Authorize(Roles = Roles.AdminOrProfesor)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubjectAverage(
        Guid subjectId,
        [FromQuery] string period,
        CancellationToken cancellationToken)
    {
        var result = await _gradeService.GetSubjectAverageAsync(subjectId, period, cancellationToken);
        return Ok(new { SubjectId = subjectId, Period = period, Average = result.Data });
    }
}
