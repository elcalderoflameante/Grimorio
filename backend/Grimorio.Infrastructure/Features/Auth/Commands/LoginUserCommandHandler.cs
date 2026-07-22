using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Auth.Commands;
using Grimorio.Domain.Entities.Auth;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Security;
using Grimorio.SharedKernel.Constants;

namespace Grimorio.Infrastructure.Features.Auth.Commands;

public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, AuthResponse>
{
    private readonly GrimorioDbContext _dbContext;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly int _accessTokenExpirationMinutes;

    public LoginUserCommandHandler(
        GrimorioDbContext dbContext,
        IJwtService jwtService,
        IPasswordHashingService passwordHashingService,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _jwtService = jwtService;
        _passwordHashingService = passwordHashingService;
        _accessTokenExpirationMinutes = int.Parse(
            configuration["JwtSettings:AccessTokenExpirationMinutes"] ?? "15"
        );
    }

    public async Task<AuthResponse> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("Usuario o contraseña inválido.");

        if (!_passwordHashingService.VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Usuario o contraseña inválido.");

        return await BuildAuthResponse(user, requireKitchenPermission: false, cancellationToken);
    }

    protected async Task<AuthResponse> BuildAuthResponse(
        User user,
        bool requireKitchenPermission,
        CancellationToken cancellationToken)
    {
        var userRoles = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == user.Id && ur.BranchId == user.BranchId)
            .Include(ur => ur.Role)
            .ToListAsync(cancellationToken);

        if (!userRoles.Any())
            throw new UnauthorizedAccessException("Usuario no tiene roles asignados en su rama.");

        var activeRoleNames = userRoles
            .Where(ur => ur.Role != null && ur.Role.IsActive)
            .Select(ur => ur.Role!.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .ToList();

        if (!activeRoleNames.Any())
            throw new UnauthorizedAccessException("Usuario no tiene roles activos en su rama.");

        var roleIds = userRoles
            .Where(ur => ur.Role != null && ur.Role.IsActive)
            .Select(ur => ur.RoleId)
            .Distinct()
            .ToList();

        var permissions = await _dbContext.RolePermissions
            .AsNoTracking()
            .Where(rp => roleIds.Contains(rp.RoleId) && rp.Permission != null && rp.Permission.IsActive)
            .Select(rp => rp.Permission!.Code)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct()
            .ToListAsync(cancellationToken);

        if (requireKitchenPermission && !permissions.Contains(AppConstants.Permissions.PosKitchenView))
            throw new UnauthorizedAccessException("Usuario no tiene permiso para usar estaciones.");

        var jwtUser = new JwtUser
        {
            UserId = user.Id,
            BranchId = user.BranchId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Roles = activeRoleNames,
            Permissions = permissions
        };

        var accessToken = _jwtService.GenerateAccessToken(jwtUser);
        var refreshToken = _jwtService.GenerateRefreshToken();

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
            ExpiresAt = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            Permissions = permissions
        };
    }
}

public class KdsLoginCommandHandler : IRequestHandler<KdsLoginCommand, AuthResponse>
{
    private readonly GrimorioDbContext _dbContext;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly int _accessTokenExpirationMinutes;

    public KdsLoginCommandHandler(
        GrimorioDbContext dbContext,
        IJwtService jwtService,
        IPasswordHashingService passwordHashingService,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _jwtService = jwtService;
        _passwordHashingService = passwordHashingService;
        _accessTokenExpirationMinutes = int.Parse(
            configuration["JwtSettings:AccessTokenExpirationMinutes"] ?? "15"
        );
    }

    public async Task<AuthResponse> Handle(KdsLoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u =>
                u.Id == request.UserId &&
                u.BranchId == request.BranchId &&
                !u.IsDeleted,
                cancellationToken);

        if (user == null || !user.IsActive || string.IsNullOrWhiteSpace(user.KdsPinHash))
            throw new UnauthorizedAccessException("PIN inválido.");

        if (!_passwordHashingService.VerifyPassword(request.Pin, user.KdsPinHash))
            throw new UnauthorizedAccessException("PIN inválido.");

        var userRoles = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == user.Id && ur.BranchId == user.BranchId)
            .Include(ur => ur.Role)
            .ToListAsync(cancellationToken);

        if (!userRoles.Any())
            throw new UnauthorizedAccessException("Usuario no tiene roles asignados en su rama.");

        var activeRoleNames = userRoles
            .Where(ur => ur.Role != null && ur.Role.IsActive)
            .Select(ur => ur.Role!.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .ToList();

        if (!activeRoleNames.Any())
            throw new UnauthorizedAccessException("Usuario no tiene roles activos en su rama.");

        var roleIds = userRoles
            .Where(ur => ur.Role != null && ur.Role.IsActive)
            .Select(ur => ur.RoleId)
            .Distinct()
            .ToList();

        var permissions = await _dbContext.RolePermissions
            .AsNoTracking()
            .Where(rp => roleIds.Contains(rp.RoleId) && rp.Permission != null && rp.Permission.IsActive)
            .Select(rp => rp.Permission!.Code)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct()
            .ToListAsync(cancellationToken);

        if (request.RequireWaitstaffRole)
        {
            if (!activeRoleNames.Contains(AppConstants.Roles.Waiter))
                throw new UnauthorizedAccessException("Usuario no tiene rol de mesero.");
        }
        else if (!permissions.Contains(AppConstants.Permissions.PosKitchenView))
        {
            throw new UnauthorizedAccessException("Usuario no tiene permiso para usar estaciones.");
        }

        var jwtUser = new JwtUser
        {
            UserId = user.Id,
            BranchId = user.BranchId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Roles = activeRoleNames,
            Permissions = permissions
        };

        var accessToken = _jwtService.GenerateAccessToken(jwtUser);
        var refreshToken = _jwtService.GenerateRefreshToken();

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
            ExpiresAt = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            Permissions = permissions
        };
    }
}
