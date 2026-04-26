using Microsoft.EntityFrameworkCore;
using Grimorio.Application.Abstractions;
using Grimorio.Domain.Entities.Auth;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Domain.Entities.Scheduling;
using Grimorio.Domain.Entities.Payroll;
using Grimorio.Domain.Entities.Inventory;
using Grimorio.Domain.Entities.Menu;
using Grimorio.Domain.Entities.POS;
using Grimorio.Domain.Entities.Purchases;

namespace Grimorio.Infrastructure.Persistence;

/// <summary>
/// DbContext principal para Grimorio.
/// Configura todas las entidades del dominio y sus relaciones con PostgreSQL.
/// </summary>
public class GrimorioDbContext : DbContext
{
    private readonly ICurrentUserContext? _currentUserContext;

    public GrimorioDbContext(DbContextOptions<GrimorioDbContext> options, ICurrentUserContext? currentUserContext = null)
        : base(options)
    {
        _currentUserContext = currentUserContext;
    }

    // === Auth & Permissions ===
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Permission> Permissions { get; set; } = null!;
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<UserPushToken> UserPushTokens { get; set; } = null!;

    // === Organization ===
    public DbSet<Branch> Branches { get; set; } = null!;
    public DbSet<Position> Positions { get; set; } = null!;
    public DbSet<Employee> Employees { get; set; } = null!;
    public DbSet<EmployeeShift> EmployeeShifts { get; set; } = null!;
    public DbSet<EmployeeClocking> EmployeeClockings { get; set; } = null!;

    // === Scheduling ===
    public DbSet<WorkArea> WorkAreas { get; set; } = null!;
    public DbSet<WorkRole> WorkRoles { get; set; } = null!;
    public DbSet<EmployeeWorkRole> EmployeeWorkRoles { get; set; } = null!;
    public DbSet<ShiftTemplate> ShiftTemplates { get; set; } = null!;
    public DbSet<ShiftAssignment> ShiftAssignments { get; set; } = null!;
    public DbSet<EmployeeAvailability> EmployeeAvailability { get; set; } = null!;
    public DbSet<ScheduleConfiguration> ScheduleConfigurations { get; set; } = null!;
    public DbSet<SpecialDate> SpecialDates { get; set; } = null!;
    public DbSet<SpecialDateTemplate> SpecialDateTemplates { get; set; } = null!;

    // === Payroll ===
    public DbSet<PayrollConfiguration> PayrollConfigurations { get; set; } = null!;
    public DbSet<PayrollAdvance> PayrollAdvances { get; set; } = null!;
    public DbSet<EmployeeConsumption> EmployeeConsumptions { get; set; } = null!;
    public DbSet<PayrollAdjustment> PayrollAdjustments { get; set; } = null!;
    public DbSet<PayrollRoleHeader> PayrollRoleHeaders { get; set; } = null!;
    public DbSet<PayrollRoleDetail> PayrollRoleDetails { get; set; } = null!;

    // === POS ===
    public DbSet<RestaurantTable> RestaurantTables { get; set; } = null!;
    public DbSet<TableServiceRequest> TableServiceRequests { get; set; } = null!;
    public DbSet<WorkStation> WorkStations { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;

    // === Menú ===
    public DbSet<MenuCategory> MenuCategories { get; set; } = null!;
    public DbSet<MenuItem> MenuItems { get; set; } = null!;
    public DbSet<RecipeIngredient> RecipeIngredients { get; set; } = null!;

    // === Purchases ===
    public DbSet<Supplier> Suppliers { get; set; } = null!;
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; } = null!;
    public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; } = null!;

    // === Inventario ===
    public DbSet<MeasurementUnit> MeasurementUnits { get; set; } = null!;
    public DbSet<UnitConversion> UnitConversions { get; set; } = null!;
    public DbSet<InventoryCategory> InventoryCategories { get; set; } = null!;
    public DbSet<InventoryArticle> InventoryArticles { get; set; } = null!;
    public DbSet<Warehouse> Warehouses { get; set; } = null!;
    public DbSet<WarehouseStock> WarehouseStock { get; set; } = null!;
    public DbSet<StockMovement> StockMovements { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Establece la codificación por defecto y otras configuraciones PostgreSQL
        modelBuilder.HasPostgresExtension("uuid-ossp");

        // Aplica todas las configuraciones de entidades
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GrimorioDbContext).Assembly);
    }

    /// <summary>
    /// Sobrescribe SaveChanges para auditoría automática.
    /// Registra CreatedBy, UpdatedBy, UpdatedAt en todas las entidades.
    /// </summary>
    public override int SaveChanges()
    {
        ApplyAuditingAndSoftDelete();
        return base.SaveChanges();
    }

    /// <summary>
    /// Versión asincrónica de SaveChanges con auditoría.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditingAndSoftDelete();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditingAndSoftDelete()
    {
        var userId = _currentUserContext?.UserId ?? Guid.Empty;
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is not Grimorio.SharedKernel.BaseEntity baseEntity)
                continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    baseEntity.CreatedAt = now;
                    baseEntity.CreatedBy = userId;
                    break;

                case EntityState.Modified:
                    baseEntity.UpdatedAt = now;
                    baseEntity.UpdatedBy = userId;
                    if (baseEntity.IsDeleted && baseEntity.DeletedAt == null)
                    {
                        baseEntity.DeletedAt = now;
                        baseEntity.DeletedBy = userId;
                    }
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    baseEntity.IsDeleted = true;
                    baseEntity.DeletedAt = now;
                    baseEntity.DeletedBy = userId;
                    break;
            }
        }
    }
}
