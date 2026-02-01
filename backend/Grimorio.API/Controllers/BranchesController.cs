using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Branches.Commands;
using Grimorio.Application.Features.Branches.Queries;

namespace Grimorio.API.Controllers;

/// <summary>
/// Controlador para gestionar la sucursal actual.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BranchesController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Inicializa una nueva instancia del controlador de sucursales.
    /// </summary>
    /// <param name="mediator">Instancia de MediatR para enviar comandos.</param>
    public BranchesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Obtiene los datos de la sucursal actual del usuario.
    /// </summary>
    /// <returns>Datos de la sucursal.</returns>
    /// <response code="200">Sucursal encontrada.</response>
    /// <response code="401">BranchId no válido en el token.</response>
    /// <response code="404">Sucursal no encontrada.</response>
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentBranch()
    {
        var branchClaim = User.FindFirst("BranchId")?.Value;
        if (branchClaim == null || !Guid.TryParse(branchClaim, out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        var result = await _mediator.Send(new GetCurrentBranchQuery { BranchId = branchId });
        if (result == null)
            return NotFound(new { message = "Sucursal no encontrada." });

        return Ok(result);
    }

    /// <summary>
    /// Actualiza los datos de la sucursal actual.
    /// </summary>
    /// <param name="dto">Datos de la sucursal a actualizar.</param>
    /// <returns>Datos actualizados de la sucursal.</returns>
    /// <response code="200">Sucursal actualizada correctamente.</response>
    /// <response code="400">Solicitud inválida.</response>
    /// <response code="401">BranchId no válido en el token.</response>
    /// <response code="404">Sucursal no encontrada.</response>
    /// <response code="500">Error interno del servidor.</response>
    [Authorize(Policy = "AdminOnly")]
    [HttpPut("current")]
    public async Task<IActionResult> UpdateCurrentBranch([FromBody] UpdateBranchDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var branchClaim = User.FindFirst("BranchId")?.Value;
        if (branchClaim == null || !Guid.TryParse(branchClaim, out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        try
        {
            var command = new UpdateBranchCommand
            {
                BranchId = branchId,
                Name = dto.Name,
                Code = dto.Code,
                Address = dto.Address,
                Phone = dto.Phone,
                Email = dto.Email,
                IsActive = dto.IsActive
            };

            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al actualizar la sucursal.", error = ex.Message });
        }
    }
}
