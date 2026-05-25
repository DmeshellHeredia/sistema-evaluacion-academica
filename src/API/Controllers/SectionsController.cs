using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaEvaluacionAcademica.Application.Common;
using SistemaEvaluacionAcademica.Application.DTOs.Sections;
using SistemaEvaluacionAcademica.Application.Interfaces;
using SistemaEvaluacionAcademica.Domain.Constants;

namespace SistemaEvaluacionAcademica.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class SectionsController : ApiControllerBase
{
    private readonly ISectionService _sectionService;

    public SectionsController(ISectionService sectionService)
    {
        _sectionService = sectionService;
    }

    /// <summary>Lookup ligero de secciones para selects. Devuelve id, subjectCode, subjectName, sectionCode, schedule.</summary>
    [HttpGet("lookup")]
    [Authorize(Roles = Roles.AdminOrProfesor)]
    [ProducesResponseType(typeof(IReadOnlyList<SectionLookupDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Lookup(
        [FromQuery] string? search = null,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _sectionService.GetLookupAsync(search, limit, cancellationToken);
        return Ok(result.Data);
    }

    /// <summary>Obtiene secciones activas paginadas. Solo Admin.</summary>
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(PagedResult<SectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest(new { error = "page debe ser >= 1; pageSize entre 1 y 100." });
        var result = await _sectionService.GetPagedAsync(page, pageSize, search, cancellationToken);
        return Ok(result.Data);
    }

    /// <summary>Obtiene las secciones del profesor autenticado. Admin ve todas; Profesor ve las suyas.</summary>
    [HttpGet("my")]
    [Authorize(Roles = Roles.AdminOrProfesor)]
    [ProducesResponseType(typeof(IEnumerable<SectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        if (UserRole == Roles.Admin)
        {
            var all = await _sectionService.GetAllAsync(cancellationToken);
            return Ok(all.Data);
        }

        var result = await _sectionService.GetByProfessorAsync(UserId, cancellationToken);
        return Ok(result.Data);
    }

    /// <summary>Obtiene las secciones de un profesor específico. Solo Admin.</summary>
    [HttpGet("professor/{professorId:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(IEnumerable<SectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByProfessor(Guid professorId, CancellationToken cancellationToken)
    {
        var result = await _sectionService.GetByProfessorAsync(professorId, cancellationToken);
        return Ok(result.Data);
    }

    /// <summary>Obtiene una sección por ID.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = Roles.AdminOrProfesor)]
    [ProducesResponseType(typeof(SectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sectionService.GetByIdAsync(id, cancellationToken);
        if (!result.IsSuccess)
            return NotFound(new { result.ErrorMessage });

        if (UserRole == Roles.Profesor)
        {
            if (result.Data!.ProfessorId != UserId)
                return Forbid();
        }

        return Ok(result.Data);
    }

    /// <summary>Obtiene los estudiantes inscritos en una sección con su calificación actual.</summary>
    [HttpGet("{id:guid}/students")]
    [Authorize(Roles = Roles.AdminOrProfesor)]
    [ProducesResponseType(typeof(IEnumerable<SectionStudentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudents(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sectionService.GetStudentsAsync(id, UserId, UserRole, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    /// <summary>Crea una nueva sección para una materia. Solo Admin.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(SectionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateSectionDto dto, CancellationToken cancellationToken)
    {
        var result = await _sectionService.CreateAsync(dto, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    /// <summary>Actualiza horario, modalidad y capacidad de una sección. Solo Admin.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(SectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSectionDto dto, CancellationToken cancellationToken)
    {
        var result = await _sectionService.UpdateAsync(id, dto, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    /// <summary>Elimina una sección (soft delete). Solo Admin.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sectionService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }
}
