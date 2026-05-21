using Grimorio.SharedKernel;

namespace Grimorio.Domain.Entities.Auth;

/// <summary>
/// Entidad de usuario del sistema.
/// Incluye autenticación JWT y asociación con roles.
/// </summary>
public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    // Relaciones
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<UserPushToken> PushTokens { get; set; } = new List<UserPushToken>();
}

public class UserPushToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Platform { get; set; } = "android";
    public string? DeviceId { get; set; }
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Relaciones
    public User? User { get; set; }
}

/// <summary>
/// Entidad de rol del sistema.
/// Los roles se asignan dinámicamente y pueden tener múltiples permisos.
/// </summary>
public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Relaciones
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

/// <summary>
/// Entidad de permiso del sistema.
/// Los permisos son strings basados (e.g., "POS.Orders.Create", "Inventory.Movements.Create").
/// </summary>
public class Permission : BaseEntity
{
    public string Code { get; set; } = string.Empty; // e.g., "POS.Orders.Create"
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // e.g., "POS", "Inventory", "Cash"
    public bool IsActive { get; set; } = true;

    // Relaciones
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>
/// Tabla de unión: Rol - Permiso.
/// Define qué permisos tiene cada rol.
/// </summary>
public class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }

    // Relaciones
    public Role? Role { get; set; }
    public Permission? Permission { get; set; }
}

/// <summary>
/// Tabla de unión: Usuario - Rol.
/// Define qué roles tiene cada usuario (multi-tenant por sucursal).
/// </summary>
public class UserRole : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }

    // Relaciones
    public User? User { get; set; }
    public Role? Role { get; set; }
}
