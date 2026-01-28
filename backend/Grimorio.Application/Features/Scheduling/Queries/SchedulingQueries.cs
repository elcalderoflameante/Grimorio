using MediatR;
using Grimorio.Application.DTOs;

namespace Grimorio.Application.Features.Scheduling.Queries;

// ======================== WorkArea Queries ========================

public class GetWorkAreasQuery : IRequest<List<WorkAreaDto>>
{
    public Guid BranchId { get; set; }
}

public class GetWorkAreaByIdQuery : IRequest<WorkAreaDto?>
{
    public Guid Id { get; set; }
}

// ======================== WorkRole Queries ========================

public class GetWorkRolesQuery : IRequest<List<WorkRoleDto>>
{
    public Guid? WorkAreaId { get; set; }
}

public class GetWorkRoleByIdQuery : IRequest<WorkRoleDto?>
{
    public Guid Id { get; set; }
}

// ======================== EmployeeWorkRole Queries ========================

public class GetEmployeeWorkRolesQuery : IRequest<List<EmployeeWorkRoleDto>>
{
    public Guid EmployeeId { get; set; }
}

// ======================== ShiftTemplate Queries ========================

public class GetShiftTemplatesQuery : IRequest<List<ShiftTemplateDto>>
{
    public Guid BranchId { get; set; }
    public DayOfWeek? DayOfWeek { get; set; }
}

public class GetShiftTemplateByIdQuery : IRequest<ShiftTemplateDto?>
{
    public Guid Id { get; set; }
}

// ======================== ShiftAssignment Queries ========================

public class GetMonthlyShiftsQuery : IRequest<List<ShiftAssignmentDto>>
{
    public Guid BranchId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
}

public class GetEmployeeMonthlyShiftsQuery : IRequest<List<ShiftAssignmentDto>>
{
    public Guid EmployeeId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
}

public class GetShiftAssignmentByIdQuery : IRequest<ShiftAssignmentDto?>
{
    public Guid Id { get; set; }
}

public class GetShiftAssignmentsByDateQuery : IRequest<List<ShiftAssignmentDto>>
{
    public Guid BranchId { get; set; }
    public DateTime Date { get; set; }
}

// ======================== EmployeeAvailability Queries ========================

public class GetEmployeeAvailabilityQuery : IRequest<List<EmployeeAvailabilityDto>>
{
    public Guid EmployeeId { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
}
