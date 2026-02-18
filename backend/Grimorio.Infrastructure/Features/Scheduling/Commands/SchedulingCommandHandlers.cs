using MediatR;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Scheduling.Commands;
using Grimorio.Domain.Enums;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Domain.Entities.Scheduling;
using Grimorio.Infrastructure.Persistence;

namespace Grimorio.Infrastructure.Features.Scheduling.Commands;

public class CreateWorkAreaCommandHandler : IRequestHandler<CreateWorkAreaCommand, WorkAreaDto>
{
    private readonly GrimorioDbContext _context;

    public CreateWorkAreaCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<WorkAreaDto> Handle(CreateWorkAreaCommand request, CancellationToken cancellationToken)
    {
        var workArea = new WorkArea
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Color = request.Color,
            DisplayOrder = request.DisplayOrder,
            BranchId = request.BranchId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        };

        _context.WorkAreas.Add(workArea);
        await _context.SaveChangesAsync(cancellationToken);

        return new WorkAreaDto
        {
            Id = workArea.Id,
            Name = workArea.Name,
            Description = workArea.Description,
            Color = workArea.Color,
            DisplayOrder = workArea.DisplayOrder,
            BranchId = workArea.BranchId,
            WorkRoles = new()
        };
    }
}

public class UpdateWorkAreaCommandHandler : IRequestHandler<UpdateWorkAreaCommand, WorkAreaDto>
{
    private readonly GrimorioDbContext _context;

    public UpdateWorkAreaCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<WorkAreaDto> Handle(UpdateWorkAreaCommand request, CancellationToken cancellationToken)
    {
        var workArea = await _context.WorkAreas
            .Include(w => w.WorkRoles)
            .FirstOrDefaultAsync(w => w.Id == request.Id && !w.IsDeleted, cancellationToken);

        if (workArea == null)
            throw new InvalidOperationException("Área de trabajo no encontrada.");

        workArea.Name = request.Name;
        workArea.Description = request.Description;
        workArea.Color = request.Color;
        workArea.DisplayOrder = request.DisplayOrder;
        workArea.UpdatedAt = DateTime.UtcNow;
        workArea.UpdatedBy = Guid.Empty;

        await _context.SaveChangesAsync(cancellationToken);

        return new WorkAreaDto
        {
            Id = workArea.Id,
            Name = workArea.Name,
            Description = workArea.Description,
            Color = workArea.Color,
            DisplayOrder = workArea.DisplayOrder,
            BranchId = workArea.BranchId,
            WorkRoles = workArea.WorkRoles
                .Where(wr => !wr.IsDeleted)
                .Select(wr => new WorkRoleDto
                {
                    Id = wr.Id,
                    Name = wr.Name,
                    Description = wr.Description,
                    WorkAreaId = wr.WorkAreaId
                }).ToList()
        };
    }
}

public class DeleteWorkAreaCommandHandler : IRequestHandler<DeleteWorkAreaCommand, bool>
{
    private readonly GrimorioDbContext _context;

    public DeleteWorkAreaCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<bool> Handle(DeleteWorkAreaCommand request, CancellationToken cancellationToken)
    {
        var workArea = await _context.WorkAreas
            .FirstOrDefaultAsync(w => w.Id == request.Id && !w.IsDeleted, cancellationToken);

        if (workArea == null)
            throw new InvalidOperationException("Área de trabajo no encontrada.");

        workArea.IsDeleted = true;
        workArea.DeletedAt = DateTime.UtcNow;
        workArea.DeletedBy = Guid.Empty;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class CreateWorkRoleCommandHandler : IRequestHandler<CreateWorkRoleCommand, WorkRoleDto>
{
    private readonly GrimorioDbContext _context;

    public CreateWorkRoleCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<WorkRoleDto> Handle(CreateWorkRoleCommand request, CancellationToken cancellationToken)
    {
        var workArea = await _context.WorkAreas
            .FirstOrDefaultAsync(w => w.Id == request.WorkAreaId && !w.IsDeleted, cancellationToken);

        if (workArea == null)
            throw new InvalidOperationException("Área de trabajo no encontrada.");

        var workRole = new WorkRole
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            WorkAreaId = request.WorkAreaId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        };

        _context.WorkRoles.Add(workRole);
        await _context.SaveChangesAsync(cancellationToken);

        return new WorkRoleDto
        {
            Id = workRole.Id,
            Name = workRole.Name,
            Description = workRole.Description,
            WorkAreaId = workRole.WorkAreaId
        };
    }
}

public class UpdateWorkRoleCommandHandler : IRequestHandler<UpdateWorkRoleCommand, WorkRoleDto>
{
    private readonly GrimorioDbContext _context;

