using Grimorio.Domain.Entities.Auth;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Seeding;

/// <summary>
/// Seeder para inicializar datos de autenticación y autorización.
/// Crea usuario admin, rama default, roles y permisos iniciales.
/// </summary>
public static class AuthSeeder
{
    public static async Task SeedAsync(GrimorioDbContext context, IPasswordHashingService passwordHashingService)
    {
        // Solo ejecutar si no hay datos
        if (await context.Users.AnyAsync())
            return;

        // 1. Crear rama default
        var defaultBranch = new Branch
        {
            Id = Guid.NewGuid(),
            BranchId = Guid.NewGuid(), // Será asignado al crear, usamos nuevo
            Name = "El Caldero Flameante - Sucursal Principal",
            Code = "MAIN",
            Address = "Alfredo Escudero s25-293 e Isinlivi",
            Phone = "+593 2 1234567",
            Email = "admin@elcalderoflameante.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty, // Sistema
            UpdatedAt = null,
            UpdatedBy = null,
            IsDeleted = false
        };

        // Usar BranchId como el mismo ID
        defaultBranch.BranchId = defaultBranch.Id;

        await context.Branches.AddAsync(defaultBranch);
        await context.SaveChangesAsync();

        // 2. Crear permisos base
        var permissions = new List<Permission>
        {
            // POS
            new Permission
            {
                Id = Guid.NewGuid(),
                BranchId = defaultBranch.Id,
                Code = "POS.Sell",
                Description = "Realizar ventas en POS",
                Category = "POS",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            },
            new Permission
            {
                Id = Guid.NewGuid(),
                BranchId = defaultBranch.Id,
                Code = "POS.ViewReports",
                Description = "Ver reportes de POS",
                Category = "POS",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            },
            // Inventory
            new Permission
            {
                Id = Guid.NewGuid(),
                BranchId = defaultBranch.Id,
                Code = "Inventory.Adjust",
                Description = "Ajustar inventario",
                Category = "Inventory",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            },
            new Permission
            {
                Id = Guid.NewGuid(),
                BranchId = defaultBranch.Id,
                Code = "Inventory.View",
                Description = "Ver inventario",
                Category = "Inventory",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            },
            // Cash
            new Permission
            {
                Id = Guid.NewGuid(),
                BranchId = defaultBranch.Id,
                Code = "Cash.Close",
                Description = "Cerrar caja",
                Category = "Cash",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            },
            // Admin
            new Permission
            {
                Id = Guid.NewGuid(),
                BranchId = defaultBranch.Id,
                Code = "Admin.ManageUsers",
                Description = "Gestionar usuarios",
                Category = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            },
            new Permission
            {
                Id = Guid.NewGuid(),
                BranchId = defaultBranch.Id,
                Code = "Admin.ManageRoles",
                Description = "Gestionar roles",
                Category = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            }
        };

        await context.Permissions.AddRangeAsync(permissions);
        await context.SaveChangesAsync();

        // 3. Crear roles
        var adminRole = new Role
        {
            Id = Guid.NewGuid(),
            BranchId = defaultBranch.Id,
            Name = "Administrador",
            Description = "Acceso total al sistema",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        };

        var vendorRole = new Role
        {
            Id = Guid.NewGuid(),
            BranchId = defaultBranch.Id,
            Name = "Vendedor",
            Description = "Acceso a POS y ventas",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        };

        await context.Roles.AddRangeAsync(adminRole, vendorRole);
        await context.SaveChangesAsync();

        // 4. Asignar permisos a roles
        var rolePermissions = new List<RolePermission>();

        // Administrador tiene todos los permisos
        foreach (var permission in permissions)
        {
            rolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                BranchId = defaultBranch.Id,
                RoleId = adminRole.Id,
                PermissionId = permission.Id,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            });
        }

        // Vendedor tiene permisos limitados
        var vendorPermissions = permissions.Where(p => 
            p.Code.StartsWith("POS.") || p.Code == "Inventory.View").ToList();

        foreach (var permission in vendorPermissions)
        {
            rolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                BranchId = defaultBranch.Id,
                RoleId = vendorRole.Id,
                PermissionId = permission.Id,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            });
        }

        await context.RolePermissions.AddRangeAsync(rolePermissions);
        await context.SaveChangesAsync();

        // 5. Crear usuario admin
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            BranchId = defaultBranch.Id,
            Email = "admin@elcalderoflameante.com",
            PasswordHash = passwordHashingService.HashPassword("Admin123"),
            FirstName = "Administrador",
            LastName = "Sistema",
            IsActive = true,
            LastLoginAt = null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        };

        await context.Users.AddAsync(adminUser);
        await context.SaveChangesAsync();

        // 6. Asignar rol Administrador al usuario admin
        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            BranchId = defaultBranch.Id,
            UserId = adminUser.Id,
            RoleId = adminRole.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        };

        await context.UserRoles.AddAsync(userRole);
        await context.SaveChangesAsync();
    }
}
