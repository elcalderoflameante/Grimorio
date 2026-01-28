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
            .FirstOrDefaultAsync(ea => ea.Id == request.Id && !ea.IsDeleted, cancellationToken);

        if (employeeAvailability == null)
            throw new InvalidOperationException("Disponibilidad no encontrada.");

        employeeAvailability.IsDeleted = true;
        employeeAvailability.DeletedAt = DateTime.UtcNow;
        employeeAvailability.DeletedBy = Guid.Empty;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class GenerateMonthlyShiftsCommandHandler : IRequestHandler<GenerateMonthlyShiftsCommand, List<ShiftAssignmentDto>>
{
    private readonly GrimorioDbContext _context;

    public GenerateMonthlyShiftsCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<List<ShiftAssignmentDto>> Handle(GenerateMonthlyShiftsCommand request, CancellationToken cancellationToken)
    {
        // TODO: Implementar algoritmo greedy de generación de horarios
        // Por ahora retorna lista vacía
        return new List<ShiftAssignmentDto>();
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
            FreeDaysParrillero = request.FreeDaysParrillero,
            FreeDaysOtherRoles = request.FreeDaysOtherRoles,
            MinStaffCocina = request.MinStaffCocina,
            MinStaffCaja = request.MinStaffCaja,
            MinStaffMesas = request.MinStaffMesas,
            MinStaffBar = request.MinStaffBar,
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
        FreeDaysParrillero = config.FreeDaysParrillero,
        FreeDaysOtherRoles = config.FreeDaysOtherRoles,
        MinStaffCocina = config.MinStaffCocina,
        MinStaffCaja = config.MinStaffCaja,
        MinStaffMesas = config.MinStaffMesas,
        MinStaffBar = config.MinStaffBar
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
        config.FreeDaysParrillero = request.FreeDaysParrillero;
        config.FreeDaysOtherRoles = request.FreeDaysOtherRoles;
        config.MinStaffCocina = request.MinStaffCocina;
        config.MinStaffCaja = request.MinStaffCaja;
        config.MinStaffMesas = request.MinStaffMesas;
        config.MinStaffBar = request.MinStaffBar;
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
        FreeDaysParrillero = config.FreeDaysParrillero,
        FreeDaysOtherRoles = config.FreeDaysOtherRoles,
        MinStaffCocina = config.MinStaffCocina,
        MinStaffCaja = config.MinStaffCaja,
        MinStaffMesas = config.MinStaffMesas,
        MinStaffBar = config.MinStaffBar
    };
}