    public UpdateWorkRoleCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<WorkRoleDto> Handle(UpdateWorkRoleCommand request, CancellationToken cancellationToken)
    {
        var workRole = await _context.WorkRoles
            .FirstOrDefaultAsync(wr => wr.Id == request.Id && !wr.IsDeleted, cancellationToken);

        if (workRole == null)
            throw new InvalidOperationException("Rol de trabajo no encontrado.");

        workRole.Name = request.Name;
        workRole.Description = request.Description;
        workRole.UpdatedAt = DateTime.UtcNow;
        workRole.UpdatedBy = Guid.Empty;

        await _context.SaveChangesAsync(cancellationToken);

        return new WorkRoleDto
        {
            Id = workRole.Id,
            Name = workRole.Name,
            Description = workRole.Description,
            WorkAreaId = workRole.WorkAreaId
        };
    }
}

public class DeleteWorkRoleCommandHandler : IRequestHandler<DeleteWorkRoleCommand, bool>
{
    private readonly GrimorioDbContext _context;

    public DeleteWorkRoleCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<bool> Handle(DeleteWorkRoleCommand request, CancellationToken cancellationToken)
    {
        var workRole = await _context.WorkRoles
            .FirstOrDefaultAsync(wr => wr.Id == request.Id && !wr.IsDeleted, cancellationToken);

        if (workRole == null)
            throw new InvalidOperationException("Rol de trabajo no encontrado.");

        workRole.IsDeleted = true;
        workRole.DeletedAt = DateTime.UtcNow;
        workRole.DeletedBy = Guid.Empty;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

// ======================== EmployeeWorkRole Commands ========================

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

        // Validar máximo 3 roles
        if (request.WorkRoleIds.Count > 3)
            throw new InvalidOperationException("Un empleado puede tener máximo 3 roles. El último rol tiene la menor prioridad.");

        // Validar al menos 1 rol
        if (request.WorkRoleIds.Count == 0)
            throw new InvalidOperationException("Un empleado debe tener al menos un rol asignado.");

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

// ======================== ShiftTemplate Commands ========================

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

// ======================== ShiftAssignment Commands ========================

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
            .AnyAsync(ewr => ewr.EmployeeId == request.EmployeeId
                && ewr.WorkRoleId == shiftAssignment.WorkRoleId
                && !ewr.IsDeleted, cancellationToken);

        if (!hasRole)
            throw new InvalidOperationException("El empleado no tiene asignado el rol de este turno.");

        // Calcular horas trabajadas
        var startTime = request.StartTime;
        var endTime = request.EndTime;
        var breakMinutes = request.BreakDuration?.TotalMinutes ?? 0;
        var lunchMinutes = request.LunchDuration?.TotalMinutes ?? 0;

        var totalMinutes = (endTime - startTime).TotalMinutes - breakMinutes - lunchMinutes;
        var workedHours = (decimal)(totalMinutes / 60.0);

        shiftAssignment.EmployeeId = request.EmployeeId;
        shiftAssignment.Date = request.Date;
        shiftAssignment.StartTime = startTime;
        shiftAssignment.EndTime = endTime;
        shiftAssignment.BreakDuration = request.BreakDuration;
        shiftAssignment.LunchDuration = request.LunchDuration;
        shiftAssignment.WorkedHours = workedHours;
        shiftAssignment.Notes = request.Notes;
        shiftAssignment.UpdatedAt = DateTime.UtcNow;
        shiftAssignment.UpdatedBy = Guid.Empty;

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

// ======================== EmployeeAvailability Commands ========================

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

// ======================== Schedule Generation Commands ========================

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

        var startDate = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        var daysInMonth = DateTime.DaysInMonth(request.Year, request.Month);
        var today = DateTime.Today;
        var generationStartDate = startDate;

        if (today.Year == request.Year && today.Month == request.Month)
        {
            generationStartDate = today.AddDays(1);
            if (generationStartDate < startDate)
                generationStartDate = startDate;

            if (generationStartDate > endDate)
                throw new InvalidOperationException("No hay días futuros para generar en este mes.");
        }

        // Cargar configuración (para límites de horas)
        var config = await _context.ScheduleConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(sc => sc.BranchId == request.BranchId && !sc.IsDeleted, cancellationToken);

        // Cargar plantillas de turno generales
        var shiftTemplates = await _context.ShiftTemplates
            .Where(st => st.BranchId == request.BranchId && !st.IsDeleted)
            .Include(st => st.WorkArea)
            .Include(st => st.WorkRole)
            .ToListAsync(cancellationToken);

        if (!shiftTemplates.Any())
            throw new InvalidOperationException("No existen plantillas de turno para esta sucursal.");

        // Cargar días especiales y sus plantillas para el mes
        var specialDates = await _context.SpecialDates
            .Where(sd => sd.BranchId == request.BranchId && !sd.IsDeleted 
                && sd.Date >= startDate && sd.Date <= endDate)
            .Include(sd => sd.Templates.Where(t => !t.IsDeleted))
            .ThenInclude(t => t.WorkArea)
            .ToListAsync(cancellationToken);

        var specialDateDict = specialDates.ToDictionary(sd => sd.Date.Date);

        // También cargar WorkRoles para SpecialDateTemplates
        var specialTemplateRoleIds = specialDates
            .SelectMany(sd => sd.Templates)
            .Select(t => t.WorkRoleId)
            .Distinct()
            .ToList();

        var workRoles = await _context.WorkRoles
            .Where(wr => specialTemplateRoleIds.Contains(wr.Id) && !wr.IsDeleted)
            .ToDictionaryAsync(wr => wr.Id, wr => wr, cancellationToken);

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

        // Cargar asignaciones existentes del mes
        var existingAssignments = await _context.ShiftAssignments
            .Where(sa => sa.BranchId == request.BranchId && sa.Date >= startDate && sa.Date <= endDate && !sa.IsDeleted)
            .ToListAsync(cancellationToken);

        var pastAssignments = existingAssignments
            .Where(a => a.Date.Date < generationStartDate.Date)
            .ToList();

        var futureAssignments = existingAssignments
            .Where(a => a.Date.Date >= generationStartDate.Date)
            .ToList();

        foreach (var assignment in futureAssignments)
        {
            assignment.IsDeleted = true;
            assignment.DeletedAt = DateTime.UtcNow;
            assignment.DeletedBy = Guid.Empty;
        }

        // Preparar estructuras de control
        var assignedDatesByEmployee = new Dictionary<Guid, HashSet<DateTime>>();
        var hoursByEmployee = new Dictionary<Guid, decimal>();
        var weeklyHoursByEmployee = new Dictionary<Guid, Dictionary<int, decimal>>();
        var assignmentsToCreate = new List<ShiftAssignment>();
        var warnings = new List<ShiftGenerationWarningDto>();

        // Sembrar asignaciones pasadas para no perder el historial del mes
        foreach (var assignment in pastAssignments)
        {
            TrackAssignment(assignment.EmployeeId, assignment.Date, assignment.WorkedHours, startDate, assignedDatesByEmployee, hoursByEmployee, weeklyHoursByEmployee);
        }

        // Agrupar roles por WorkRoleId para asignación rápida
        var roleCandidates = employeeWorkRoles
            .GroupBy(ewr => ewr.WorkRoleId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Mapear roles por empleado para calcular demanda de días libres (full time)
        var roleIdsByEmployee = employeeWorkRoles
            .Where(ewr => ewr.Employee != null)
            .GroupBy(ewr => ewr.EmployeeId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.WorkRoleId).ToHashSet());

        var plannedOffDaysByEmployee = BuildPlannedOffDaysByEmployee(
            startDate,
            endDate,
            shiftTemplates,
            specialDateDict,
            roleIdsByEmployee,
            employeeWorkRoles
                .Where(ewr => ewr.Employee != null)
                .Select(ewr => ewr.Employee!)
                .GroupBy(e => e.Id)
                .Select(g => g.First()));

        // Validación previa: capacidad por rol vs requerimientos (respeta días libres y disponibilidad)
        warnings.AddRange(BuildPreGenerationWarnings(
            generationStartDate,
            endDate,
            daysInMonth,
            shiftTemplates,
            specialDates,
            workRoles,
            roleCandidates,
            availabilityByEmployee,
            assignedDatesByEmployee));

        for (var currentDate = generationStartDate; currentDate <= endDate; currentDate = currentDate.AddDays(1))
        {
            var dayOfWeek = currentDate.DayOfWeek;

            // Verificar si es día especial
            List<(Guid WorkAreaId, Guid WorkRoleId, TimeSpan StartTime, TimeSpan EndTime, TimeSpan? BreakDuration, TimeSpan? LunchDuration, int RequiredCount, string? Notes, WorkArea? WorkArea, WorkRole? WorkRole)> templatesForDay;

            if (specialDateDict.TryGetValue(currentDate.Date, out var specialDate) && specialDate.Templates.Any())
            {
                // Es día especial: usar SOLO las plantillas del día especial
                templatesForDay = specialDate.Templates
                    .Select(t => (
                        WorkAreaId: t.WorkAreaId,
                        WorkRoleId: t.WorkRoleId,
                        StartTime: t.StartTime,
                        EndTime: t.EndTime,
                        BreakDuration: t.BreakDuration,
                        LunchDuration: t.LunchDuration,
                        RequiredCount: t.RequiredCount,
                        Notes: t.Notes,
                        WorkArea: t.WorkArea,
                        WorkRole: workRoles.TryGetValue(t.WorkRoleId, out var wr) ? wr : null
                    ))
                    .ToList();
            }
            else
            {
                // Día normal: usar plantillas generales según día de la semana
                templatesForDay = shiftTemplates
                    .Where(t => t.DayOfWeek == dayOfWeek)
                    .Select(t => (
                        WorkAreaId: t.WorkAreaId,
                        WorkRoleId: t.WorkRoleId,
                        StartTime: t.StartTime,
                        EndTime: t.EndTime,
                        BreakDuration: t.BreakDuration,
                        LunchDuration: t.LunchDuration,
                        RequiredCount: t.RequiredCount,
                        Notes: t.Notes,
                        WorkArea: t.WorkArea,
                        WorkRole: t.WorkRole
                    ))
                    .ToList();
            }

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
                        .Where(c => CanWorkOnDateByContract(c.Employee!, currentDate, plannedOffDaysByEmployee))
                        .Where(c => CanAssignByHours(c.Employee!, currentDate, startDate, template.StartTime, template.EndTime, template.BreakDuration, template.LunchDuration, weeklyHoursByEmployee))
                        .OrderByDescending(c => GetRemainingDaysToAssign(c, daysInMonth, assignedDatesByEmployee)) // Priorizar cumplimiento de dias libres
                        .ThenByDescending(c => c.IsPrimary)
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
                    var workedHours = CalculateWorkedHours(template.StartTime, template.EndTime, template.BreakDuration, template.LunchDuration);

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
                    TrackAssignment(employeeId, currentDate, workedHours, startDate, assignedDatesByEmployee, hoursByEmployee, weeklyHoursByEmployee);
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

        // Validar que cada empleado tenga exactamente los días libres configurados
        var employeesToValidate = employeeWorkRoles
            .Where(ewr => ewr.Employee != null)
            .Select(ewr => ewr.Employee!)
            .GroupBy(e => e.Id)
            .Select(g => g.First())
            .ToList();

        foreach (var employee in employeesToValidate)
        {
            var assignedDays = GetEmployeeAssignedDays(employee.Id, assignedDatesByEmployee);
            var requiredWorkingDays = daysInMonth - employee.FreeDaysPerMonth;
            var actualFreeDays = daysInMonth - assignedDays;
            
            if (employee.ContractType != Domain.Enums.ContractType.PartTime && actualFreeDays != employee.FreeDaysPerMonth)
            {
                warnings.Add(new ShiftGenerationWarningDto
                {
                    Date = startDate,
                    DayOfWeek = startDate.DayOfWeek,
                    WorkAreaName = "Cuota empleado",
                    WorkRoleName = "(No aplica)",
                    RequiredCount = requiredWorkingDays,
                    AssignedCount = assignedDays,
                    Reason = $"Empleado {employee.FirstName} {employee.LastName} configurado para {employee.FreeDaysPerMonth} días libres pero tiene {actualFreeDays}. Trabajó {assignedDays} de {requiredWorkingDays} días requeridos."
                });
            }

            if (employee.ContractType == Domain.Enums.ContractType.FullTime
                && plannedOffDaysByEmployee.TryGetValue(employee.Id, out var plannedOffDays)
                && plannedOffDays.Any(d => assignedDatesByEmployee.TryGetValue(employee.Id, out var assigned) && assigned.Contains(d)))
            {
                warnings.Add(new ShiftGenerationWarningDto
                {
                    Date = startDate,
                    DayOfWeek = startDate.DayOfWeek,
                    WorkAreaName = "Cuota empleado",
                    WorkRoleName = "(No aplica)",
                    RequiredCount = plannedOffDays.Count,
                    AssignedCount = plannedOffDays.Count(d => assignedDatesByEmployee.TryGetValue(employee.Id, out var assigned) && assigned.Contains(d)),
                    Reason = $"Empleado {employee.FirstName} {employee.LastName} trabajó en días libres planificados del patrón 1-2-1-2."
                });
            }
        }

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
            TotalShiftsNotCovered = warnings
                .Where(w => w.WorkAreaName != "Cuota empleado")
                .Where(w => !w.Reason.StartsWith("PreCheck:", StringComparison.OrdinalIgnoreCase))
                .Sum(w => w.RequiredCount - w.AssignedCount)
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
        if (ewr.Employee == null)
            return false;

        if (ewr.Employee.ContractType == Domain.Enums.ContractType.PartTime)
            return true;
            
        var freeDaysPerMonth = ewr.Employee.FreeDaysPerMonth;
        var requiredWorkingDays = daysInMonth - freeDaysPerMonth;
        
        // Asegurar que requiredWorkingDays sea al menos 0
        if (requiredWorkingDays < 0)
            requiredWorkingDays = 0;
            
        var assignedDays = GetEmployeeAssignedDays(ewr.EmployeeId, assignedDatesByEmployee);
        
        // El empleado debe trabajar EXACTAMENTE requiredWorkingDays días
        // No puede trabajar más de lo requerido
        return assignedDays < requiredWorkingDays;
    }

    private static bool CanWorkOnDateByContract(
        Employee employee,
        DateTime dateToAssign,
        Dictionary<Guid, HashSet<DateTime>> plannedOffDaysByEmployee)
    {
        if (employee.ContractType == Domain.Enums.ContractType.PartTime)
            return true;

        return !plannedOffDaysByEmployee.TryGetValue(employee.Id, out var offDays)
               || !offDays.Contains(dateToAssign.Date);
    }
    
    private static int GetRemainingDaysToAssign(EmployeeWorkRole ewr, int daysInMonth, Dictionary<Guid, HashSet<DateTime>> assignedDatesByEmployee)
    {
        if (ewr.Employee == null)
            return 0;

        if (ewr.Employee.ContractType == Domain.Enums.ContractType.PartTime)
        {
            var assignedDays = GetEmployeeAssignedDays(ewr.EmployeeId, assignedDatesByEmployee);
            return Math.Max(0, daysInMonth - assignedDays);
        }
            
        var freeDaysPerMonth = ewr.Employee.FreeDaysPerMonth;
        var requiredWorkingDays = daysInMonth - freeDaysPerMonth;
        if (requiredWorkingDays < 0)
            requiredWorkingDays = 0;
            
        var assignedDaysForEmployee = GetEmployeeAssignedDays(ewr.EmployeeId, assignedDatesByEmployee);
        return Math.Max(0, requiredWorkingDays - assignedDaysForEmployee);
    }

    private static List<ShiftGenerationWarningDto> BuildPreGenerationWarnings(
        DateTime generationStartDate,
        DateTime endDate,
        int daysInMonth,
        List<ShiftTemplate> shiftTemplates,
        List<SpecialDate> specialDates,
        Dictionary<Guid, WorkRole> specialDateRoles,
        Dictionary<Guid, List<EmployeeWorkRole>> roleCandidates,
        Dictionary<Guid, HashSet<DateTime>> availabilityByEmployee,
        Dictionary<Guid, HashSet<DateTime>> assignedDatesByEmployee)
    {
        var warnings = new List<ShiftGenerationWarningDto>();
        var remainingDays = (endDate.Date - generationStartDate.Date).Days + 1;

        var specialDateDict = specialDates.ToDictionary(sd => sd.Date.Date);
        var requiredByRole = new Dictionary<Guid, (int Required, string WorkAreaName, string WorkRoleName)>();
        var requiredHoursByRole = new Dictionary<Guid, decimal>();

        for (var day = 0; day < remainingDays; day++)
        {
            var currentDate = generationStartDate.AddDays(day);
            var dayOfWeek = currentDate.DayOfWeek;

            List<(Guid WorkRoleId, int RequiredCount, string WorkAreaName, string WorkRoleName, decimal NetHours)> templatesForDay;

            if (specialDateDict.TryGetValue(currentDate.Date, out var specialDate) && specialDate.Templates.Any())
            {
                templatesForDay = specialDate.Templates
                    .Select(t => (
                        WorkRoleId: t.WorkRoleId,
                        RequiredCount: t.RequiredCount,
                        WorkAreaName: t.WorkArea?.Name ?? "Desconocida",
                        WorkRoleName: specialDateRoles.TryGetValue(t.WorkRoleId, out var role)
                            ? role.Name
                            : "Desconocido",
                        NetHours: CalculateWorkedHours(t.StartTime, t.EndTime, t.BreakDuration, t.LunchDuration)))
                    .ToList();
            }
            else
            {
                templatesForDay = shiftTemplates
                    .Where(t => t.DayOfWeek == dayOfWeek)
                    .Select(t => (
                        WorkRoleId: t.WorkRoleId,
                        RequiredCount: t.RequiredCount,
                        WorkAreaName: t.WorkArea?.Name ?? "Desconocida",
                        WorkRoleName: t.WorkRole?.Name ?? "Desconocido",
                        NetHours: CalculateWorkedHours(t.StartTime, t.EndTime, t.BreakDuration, t.LunchDuration)))
                    .ToList();
            }

            foreach (var template in templatesForDay)
            {
                if (!requiredByRole.TryGetValue(template.WorkRoleId, out var current))
                {
                    requiredByRole[template.WorkRoleId] = (template.RequiredCount, template.WorkAreaName, template.WorkRoleName);
                }
                else
                {
                    requiredByRole[template.WorkRoleId] = (
                        current.Required + template.RequiredCount,
                        current.WorkAreaName,
                        current.WorkRoleName);
                }

                if (!requiredHoursByRole.ContainsKey(template.WorkRoleId))
                    requiredHoursByRole[template.WorkRoleId] = 0m;

                requiredHoursByRole[template.WorkRoleId] += template.NetHours * template.RequiredCount;
            }
        }

        foreach (var kvp in requiredByRole)
        {
            var workRoleId = kvp.Key;
            var required = kvp.Value.Required;
            var workAreaName = kvp.Value.WorkAreaName;
            var workRoleName = kvp.Value.WorkRoleName;

            if (!roleCandidates.TryGetValue(workRoleId, out var candidatesForRole) || candidatesForRole.Count == 0)
            {
                warnings.Add(new ShiftGenerationWarningDto
                {
                    Date = generationStartDate,
                    DayOfWeek = generationStartDate.DayOfWeek,
                    WorkAreaName = workAreaName,
                    WorkRoleName = workRoleName,
                    RequiredCount = required,
                    AssignedCount = 0,
                    Reason = "PreCheck: No hay empleados asignados a este rol para cubrir la demanda mensual."
                });
                continue;
            }

            var capacity = 0;
            var capacityHours = 0m;
            var distinctEmployees = candidatesForRole
                .Where(c => c.Employee != null)
                .GroupBy(c => c.EmployeeId)
                .Select(g => g.First());

            foreach (var candidate in distinctEmployees)
            {
                var employee = candidate.Employee!;
                var unavailableDays = availabilityByEmployee.TryGetValue(employee.Id, out var dates)
                    ? dates.Count(d => d >= generationStartDate.Date && d <= endDate.Date)
                    : 0;

                var assignedDaysSoFar = GetEmployeeAssignedDays(employee.Id, assignedDatesByEmployee);
                var availableDaysRemaining = Math.Max(0, remainingDays - unavailableDays);
                var remainingWorkingDays = employee.ContractType == Domain.Enums.ContractType.PartTime
                    ? availableDaysRemaining
                    : Math.Max(0, (daysInMonth - employee.FreeDaysPerMonth) - assignedDaysSoFar);
                var capacityDays = Math.Min(remainingWorkingDays, availableDaysRemaining);

                capacity += capacityDays;
                // Capacidad de horas se basa en WeeklyMaxHours del empleado
                // Estimamos horas disponibles como: capacityDays * (WeeklyMaxHours / 5 días promedio de trabajo por semana)
                var hoursPerDay = employee.WeeklyMaxHours / 5m; // Asumiendo 5 días de trabajo por semana
                capacityHours += capacityDays * hoursPerDay;
            }

            if (capacity < required)
            {
                warnings.Add(new ShiftGenerationWarningDto
                {
                    Date = generationStartDate,
                    DayOfWeek = generationStartDate.DayOfWeek,
                    WorkAreaName = workAreaName,
                    WorkRoleName = workRoleName,
                    RequiredCount = required,
                    AssignedCount = capacity,
                    Reason = "PreCheck: Capacidad mensual insuficiente considerando dias libres y disponibilidad."
                });
            }

            if (requiredHoursByRole.TryGetValue(workRoleId, out var requiredHours))
            {
                if (capacityHours < requiredHours)
                {
                    warnings.Add(new ShiftGenerationWarningDto
                    {
                        Date = generationStartDate,
                        DayOfWeek = generationStartDate.DayOfWeek,
                        WorkAreaName = workAreaName,
                        WorkRoleName = workRoleName,
                        RequiredCount = (int)Math.Ceiling(requiredHours),
                        AssignedCount = (int)Math.Floor(capacityHours),
                        Reason = $"PreCheck: Horas insuficientes para cubrir la demanda. Requeridas {requiredHours:F1}h, capacidad {capacityHours:F1}h."
                    });
                }
                else if (capacityHours > requiredHours)
                {
                    warnings.Add(new ShiftGenerationWarningDto
                    {
                        Date = generationStartDate,
                        DayOfWeek = generationStartDate.DayOfWeek,
                        WorkAreaName = workAreaName,
                        WorkRoleName = workRoleName,
                        RequiredCount = (int)Math.Ceiling(requiredHours),
                        AssignedCount = (int)Math.Floor(capacityHours),
                        Reason = $"PreCheck: Exceso de capacidad de horas. Requeridas {requiredHours:F1}h, capacidad {capacityHours:F1}h. Algunos empleados no alcanzaran sus horas."
                    });
                }
            }
        }

        return warnings;
    }

    private static bool CanAssignByHours(
        Employee employee,
        DateTime dateToAssign,
        DateTime monthStart,
        TimeSpan startTime,
        TimeSpan endTime,
        TimeSpan? breakDuration,
        TimeSpan? lunchDuration,
        Dictionary<Guid, Dictionary<int, decimal>> weeklyHoursByEmployee)
    {
        var maxHours = employee.WeeklyMaxHours;
        var currentHours = GetEmployeeWeeklyHours(employee.Id, dateToAssign, monthStart, weeklyHoursByEmployee);
        var workedHours = CalculateWorkedHours(startTime, endTime, breakDuration, lunchDuration);
        return currentHours + workedHours <= maxHours;
    }

    private static void TrackAssignment(
        Guid employeeId,
        DateTime date,
        decimal workedHours,
        DateTime monthStart,
        Dictionary<Guid, HashSet<DateTime>> assignedDatesByEmployee,
        Dictionary<Guid, decimal> hoursByEmployee,
        Dictionary<Guid, Dictionary<int, decimal>> weeklyHoursByEmployee)
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

        var weekIndex = GetWeekIndex(date, monthStart);
        if (!weeklyHoursByEmployee.TryGetValue(employeeId, out var weeklyHours))
        {
            weeklyHours = new Dictionary<int, decimal>();
            weeklyHoursByEmployee[employeeId] = weeklyHours;
        }

        if (!weeklyHours.ContainsKey(weekIndex))
            weeklyHours[weekIndex] = 0m;

        weeklyHours[weekIndex] += workedHours;
    }
    
