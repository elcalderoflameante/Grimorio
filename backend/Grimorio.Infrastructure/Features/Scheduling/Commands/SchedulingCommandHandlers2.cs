using MediatR;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Scheduling.Commands;
using Grimorio.Domain.Entities.Organization;
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

        // Eliminar roles existentes
        await _context.EmployeeWorkRoles
            .Where(ewr => ewr.EmployeeId == request.EmployeeId)
            .ExecuteDeleteAsync(cancellationToken);

        // Agregar nuevos roles
        foreach (var (roleId, index) in request.WorkRoleIds.Select((r, i) => (r, i)))
        {
            var workRole = await _context.WorkRoles
                .FirstOrDefaultAsync(wr => wr.Id == roleId && !wr.IsDeleted, cancellationToken);

            if (workRole == null)
                throw new InvalidOperationException($"Rol de trabajo con ID {roleId} no encontrado.");

            var employeeWorkRole = new EmployeeWorkRole
            {
                Id = Guid.NewGuid(),
                EmployeeId = request.EmployeeId,
                WorkRoleId = roleId,
                IsPrimary = index == 0, // Primer rol es el primario
                Priority = index + 1,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            };

            _context.EmployeeWorkRoles.Add(employeeWorkRole);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Retornar roles asignados
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
            }).OrderByDescending(ewr => ewr.IsPrimary)
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
            .FirstOrDefaultAsync(ewr => ewr.EmployeeId == request.EmployeeId && ewr.WorkRoleId == request.WorkRoleId && !ewr.IsDeleted, cancellationToken);

        if (employeeWorkRole == null)
            throw new InvalidOperationException("Asignación de rol no encontrada.");

        employeeWorkRole.IsDeleted = true;
        employeeWorkRole.DeletedAt = DateTime.UtcNow;
        employeeWorkRole.DeletedBy = Guid.Empty;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class CreateShiftTemplateCommandHandler : IRequestHandler<CreateShiftTemplateCommand, ShiftTemplateDto>
{
    private readonly GrimorioDbContext _context;

    public CreateShiftTemplateCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<ShiftTemplateDto> Handle(CreateShiftTemplateCommand request, CancellationToken cancellationToken)
    {
        var workArea = await _context.WorkAreas
            .FirstOrDefaultAsync(w => w.Id == request.WorkAreaId && !w.IsDeleted, cancellationToken);

        if (workArea == null)
            throw new InvalidOperationException("Área de trabajo no encontrada.");

        var workRole = await _context.WorkRoles
            .FirstOrDefaultAsync(wr => wr.Id == request.WorkRoleId && !wr.IsDeleted, cancellationToken);

        if (workRole == null)
            throw new InvalidOperationException("Rol de trabajo no encontrado.");

        var shiftTemplate = new ShiftTemplate
        {
            Id = Guid.NewGuid(),
            BranchId = request.BranchId,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            BreakDuration = request.BreakDuration,
            LunchDuration = request.LunchDuration,
            WorkAreaId = request.WorkAreaId,
            WorkRoleId = request.WorkRoleId,
            RequiredCount = request.RequiredCount,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        };

        _context.ShiftTemplates.Add(shiftTemplate);
        await _context.SaveChangesAsync(cancellationToken);

        return new ShiftTemplateDto
        {
            Id = shiftTemplate.Id,
            BranchId = shiftTemplate.BranchId,
            DayOfWeek = shiftTemplate.DayOfWeek,
            StartTime = shiftTemplate.StartTime,
            EndTime = shiftTemplate.EndTime,
            BreakDuration = shiftTemplate.BreakDuration,
            LunchDuration = shiftTemplate.LunchDuration,
            WorkAreaId = shiftTemplate.WorkAreaId,
            WorkAreaName = workArea.Name,
            WorkRoleId = shiftTemplate.WorkRoleId,
            WorkRoleName = workRole.Name,
            RequiredCount = shiftTemplate.RequiredCount,
            Notes = shiftTemplate.Notes
        };
    }
}

