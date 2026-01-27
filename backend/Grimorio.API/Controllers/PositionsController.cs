using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Positions.Commands;
using Grimorio.Application.Features.Positions.Queries;

namespace Grimorio.API.Controllers;

/// <summary>
/// Controlador para gestionar posiciones (puestos de trabajo).
/// Todos los endpoints requieren autenticación.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PositionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PositionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Obtiene una posición por su ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPosition(Guid id)
    {
        var branchId = User.FindFirst("BranchId");
        if (branchId == null || !Guid.TryParse(branchId.Value, out var parsedBranchId))
            return Unauthorized("BranchId no válido en el token.");

        var query = new GetPositionQuery
        {
            PositionId = id,
            BranchId = parsedBranchId
        };

        var result = await _mediator.Send(query);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Obtiene todas las posiciones de la rama del usuario.
    /// Soporta paginación.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPositions([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var branchId = User.FindFirst("BranchId");
        if (branchId == null || !Guid.TryParse(branchId.Value, out var parsedBranchId))
            return Unauthorized("BranchId no válido en el token.");

        var query = new GetPositionsQuery
        {
            BranchId = parsedBranchId,
            OnlyActive = true,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Crea una nueva posición.
    /// Requiere permiso: "Admin.ManageRoles" o similar.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreatePosition([FromBody] CreatePositionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var branchId = User.FindFirst("BranchId");
        if (branchId == null || !Guid.TryParse(branchId.Value, out var parsedBranchId))
            return Unauthorized("BranchId no válido en el token.");

        var command = new CreatePositionCommand
        {
            BranchId = parsedBranchId,
            Name = dto.Name,
            Description = dto.Description
        };

        try
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetPosition), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al crear posición.", error = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza una posición existente.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdatePosition(Guid id, [FromBody] UpdatePositionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var branchId = User.FindFirst("BranchId");
        if (branchId == null || !Guid.TryParse(branchId.Value, out var parsedBranchId))
            return Unauthorized("BranchId no válido en el token.");

        var command = new UpdatePositionCommand
        {
            PositionId = id,
            BranchId = parsedBranchId,
            Name = dto.Name,
            Description = dto.Description,
            IsActive = dto.IsActive
        };

        try
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al actualizar posición.", error = ex.Message });
        }
    }

    /// <summary>
    /// Elimina (soft delete) una posición.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeletePosition(Guid id)
    {
        var branchId = User.FindFirst("BranchId");
        if (branchId == null || !Guid.TryParse(branchId.Value, out var parsedBranchId))
            return Unauthorized("BranchId no válido en el token.");

        var command = new DeletePositionCommand
        {
            PositionId = id,
            BranchId = parsedBranchId
        };

        try
        {
            var result = await _mediator.Send(command);
            return Ok(new { message = "Posición eliminada correctamente." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al eliminar posición.", error = ex.Message });
        }
    }
}
