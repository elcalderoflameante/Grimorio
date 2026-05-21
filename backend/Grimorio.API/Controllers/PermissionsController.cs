using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Permissions.Commands;
using Grimorio.Application.Features.Permissions.Queries;

namespace Grimorio.API.Controllers;

/// <summary>
/// Controlador para gestionar permisos.
/// Todos los endpoints requieren permisos de administración.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PermissionsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Obtiene todos los permisos.
    /// </summary>
    [Authorize(Policy = "Admin.Permissions.View")]
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _mediator.Send(new GetPermissionsQuery()));

    /// <summary>
    /// Obtiene un permiso por su ID.
    /// </summary>
    [Authorize(Policy = "Admin.Permissions.View")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetPermissionByIdQuery { PermissionId = id });
        if (result == null)
            return NotFound(new { message = "Permiso no encontrado." });

        return Ok(result);
    }

    /// <summary>
    /// Crea un nuevo permiso.
    /// </summary>
    [Authorize(Policy = "Admin.Permissions.Manage")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePermissionDto dto)
    {
        try
        {
            var result = await _mediator.Send(new CreatePermissionCommand { Dto = dto });
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al crear permiso.", error = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza un permiso existente.
    /// </summary>
    [Authorize(Policy = "Admin.Permissions.Manage")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePermissionDto dto)
    {
        try
        {
            var result = await _mediator.Send(new UpdatePermissionCommand { PermissionId = id, Dto = dto });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al actualizar permiso.", error = ex.Message });
        }
    }

    /// <summary>
    /// Elimina un permiso.
    /// </summary>
    [Authorize(Policy = "Admin.Permissions.Manage")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _mediator.Send(new DeletePermissionCommand { PermissionId = id });
            return Ok(new { message = "Permiso eliminado correctamente." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al eliminar permiso.", error = ex.Message });
        }
    }
}
