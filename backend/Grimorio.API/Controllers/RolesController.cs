using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Roles.Commands;
using Grimorio.Application.Features.Roles.Queries;

namespace Grimorio.API.Controllers;

/// <summary>
/// Controlador para gestionar roles.
/// Todos los endpoints requieren permisos de administración.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;
    public RolesController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Obtiene todos los roles.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _mediator.Send(new GetRolesQuery()));

    /// <summary>
    /// Obtiene un rol por su ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetRoleByIdQuery { RoleId = id });
        if (result == null)
            return NotFound(new { message = "Rol no encontrado." });

        return Ok(result);
    }

    /// <summary>
    /// Crea un nuevo rol.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleDto dto)
    {
        try
        {
            var result = await _mediator.Send(new CreateRoleCommand { Dto = dto });
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al crear rol.", error = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza un rol existente.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleDto dto)
    {
        try
        {
            var result = await _mediator.Send(new UpdateRoleCommand { RoleId = id, Dto = dto });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al actualizar rol.", error = ex.Message });
        }
    }

    /// <summary>
    /// Elimina un rol.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _mediator.Send(new DeleteRoleCommand { RoleId = id });
            return Ok(new { message = "Rol eliminado correctamente." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al eliminar rol.", error = ex.Message });
        }
    }

    /// <summary>
    /// Asigna permisos a un rol.
    /// </summary>
    [HttpPost("{id}/permissions")]
    public async Task<IActionResult> AssignPermissions(Guid id, [FromBody] AssignPermissionsDto dto) => Ok(await _mediator.Send(new AssignPermissionsToRoleCommand { RoleId = id, PermissionIds = dto.PermissionIds }));
}
