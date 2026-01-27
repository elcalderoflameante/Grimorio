using MediatR;
using Grimorio.Application.DTOs;

namespace Grimorio.Application.Features.Auth.Commands;

/// <summary>
/// Comando para refrescar el JWT usando un refresh token.
/// </summary>
public class RefreshTokenCommand : IRequest<AuthResponse>
{
    public string RefreshToken { get; set; } = string.Empty;
}
