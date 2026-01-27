using MediatR;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Security;
using Grimorio.Domain.Entities.Auth;

namespace Grimorio.Infrastructure.Features.Auth.Commands;

public class LoginUserCommandHandler : IRequestHandler<Application.Features.Auth.Commands.LoginUserCommand, AuthResponse>
{
    private readonly GrimorioDbContext _dbContext;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHashingService _passwordHashingService;

    public LoginUserCommandHandler(
        GrimorioDbContext dbContext,
        IJwtService jwtService,
        IPasswordHashingService passwordHashingService)
    {
        _dbContext = dbContext;
        _jwtService = jwtService;
        _passwordHashingService = passwordHashingService;
    }

    public async Task<AuthResponse> Handle(Application.Features.Auth.Commands.LoginUserCommand request, CancellationToken cancellationToken)
    {
        // Buscar usuario por email
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("Usuario o contraseña inválido.");

        // Validar contraseña
        if (!_passwordHashingService.VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Usuario o contraseña inválido.");

        // Obtener roles y permisos del usuario en su rama asignada
        var userRoles = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == user.Id && ur.BranchId == user.BranchId)
            .Include(ur => ur.Role)
            .ToListAsync(cancellationToken);

        if (!userRoles.Any())
            throw new UnauthorizedAccessException("Usuario no tiene roles asignados en su rama.");

        // Obtener permisos de los roles
        var roleIds = userRoles.Select(ur => ur.RoleId).ToList();
        var permissions = await _dbContext.RolePermissions
            .AsNoTracking()
            .Where(rp => roleIds.Contains(rp.RoleId) && rp.Permission!.IsActive)
            .Select(rp => rp.Permission!.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Crear JWT user
        var jwtUser = new JwtUser
        {
            UserId = user.Id,
            BranchId = user.BranchId,
            Roles = userRoles.Select(ur => ur.Role!.Name).ToList(),
            Permissions = permissions
        };

        // Generar tokens
        var accessToken = _jwtService.GenerateAccessToken(jwtUser);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Actualizar último login
        user.LastLoginAt = DateTime.UtcNow;
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            Permissions = permissions
        };
    }
}
