using MediatR;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Scheduling.Commands;
using Grimorio.Domain.Entities.Scheduling;
using Grimorio.Infrastructure.Persistence;

namespace Grimorio.Infrastructure.Features.Scheduling.Commands;

public class CreateShiftAssignmentCommandHandler : IRequestHandler<CreateShiftAssignmentCommand, ShiftAssignmentDto>
{
    private readonly GrimorioDbContext _context;

    public CreateShiftAssignmentCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<ShiftAssignmentDto> Handle(CreateShiftAssignmentCommand request, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId && !e.IsDeleted, cancellationToken);

        if (employee == null)
            throw new InvalidOperationException("Empleado no encontrado.");

        var workArea = await _context.WorkAreas
            .FirstOrDefaultAsync(w => w.Id == request.WorkAreaId && !w.IsDeleted, cancellationToken);

        if (workArea == null)
            throw new InvalidOperationException("Área de trabajo no encontrada.");

        var workRole = await _context.WorkRoles
            .FirstOrDefaultAsync(wr => wr.Id == request.WorkRoleId && !wr.IsDeleted, cancellationToken);

        if (workRole == null)
            throw new InvalidOperationException("Rol de trabajo no encontrado.");

        var matchingTemplates = await _context.ShiftTemplates
            .AsNoTracking()
            .Where(st =>
                st.BranchId == employee.BranchId &&
                st.DayOfWeek == request.Date.DayOfWeek &&
                st.WorkAreaId == request.WorkAreaId &&
                st.WorkRoleId == request.WorkRoleId &&
                st.StartTime == request.StartTime &&
                st.EndTime == request.EndTime &&
                !st.IsDeleted)
            .ToListAsync(cancellationToken);

        if (matchingTemplates.Count == 0)
            throw new InvalidOperationException("El turno no coincide con ninguna plantilla creada para ese día, área, rol y horario.");

        var requiredTemplateCount = matchingTemplates.Sum(st => st.RequiredCount);

        var assignedTemplateCount = await _context.ShiftAssignments
            .CountAsync(sa =>
                sa.BranchId == employee.BranchId &&
                sa.Date.Date == request.Date.Date &&
                sa.WorkAreaId == request.WorkAreaId &&
                sa.WorkRoleId == request.WorkRoleId &&
                sa.StartTime == request.StartTime &&
                sa.EndTime == request.EndTime &&
                !sa.IsDeleted, cancellationToken);

        if (assignedTemplateCount >= requiredTemplateCount)
            throw new InvalidOperationException("La plantilla para ese turno ya tiene todos sus cupos asignados.");

        var employeeAlreadyAssigned = await _context.ShiftAssignments
            .AnyAsync(sa =>
                sa.EmployeeId == request.EmployeeId &&
                sa.Date.Date == request.Date.Date &&
                !sa.IsDeleted, cancellationToken);

        if (employeeAlreadyAssigned)
            throw new InvalidOperationException("El empleado ya tiene un turno asignado en este día.");

        var breakMinutes = request.BreakDuration?.TotalMinutes ?? 0;
        var lunchMinutes = request.LunchDuration?.TotalMinutes ?? 0;
        var totalMinutes = (request.EndTime - request.StartTime).TotalMinutes - breakMinutes - lunchMinutes;
        var workedHours = (decimal)(totalMinutes / 60.0);

        var shiftAssignment = new ShiftAssignment
        {
            BranchId = employee.BranchId,
            EmployeeId = request.EmployeeId,
            Date = request.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            BreakDuration = request.BreakDuration,
            LunchDuration = request.LunchDuration,
            WorkAreaId = request.WorkAreaId,
            WorkRoleId = request.WorkRoleId,
            WorkedHours = workedHours,
            Notes = request.Notes,
            IsApproved = false,
        };

        _context.ShiftAssignments.Add(shiftAssignment);
        await _context.SaveChangesAsync(cancellationToken);

