using MediatR;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Scheduling.Commands;
using Grimorio.Domain.Entities.Scheduling;
using Grimorio.Infrastructure.Persistence;

namespace Grimorio.Infrastructure.Features.Scheduling.Commands;

internal static class EffectiveShiftTemplateCapacity
{
    public static async Task<int> GetAsync(
        GrimorioDbContext context,
        Guid branchId,
        DateTime date,
        Guid workAreaId,
        Guid workRoleId,
        TimeSpan startTime,
        TimeSpan endTime,
        CancellationToken cancellationToken)
    {
        var utcDayStart = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var utcDayEnd = utcDayStart.AddDays(1);
        var specialDate = await context.SpecialDates
            .AsNoTracking()
            .FirstOrDefaultAsync(sd =>
                sd.BranchId == branchId &&
                sd.Date >= utcDayStart &&
                sd.Date < utcDayEnd &&
                !sd.IsDeleted, cancellationToken);

        if (specialDate != null)
        {
            var specialTemplates = await context.SpecialDateTemplates
                .AsNoTracking()
                .Where(st => st.SpecialDateId == specialDate.Id && !st.IsDeleted)
                .ToListAsync(cancellationToken);

            if (specialTemplates.Count > 0)
            {
                return specialTemplates
                    .Where(st =>
                        st.WorkAreaId == workAreaId &&
                        st.WorkRoleId == workRoleId &&
                        st.StartTime == startTime &&
                        st.EndTime == endTime)
                    .Sum(st => st.RequiredCount);
            }
        }

        return await context.ShiftTemplates
            .AsNoTracking()
            .Where(st =>
                st.BranchId == branchId &&
                st.DayOfWeek == date.DayOfWeek &&
                st.WorkAreaId == workAreaId &&
                st.WorkRoleId == workRoleId &&
                st.StartTime == startTime &&
                st.EndTime == endTime &&
                !st.IsDeleted)
            .SumAsync(st => st.RequiredCount, cancellationToken);
    }
}

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

        var requiredTemplateCount = await EffectiveShiftTemplateCapacity.GetAsync(
            _context,
            employee.BranchId,
            request.Date,
            request.WorkAreaId,
            request.WorkRoleId,
            request.StartTime,
            request.EndTime,
            cancellationToken);

        if (requiredTemplateCount == 0)
            throw new InvalidOperationException("El turno no coincide con ninguna plantilla efectiva para ese día, área, rol y horario.");

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

