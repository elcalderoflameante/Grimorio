using MediatR;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Roles.Queries;
using Grimorio.Infrastructure.Persistence;

namespace Grimorio.Infrastructure.Features.Roles.Queries;

public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, List<RoleDto>>
{
    private readonly GrimorioDbContext _context;

    public GetRolesQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<List<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await _context.Roles
            .Where(r => !r.IsDeleted)
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsActive = r.IsActive,
                Permissions = r.RolePermissions.Select(rp => rp.Permission!.Code).ToList()
            })
            .ToListAsync(cancellationToken);

        return roles;
    }
}

public class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, RoleDto>
{
    private readonly GrimorioDbContext _context;

    public GetRoleByIdQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<RoleDto> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var role = await _context.Roles
            .Where(r => !r.IsDeleted && r.Id == request.RoleId)
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsActive = r.IsActive,
                Permissions = r.RolePermissions.Select(rp => rp.Permission!.Code).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (role == null)
            throw new InvalidOperationException("Rol no encontrado.");

        return role;
    }
}
