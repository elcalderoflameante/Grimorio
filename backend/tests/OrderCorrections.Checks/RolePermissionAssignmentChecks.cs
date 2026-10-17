using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Grimorio.Domain.Entities.Auth;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Security;
using Grimorio.Infrastructure.Seeding;
using Grimorio.SharedKernel.Constants;
using Microsoft.EntityFrameworkCore;

internal static class RolePermissionAssignmentChecks
{
    internal static async Task Run(IServiceProvider services, HttpClient client, string adminToken, string deniedToken, string foreignToken,
        Guid branchId, Guid foreignBranchId, Action<bool, string> check)
    {
        async Task Read(Func<GrimorioDbContext, Task> action)
        {
            await using var scope = services.CreateAsyncScope();
            await action(scope.ServiceProvider.GetRequiredService<GrimorioDbContext>());
        }
        var role = new Role { BranchId = branchId, Name = "Assignment checks" };
        var first = new Permission { BranchId = branchId, Code = "Checks.First", Description = "First" };
        var second = new Permission { BranchId = branchId, Code = "Checks.Second", Description = "Second" };
        var foreign = new Permission { BranchId = foreignBranchId, Code = "Checks.Foreign", Description = "Foreign" };
        var original = new RolePermission { BranchId = branchId, RoleId = role.Id, PermissionId = first.Id };
        await Read(async db => { db.AddRange(role, first, second, foreign, original); await db.SaveChangesAsync(); });

        async Task Assign(Guid[] ids, HttpStatusCode expected = HttpStatusCode.OK, string? token = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/roles/{role.Id}/permissions")
            { Content = JsonContent.Create(new { permissionIds = ids }) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token ?? adminToken);
            using var response = await client.SendAsync(request);
            check(response.StatusCode == expected, $"Permission assignment expected {expected}, received {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }

        await Assign([first.Id, second.Id]);
        await Read(async db => {
            var grants = await db.RolePermissions.Where(g => g.RoleId == role.Id).ToListAsync();
            check(grants.Count == 2 && grants.Single(g => g.PermissionId == first.Id).Id == original.Id
                && grants.All(g => g.BranchId == branchId), "Adding permission preserves existing grant identity and records branch");
        });
        await Assign([first.Id, second.Id]);
        await Assign([first.Id, first.Id, second.Id]);
        await Read(async db => check(await db.RolePermissions.IgnoreQueryFilters().CountAsync(g => g.RoleId == role.Id) == 2,
            "Repeated and duplicate selections do not create duplicate grants"));
        await Assign([second.Id]);
        await Read(async db => check(!await db.RolePermissions.AnyAsync(g => g.Id == original.Id)
            && await db.RolePermissions.IgnoreQueryFilters().AnyAsync(g => g.Id == original.Id && g.IsDeleted),
            "Removing permission soft-deletes only its assignment"));
        await Assign([first.Id, second.Id]);
        await Read(async db => {
            var grant = await db.RolePermissions.SingleAsync(g => g.PermissionId == first.Id && g.RoleId == role.Id);
            check(grant.Id == original.Id && grant.DeletedAt == null && grant.DeletedBy == null,
                "Reassigning permission restores the original grant without unique-index conflict");
        });
        await Assign([second.Id, Guid.NewGuid()], HttpStatusCode.BadRequest);
        await Assign([second.Id, foreign.Id], HttpStatusCode.BadRequest);
        await Assign([second.Id], HttpStatusCode.BadRequest, foreignToken);
        await Assign([second.Id], HttpStatusCode.Forbidden, deniedToken);
        await Read(async db => check(await db.RolePermissions.CountAsync(g => g.RoleId == role.Id) == 2,
            "Unknown, foreign and unauthorized assignment requests preserve all current grants"));
        await Task.WhenAll(Assign([first.Id, second.Id]), Assign([first.Id, second.Id]));
        await Read(async db => check(await db.RolePermissions.IgnoreQueryFilters().CountAsync(g => g.RoleId == role.Id) == 2,
            "Concurrent repeated saves keep one row per role and permission"));

        await Read(db => AuthSeeder.SeedAsync(db, new PasswordHashingService()));
        Guid revokedId = Guid.Empty;
        await Read(async db => {
            var cashier = await db.Roles.SingleAsync(r => r.BranchId == branchId && r.Name == AppConstants.Roles.Cashier);
            var grant = await db.RolePermissions.Include(g => g.Permission).SingleAsync(g => g.RoleId == cashier.Id
                && g.Permission!.Code == AppConstants.Permissions.BillingCashView);
            revokedId = grant.Id;
            grant.BranchId = Guid.Empty;
            db.RolePermissions.Remove(grant);
            await db.SaveChangesAsync();
        });
        await Read(db => AuthSeeder.SeedAsync(db, new PasswordHashingService()));
        await Read(db => AuthSeeder.SeedAsync(db, new PasswordHashingService()));
        await Read(async db => check(await db.RolePermissions.IgnoreQueryFilters().AnyAsync(g => g.Id == revokedId && g.IsDeleted)
            && !await db.RolePermissions.AnyAsync(g => g.Id == revokedId),
            "Repeated startup seed preserves explicit revocations, including legacy grants without branch metadata"));
    }
}