public class ReplaceWeeklyShiftAssignmentsCommandHandler
    : IRequestHandler<ReplaceWeeklyShiftAssignmentsCommand, List<ShiftAssignmentDto>>
{
    private readonly GrimorioDbContext _context;

    public ReplaceWeeklyShiftAssignmentsCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<List<ShiftAssignmentDto>> Handle(
        ReplaceWeeklyShiftAssignmentsCommand request,
        CancellationToken cancellationToken)
    {
        var startDate = request.StartDate.Date;
        var endDate = request.EndDate.Date;
        var assignments = request.Assignments;

        if (assignments.Any(a => a.Date.Date < startDate || a.Date.Date > endDate))
            throw new InvalidOperationException("Hay turnos fuera del rango de la semana a confirmar.");

        var duplicateEmployeeDay = assignments
            .GroupBy(a => new { a.EmployeeId, Date = a.Date.Date })
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateEmployeeDay != null)
            throw new InvalidOperationException("Un empleado no puede ocupar más de un turno en el mismo día.");

        var employeeIds = assignments.Select(a => a.EmployeeId).Distinct().ToList();
        var employees = await _context.Employees
            .AsNoTracking()
            .Where(e =>
                employeeIds.Contains(e.Id) &&
                e.BranchId == request.BranchId &&
                !e.IsDeleted)
            .ToDictionaryAsync(e => e.Id, cancellationToken);
        if (employees.Count != employeeIds.Count)
            throw new InvalidOperationException("Uno o más empleados no existen en la sucursal seleccionada.");

        var areaIds = assignments.Select(a => a.WorkAreaId).Distinct().ToList();
        var areas = await _context.WorkAreas
            .AsNoTracking()
            .Where(a => areaIds.Contains(a.Id) && a.BranchId == request.BranchId && !a.IsDeleted)
            .ToDictionaryAsync(a => a.Id, cancellationToken);
        if (areas.Count != areaIds.Count)
            throw new InvalidOperationException("Una o más áreas de trabajo no existen en la sucursal seleccionada.");

        var roleIds = assignments.Select(a => a.WorkRoleId).Distinct().ToList();
        var roles = await _context.WorkRoles
            .AsNoTracking()
            .Where(r => roleIds.Contains(r.Id) && !r.IsDeleted)
            .ToDictionaryAsync(r => r.Id, cancellationToken);
        if (roles.Count != roleIds.Count)
            throw new InvalidOperationException("Uno o más roles de trabajo no existen.");

        var templateGroups = assignments.GroupBy(a => new
        {
            Date = a.Date.Date,
            a.WorkAreaId,
            a.WorkRoleId,
            a.StartTime,
            a.EndTime
        });
        foreach (var group in templateGroups)
        {
            var capacity = await EffectiveShiftTemplateCapacity.GetAsync(
                _context,
                request.BranchId,
                group.Key.Date,
                group.Key.WorkAreaId,
                group.Key.WorkRoleId,
                group.Key.StartTime,
                group.Key.EndTime,
                cancellationToken);

            if (capacity == 0)
                throw new InvalidOperationException(
                    $"No existe una plantilla efectiva para el turno del {group.Key.Date:dd/MM/yyyy}.");
            if (group.Count() > capacity)
                throw new InvalidOperationException(
                    $"Los turnos del {group.Key.Date:dd/MM/yyyy} superan los cupos de la plantilla.");
        }

        var existingAssignments = await _context.ShiftAssignments
            .Where(sa =>
                sa.BranchId == request.BranchId &&
                sa.Date.Date >= startDate &&
                sa.Date.Date <= endDate &&
                !sa.IsDeleted)
            .ToListAsync(cancellationToken);
        foreach (var existing in existingAssignments)
        {
            existing.IsDeleted = true;
            existing.DeletedAt = DateTime.UtcNow;
        }

        var createdAssignments = assignments.Select(item =>
        {
            var breakMinutes = item.BreakDuration?.TotalMinutes ?? 0;
            var lunchMinutes = item.LunchDuration?.TotalMinutes ?? 0;
            var totalMinutes = (item.EndTime - item.StartTime).TotalMinutes - breakMinutes - lunchMinutes;

            return new ShiftAssignment
            {
                BranchId = request.BranchId,
                EmployeeId = item.EmployeeId,
                Date = item.Date.Date,
                StartTime = item.StartTime,
                EndTime = item.EndTime,
                BreakDuration = item.BreakDuration,
                LunchDuration = item.LunchDuration,
                WorkAreaId = item.WorkAreaId,
                WorkRoleId = item.WorkRoleId,
                WorkedHours = (decimal)(totalMinutes / 60.0),
                Notes = item.Notes,
                IsApproved = false,
            };
        }).ToList();

        _context.ShiftAssignments.AddRange(createdAssignments);
        await _context.SaveChangesAsync(cancellationToken);

        return createdAssignments.Select(assignment => new ShiftAssignmentDto
        {
            Id = assignment.Id,
            EmployeeId = assignment.EmployeeId,
            EmployeeName = $"{employees[assignment.EmployeeId].FirstName} {employees[assignment.EmployeeId].LastName}",
            Date = assignment.Date,
            StartTime = assignment.StartTime,
            EndTime = assignment.EndTime,
            BreakDuration = assignment.BreakDuration,
            LunchDuration = assignment.LunchDuration,
            WorkAreaId = assignment.WorkAreaId,
            WorkAreaName = areas[assignment.WorkAreaId].Name,
            WorkAreaColor = areas[assignment.WorkAreaId].Color,
            WorkRoleId = assignment.WorkRoleId,
            WorkRoleName = roles[assignment.WorkRoleId].Name,
            WorkedHours = assignment.WorkedHours,
            Notes = assignment.Notes,
            IsApproved = assignment.IsApproved,
        }).ToList();
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

        var requiredTemplateCount = await EffectiveShiftTemplateCapacity.GetAsync(
            _context,
            employee.BranchId,
            request.Date,
            shiftAssignment.WorkAreaId,
            shiftAssignment.WorkRoleId,
            request.StartTime,
            request.EndTime,
            cancellationToken);

        if (requiredTemplateCount == 0)
            throw new InvalidOperationException("El turno no coincide con ninguna plantilla efectiva para ese día, área, rol y horario.");

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
