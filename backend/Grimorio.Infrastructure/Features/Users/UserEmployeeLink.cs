using Grimorio.Domain.Entities.Organization;
using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Users;

public static class UserEmployeeLink
{
    public static async Task LockEmployeeAsync(GrimorioDbContext db, Guid branchId, Guid employeeId, CancellationToken ct)
    {
        await db.Employees.FromSqlInterpolated($"""
            SELECT * FROM organization."Employees"
            WHERE "Id" = {employeeId} AND "BranchId" = {branchId}
            FOR UPDATE
            """).IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(ct);
    }

    public static async Task<Employee?> ValidateAsync(GrimorioDbContext db, Guid branchId, Guid userId,
        Guid? employeeId, Guid? previousEmployeeId, bool activeUser, CancellationToken ct)
    {
        // Lock both sides of a relink in a stable order, also serializing employee deactivation.
        foreach (var id in new[] { previousEmployeeId, employeeId }.OfType<Guid>().Distinct().Order())
            await LockEmployeeAsync(db, branchId, id, ct);
        if (employeeId is null) return null;

        var employee = await db.Employees.IgnoreQueryFilters().FirstOrDefaultAsync(e =>
            e.Id == employeeId && e.BranchId == branchId && (!e.IsDeleted || previousEmployeeId == e.Id), ct)
            ?? throw new InvalidOperationException("El empleado no existe o pertenece a otra sucursal.");
        if (activeUser && (!employee.IsActive || employee.IsDeleted))
            throw new InvalidOperationException("No se puede habilitar una cuenta vinculada a un empleado inactivo o eliminado.");
        if (await db.Users.AnyAsync(u => u.EmployeeId == employeeId && u.Id != userId && !u.IsDeleted, ct))
            throw new InvalidOperationException("El empleado ya tiene un usuario vinculado.");
        return employee;
    }
}
