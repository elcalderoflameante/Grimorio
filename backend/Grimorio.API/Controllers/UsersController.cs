using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Users.Commands;
using Grimorio.Application.Features.Users.Queries;
using Grimorio.API.Services;
using Grimorio.SharedKernel.Constants;
using System.Security.Claims;

namespace Grimorio.API.Controllers;

/// <summary>
/// Controlador para gestionar usuarios.
/// Todos los endpoints requieren permisos de administración.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    public UsersController(IMediator mediator) => _mediator = mediator;

    [Authorize(Roles = AppConstants.Roles.Admin)]
    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId) || !Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor))
            return Unauthorized();
        try
        {
            await _mediator.Send(new ResetUserPasswordCommand
            {
                UserId = id, BranchId = branchId, ActorUserId = actor, TemporaryPassword = request.TemporaryPassword,
            }, ct);
            HttpContext.RequestServices.GetRequiredService<UserSessionRegistry>().Revoke(id);
            return Ok(new { message = "Contrasena restablecida. El usuario debe cambiarla al ingresar por la web." });
        }
        catch (InvalidOperationException error) { return BadRequest(new { message = error.Message }); }
    }

    /// <summary>
    /// Obtiene todos los usuarios.
    /// </summary>
    [Authorize(Policy = "Admin.Users.View")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetUsersQuery { BranchId = branchId }));
    }

    [Authorize(Policy = "Admin.Users.View")]
    [HttpGet("employee-options")]
    public async Task<IActionResult> GetEmployeeOptions()
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetUserEmployeeOptionsQuery { BranchId = branchId }));
    }

    /// <summary>
    /// Obtiene un usuario por su ID.
    /// </summary>
    [Authorize(Policy = "Admin.Users.View")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new GetUserByIdQuery { UserId = id, BranchId = branchId });
        if (result == null)
            return NotFound(new { message = "Usuario no encontrado." });

        return Ok(result);
    }

    /// <summary>
    /// Crea un nuevo usuario.
    /// </summary>
    [Authorize(Policy = "Admin.Users.Create")]
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
    [Authorize(Policy = "Admin.Users.Update")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        try
        {
            var result = await _mediator.Send(new UpdateUserCommand { UserId = id, BranchId = branchId, Dto = dto });
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
    [Authorize(Policy = "Admin.Users.Delete")]
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
    [Authorize(Policy = "Admin.Users.Update")]
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

    [Authorize(Policy = "Admin.Users.Update")]
    [HttpPost("{id}/kds-pin")]
    public async Task<IActionResult> SetKdsPin(Guid id, [FromBody] SetKdsPinDto dto)
    {
        try
        {
            var result = await _mediator.Send(new SetKdsPinCommand { UserId = id, Pin = dto.Pin });
            return Ok(new { success = result, message = "PIN KDS actualizado correctamente." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Error al actualizar PIN KDS.", error = ex.Message });
        }
    }

    /// <summary>
    /// Cambia la contraseña de un usuario.
    /// </summary>
    [HttpPost("{id}/change-password")]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest request)
    {
        try
        {
            // Verificar que el usuario solo pueda cambiar su propia contraseña
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var currentUserId) || currentUserId != id)
                return Forbid();

            var result = await _mediator.Send(new ChangePasswordCommand 
            { 
                UserId = id, 
                CurrentPassword = request.CurrentPassword,
                NewPassword = request.NewPassword
            });
            HttpContext.RequestServices.GetRequiredService<UserSessionRegistry>().Revoke(id);
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
    private bool TryGetBranchId(out Guid branchId) =>
        Guid.TryParse(User.FindFirst("BranchId")?.Value, out branchId);
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string TemporaryPassword { get; set; } = string.Empty;
}

