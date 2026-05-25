using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaEvaluacionAcademica.Application.DTOs.Students;
using SistemaEvaluacionAcademica.Application.Interfaces;
using SistemaEvaluacionAcademica.Domain.Constants;

namespace SistemaEvaluacionAcademica.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class StudentsController : ApiControllerBase
{
    private readonly IStudentService _studentService;
    private readonly IValidator<UpdateStudentDto> _updateValidator;

    public StudentsController(IStudentService studentService, IValidator<UpdateStudentDto> updateValidator)
    {
        _studentService  = studentService;
        _updateValidator = updateValidator;
    }

    /// <summary>Devuelve el perfil del estudiante autenticado (solo Estudiante).</summary>
    [HttpGet("me")]
    [Authorize(Roles = Roles.Estudiante)]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var result = await _studentService.GetByUserIdAsync(UserId, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : NotFound(new { result.ErrorMessage });
    }

    /// <summary>Obtiene todos los estudiantes paginados, con búsqueda opcional.</summary>
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest(new { error = "page debe ser >= 1; pageSize entre 1 y 100." });
        var result = await _studentService.GetAllAsync(page, pageSize, search, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Data)
            : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    /// <summary>Obtiene un estudiante por su ID.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = Roles.AdminOrProfesorOrEstudiante)]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (UserRole == Roles.Estudiante)
        {
            var own = await _studentService.GetByUserIdAsync(UserId, cancellationToken);
            if (!own.IsSuccess || own.Data!.Id != id)
                return Forbid();
        }

        var result = await _studentService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : NotFound(new { result.ErrorMessage });
    }

    /// <summary>Crea un nuevo estudiante (Admin únicamente).</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateStudentDto dto, CancellationToken cancellationToken)
    {
        var result = await _studentService.CreateAsync(dto, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : BadRequest(new { result.ErrorMessage });
    }

    /// <summary>Actualiza la carrera y semestre de un estudiante.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStudentDto dto, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return UnprocessableEntity(validation.Errors.Select(e => e.ErrorMessage));

        var result = await _studentService.UpdateAsync(id, dto, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    /// <summary>Desactiva un estudiante (soft delete).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _studentService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    /// <summary>Obtiene el reporte completo de calificaciones de un estudiante.</summary>
    [HttpGet("{id:guid}/grade-report")]
    [Authorize(Roles = Roles.AdminOrProfesorOrEstudiante)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGradeReport(Guid id, CancellationToken cancellationToken)
    {
        if (UserRole == Roles.Estudiante)
        {
            var own = await _studentService.GetByUserIdAsync(UserId, cancellationToken);
            if (!own.IsSuccess || own.Data!.Id != id)
                return Forbid();
        }

        var result = await _studentService.GetGradeReportAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }
}
