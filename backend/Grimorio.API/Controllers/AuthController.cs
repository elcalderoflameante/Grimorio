using MediatR;
using Microsoft.AspNetCore.Mvc;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Auth.Commands;

namespace Grimorio.API.Controllers;

/// <summary>
/// Controlador para autenticación y autorización.
/// Maneja login, refresh tokens, y otros endpoints de auth.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Inicializa una nueva instancia del controlador de autenticación.
    /// </summary>
    /// <param name="mediator">Instancia de MediatR para enviar comandos.</param>
    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Autentica un usuario y devuelve JWT + refresh token.
    /// </summary>
    /// <param name="request">Solicitud de login con email y contraseña.</param>
    /// <returns>AuthResponse con tokens y datos del usuario.</returns>
    /// <response code="200">Autenticación exitosa, devuelve JWT y refresh token.</response>
    /// <response code="400">Solicitud inválida (ModelState no válido).</response>
    /// <response code="401">Credenciales inválidas o usuario inactivo.</response>
    /// <response code="500">Error interno del servidor al procesar login.</response>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var command = new LoginUserCommand
            {
                Email = request.Email,
                Password = request.Password
            };

            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor.", error = ex.Message });
        }
    }

    /// <summary>
    /// Refresca el JWT usando un refresh token.
    /// Nota: Por ahora no implementado completamente.
    /// El refresh token debe ser incluido en el body de la solicitud.
    /// </summary>
    /// <param name="request">Solicitud con refresh token válido.</param>
    /// <returns>AuthResponse con nuevo JWT.</returns>
    /// <response code="200">Refresh exitoso, devuelve nuevo JWT.</response>
    /// <response code="400">Solicitud inválida (ModelState no válido).</response>
    /// <response code="500">Error al refrescar token (refresh token inválido o expirado).</response>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var command = new RefreshTokenCommand
            {
                RefreshToken = request.RefreshToken
            };

            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al refrescar token.", error = ex.Message });
        }
    }
}