public class UpdateShiftTemplateCommandHandler : IRequestHandler<UpdateShiftTemplateCommand, ShiftTemplateDto>
{
    private readonly GrimorioDbContext _context;

    public UpdateShiftTemplateCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<ShiftTemplateDto> Handle(UpdateShiftTemplateCommand request, CancellationToken cancellationToken)
    {
        var shiftTemplate = await _context.ShiftTemplates
            .Include(st => st.WorkArea)
            .Include(st => st.WorkRole)
            .FirstOrDefaultAsync(st => st.Id == request.Id && !st.IsDeleted, cancellationToken);

        if (shiftTemplate == null)
            throw new InvalidOperationException("Plantilla de turno no encontrada.");

        shiftTemplate.DayOfWeek = request.DayOfWeek;
        shiftTemplate.StartTime = request.StartTime;
        shiftTemplate.EndTime = request.EndTime;
        shiftTemplate.BreakDuration = request.BreakDuration;
        shiftTemplate.LunchDuration = request.LunchDuration;
        shiftTemplate.RequiredCount = request.RequiredCount;
        shiftTemplate.Notes = request.Notes;
        shiftTemplate.UpdatedAt = DateTime.UtcNow;
        shiftTemplate.UpdatedBy = Guid.Empty;

        await _context.SaveChangesAsync(cancellationToken);

        return new ShiftTemplateDto
        {
            Id = shiftTemplate.Id,
            BranchId = shiftTemplate.BranchId,
            DayOfWeek = shiftTemplate.DayOfWeek,
            StartTime = shiftTemplate.StartTime,
            EndTime = shiftTemplate.EndTime,
            BreakDuration = shiftTemplate.BreakDuration,
            LunchDuration = shiftTemplate.LunchDuration,
            WorkAreaId = shiftTemplate.WorkAreaId,
            WorkAreaName = shiftTemplate.WorkArea!.Name,
            WorkRoleId = shiftTemplate.WorkRoleId,
            WorkRoleName = shiftTemplate.WorkRole!.Name,
            RequiredCount = shiftTemplate.RequiredCount,
            Notes = shiftTemplate.Notes
        };
    }
}

public class DeleteShiftTemplateCommandHandler : IRequestHandler<DeleteShiftTemplateCommand, bool>
{
    private readonly GrimorioDbContext _context;

    public DeleteShiftTemplateCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<bool> Handle(DeleteShiftTemplateCommand request, CancellationToken cancellationToken)
    {
        var shiftTemplate = await _context.ShiftTemplates
            .FirstOrDefaultAsync(st => st.Id == request.Id && !st.IsDeleted, cancellationToken);

        if (shiftTemplate == null)
            throw new InvalidOperationException("Plantilla de turno no encontrada.");

        shiftTemplate.IsDeleted = true;
        shiftTemplate.DeletedAt = DateTime.UtcNow;
        shiftTemplate.DeletedBy = Guid.Empty;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
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

        // Calcular horas trabajadas
        var startTime = request.StartTime;
        var endTime = request.EndTime;
        var breakMinutes = request.BreakDuration?.TotalMinutes ?? 0;
        var lunchMinutes = request.LunchDuration?.TotalMinutes ?? 0;
        
        var totalMinutes = (endTime - startTime).TotalMinutes - breakMinutes - lunchMinutes;
        var workedHours = (decimal)(totalMinutes / 60.0);

        var shiftAssignment = new ShiftAssignment
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            Date = request.Date,
            StartTime = startTime,
            EndTime = endTime,
            BreakDuration = request.BreakDuration,
            LunchDuration = request.LunchDuration,
            WorkAreaId = request.WorkAreaId,
            WorkRoleId = request.WorkRoleId,
            WorkedHours = workedHours,
            Notes = request.Notes,
            IsApproved = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
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
        shiftAssignment.UpdatedAt = DateTime.UtcNow;
        shiftAssignment.UpdatedBy = Guid.Empty;

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
        shiftAssignment.DeletedAt = DateTime.UtcNow;
        shiftAssignment.DeletedBy = Guid.Empty;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class AddEmployeeAvailabilityCommandHandler : IRequestHandler<AddEmployeeAvailabilityCommand, EmployeeAvailabilityDto>
{
    private readonly GrimorioDbContext _context;

    public AddEmployeeAvailabilityCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<EmployeeAvailabilityDto> Handle(AddEmployeeAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId && !e.IsDeleted, cancellationToken);

        if (employee == null)
            throw new InvalidOperationException("Empleado no encontrado.");

        var employeeAvailability = new EmployeeAvailability
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            UnavailableDate = request.UnavailableDate,
            Reason = request.Reason,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        };

        _context.EmployeeAvailability.Add(employeeAvailability);
        await _context.SaveChangesAsync(cancellationToken);

        return new EmployeeAvailabilityDto
        {
            Id = employeeAvailability.Id,
            EmployeeId = employeeAvailability.EmployeeId,
            UnavailableDate = employeeAvailability.UnavailableDate,
            Reason = employeeAvailability.Reason
        };
    }
}

