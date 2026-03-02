using FluentValidation;
using Grimorio.Application.Features.Scheduling.Queries;

namespace Grimorio.Application.Features.Scheduling.Validators;

public class GetWorkAreasQueryValidator : AbstractValidator<GetWorkAreasQuery>
{
    public GetWorkAreasQueryValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
    }
}

public class GetWorkAreaByIdQueryValidator : AbstractValidator<GetWorkAreaByIdQuery>
{
    public GetWorkAreaByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetWorkRolesQueryValidator : AbstractValidator<GetWorkRolesQuery>
{
    public GetWorkRolesQueryValidator()
    {
        RuleFor(x => x.WorkAreaId).Must(id => !id.HasValue || id.Value != Guid.Empty);
    }
}

public class GetWorkRoleByIdQueryValidator : AbstractValidator<GetWorkRoleByIdQuery>
{
    public GetWorkRoleByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetEmployeeWorkRolesQueryValidator : AbstractValidator<GetEmployeeWorkRolesQuery>
{
    public GetEmployeeWorkRolesQueryValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
    }
}

public class GetShiftTemplatesQueryValidator : AbstractValidator<GetShiftTemplatesQuery>
{
    public GetShiftTemplatesQueryValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.DayOfWeek).IsInEnum().When(x => x.DayOfWeek.HasValue);
        RuleFor(x => x.Month).InclusiveBetween(1, 12).When(x => x.Month.HasValue);
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100).When(x => x.Year.HasValue);
    }
}

public class GetShiftTemplateByIdQueryValidator : AbstractValidator<GetShiftTemplateByIdQuery>
{
    public GetShiftTemplateByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetMonthlyShiftsQueryValidator : AbstractValidator<GetMonthlyShiftsQuery>
{
    public GetMonthlyShiftsQueryValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

public class GetEmployeeMonthlyShiftsQueryValidator : AbstractValidator<GetEmployeeMonthlyShiftsQuery>
{
    public GetEmployeeMonthlyShiftsQueryValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

public class GetShiftAssignmentByIdQueryValidator : AbstractValidator<GetShiftAssignmentByIdQuery>
{
    public GetShiftAssignmentByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetShiftAssignmentsByDateQueryValidator : AbstractValidator<GetShiftAssignmentsByDateQuery>
{
    public GetShiftAssignmentsByDateQueryValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
    }
}

public class GetSpecialDateTemplatesQueryValidator : AbstractValidator<GetSpecialDateTemplatesQuery>
{
    public GetSpecialDateTemplatesQueryValidator()
    {
        RuleFor(x => x.SpecialDateId).NotEmpty();
    }
}

public class GetSpecialDateTemplateByIdQueryValidator : AbstractValidator<GetSpecialDateTemplateByIdQuery>
{
    public GetSpecialDateTemplateByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetEmployeeAvailabilityQueryValidator : AbstractValidator<GetEmployeeAvailabilityQuery>
{
    public GetEmployeeAvailabilityQueryValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Month).InclusiveBetween(1, 12).When(x => x.Month.HasValue);
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100).When(x => x.Year.HasValue);
    }
}

public class GetSchedulableEmployeesQueryValidator : AbstractValidator<GetSchedulableEmployeesQuery>
{
    public GetSchedulableEmployeesQueryValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
    }
}

public class GetScheduleConfigurationQueryValidator : AbstractValidator<GetScheduleConfigurationQuery>
{
    public GetScheduleConfigurationQueryValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
    }
}

public class GetFreeEmployeesByDateQueryValidator : AbstractValidator<GetFreeEmployeesByDateQuery>
{
    public GetFreeEmployeesByDateQueryValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
    }
}

public class GetSpecialDatesQueryValidator : AbstractValidator<GetSpecialDatesQuery>
{
    public GetSpecialDatesQueryValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
    }
}

public class GetSpecialDateByIdQueryValidator : AbstractValidator<GetSpecialDateByIdQuery>
{
    public GetSpecialDateByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetSpecialDateByDateQueryValidator : AbstractValidator<GetSpecialDateByDateQuery>
{
    public GetSpecialDateByDateQueryValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
    }
}