    private static Dictionary<Guid, HashSet<DateTime>> BuildPlannedOffDaysByEmployee(
        DateTime monthStart,
        DateTime monthEnd,
        List<ShiftTemplate> shiftTemplates,
        Dictionary<DateTime, SpecialDate> specialDateDict,
        Dictionary<Guid, HashSet<Guid>> roleIdsByEmployee,
        IEnumerable<Employee> employees)
    {
        var demandByDateRole = BuildDemandByDateForRoles(monthStart, monthEnd, shiftTemplates, specialDateDict);
        var planned = new Dictionary<Guid, HashSet<DateTime>>();
        var pattern = new[] { 1, 2, 1, 2 };

        foreach (var employee in employees)
        {
            if (employee.ContractType != ContractType.FullTime)
                continue;

            var remaining = Math.Max(0, employee.FreeDaysPerMonth);
            var offDays = new HashSet<DateTime>();
            var roleIds = roleIdsByEmployee.TryGetValue(employee.Id, out var roles)
                ? roles
                : new HashSet<Guid>();

            var patternIndex = 0;
            for (var weekStart = monthStart.Date; weekStart <= monthEnd.Date && remaining > 0; weekStart = weekStart.AddDays(7), patternIndex++)
            {
                var weekEnd = weekStart.AddDays(6);
                if (weekEnd > monthEnd.Date)
                    weekEnd = monthEnd.Date;

                var weekDays = new List<DateTime>();
                for (var day = weekStart.Date; day <= weekEnd.Date; day = day.AddDays(1))
                {
                    if (IsWeekday(day))
                        weekDays.Add(day.Date);
                }

                if (weekDays.Count == 0)
                    continue;

                var offCount = Math.Min(pattern[patternIndex % pattern.Length], remaining);
                var selected = SelectOffDaysForWeek(weekDays, offCount, demandByDateRole, roleIds);
                foreach (var selectedDay in selected)
                    offDays.Add(selectedDay);

                remaining -= selected.Count;
            }

            if (offDays.Count > 0)
                planned[employee.Id] = offDays;
        }

        return planned;
    }

