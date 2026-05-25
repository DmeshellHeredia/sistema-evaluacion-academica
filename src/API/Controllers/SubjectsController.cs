using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaEvaluacionAcademica.Application.Common;
using SistemaEvaluacionAcademica.Application.DTOs.Subjects;
using SistemaEvaluacionAcademica.Application.Interfaces;
using SistemaEvaluacionAcademica.Domain.Constants;

namespace SistemaEvaluacionAcademica.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class SubjectsController : ControllerBase
{
    private readonly ISubjectService _subjectService;
    private readonly IValidator<CreateSubjectDto> _createValidator;
    private readonly IValidator<UpdateSubjectDto> _updateValidator;

    public SubjectsController(
        ISubjectService subjectService,
        IValidator<CreateSubjectDto> createValidator,
        IValidator<UpdateSubjectDto> updateValidator)
    {
        _subjectService = subjectService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Lookup ligero de materias para selects. Devuelve id, code, name, semesterLevel.</summary>
    [HttpGet("lookup")]
    [Authorize(Roles = Roles.AdminOrProfesor)]
    [ProducesResponseType(typeof(IReadOnlyList<SubjectLookupDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Lookup(
        [FromQuery] string? search = null,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _subjectService.GetLookupAsync(search, limit, cancellationToken);
        return Ok(result.Data);
    }

    /// <summary>Obtiene materias activas paginadas. Estudiantes usan /enrollments/catalog/me.</summary>
    [HttpGet]
    [Authorize(Roles = Roles.AdminOrProfesor)]
    [ProducesResponseType(typeof(PagedResult<SubjectDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest(new { error = "page debe ser >= 1; pageSize entre 1 y 100." });
        var result = await _subjectService.GetPagedAsync(page, pageSize, search, cancellationToken);
        return Ok(result.Data);
    }

    /// <summary>Obtiene una materia por su ID.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = Roles.AdminOrProfesor)]
    [ProducesResponseType(typeof(SubjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _subjectService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : NotFound(new { result.ErrorMessage });
    }

    /// <summary>Crea una nueva materia. Solo Admin.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(SubjectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateSubjectDto dto, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return UnprocessableEntity(validation.Errors.Select(e => e.ErrorMessage));

        var result = await _subjectService.CreateAsync(dto, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    /// <summary>Actualiza nombre, descripción y créditos de una materia. Solo Admin.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(SubjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSubjectDto dto, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return UnprocessableEntity(validation.Errors.Select(e => e.ErrorMessage));

        var existing = await _subjectService.GetByIdAsync(id, cancellationToken);
        if (!existing.IsSuccess)
            return NotFound(new { existing.ErrorMessage });

        var createDto = new CreateSubjectDto(existing.Data!.Code, dto.Name, dto.Description, dto.Credits, dto.SemesterLevel, dto.AppliesToAllCareers, dto.Careers);
        var result = await _subjectService.UpdateAsync(id, createDto, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    /// <summary>Elimina una materia (soft delete). Solo Admin.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _subjectService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    // Prerequisites

    /// <summary>Lista los prerequisitos de una materia.</summary>
    [HttpGet("{id:guid}/prerequisites")]
    [ProducesResponseType(typeof(IEnumerable<PrerequisiteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPrerequisites(Guid id, CancellationToken cancellationToken)
    {
        var result = await _subjectService.GetPrerequisitesAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    /// <summary>
    /// Reemplaza todos los prerequisitos de una materia. Solo Admin.
    /// Valida: no ciclos, no auto-referencia, no duplicados.
    /// </summary>
    [HttpPut("{id:guid}/prerequisites")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(IEnumerable<PrerequisiteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPrerequisites(Guid id, [FromBody] SetPrerequisitesDto dto, CancellationToken cancellationToken)
    {
        var result = await _subjectService.SetPrerequisitesAsync(id, dto.PrerequisiteIds, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    /// <summary>Agrega un prerequisito a una materia. Solo Admin.</summary>
    [HttpPost("{id:guid}/prerequisites/{prereqId:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(PrerequisiteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddPrerequisite(Guid id, Guid prereqId, CancellationToken cancellationToken)
    {
        var result = await _subjectService.AddPrerequisiteAsync(id, prereqId, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetPrerequisites), new { id }, result.Data)
            : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    /// <summary>Elimina un prerequisito de una materia. Solo Admin.</summary>
    [HttpDelete("{id:guid}/prerequisites/{prereqId:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePrerequisite(Guid id, Guid prereqId, CancellationToken cancellationToken)
    {
        var result = await _subjectService.RemovePrerequisiteAsync(id, prereqId, cancellationToken);
        return result.IsSuccess ? NoContent() : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

}
