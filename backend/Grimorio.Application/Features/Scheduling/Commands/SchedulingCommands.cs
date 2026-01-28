using MediatR;
using Grimorio.Application.DTOs;

namespace Grimorio.Application.Features.Scheduling.Commands;

// ======================== WorkArea Commands ========================

public class CreateWorkAreaCommand : IRequest<WorkAreaDto>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "#808080";
    public int DisplayOrder { get; set; }
    public Guid BranchId { get; set; }
}

public class UpdateWorkAreaCommand : IRequest<WorkAreaDto>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

public class DeleteWorkAreaCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

// ======================== WorkRole Commands ========================

public class CreateWorkRoleCommand : IRequest<WorkRoleDto>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid WorkAreaId { get; set; }
    public int FreeDaysPerMonth { get; set; } = 6;
    public decimal DailyHoursTarget { get; set; } = 8.0m;
}

public class UpdateWorkRoleCommand : IRequest<WorkRoleDto>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int FreeDaysPerMonth { get; set; }
    public decimal DailyHoursTarget { get; set; }
}

public class DeleteWorkRoleCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

// ======================== EmployeeWorkRole Commands ========================

public class AssignWorkRolesToEmployeeCommand : IRequest<List<EmployeeWorkRoleDto>>
{
    public Guid EmployeeId { get; set; }
    public List<Guid> WorkRoleIds { get; set; } = new();
}

public class RemoveWorkRoleFromEmployeeCommand : IRequest<bool>
{
    public Guid EmployeeId { get; set; }
    public Guid WorkRoleId { get; set; }
}

// ======================== ShiftTemplate Commands ========================

public class CreateShiftTemplateCommand : IRequest<ShiftTemplateDto>
{
    public Guid BranchId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public TimeSpan? BreakDuration { get; set; }
    public TimeSpan? LunchDuration { get; set; }
    public Guid WorkAreaId { get; set; }
    public Guid WorkRoleId { get; set; }
    public int RequiredCount { get; set; } = 1;
    public string? Notes { get; set; }
}

public class UpdateShiftTemplateCommand : IRequest<ShiftTemplateDto>
{
    public Guid Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public TimeSpan? BreakDuration { get; set; }
    public TimeSpan? LunchDuration { get; set; }
    public int RequiredCount { get; set; }
    public string? Notes { get; set; }
}

public class DeleteShiftTemplateCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

// ======================== ShiftAssignment Commands ========================

public class CreateShiftAssignmentCommand : IRequest<ShiftAssignmentDto>
{
    public Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public TimeSpan? BreakDuration { get; set; }
    public TimeSpan? LunchDuration { get; set; }
    public Guid WorkAreaId { get; set; }
    public Guid WorkRoleId { get; set; }
    public string? Notes { get; set; }
}

public class ApproveShiftAssignmentCommand : IRequest<bool>
{
    public Guid ShiftAssignmentId { get; set; }
    public Guid ApprovedBy { get; set; }
}

public class DeleteShiftAssignmentCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

// ======================== EmployeeAvailability Commands ========================

public class AddEmployeeAvailabilityCommand : IRequest<EmployeeAvailabilityDto>
{
    public Guid EmployeeId { get; set; }
    public DateTime UnavailableDate { get; set; }
    public string? Reason { get; set; }
}

public class RemoveEmployeeAvailabilityCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

// ======================== Shift Generation Command ========================

public class GenerateMonthlyShiftsCommand : IRequest<List<ShiftAssignmentDto>>
{
    public Guid BranchId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
}

// ======================== Schedule Configuration Commands ========================

public class CreateScheduleConfigurationCommand : IRequest<ScheduleConfigurationDto>
{
    public Guid BranchId { get; set; }
    public decimal MinHoursPerMonth { get; set; }
    public decimal MaxHoursPerMonth { get; set; }
    public decimal HoursMondayThursday { get; set; }
    public decimal HoursFridaySaturday { get; set; }
    public decimal HoursSunday { get; set; }
    public int FreeDaysParrillero { get; set; }
    public int FreeDaysOtherRoles { get; set; }
    public int MinStaffCocina { get; set; }
    public int MinStaffCaja { get; set; }
    public int MinStaffMesas { get; set; }
    public int MinStaffBar { get; set; }
}

public class UpdateScheduleConfigurationCommand : IRequest<ScheduleConfigurationDto>
{
    public Guid Id { get; set; }
    public decimal MinHoursPerMonth { get; set; }
    public decimal MaxHoursPerMonth { get; set; }
    public decimal HoursMondayThursday { get; set; }
    public decimal HoursFridaySaturday { get; set; }
    public decimal HoursSunday { get; set; }
    public int FreeDaysParrillero { get; set; }
    public int FreeDaysOtherRoles { get; set; }
    public int MinStaffCocina { get; set; }
    public int MinStaffCaja { get; set; }
    public int MinStaffMesas { get; set; }
    public int MinStaffBar { get; set; }
}
