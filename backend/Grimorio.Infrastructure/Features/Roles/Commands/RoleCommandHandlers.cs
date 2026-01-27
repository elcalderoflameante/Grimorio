using MediatR;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Roles.Commands;
using Grimorio.Domain.Entities.Auth;
using Grimorio.Infrastructure.Persistence;

namespace Grimorio.Infrastructure.Features.Roles.Commands;

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, RoleDto>
{
    private readonly GrimorioDbContext _context;

    public CreateRoleCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<RoleDto> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var branchId = Guid.Empty;

        var existingRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == dto.Name && r.BranchId == branchId && !r.IsDeleted, cancellationToken);

        if (existingRole != null)
            throw new InvalidOperationException("Ya existe un rol con ese nombre en esta rama.");

        var role = new Role
        {
            Id = Guid.NewGuid(),
            BranchId = branchId,
            Name = dto.Name,
            Description = dto.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync(cancellationToken);

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsActive = role.IsActive,
            Permissions = new List<string>()
        };
    }
}

public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, RoleDto>
{
    private readonly GrimorioDbContext _context;

    public UpdateRoleCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<RoleDto> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId && !r.IsDeleted, cancellationToken);

        if (role == null)
            throw new InvalidOperationException("Rol no encontrado.");

        role.Name = request.Dto.Name;
        role.Description = request.Dto.Description;
        role.IsActive = request.Dto.IsActive;
        role.UpdatedAt = DateTime.UtcNow;
        role.UpdatedBy = Guid.Empty;

        await _context.SaveChangesAsync(cancellationToken);

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsActive = role.IsActive,
            Permissions = role.RolePermissions.Select(rp => rp.Permission!.Code).ToList()
        };
    }
}

public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, bool>
{
    private readonly GrimorioDbContext _context;

    public DeleteRoleCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<bool> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == request.RoleId && !r.IsDeleted, cancellationToken);

        if (role == null)
            throw new InvalidOperationException("Rol no encontrado.");

        role.IsDeleted = true;
        role.DeletedAt = DateTime.UtcNow;
        role.DeletedBy = Guid.Empty;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class AssignPermissionsToRoleCommandHandler : IRequestHandler<AssignPermissionsToRoleCommand, bool>
{
    private readonly GrimorioDbContext _context;

    public AssignPermissionsToRoleCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<bool> Handle(AssignPermissionsToRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId && !r.IsDeleted, cancellationToken);

        if (role == null)
            throw new InvalidOperationException("Rol no encontrado.");

        _context.RolePermissions.RemoveRange(role.RolePermissions);

        foreach (var permissionId in request.PermissionIds)
        {
            var permission = await _context.Permissions
                .FirstOrDefaultAsync(p => p.Id == permissionId && !p.IsDeleted, cancellationToken);

            if (permission == null)
                throw new InvalidOperationException($"Permiso con ID {permissionId} no encontrado.");

            var rolePermission = new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = role.Id,
                PermissionId = permissionId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            };

            _context.RolePermissions.Add(rolePermission);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
