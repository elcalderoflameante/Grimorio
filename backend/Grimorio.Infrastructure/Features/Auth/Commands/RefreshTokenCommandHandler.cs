using MediatR;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Security;

namespace Grimorio.Infrastructure.Features.Auth.Commands;

public class RefreshTokenCommandHandler : IRequestHandler<Application.Features.Auth.Commands.RefreshTokenCommand, AuthResponse>
{
    private readonly GrimorioDbContext _dbContext;
    private readonly IJwtService _jwtService;

    public RefreshTokenCommandHandler(
        GrimorioDbContext dbContext,
        IJwtService jwtService)
    {
        _dbContext = dbContext;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse> Handle(Application.Features.Auth.Commands.RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Por ahora, simplemente generamos un nuevo refresh token
        // En producción, validarías que el refresh token está en BD y no ha expirado
        
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        throw new NotImplementedException(
            "RefreshToken handler requiere implementación con BD si se desea revocation stateful. " +
            "Por ahora, genera nuevo token automáticamente en el frontend después de cierto tiempo."
        );
    }
}