    private static Dictionary<DateTime, Dictionary<Guid, int>> BuildDemandByDateForRoles(
        DateTime monthStart,
        DateTime monthEnd,
        List<ShiftTemplate> shiftTemplates,
        Dictionary<DateTime, SpecialDate> specialDateDict)
    {
        var demand = new Dictionary<DateTime, Dictionary<Guid, int>>();

        for (var date = monthStart.Date; date <= monthEnd.Date; date = date.AddDays(1))
        {
            var dayOfWeek = date.DayOfWeek;
            var perRole = new Dictionary<Guid, int>();

            if (specialDateDict.TryGetValue(date.Date, out var specialDate) && specialDate.Templates.Any())
            {
                foreach (var template in specialDate.Templates)
                {
                    if (!perRole.ContainsKey(template.WorkRoleId))
                        perRole[template.WorkRoleId] = 0;

                    perRole[template.WorkRoleId] += template.RequiredCount;
                }
            }
            else
            {
                foreach (var template in shiftTemplates.Where(t => t.DayOfWeek == dayOfWeek))
                {
                    if (!perRole.ContainsKey(template.WorkRoleId))
                        perRole[template.WorkRoleId] = 0;

                    perRole[template.WorkRoleId] += template.RequiredCount;
                }
            }

            demand[date.Date] = perRole;
        }

        return demand;
    }

