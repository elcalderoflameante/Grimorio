using MediatR;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Users.Commands;
using Grimorio.Domain.Entities.Auth;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Security;

namespace Grimorio.Infrastructure.Features.Users.Commands;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly GrimorioDbContext _context;
    private readonly IPasswordHashingService _passwordHashingService;

    public CreateUserCommandHandler(GrimorioDbContext context, IPasswordHashingService passwordHashingService)
    {
        _context = context;
        _passwordHashingService = passwordHashingService;
    }

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email && !u.IsDeleted, cancellationToken);

        if (existingUser != null)
            throw new InvalidOperationException("El email ya está registrado.");

        if (request.BranchId == Guid.Empty)
            throw new InvalidOperationException("BranchId requerido para crear usuario.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            BranchId = request.BranchId,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PasswordHash = _passwordHashingService.HashPassword(dto.Password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            Roles = new List<string>(),
            RoleDetails = new List<UserRoleDto>()
        };
    }
}

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDto>
{
    private readonly GrimorioDbContext _context;

    public UpdateUserCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);

        if (user == null)
            throw new InvalidOperationException("Usuario no encontrado.");

        user.FirstName = request.Dto.FirstName;
        user.LastName = request.Dto.LastName;
        user.IsActive = request.Dto.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = Guid.Empty;

        await _context.SaveChangesAsync(cancellationToken);

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            Roles = user.UserRoles.Select(ur => ur.Role!.Name).ToList(),
            RoleDetails = user.UserRoles.Select(ur => new UserRoleDto
            {
                RoleId = ur.RoleId,
                RoleName = ur.Role!.Name
            }).ToList()
        };
    }
}

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, bool>
{
    private readonly GrimorioDbContext _context;

    public DeleteUserCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);

        if (user == null)
            throw new InvalidOperationException("Usuario no encontrado.");

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.DeletedBy = Guid.Empty;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class AssignRolesToUserCommandHandler : IRequestHandler<AssignRolesToUserCommand, bool>
{
    private readonly GrimorioDbContext _context;

    public AssignRolesToUserCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<bool> Handle(AssignRolesToUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);

            if (user == null)
                throw new InvalidOperationException("Usuario no encontrado.");

            var branchId = request.BranchId;
            if (branchId == Guid.Empty)
                throw new InvalidOperationException("BranchId requerido para asignar roles.");

            if (user.BranchId == Guid.Empty)
            {
                user.BranchId = branchId;
            }
            else if (user.BranchId != branchId)
            {
                throw new InvalidOperationException("El usuario pertenece a otra rama.");
            }

            await _context.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .ExecuteDeleteAsync(cancellationToken);

            foreach (var roleId in request.RoleIds)
            {
                var role = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Id == roleId && !r.IsDeleted, cancellationToken);

                if (role == null)
                {
                    throw new InvalidOperationException($"Rol con ID {roleId} no encontrado.");
                }

                var userRole = new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    RoleId = roleId,
                    BranchId = role.BranchId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = Guid.Empty
                };

                _context.UserRoles.Add(userRole);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}
