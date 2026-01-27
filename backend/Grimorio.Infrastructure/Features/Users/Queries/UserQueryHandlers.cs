using MediatR;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Users.Queries;
using Grimorio.Infrastructure.Persistence;

namespace Grimorio.Infrastructure.Features.Users.Queries;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, List<UserDto>>
{
    private readonly GrimorioDbContext _context;

    public GetUsersQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<List<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _context.Users
            .Where(u => !u.IsDeleted)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.IsActive,
                Roles = u.UserRoles.Select(ur => ur.Role!.Name).ToList(),
                RoleDetails = u.UserRoles.Select(ur => new UserRoleDto
                {
                    RoleId = ur.RoleId,
                    RoleName = ur.Role!.Name
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        return users;
    }
}

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto>
{
    private readonly GrimorioDbContext _context;

    public GetUserByIdQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Where(u => !u.IsDeleted && u.Id == request.UserId)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.IsActive,
                Roles = u.UserRoles.Select(ur => ur.Role!.Name).ToList(),
                RoleDetails = u.UserRoles.Select(ur => new UserRoleDto
                {
                    RoleId = ur.RoleId,
                    RoleName = ur.Role!.Name
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
            throw new InvalidOperationException("Usuario no encontrado.");

        return user;
    }
}