    private static List<DateTime> SelectOffDaysForWeek(
        List<DateTime> weekDays,
        int offCount,
        Dictionary<DateTime, Dictionary<Guid, int>> demandByDateRole,
        HashSet<Guid> roleIds)
    {
        var selected = new List<DateTime>();
        if (offCount <= 0 || weekDays.Count == 0)
            return selected;

        if (offCount == 1)
        {
            var best = weekDays
                .OrderBy(day => GetDemandScore(day, demandByDateRole, roleIds))
                .ThenBy(day => day)
                .First();
            selected.Add(best);
            return selected;
        }

        // Intentar 2 días consecutivos
        var bestPair = weekDays
            .Zip(weekDays.Skip(1), (first, second) => new { first, second })
            .Where(pair => pair.first.AddDays(1) == pair.second)
            .Select(pair => new
            {
                pair.first,
                pair.second,
                score = GetDemandScore(pair.first, demandByDateRole, roleIds)
                        + GetDemandScore(pair.second, demandByDateRole, roleIds)
            })
            .OrderBy(pair => pair.score)
            .ThenBy(pair => pair.first)
            .FirstOrDefault();

        if (bestPair != null)
        {
            selected.Add(bestPair.first);
            selected.Add(bestPair.second);
            return selected;
        }

        // Fallback: tomar los dias con menor demanda
        selected.AddRange(
            weekDays
                .OrderBy(day => GetDemandScore(day, demandByDateRole, roleIds))
                .ThenBy(day => day)
                .Take(offCount));

        return selected;
    }

