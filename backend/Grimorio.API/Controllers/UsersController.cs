using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Users.Commands;
using Grimorio.Application.Features.Users.Queries;

namespace Grimorio.API.Controllers;

/// <summary>
/// Controlador para gestionar usuarios.
/// Todos los endpoints requieren permisos de administración.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    public UsersController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Obtiene todos los usuarios.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _mediator.Send(new GetUsersQuery()));

    /// <summary>
    /// Obtiene un usuario por su ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetUserByIdQuery { UserId = id });
        if (result == null)
            return NotFound(new { message = "Usuario no encontrado." });

        return Ok(result);
    }

    /// <summary>
    /// Crea un nuevo usuario.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        var branchClaim = User.FindFirst("BranchId")?.Value;
        if (branchClaim == null || !Guid.TryParse(branchClaim, out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        try
        {
            var result = await _mediator.Send(new CreateUserCommand { Dto = dto, BranchId = branchId });
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al crear usuario.", error = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza un usuario existente.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto)
    {
        try
        {
            var result = await _mediator.Send(new UpdateUserCommand { UserId = id, Dto = dto });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al actualizar usuario.", error = ex.Message });
        }
    }

    /// <summary>
    /// Elimina un usuario.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _mediator.Send(new DeleteUserCommand { UserId = id });
            return Ok(new { message = "Usuario eliminado correctamente." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al eliminar usuario.", error = ex.Message });
        }
    }

    /// <summary>
    /// Asigna roles a un usuario.
    /// </summary>
    [HttpPost("{id}/roles")]
    public async Task<IActionResult> AssignRoles(Guid id, [FromBody] AssignRolesDto dto)
    {
        try
        {
            var branchClaim = User.FindFirst("BranchId")?.Value;
            if (branchClaim == null || !Guid.TryParse(branchClaim, out var branchId))
                return Unauthorized("BranchId no válido en el token.");

            // Convertir strings a GUIDs
            var roleIds = new List<Guid>();
            foreach (var roleIdStr in dto.RoleIds)
            {
                if (!Guid.TryParse(roleIdStr, out var roleId))
                {
                    return BadRequest($"ID de rol inválido: {roleIdStr}");
                }
                roleIds.Add(roleId);
            }

            return Ok(await _mediator.Send(new AssignRolesToUserCommand { UserId = id, RoleIds = roleIds, BranchId = branchId }));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UsersController.AssignRoles Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// Cambia la contraseña de un usuario.
    /// </summary>
    [HttpPost("{id}/change-password")]
    [AllowAnonymous] // Permitir acceso sin ser admin para cambiar contraseña propia
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest request)
    {
        try
        {
            // Verificar que el usuario solo pueda cambiar su propia contraseña
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var currentUserId) || currentUserId != id)
                return Forbid("No puedes cambiar la contraseña de otro usuario.");

            var result = await _mediator.Send(new ChangePasswordCommand 
            { 
                UserId = id, 
                CurrentPassword = request.CurrentPassword,
                NewPassword = request.NewPassword
            });
            return Ok(new { success = result, message = "Contraseña actualizada correctamente." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UsersController.ChangePassword Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

