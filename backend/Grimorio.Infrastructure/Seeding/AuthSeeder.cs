using Grimorio.Domain.Entities.Auth;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Security;
using Grimorio.SharedKernel.Constants;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Seeding;

/// <summary>
/// Inicializa el catalogo base de autenticacion y autorizacion.
/// El seed es idempotente: agrega permisos/roles faltantes sin borrar asignaciones existentes.
/// </summary>
public static class AuthSeeder
{
    public static async Task SeedAsync(GrimorioDbContext context, IPasswordHashingService passwordHashingService)
    {
        var branches = await EnsureBranchesAsync(context);

        foreach (var branch in branches)
        {
            await EnsurePermissionsAsync(context, branch.Id);
            var roles = await EnsureRolesAsync(context, branch.Id);
            await EnsureRolePermissionsAsync(context, branch.Id, roles);
        }

        await EnsureInitialAdminUserAsync(context, passwordHashingService, branches[0].Id);
    }

    private static async Task<List<Branch>> EnsureBranchesAsync(GrimorioDbContext context)
    {
        var branches = await context.Branches
            .Where(b => b.IsActive && !b.IsDeleted)
            .ToListAsync();

        if (branches.Count > 0)
            return branches;

        var defaultBranch = new Branch
        {
            Id = Guid.NewGuid(),
            Name = "El Caldero Flameante - Sucursal Principal",
            Code = "MAIN",
            Address = "Alfredo Escudero s25-293 e Isinlivi",
            Phone = "+593 964135806",
            Email = "admin@elcalderoflameante.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        };
        defaultBranch.BranchId = defaultBranch.Id;

        await context.Branches.AddAsync(defaultBranch);
        await context.SaveChangesAsync();

        return [defaultBranch];
    }

    private static async Task EnsurePermissionsAsync(GrimorioDbContext context, Guid branchId)
    {
        var existingPermissions = await context.Permissions
            .Where(p => p.BranchId == branchId)
            .ToDictionaryAsync(p => p.Code);

        foreach (var definition in AppConstants.Permissions.All)
        {
            if (existingPermissions.TryGetValue(definition.Code, out var permission))
            {
                permission.Description = definition.Description;
                permission.Category = definition.Category;
                permission.IsActive = true;
                permission.UpdatedAt = DateTime.UtcNow;
                permission.UpdatedBy = Guid.Empty;
                continue;
            }

            await context.Permissions.AddAsync(new Permission
            {
                Id = Guid.NewGuid(),
                BranchId = branchId,
                Code = definition.Code,
                Description = definition.Description,
                Category = definition.Category,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task<Dictionary<string, Role>> EnsureRolesAsync(GrimorioDbContext context, Guid branchId)
    {
        var roleDefinitions = new Dictionary<string, string>
        {
            [AppConstants.Roles.Admin] = "Acceso total al sistema",
            [AppConstants.Roles.Supervisor] = "Supervision operativa del restaurante",
            [AppConstants.Roles.Cashier] = "Caja, cobros y facturacion operativa",
            [AppConstants.Roles.Waiter] = "Pedidos y atencion de mesa",
            [AppConstants.Roles.Kitchen] = "Operacion de cocina y estaciones",
            [AppConstants.Roles.Warehouse] = "Inventario y bodega",
            [AppConstants.Roles.Purchases] = "Compras y proveedores",
            [AppConstants.Roles.Accounting] = "Facturacion, SRI y revision contable",
            [AppConstants.Roles.HumanResources] = "Gestion de RRHH, horarios y nomina"
        };

        var roles = await context.Roles
            .Where(r => r.BranchId == branchId)
            .ToDictionaryAsync(r => r.Name);

        foreach (var (name, description) in roleDefinitions)
        {
            if (roles.TryGetValue(name, out var role))
            {
                role.Description = description;
                role.IsActive = true;
                role.UpdatedAt = DateTime.UtcNow;
                role.UpdatedBy = Guid.Empty;
                continue;
            }

            role = new Role
            {
                Id = Guid.NewGuid(),
                BranchId = branchId,
                Name = name,
                Description = description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            };

            await context.Roles.AddAsync(role);
            roles[name] = role;
        }

        await context.SaveChangesAsync();
        return roles;
    }

    private static async Task EnsureRolePermissionsAsync(
        GrimorioDbContext context,
        Guid branchId,
        Dictionary<string, Role> roles)
    {
        var permissions = await context.Permissions
            .Where(p => p.BranchId == branchId && p.IsActive)
            .ToDictionaryAsync(p => p.Code);

        var rolePermissionCodes = new Dictionary<string, IEnumerable<string>>
        {
            [AppConstants.Roles.Admin] = permissions.Keys,
            [AppConstants.Roles.Supervisor] = permissions.Keys
                .Where(code => !code.StartsWith("Admin.Users.", StringComparison.Ordinal)
                    && !code.StartsWith("Admin.Roles.", StringComparison.Ordinal)
                    && !code.StartsWith("Admin.Permissions.", StringComparison.Ordinal)
                    && code != AppConstants.Permissions.AdminBranchUpdate
                    && code != AppConstants.Permissions.BillingTaxManage
                    && code != AppConstants.Permissions.BillingSriManage),
            [AppConstants.Roles.Cashier] =
            [
                AppConstants.Permissions.AdminBranchView,
                AppConstants.Permissions.PosOrdersView,
                AppConstants.Permissions.PosTablesView,
                AppConstants.Permissions.BillingCustomersView,
                AppConstants.Permissions.BillingCustomersManage,
                AppConstants.Permissions.BillingCashView,
                AppConstants.Permissions.BillingCashOpen,
                AppConstants.Permissions.BillingCashClose,
                AppConstants.Permissions.BillingCashCharge,
                AppConstants.Permissions.BillingCashRegistersView,
                AppConstants.Permissions.BillingPaymentMethodsView,
                AppConstants.Permissions.BillingSriView,
                AppConstants.Permissions.BillingSriGenerate
            ],
            [AppConstants.Roles.Waiter] =
            [
                AppConstants.Permissions.AdminBranchView,
                AppConstants.Permissions.MenuCategoriesView,
                AppConstants.Permissions.MenuItemsView,
                AppConstants.Permissions.PosTablesView,
                AppConstants.Permissions.PosOrdersView,
                AppConstants.Permissions.PosOrdersCreate,
                AppConstants.Permissions.PosOrdersUpdate,
                AppConstants.Permissions.PosTableRequestsView,
                AppConstants.Permissions.PosTableRequestsUpdate
            ],
            [AppConstants.Roles.Kitchen] =
            [
                AppConstants.Permissions.AdminBranchView,
                AppConstants.Permissions.PosKitchenView,
                AppConstants.Permissions.PosKitchenUpdate,
                AppConstants.Permissions.PosStationsView
            ],
            [AppConstants.Roles.Warehouse] =
            [
                AppConstants.Permissions.AdminBranchView,
                AppConstants.Permissions.InventoryConfigView,
                AppConstants.Permissions.InventoryConfigManage,
                AppConstants.Permissions.InventoryArticlesView,
                AppConstants.Permissions.InventoryArticlesManage,
                AppConstants.Permissions.InventoryStockView,
                AppConstants.Permissions.InventoryMovementsView,
                AppConstants.Permissions.InventoryMovementsCreate,
                AppConstants.Permissions.PurchasesSuppliersView,
                AppConstants.Permissions.PurchasesOrdersView
            ],
            [AppConstants.Roles.Purchases] =
            [
                AppConstants.Permissions.AdminBranchView,
                AppConstants.Permissions.PurchasesSuppliersView,
                AppConstants.Permissions.PurchasesSuppliersManage,
                AppConstants.Permissions.PurchasesOrdersView,
                AppConstants.Permissions.PurchasesOrdersCreate,
                AppConstants.Permissions.PurchasesOrdersUpdate,
                AppConstants.Permissions.PurchasesOrdersCancel,
                AppConstants.Permissions.PurchasesOrdersDelete,
                AppConstants.Permissions.InventoryArticlesView,
                AppConstants.Permissions.InventoryStockView,
                AppConstants.Permissions.InventoryMovementsView
            ],
            [AppConstants.Roles.Accounting] = permissions.Keys
                .Where(code => code.StartsWith("Billing.", StringComparison.Ordinal)
                    || code == AppConstants.Permissions.AdminBranchView
                    || code == AppConstants.Permissions.PurchasesSuppliersView
                    || code == AppConstants.Permissions.PurchasesOrdersView
                    || code == AppConstants.Permissions.InventoryStockView),
            [AppConstants.Roles.HumanResources] = permissions.Keys
                .Where(code => code.StartsWith("RRHH.", StringComparison.Ordinal)
                    || code == AppConstants.Permissions.AdminBranchView)
        };

        var existingRolePermissions = await context.RolePermissions
            .Where(rp => rp.BranchId == branchId)
            .Select(rp => new { rp.RoleId, rp.PermissionId })
            .ToListAsync();

        var existingPairs = existingRolePermissions
            .Select(rp => (rp.RoleId, rp.PermissionId))
            .ToHashSet();

        foreach (var (roleName, permissionCodes) in rolePermissionCodes)
        {
            if (!roles.TryGetValue(roleName, out var role))
                continue;

            foreach (var code in permissionCodes.Distinct())
            {
                if (!permissions.TryGetValue(code, out var permission))
                    continue;

                var pair = (role.Id, permission.Id);
                if (existingPairs.Contains(pair))
                    continue;

                await context.RolePermissions.AddAsync(new RolePermission
                {
                    Id = Guid.NewGuid(),
                    BranchId = branchId,
                    RoleId = role.Id,
                    PermissionId = permission.Id,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = Guid.Empty
                });
                existingPairs.Add(pair);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureInitialAdminUserAsync(
        GrimorioDbContext context,
        IPasswordHashingService passwordHashingService,
        Guid branchId)
    {
        if (await context.Users.AnyAsync())
            return;

        var adminRole = await context.Roles
            .FirstAsync(r => r.BranchId == branchId && r.Name == AppConstants.Roles.Admin);

        var adminEmail = Environment.GetEnvironmentVariable("GRIMORIO_ADMIN_EMAIL")
            ?? "admin@elcalderoflameante.com";
        var adminPassword = Environment.GetEnvironmentVariable("GRIMORIO_ADMIN_PASSWORD")
            ?? "Admin123";

        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            BranchId = branchId,
            Email = adminEmail,
            PasswordHash = passwordHashingService.HashPassword(adminPassword),
            FirstName = "Administrador",
            LastName = "Sistema",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        };

        await context.Users.AddAsync(adminUser);
        await context.SaveChangesAsync();

        await context.UserRoles.AddAsync(new UserRole
        {
            Id = Guid.NewGuid(),
            BranchId = branchId,
            UserId = adminUser.Id,
            RoleId = adminRole.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        });

        await context.SaveChangesAsync();
    }
}
