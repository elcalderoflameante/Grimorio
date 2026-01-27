using MediatR;
using Grimorio.Application.DTOs;

namespace Grimorio.Application.Features.Auth.Commands;

/// <summary>
/// Comando para autenticar un usuario y obtener JWT.
/// </summary>
public class LoginUserCommand : IRequest<AuthResponse>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
