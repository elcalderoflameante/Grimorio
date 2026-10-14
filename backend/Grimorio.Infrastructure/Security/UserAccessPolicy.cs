using System.Linq.Expressions;
using Grimorio.Domain.Entities.Auth;
using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Security;

public static class UserAccessPolicy
{
    public static readonly Expression<Func<User, bool>> ActiveAccount = user =>
        user.IsActive && !user.IsDeleted && (user.EmployeeId == null ||
            (user.Employee != null && user.Employee.IsActive && !user.Employee.IsDeleted &&
                user.Employee.BranchId == user.BranchId));

    private static readonly Func<User, bool> IsActive = ActiveAccount.Compile();
    public static bool CanAuthenticate(User user) => IsActive(user);

    public static Task<bool> HasActiveAccountAsync(GrimorioDbContext db, Guid userId, Guid branchId, CancellationToken ct) =>
        db.Users.AsNoTracking().Where(ActiveAccount)
            .AnyAsync(user => user.Id == userId && user.BranchId == branchId, ct);
}
