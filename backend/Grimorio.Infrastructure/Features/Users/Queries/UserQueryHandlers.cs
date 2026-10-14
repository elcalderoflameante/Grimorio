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
            .Where(u => !u.IsDeleted && u.BranchId == request.BranchId)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Select(u => new UserDto
            {
                Id = u.Id,
                EmployeeId = u.EmployeeId,
                EmployeeName = u.Employee == null ? null : u.Employee.FirstName + " " + u.Employee.LastName,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.IsActive,
                HasKdsPin = u.KdsPinHash != null && u.KdsPinHash != "",
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
            .Where(u => !u.IsDeleted && u.Id == request.UserId && u.BranchId == request.BranchId)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Select(u => new UserDto
            {
                Id = u.Id,
                EmployeeId = u.EmployeeId,
                EmployeeName = u.Employee == null ? null : u.Employee.FirstName + " " + u.Employee.LastName,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.IsActive,
                HasKdsPin = u.KdsPinHash != null && u.KdsPinHash != "",
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

public class GetUserEmployeeOptionsHandler(GrimorioDbContext db)
    : IRequestHandler<GetUserEmployeeOptionsQuery, List<UserEmployeeOptionDto>>
{
    public Task<List<UserEmployeeOptionDto>> Handle(GetUserEmployeeOptionsQuery request, CancellationToken ct) =>
        db.Employees.AsNoTracking().Where(e => e.BranchId == request.BranchId && !e.IsDeleted)
            .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
            .Select(e => new UserEmployeeOptionDto
            {
                Id = e.Id, FirstName = e.FirstName, LastName = e.LastName,
                Email = e.Email, IdentificationNumber = e.IdentificationNumber, IsActive = e.IsActive,
                LinkedUserId = e.UserAccount == null ? null : (Guid?)e.UserAccount.Id,
            }).ToListAsync(ct);
}
