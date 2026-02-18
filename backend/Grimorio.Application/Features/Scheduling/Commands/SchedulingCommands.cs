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
}

public class UpdateWorkRoleCommand : IRequest<WorkRoleDto>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
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

public class UpdateShiftAssignmentCommand : IRequest<ShiftAssignmentDto>
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public TimeSpan? BreakDuration { get; set; }
    public TimeSpan? LunchDuration { get; set; }
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
    public Guid EmployeeId { get; set; }
}

// ======================== Shift Generation Command ========================

public class GenerateMonthlyShiftsCommand : IRequest<ShiftGenerationResultDto>
{
    public Guid BranchId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
}

// ======================== Schedule Configuration Commands ========================

public class CreateScheduleConfigurationCommand : IRequest<ScheduleConfigurationDto>
{
    public Guid BranchId { get; set; }
    public decimal HoursPerDay { get; set; } = 8.0m;
    public string FreeDayColor { get; set; } = "#E8E8E8";
}

public class UpdateScheduleConfigurationCommand : IRequest<ScheduleConfigurationDto>
{
    public Guid Id { get; set; }
    public decimal HoursPerDay { get; set; } = 8.0m;
    public string FreeDayColor { get; set; } = "#E8E8E8";
}

// ======================== SpecialDate Commands ========================

public class CreateSpecialDateCommand : IRequest<SpecialDateDto>
{
    public Guid BranchId { get; set; }
    public DateTime Date { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateSpecialDateCommand : IRequest<SpecialDateDto>
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class DeleteSpecialDateCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

// ======================== SpecialDateTemplate Commands ========================

public class CreateSpecialDateTemplateCommand : IRequest<SpecialDateTemplateDto>
{
    public Guid SpecialDateId { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public TimeSpan? BreakDuration { get; set; }
    public TimeSpan? LunchDuration { get; set; }
    public Guid WorkAreaId { get; set; }
    public Guid WorkRoleId { get; set; }
    public int RequiredCount { get; set; } = 1;
    public string? Notes { get; set; }
}

public class UpdateSpecialDateTemplateCommand : IRequest<SpecialDateTemplateDto>
{
    public Guid Id { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public TimeSpan? BreakDuration { get; set; }
    public TimeSpan? LunchDuration { get; set; }
    public Guid WorkAreaId { get; set; }
    public Guid WorkRoleId { get; set; }
    public int RequiredCount { get; set; }
    public string? Notes { get; set; }
}

public class DeleteSpecialDateTemplateCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

