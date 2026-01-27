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

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Autentica un usuario y devuelve JWT + refresh token.
    /// </summary>
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
    /// </summary>
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