    private static int GetDemandScore(
        DateTime day,
        Dictionary<DateTime, Dictionary<Guid, int>> demandByDateRole,
        HashSet<Guid> roleIds)
    {
        if (!demandByDateRole.TryGetValue(day.Date, out var perRole))
            return 0;

        var score = 0;
        foreach (var roleId in roleIds)
        {
            if (perRole.TryGetValue(roleId, out var count))
                score += count;
        }

        return score;
    }

    private static bool IsWeekday(DateTime date)
    {
        return date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday;
    }

    private static decimal CalculateWorkedHours(TimeSpan startTime, TimeSpan endTime, TimeSpan? breakDuration, TimeSpan? lunchDuration)
    {
        var breakMinutes = breakDuration?.TotalMinutes ?? 0;
        var lunchMinutes = lunchDuration?.TotalMinutes ?? 0;
        var totalMinutes = (endTime - startTime).TotalMinutes - breakMinutes - lunchMinutes;
        return (decimal)(totalMinutes / 60.0);
    }

    private static decimal GetEmployeeHours(Guid employeeId, Dictionary<Guid, decimal> hoursByEmployee)
    {
        return hoursByEmployee.TryGetValue(employeeId, out var hours) ? hours : 0m;
    }

    private static decimal GetEmployeeWeeklyHours(
        Guid employeeId,
        DateTime date,
        DateTime monthStart,
        Dictionary<Guid, Dictionary<int, decimal>> weeklyHoursByEmployee)
    {
        var weekIndex = GetWeekIndex(date, monthStart);
        if (!weeklyHoursByEmployee.TryGetValue(employeeId, out var weeklyHours))
            return 0m;

        return weeklyHours.TryGetValue(weekIndex, out var hours) ? hours : 0m;
    }

    private static int GetWeekIndex(DateTime date, DateTime monthStart)
    {
        return Math.Max(0, (date.Date - monthStart.Date).Days / 7);
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
            HoursPerDay = request.HoursPerDay,
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
        HoursPerDay = config.HoursPerDay,
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

        config.HoursPerDay = request.HoursPerDay;
        config.FreeDayColor = string.IsNullOrWhiteSpace(request.FreeDayColor) ? "#E8E8E8" : request.FreeDayColor;
        config.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(config);
    }

    private static ScheduleConfigurationDto MapToDto(ScheduleConfiguration config) => new()
    {
        Id = config.Id,
        BranchId = config.BranchId,
        HoursPerDay = config.HoursPerDay,
        FreeDayColor = string.IsNullOrWhiteSpace(config.FreeDayColor) ? "#E8E8E8" : config.FreeDayColor
    };
}
