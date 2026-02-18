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
    public int? Month { get; set; } // 1-12, para filtrar plantillas de un mes específico
    public int? Year { get; set; } // Para filtrar plantillas de un año específico
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
// ======================== SpecialDateTemplate Queries ========================

public class GetSpecialDateTemplatesQuery : IRequest<List<SpecialDateTemplateDto>>
{
    public Guid SpecialDateId { get; set; }
}

public class GetSpecialDateTemplateByIdQuery : IRequest<SpecialDateTemplateDto?>
{
    public Guid Id { get; set; }
}
// ======================== EmployeeAvailability Queries ========================

public class GetEmployeeAvailabilityQuery : IRequest<List<EmployeeAvailabilityDto>>
{
    public Guid EmployeeId { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
}

// ======================== Schedulable Employees Query ========================

public class GetSchedulableEmployeesQuery : IRequest<List<EmployeeDto>>
{
    public Guid BranchId { get; set; }
}

// ======================== Schedule Configuration Queries ========================

public class GetScheduleConfigurationQuery : IRequest<ScheduleConfigurationDto?>
{
    public Guid BranchId { get; set; }
}

// ======================== Free Employees Query ========================

public class GetFreeEmployeesByDateQuery : IRequest<List<EmployeeDto>>
{
    public Guid BranchId { get; set; }
    public DateTime Date { get; set; }
}
// ======================== SpecialDate Queries ========================

public class GetSpecialDatesQuery : IRequest<List<SpecialDateDto>>
{
    public Guid BranchId { get; set; }
}

public class GetSpecialDateByIdQuery : IRequest<SpecialDateDto?>
{
    public Guid Id { get; set; }
}

public class GetSpecialDateByDateQuery : IRequest<SpecialDateDto?>
{
    public Guid BranchId { get; set; }
    public DateTime Date { get; set; }
}