public class RemoveEmployeeAvailabilityCommandHandler : IRequestHandler<RemoveEmployeeAvailabilityCommand, bool>
{
    private readonly GrimorioDbContext _context;

    public RemoveEmployeeAvailabilityCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<bool> Handle(RemoveEmployeeAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var employeeAvailability = await _context.EmployeeAvailability
            .FirstOrDefaultAsync(ea => ea.Id == request.Id && ea.EmployeeId == request.EmployeeId && !ea.IsDeleted, cancellationToken);

        if (employeeAvailability == null)
            throw new InvalidOperationException("Disponibilidad no encontrada.");

        employeeAvailability.IsDeleted = true;
        employeeAvailability.DeletedAt = DateTime.UtcNow;
        employeeAvailability.DeletedBy = Guid.Empty;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class GenerateMonthlyShiftsCommandHandler : IRequestHandler<GenerateMonthlyShiftsCommand, ShiftGenerationResultDto>
{
    private readonly GrimorioDbContext _context;

    public GenerateMonthlyShiftsCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<ShiftGenerationResultDto> Handle(GenerateMonthlyShiftsCommand request, CancellationToken cancellationToken)
    {
        if (request.Month < 1 || request.Month > 12)
            throw new InvalidOperationException("Mes inválido.");

        if (request.Year < 2000)
            throw new InvalidOperationException("Año inválido.");

        var startDate = new DateTime(request.Year, request.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        var daysInMonth = DateTime.DaysInMonth(request.Year, request.Month);

        // Cargar configuración (para límites de horas)
        var config = await _context.ScheduleConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(sc => sc.BranchId == request.BranchId && !sc.IsDeleted, cancellationToken);

        // Cargar plantillas de turno
        var shiftTemplates = await _context.ShiftTemplates
            .Where(st => st.BranchId == request.BranchId && !st.IsDeleted)
            .Include(st => st.WorkArea)
            .Include(st => st.WorkRole)
            .ToListAsync(cancellationToken);

        if (!shiftTemplates.Any())
            throw new InvalidOperationException("No existen plantillas de turno para esta sucursal.");

        // Cargar roles asignados a empleados (con roles y empleados)
        var employeeWorkRoles = await _context.EmployeeWorkRoles
            .Where(ewr => !ewr.IsDeleted)
            .Include(ewr => ewr.Employee)
            .Include(ewr => ewr.WorkRole)
            .Where(ewr => ewr.Employee != null && ewr.Employee.IsActive && ewr.Employee.BranchId == request.BranchId)
            .ToListAsync(cancellationToken);

        if (!employeeWorkRoles.Any())
            throw new InvalidOperationException("No hay empleados elegibles con roles asignados.");

        // Disponibilidad (días no disponibles)
        var availability = await _context.EmployeeAvailability
            .Where(ea => !ea.IsDeleted && ea.UnavailableDate >= startDate && ea.UnavailableDate <= endDate)
            .ToListAsync(cancellationToken);

        var availabilityByEmployee = availability
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(a => a.UnavailableDate.Date).ToHashSet()
            );

        // Limpiar asignaciones existentes del mes (soft delete)
        var existingAssignments = await _context.ShiftAssignments
            .Where(sa => sa.BranchId == request.BranchId && sa.Date >= startDate && sa.Date <= endDate && !sa.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var assignment in existingAssignments)
        {
            assignment.IsDeleted = true;
            assignment.DeletedAt = DateTime.UtcNow;
            assignment.DeletedBy = Guid.Empty;
        }

        // Preparar estructuras de control
        var assignedDatesByEmployee = new Dictionary<Guid, HashSet<DateTime>>();
        var hoursByEmployee = new Dictionary<Guid, decimal>();
        var assignmentsToCreate = new List<ShiftAssignment>();
        var warnings = new List<ShiftGenerationWarningDto>();

        // Agrupar roles por WorkRoleId para asignación rápida
        var roleCandidates = employeeWorkRoles
            .GroupBy(ewr => ewr.WorkRoleId)
            .ToDictionary(g => g.Key, g => g.ToList());

        for (var day = 0; day < daysInMonth; day++)
        {
            var currentDate = startDate.AddDays(day);
            var dayOfWeek = currentDate.DayOfWeek;

            var templatesForDay = shiftTemplates
                .Where(t => t.DayOfWeek == dayOfWeek)
                .ToList();

            foreach (var template in templatesForDay)
            {
                var assignedForTemplate = 0;
                
                if (!roleCandidates.TryGetValue(template.WorkRoleId, out var candidatesForRole))
                {
                    // No hay empleados con este rol
                    warnings.Add(new ShiftGenerationWarningDto
                    {
                        Date = currentDate,
                        DayOfWeek = dayOfWeek,
                        WorkAreaName = template.WorkArea?.Name ?? "Desconocida",
                        WorkRoleName = template.WorkRole?.Name ?? "Desconocido",
                        RequiredCount = template.RequiredCount,
                        AssignedCount = 0,
                        Reason = "No hay empleados con este rol asignado"
                    });
                    continue;
                }

                for (var i = 0; i < template.RequiredCount; i++)
                {
                    var eligibleCandidates = candidatesForRole
                        .Where(c => c.Employee != null)
                        .Where(c => IsEmployeeAvailable(c.Employee!.Id, currentDate, availabilityByEmployee))
                        .Where(c => !IsEmployeeAlreadyAssigned(c.Employee!.Id, currentDate, assignedDatesByEmployee))
                        .Where(c => CanAssignByFreeDays(c, daysInMonth, assignedDatesByEmployee))
                        .Where(c => CanAssignByHours(c.Employee!.Id, template, hoursByEmployee, config))
                        .OrderByDescending(c => c.IsPrimary)
                        .ThenBy(c => c.Priority)
                        .ThenBy(c => GetEmployeeHours(c.Employee!.Id, hoursByEmployee))
                        .ThenBy(c => GetEmployeeAssignedDays(c.Employee!.Id, assignedDatesByEmployee))
                        .ToList();

                    var selected = eligibleCandidates.FirstOrDefault();
                    if (selected == null)
                    {
                        // No se pudo asignar este turno
                        var reason = candidatesForRole.All(c => c.Employee == null) 
                            ? "No hay empleados disponibles"
                            : candidatesForRole.Any(c => c.Employee != null && !IsEmployeeAvailable(c.Employee.Id, currentDate, availabilityByEmployee))
                            ? "Empleados no disponibles o ya asignados"
                            : "Límite de horas o días libres alcanzado";
                        
                        continue; // Registraremos la advertencia después del loop
                    }

                    var employeeId = selected.Employee!.Id;
                    var workedHours = CalculateWorkedHours(template);

                    assignmentsToCreate.Add(new ShiftAssignment
                    {
                        Id = Guid.NewGuid(),
                        BranchId = request.BranchId,
                        EmployeeId = employeeId,
                        Date = currentDate,
                        StartTime = template.StartTime,
                        EndTime = template.EndTime,
                        BreakDuration = template.BreakDuration,
                        LunchDuration = template.LunchDuration,
                        WorkAreaId = template.WorkAreaId,
                        WorkRoleId = template.WorkRoleId,
                        WorkedHours = workedHours,
                        Notes = template.Notes,
                        IsApproved = false,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = Guid.Empty
                    });

                    assignedForTemplate++;
                    TrackAssignment(employeeId, currentDate, workedHours, assignedDatesByEmployee, hoursByEmployee);
                }
                
                // Si no se cubrieron todos los turnos requeridos, agregar advertencia
                if (assignedForTemplate < template.RequiredCount)
                {
                    warnings.Add(new ShiftGenerationWarningDto
                    {
                        Date = currentDate,
                        DayOfWeek = dayOfWeek,
                        WorkAreaName = template.WorkArea?.Name ?? "Desconocida",
                        WorkRoleName = template.WorkRole?.Name ?? "Desconocido",
                        RequiredCount = template.RequiredCount,
                        AssignedCount = assignedForTemplate,
                        Reason = assignedForTemplate == 0 
                            ? "No hay empleados disponibles con este rol" 
                            : "No hay suficientes empleados disponibles"
                    });
                }
            }
        }

        _context.ShiftAssignments.AddRange(assignmentsToCreate);
        await _context.SaveChangesAsync(cancellationToken);

        // Mapear a DTO
        var employeeNames = employeeWorkRoles
            .Where(ewr => ewr.Employee != null)
            .Select(ewr => ewr.Employee!)
            .GroupBy(e => e.Id)
            .ToDictionary(g => g.Key, g => g.First());

        var assignments = assignmentsToCreate
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StartTime)
            .Select(a => new ShiftAssignmentDto
            {
                Id = a.Id,
                EmployeeId = a.EmployeeId,
                EmployeeName = employeeNames.TryGetValue(a.EmployeeId, out var emp)
                    ? $"{emp.FirstName} {emp.LastName}"
                    : string.Empty,
                Date = a.Date,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                BreakDuration = a.BreakDuration,
                LunchDuration = a.LunchDuration,
                WorkAreaId = a.WorkAreaId,
                WorkAreaName = shiftTemplates.First(t => t.WorkAreaId == a.WorkAreaId).WorkArea?.Name ?? string.Empty,
                WorkAreaColor = shiftTemplates.First(t => t.WorkAreaId == a.WorkAreaId).WorkArea?.Color ?? "#808080",
                WorkRoleId = a.WorkRoleId,
                WorkRoleName = shiftTemplates.First(t => t.WorkRoleId == a.WorkRoleId).WorkRole?.Name ?? string.Empty,
                WorkedHours = a.WorkedHours,
                Notes = a.Notes,
                IsApproved = a.IsApproved,
                ApprovedBy = a.ApprovedBy,
                ApprovedAt = a.ApprovedAt
            })
            .ToList();

        return new ShiftGenerationResultDto
        {
            Assignments = assignments,
            Warnings = warnings,
            TotalShiftsGenerated = assignments.Count,
            TotalShiftsNotCovered = warnings.Sum(w => w.RequiredCount - w.AssignedCount)
        };
    }

    private static bool IsEmployeeAvailable(Guid employeeId, DateTime date, Dictionary<Guid, HashSet<DateTime>> availabilityByEmployee)
    {
        if (!availabilityByEmployee.TryGetValue(employeeId, out var unavailableDates))
            return true;

        return !unavailableDates.Contains(date.Date);
    }

    private static bool IsEmployeeAlreadyAssigned(Guid employeeId, DateTime date, Dictionary<Guid, HashSet<DateTime>> assignedDatesByEmployee)
    {
        if (!assignedDatesByEmployee.TryGetValue(employeeId, out var dates))
            return false;

        return dates.Contains(date.Date);
    }

    private static bool CanAssignByFreeDays(EmployeeWorkRole ewr, int daysInMonth, Dictionary<Guid, HashSet<DateTime>> assignedDatesByEmployee)
    {
        var maxWorkingDays = Math.Max(0, daysInMonth - (ewr.WorkRole?.FreeDaysPerMonth ?? 0));
        var assignedDays = GetEmployeeAssignedDays(ewr.EmployeeId, assignedDatesByEmployee);
        return assignedDays < maxWorkingDays;
    }

    private static bool CanAssignByHours(Guid employeeId, ShiftTemplate template, Dictionary<Guid, decimal> hoursByEmployee, ScheduleConfiguration? config)
    {
        if (config == null)
            return true;

        var maxHours = config.MaxHoursPerMonth;
        var currentHours = GetEmployeeHours(employeeId, hoursByEmployee);
        var workedHours = CalculateWorkedHours(template);
        return currentHours + workedHours <= maxHours;
    }

    private static void TrackAssignment(
        Guid employeeId,
        DateTime date,
        decimal workedHours,
        Dictionary<Guid, HashSet<DateTime>> assignedDatesByEmployee,
        Dictionary<Guid, decimal> hoursByEmployee)
    {
        if (!assignedDatesByEmployee.TryGetValue(employeeId, out var dates))
        {
            dates = new HashSet<DateTime>();
            assignedDatesByEmployee[employeeId] = dates;
        }

        dates.Add(date.Date);

        if (!hoursByEmployee.ContainsKey(employeeId))
            hoursByEmployee[employeeId] = 0m;

        hoursByEmployee[employeeId] += workedHours;
    }

    private static decimal CalculateWorkedHours(ShiftTemplate template)
    {
        var breakMinutes = template.BreakDuration?.TotalMinutes ?? 0;
        var lunchMinutes = template.LunchDuration?.TotalMinutes ?? 0;
        var totalMinutes = (template.EndTime - template.StartTime).TotalMinutes - breakMinutes - lunchMinutes;
        return (decimal)(totalMinutes / 60.0);
    }

    private static decimal GetEmployeeHours(Guid employeeId, Dictionary<Guid, decimal> hoursByEmployee)
    {
        return hoursByEmployee.TryGetValue(employeeId, out var hours) ? hours : 0m;
    }

    private static int GetEmployeeAssignedDays(Guid employeeId, Dictionary<Guid, HashSet<DateTime>> assignedDatesByEmployee)
    {
        return assignedDatesByEmployee.TryGetValue(employeeId, out var dates) ? dates.Count : 0;
    }
}

// ======================== ScheduleConfiguration Commands ========================

public class CreateScheduleConfigurationCommandHandler : IRequestHandler<CreateScheduleConfigurationCommand, ScheduleConfigurationDto>
{
    private readonly GrimorioDbContext _context;

    public CreateScheduleConfigurationCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<ScheduleConfigurationDto> Handle(CreateScheduleConfigurationCommand request, CancellationToken cancellationToken)
    {
        // Verificar si ya existe configuración para esta sucursal
        var existingConfig = await _context.ScheduleConfigurations
            .FirstOrDefaultAsync(sc => sc.BranchId == request.BranchId && !sc.IsDeleted, cancellationToken);

        if (existingConfig != null)
            throw new InvalidOperationException("Ya existe una configuración de horarios para esta sucursal.");

        var config = new ScheduleConfiguration
        {
            Id = Guid.NewGuid(),
            BranchId = request.BranchId,
            MinHoursPerMonth = request.MinHoursPerMonth,
            MaxHoursPerMonth = request.MaxHoursPerMonth,
            HoursMondayThursday = request.HoursMondayThursday,
            HoursFridaySaturday = request.HoursFridaySaturday,
            HoursSunday = request.HoursSunday,
            MinStaffCocina = request.MinStaffCocina,
            MinStaffCaja = request.MinStaffCaja,
            MinStaffMesas = request.MinStaffMesas,
            MinStaffBar = request.MinStaffBar,
            FreeDayColor = string.IsNullOrWhiteSpace(request.FreeDayColor) ? "#E8E8E8" : request.FreeDayColor,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        };

        _context.ScheduleConfigurations.Add(config);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(config);
    }