        return new ShiftAssignmentDto
        {
            Id = shiftAssignment.Id,
            EmployeeId = shiftAssignment.EmployeeId,
            EmployeeName = $"{employee.FirstName} {employee.LastName}",
            Date = shiftAssignment.Date,
            StartTime = shiftAssignment.StartTime,
            EndTime = shiftAssignment.EndTime,
            BreakDuration = shiftAssignment.BreakDuration,
            LunchDuration = shiftAssignment.LunchDuration,
            WorkAreaId = shiftAssignment.WorkAreaId,
            WorkAreaName = workArea.Name,
            WorkAreaColor = workArea.Color,
            WorkRoleId = shiftAssignment.WorkRoleId,
            WorkRoleName = workRole.Name,
            WorkedHours = shiftAssignment.WorkedHours,
            Notes = shiftAssignment.Notes,
            IsApproved = shiftAssignment.IsApproved,
        };
    }
}

public class UpdateShiftAssignmentCommandHandler : IRequestHandler<UpdateShiftAssignmentCommand, ShiftAssignmentDto>
{
    private readonly GrimorioDbContext _context;

    public UpdateShiftAssignmentCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<ShiftAssignmentDto> Handle(UpdateShiftAssignmentCommand request, CancellationToken cancellationToken)
    {
        var shiftAssignment = await _context.ShiftAssignments
            .FirstOrDefaultAsync(sa => sa.Id == request.Id && !sa.IsDeleted, cancellationToken);

        if (shiftAssignment == null)
            throw new InvalidOperationException("Asignación de turno no encontrada.");

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId && !e.IsDeleted, cancellationToken);

        if (employee == null)
            throw new InvalidOperationException("Empleado no encontrado.");

        var workArea = await _context.WorkAreas
            .FirstOrDefaultAsync(w => w.Id == shiftAssignment.WorkAreaId && !w.IsDeleted, cancellationToken);

        if (workArea == null)
            throw new InvalidOperationException("Área de trabajo no encontrada.");

        var workRole = await _context.WorkRoles
            .FirstOrDefaultAsync(wr => wr.Id == shiftAssignment.WorkRoleId && !wr.IsDeleted, cancellationToken);

        if (workRole == null)
            throw new InvalidOperationException("Rol de trabajo no encontrado.");

        var hasRole = await _context.EmployeeWorkRoles
            .AnyAsync(ewr =>
                ewr.EmployeeId == request.EmployeeId &&
                ewr.WorkRoleId == shiftAssignment.WorkRoleId &&
                !ewr.IsDeleted, cancellationToken);

        if (!hasRole)
            throw new InvalidOperationException("El empleado no tiene asignado el rol de este turno.");

        var matchingTemplates = await _context.ShiftTemplates
            .AsNoTracking()
            .Where(st =>
                st.BranchId == employee.BranchId &&
                st.DayOfWeek == request.Date.DayOfWeek &&
                st.WorkAreaId == shiftAssignment.WorkAreaId &&
                st.WorkRoleId == shiftAssignment.WorkRoleId &&
                st.StartTime == request.StartTime &&
                st.EndTime == request.EndTime &&
                !st.IsDeleted)
            .ToListAsync(cancellationToken);

        if (matchingTemplates.Count == 0)
            throw new InvalidOperationException("El turno no coincide con ninguna plantilla creada para ese día, área, rol y horario.");

        var requiredTemplateCount = matchingTemplates.Sum(st => st.RequiredCount);

        var assignedTemplateCount = await _context.ShiftAssignments
            .CountAsync(sa =>
                sa.Id != request.Id &&
                sa.BranchId == employee.BranchId &&
                sa.Date.Date == request.Date.Date &&
                sa.WorkAreaId == shiftAssignment.WorkAreaId &&
                sa.WorkRoleId == shiftAssignment.WorkRoleId &&
                sa.StartTime == request.StartTime &&
                sa.EndTime == request.EndTime &&
                !sa.IsDeleted, cancellationToken);

