using MediatR;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Permissions.Commands;
using Grimorio.Domain.Entities.Auth;
using Grimorio.Infrastructure.Persistence;

namespace Grimorio.Infrastructure.Features.Permissions.Commands;

public class CreatePermissionCommandHandler : IRequestHandler<CreatePermissionCommand, PermissionDto>
{
    private readonly GrimorioDbContext _context;

    public CreatePermissionCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<PermissionDto> Handle(CreatePermissionCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var existingPermission = await _context.Permissions
            .FirstOrDefaultAsync(p => p.Code == dto.Code && !p.IsDeleted, cancellationToken);

        if (existingPermission != null)
            throw new InvalidOperationException("Ya existe un permiso con ese código.");

        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            Description = dto.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        };

        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync(cancellationToken);

        return new PermissionDto
        {
            Id = permission.Id,
            Code = permission.Code,
            Description = permission.Description,
            IsActive = permission.IsActive
        };
    }
}

public class UpdatePermissionCommandHandler : IRequestHandler<UpdatePermissionCommand, PermissionDto>
{
    private readonly GrimorioDbContext _context;

    public UpdatePermissionCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<PermissionDto> Handle(UpdatePermissionCommand request, CancellationToken cancellationToken)
    {
        var permission = await _context.Permissions
            .FirstOrDefaultAsync(p => p.Id == request.PermissionId && !p.IsDeleted, cancellationToken);

        if (permission == null)
            throw new InvalidOperationException("Permiso no encontrado.");

        permission.Description = request.Dto.Description;
        permission.IsActive = request.Dto.IsActive;
        permission.UpdatedAt = DateTime.UtcNow;
        permission.UpdatedBy = Guid.Empty;

        await _context.SaveChangesAsync(cancellationToken);

        return new PermissionDto
        {
            Id = permission.Id,
            Code = permission.Code,
            Description = permission.Description,
            IsActive = permission.IsActive
        };
    }
}

public class DeletePermissionCommandHandler : IRequestHandler<DeletePermissionCommand, bool>
{
    private readonly GrimorioDbContext _context;

    public DeletePermissionCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<bool> Handle(DeletePermissionCommand request, CancellationToken cancellationToken)
    {
        var permission = await _context.Permissions
            .FirstOrDefaultAsync(p => p.Id == request.PermissionId && !p.IsDeleted, cancellationToken);

        if (permission == null)
            throw new InvalidOperationException("Permiso no encontrado.");

        permission.IsDeleted = true;
        permission.DeletedAt = DateTime.UtcNow;
        permission.DeletedBy = Guid.Empty;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
