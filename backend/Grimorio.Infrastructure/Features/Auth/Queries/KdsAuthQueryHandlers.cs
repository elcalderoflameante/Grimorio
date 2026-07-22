using MediatR;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Auth.Queries;
using Grimorio.Infrastructure.Persistence;
using Grimorio.SharedKernel.Constants;

namespace Grimorio.Infrastructure.Features.Auth.Queries;

public class GetKdsBranchesQueryHandler : IRequestHandler<GetKdsBranchesQuery, List<KdsBranchDto>>
{
    private readonly GrimorioDbContext _context;

    public GetKdsBranchesQueryHandler(GrimorioDbContext context) => _context = context;

    public Task<List<KdsBranchDto>> Handle(GetKdsBranchesQuery request, CancellationToken cancellationToken)
    {
        return _context.Branches
            .AsNoTracking()
            .Where(b => !b.IsDeleted && b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new KdsBranchDto
            {
                Id = b.Id,
                Name = b.Name,
                Code = b.Code
            })
            .ToListAsync(cancellationToken);
    }
}

public class GetKdsUsersQueryHandler : IRequestHandler<GetKdsUsersQuery, List<KdsUserDto>>
{
    private readonly GrimorioDbContext _context;

    public GetKdsUsersQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<List<KdsUserDto>> Handle(GetKdsUsersQuery request, CancellationToken cancellationToken)
    {
        var userIds = await _context.UserRoles
            .AsNoTracking()
            .Where(ur =>
                ur.BranchId == request.BranchId &&
                !ur.IsDeleted &&
                ur.Role != null &&
                !ur.Role.IsDeleted &&
                ur.Role.IsActive &&
                ur.Role.RolePermissions.Any(rp =>
                    !rp.IsDeleted &&
                    rp.Permission != null &&
                    !rp.Permission.IsDeleted &&
                    rp.Permission.IsActive &&
                    rp.Permission.Code == AppConstants.Permissions.PosKitchenView))
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return await _context.Users
            .AsNoTracking()
            .Where(u =>
                userIds.Contains(u.Id) &&
                u.BranchId == request.BranchId &&
                !u.IsDeleted &&
                u.IsActive)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Select(u => new KdsUserDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                HasKdsPin = u.KdsPinHash != null && u.KdsPinHash != ""
            })
            .ToListAsync(cancellationToken);
    }
}

public class GetWaitstaffUsersQueryHandler : IRequestHandler<GetWaitstaffUsersQuery, List<KdsUserDto>>
{
    private readonly GrimorioDbContext _context;

    public GetWaitstaffUsersQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<List<KdsUserDto>> Handle(GetWaitstaffUsersQuery request, CancellationToken cancellationToken)
    {
        var userIds = await _context.UserRoles
            .AsNoTracking()
            .Where(ur =>
                ur.BranchId == request.BranchId &&
                !ur.IsDeleted &&
                ur.Role != null &&
                !ur.Role.IsDeleted &&
                ur.Role.IsActive &&
                ur.Role.Name == AppConstants.Roles.Waiter)
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return await _context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id) && u.BranchId == request.BranchId && !u.IsDeleted && u.IsActive)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Select(u => new KdsUserDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                HasKdsPin = !string.IsNullOrEmpty(u.KdsPinHash)
            })
            .ToListAsync(cancellationToken);
    }
}