        if (assignedTemplateCount >= requiredTemplateCount)
            throw new InvalidOperationException("La plantilla para ese turno ya tiene todos sus cupos asignados.");

        var employeeAlreadyAssigned = await _context.ShiftAssignments
            .AnyAsync(sa =>
                sa.Id != request.Id &&
                sa.EmployeeId == request.EmployeeId &&
                sa.Date.Date == request.Date.Date &&
                !sa.IsDeleted, cancellationToken);

        if (employeeAlreadyAssigned)
            throw new InvalidOperationException("El empleado ya tiene un turno asignado en este día.");

        var breakMinutes = request.BreakDuration?.TotalMinutes ?? 0;
        var lunchMinutes = request.LunchDuration?.TotalMinutes ?? 0;
        var totalMinutes = (request.EndTime - request.StartTime).TotalMinutes - breakMinutes - lunchMinutes;
        var workedHours = (decimal)(totalMinutes / 60.0);

        shiftAssignment.EmployeeId = request.EmployeeId;
        shiftAssignment.BranchId = employee.BranchId;
        shiftAssignment.Date = request.Date;
        shiftAssignment.StartTime = request.StartTime;
        shiftAssignment.EndTime = request.EndTime;
        shiftAssignment.BreakDuration = request.BreakDuration;
        shiftAssignment.LunchDuration = request.LunchDuration;
        shiftAssignment.WorkedHours = workedHours;
        shiftAssignment.Notes = request.Notes;

        await _context.SaveChangesAsync(cancellationToken);

        return new ShiftAssignmentDto
        {
            Id = shiftAssignment.Id,
            EmployeeId = shiftAssignment.EmployeeId,
            EmployeeName = $"{employee.FirstName} {employee.LastName}",
            Date = shiftAssignment.Date,
            StartTime = shiftAssignment.StartTime,
            EndTime = shiftAssignment.EndTime,
            BreakDuration = shiftAssignment.BreakDuration,
            LunchDuration = shiftAssignment.LunchDuration,
            WorkAreaId = shiftAssignment.WorkAreaId,
            WorkAreaName = workArea.Name,
            WorkAreaColor = workArea.Color,
            WorkRoleId = shiftAssignment.WorkRoleId,
            WorkRoleName = workRole.Name,
            WorkedHours = shiftAssignment.WorkedHours,
            Notes = shiftAssignment.Notes,
            IsApproved = shiftAssignment.IsApproved,
            ApprovedBy = shiftAssignment.ApprovedBy,
            ApprovedAt = shiftAssignment.ApprovedAt
        };
    }
}

public class ApproveShiftAssignmentCommandHandler : IRequestHandler<ApproveShiftAssignmentCommand, bool>
{
    private readonly GrimorioDbContext _context;

    public ApproveShiftAssignmentCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<bool> Handle(ApproveShiftAssignmentCommand request, CancellationToken cancellationToken)
    {
        var shiftAssignment = await _context.ShiftAssignments
            .FirstOrDefaultAsync(sa => sa.Id == request.ShiftAssignmentId && !sa.IsDeleted, cancellationToken);

        if (shiftAssignment == null)
            throw new InvalidOperationException("Asignación de turno no encontrada.");

        shiftAssignment.IsApproved = true;
        shiftAssignment.ApprovedBy = request.ApprovedBy;
        shiftAssignment.ApprovedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class DeleteShiftAssignmentCommandHandler : IRequestHandler<DeleteShiftAssignmentCommand, bool>
{
    private readonly GrimorioDbContext _context;

    public DeleteShiftAssignmentCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<bool> Handle(DeleteShiftAssignmentCommand request, CancellationToken cancellationToken)
    {
        var shiftAssignment = await _context.ShiftAssignments
            .FirstOrDefaultAsync(sa => sa.Id == request.Id && !sa.IsDeleted, cancellationToken);

        if (shiftAssignment == null)
            throw new InvalidOperationException("Asignación de turno no encontrada.");

        shiftAssignment.IsDeleted = true;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
