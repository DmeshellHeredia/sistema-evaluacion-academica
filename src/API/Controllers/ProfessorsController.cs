using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaEvaluacionAcademica.Application.DTOs.Professors;
using SistemaEvaluacionAcademica.Application.Interfaces;
using SistemaEvaluacionAcademica.Domain.Constants;

namespace SistemaEvaluacionAcademica.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Admin)]
[Produces("application/json")]
public class ProfessorsController : ControllerBase
{
    private readonly IProfessorService _professorService;

    public ProfessorsController(IProfessorService professorService)
    {
        _professorService = professorService;
    }

    /// <summary>Obtiene todos los profesores paginados, con búsqueda opcional.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest(new { error = "page debe ser >= 1; pageSize entre 1 y 100." });
        var result = await _professorService.GetAllAsync(page, pageSize, search, cancellationToken);
        return Ok(result.Data);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProfessorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _professorService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : NotFound(new { result.ErrorMessage });
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProfessorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateProfessorDto dto, CancellationToken cancellationToken)
    {
        var result = await _professorService.CreateAsync(dto, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : BadRequest(new { result.ErrorMessage });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProfessorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProfessorDto dto, CancellationToken cancellationToken)
    {
        var result = await _professorService.UpdateAsync(id, dto, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _professorService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }
}
