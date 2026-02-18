using Microsoft.EntityFrameworkCore;
using Grimorio.Domain.Entities.Auth;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Domain.Entities.Scheduling;

namespace Grimorio.Infrastructure.Persistence;

/// <summary>
/// DbContext principal para Grimorio.
/// Configura todas las entidades del dominio y sus relaciones con PostgreSQL.
/// </summary>
public class GrimorioDbContext : DbContext
{
    public GrimorioDbContext(DbContextOptions<GrimorioDbContext> options)
        : base(options)
    {
    }

    // === Auth & Permissions ===
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Permission> Permissions { get; set; } = null!;
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;

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

    /// <summary>
    /// Aplica auditoría y soft delete automáticamente a todas las entidades.
    /// TODO: Obtener userId del contexto autenticado (IUserContext o similar).
    /// </summary>
    private void ApplyAuditingAndSoftDelete()
    {
        var entries = ChangeTracker.Entries();

        foreach (var entry in entries)
        {
            if (entry.Entity is not Grimorio.SharedKernel.BaseEntity baseEntity)
                continue;

            // TODO: Inyectar IUserContext para obtener el usuario actual
            var userId = Guid.NewGuid(); // Placeholder por ahora

            switch (entry.State)
            {
                case EntityState.Added:
                    baseEntity.CreatedAt = DateTime.UtcNow;
                    baseEntity.CreatedBy = userId;
                    break;

                case EntityState.Modified:
                    baseEntity.UpdatedAt = DateTime.UtcNow;
                    baseEntity.UpdatedBy = userId;
                    break;

                case EntityState.Deleted:
                    // Soft delete: marcar como eliminado en lugar de borrar
                    entry.State = EntityState.Modified;
                    baseEntity.IsDeleted = true;
                    baseEntity.DeletedAt = DateTime.UtcNow;
                    baseEntity.DeletedBy = userId;
                    break;
            }
        }
    }
}
