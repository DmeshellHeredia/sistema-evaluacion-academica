using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaEvaluacionAcademica.Application.DTOs.Periods;
using SistemaEvaluacionAcademica.Application.Interfaces;
using SistemaEvaluacionAcademica.Domain.Constants;

namespace SistemaEvaluacionAcademica.API.Controllers;

[ApiController]
[Route("api/academic-periods")]
[Authorize]
[Produces("application/json")]
public class AcademicPeriodsController : ControllerBase
{
    private readonly IAcademicPeriodService _periodService;

    public AcademicPeriodsController(IAcademicPeriodService periodService)
    {
        _periodService = periodService;
    }

    /// <summary>Estado de selección de materias — todos los roles autenticados.</summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(EnrollmentStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var result = await _periodService.GetEnrollmentStatusAsync(cancellationToken);
        return Ok(result.Data);
    }

    /// <summary>Listar todos los períodos. Solo Admin.</summary>
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(IEnumerable<AcademicPeriodDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _periodService.GetAllAsync(cancellationToken);
        return Ok(result.Data);
    }

    /// <summary>Obtener período por ID. Solo Admin.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(AcademicPeriodDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _periodService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : NotFound(new { result.ErrorMessage });
    }

    /// <summary>Crear período. Solo Admin.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(AcademicPeriodDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateAcademicPeriodDto dto, CancellationToken cancellationToken)
    {
        var result = await _periodService.CreateAsync(dto, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorMessage });

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>Actualizar período. Solo Admin.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(AcademicPeriodDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAcademicPeriodDto dto, CancellationToken cancellationToken)
    {
        var result = await _periodService.UpdateAsync(id, dto, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Data)
            : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    /// <summary>Eliminar período. Solo Admin.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _periodService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    /// <summary>Abrir selección de materias. Solo Admin. Cierra todos los demás períodos abiertos.</summary>
    [HttpPost("{id:guid}/open")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(AcademicPeriodDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> OpenEnrollment(Guid id, CancellationToken cancellationToken)
    {
        var result = await _periodService.OpenEnrollmentAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }

    /// <summary>Cerrar selección de materias. Solo Admin.</summary>
    [HttpPost("{id:guid}/close")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(AcademicPeriodDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CloseEnrollment(Guid id, CancellationToken cancellationToken)
    {
        var result = await _periodService.CloseEnrollmentAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { result.ErrorMessage });
    }
}
