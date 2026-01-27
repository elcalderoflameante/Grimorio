using MediatR;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Permissions.Queries;
using Grimorio.Infrastructure.Persistence;

namespace Grimorio.Infrastructure.Features.Permissions.Queries;

public class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, List<PermissionDto>>
{
    private readonly GrimorioDbContext _context;

    public GetPermissionsQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<List<PermissionDto>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissions = await _context.Permissions
            .Where(p => !p.IsDeleted)
            .Select(p => new PermissionDto
            {
                Id = p.Id,
                Code = p.Code,
                Description = p.Description,
                IsActive = p.IsActive
            })
            .ToListAsync(cancellationToken);

        return permissions;
    }
}

public class GetPermissionByIdQueryHandler : IRequestHandler<GetPermissionByIdQuery, PermissionDto>
{
    private readonly GrimorioDbContext _context;

    public GetPermissionByIdQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<PermissionDto> Handle(GetPermissionByIdQuery request, CancellationToken cancellationToken)
    {
        var permission = await _context.Permissions
            .Where(p => !p.IsDeleted && p.Id == request.PermissionId)
            .Select(p => new PermissionDto
            {
                Id = p.Id,
                Code = p.Code,
                Description = p.Description,
                IsActive = p.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (permission == null)
            throw new InvalidOperationException("Permiso no encontrado.");

        return permission;
    }
}
