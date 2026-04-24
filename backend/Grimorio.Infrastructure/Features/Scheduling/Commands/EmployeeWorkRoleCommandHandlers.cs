using MediatR;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Scheduling.Commands;
using Grimorio.Domain.Entities.Scheduling;
using Grimorio.Infrastructure.Persistence;

namespace Grimorio.Infrastructure.Features.Scheduling.Commands;

public class AssignWorkRolesToEmployeeCommandHandler : IRequestHandler<AssignWorkRolesToEmployeeCommand, List<EmployeeWorkRoleDto>>
{
    private readonly GrimorioDbContext _context;

    public AssignWorkRolesToEmployeeCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<List<EmployeeWorkRoleDto>> Handle(AssignWorkRolesToEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId && !e.IsDeleted, cancellationToken);

        if (employee == null)
            throw new InvalidOperationException("Empleado no encontrado.");

        if (request.WorkRoleIds.Count > 3)
            throw new InvalidOperationException("Un empleado puede tener máximo 3 roles. El último rol tiene la menor prioridad.");

        if (request.WorkRoleIds.Count == 0)
            throw new InvalidOperationException("Un empleado debe tener al menos un rol asignado.");

        await _context.EmployeeWorkRoles
            .Where(ewr => ewr.EmployeeId == request.EmployeeId)
            .ExecuteDeleteAsync(cancellationToken);

        foreach (var (roleId, index) in request.WorkRoleIds.Select((r, i) => (r, i)))
        {
            var workRole = await _context.WorkRoles
                .FirstOrDefaultAsync(wr => wr.Id == roleId && !wr.IsDeleted, cancellationToken);

            if (workRole == null)
                throw new InvalidOperationException($"Rol de trabajo con ID {roleId} no encontrado.");

            _context.EmployeeWorkRoles.Add(new EmployeeWorkRole
            {
                EmployeeId = request.EmployeeId,
                WorkRoleId = roleId,
                IsPrimary = index == 0,
                Priority = index + 1,
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.EmployeeWorkRoles
            .Where(ewr => ewr.EmployeeId == request.EmployeeId && !ewr.IsDeleted)
            .Include(ewr => ewr.WorkRole)
            .ThenInclude(wr => wr!.WorkArea)
            .Select(ewr => new EmployeeWorkRoleDto
            {
                Id = ewr.Id,
                EmployeeId = ewr.EmployeeId,
                WorkRoleId = ewr.WorkRoleId,
                WorkRoleName = ewr.WorkRole!.Name,
                WorkAreaName = ewr.WorkRole!.WorkArea!.Name,
                IsPrimary = ewr.IsPrimary,
                Priority = ewr.Priority
            })
            .OrderByDescending(ewr => ewr.IsPrimary)
            .ThenBy(ewr => ewr.Priority)
            .ToListAsync(cancellationToken);
    }
}

public class RemoveWorkRoleFromEmployeeCommandHandler : IRequestHandler<RemoveWorkRoleFromEmployeeCommand, bool>
{
    private readonly GrimorioDbContext _context;

    public RemoveWorkRoleFromEmployeeCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<bool> Handle(RemoveWorkRoleFromEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employeeWorkRole = await _context.EmployeeWorkRoles
            .FirstOrDefaultAsync(ewr =>
                ewr.EmployeeId == request.EmployeeId &&
                ewr.WorkRoleId == request.WorkRoleId &&
                !ewr.IsDeleted, cancellationToken);

        if (employeeWorkRole == null)
            throw new InvalidOperationException("Asignación de rol no encontrada.");

        employeeWorkRole.IsDeleted = true;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