    private static ScheduleConfigurationDto MapToDto(ScheduleConfiguration config) => new()
    {
        Id = config.Id,
        BranchId = config.BranchId,
        MinHoursPerMonth = config.MinHoursPerMonth,
        MaxHoursPerMonth = config.MaxHoursPerMonth,
        HoursMondayThursday = config.HoursMondayThursday,
        HoursFridaySaturday = config.HoursFridaySaturday,
        HoursSunday = config.HoursSunday,
        MinStaffCocina = config.MinStaffCocina,
        MinStaffCaja = config.MinStaffCaja,
        MinStaffMesas = config.MinStaffMesas,
        MinStaffBar = config.MinStaffBar,
        FreeDayColor = string.IsNullOrWhiteSpace(config.FreeDayColor) ? "#E8E8E8" : config.FreeDayColor
    };
}

public class UpdateScheduleConfigurationCommandHandler : IRequestHandler<UpdateScheduleConfigurationCommand, ScheduleConfigurationDto>
{
    private readonly GrimorioDbContext _context;

    public UpdateScheduleConfigurationCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<ScheduleConfigurationDto> Handle(UpdateScheduleConfigurationCommand request, CancellationToken cancellationToken)
    {
        var config = await _context.ScheduleConfigurations
            .FirstOrDefaultAsync(sc => sc.Id == request.Id && !sc.IsDeleted, cancellationToken);

        if (config == null)
            throw new InvalidOperationException("Configuración de horarios no encontrada.");

        config.MinHoursPerMonth = request.MinHoursPerMonth;
        config.MaxHoursPerMonth = request.MaxHoursPerMonth;
        config.HoursMondayThursday = request.HoursMondayThursday;
        config.HoursFridaySaturday = request.HoursFridaySaturday;
        config.HoursSunday = request.HoursSunday;
        config.MinStaffCocina = request.MinStaffCocina;
        config.MinStaffCaja = request.MinStaffCaja;
        config.MinStaffMesas = request.MinStaffMesas;
        config.MinStaffBar = request.MinStaffBar;
        config.FreeDayColor = string.IsNullOrWhiteSpace(request.FreeDayColor) ? "#E8E8E8" : request.FreeDayColor;
        config.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(config);
    }

    private static ScheduleConfigurationDto MapToDto(ScheduleConfiguration config) => new()
    {
        Id = config.Id,
        BranchId = config.BranchId,
        MinHoursPerMonth = config.MinHoursPerMonth,
        MaxHoursPerMonth = config.MaxHoursPerMonth,
        HoursMondayThursday = config.HoursMondayThursday,
        HoursFridaySaturday = config.HoursFridaySaturday,
        HoursSunday = config.HoursSunday,
        MinStaffCocina = config.MinStaffCocina,
        MinStaffCaja = config.MinStaffCaja,
        MinStaffMesas = config.MinStaffMesas,
        MinStaffBar = config.MinStaffBar,
        FreeDayColor = string.IsNullOrWhiteSpace(config.FreeDayColor) ? "#E8E8E8" : config.FreeDayColor
    };
}
