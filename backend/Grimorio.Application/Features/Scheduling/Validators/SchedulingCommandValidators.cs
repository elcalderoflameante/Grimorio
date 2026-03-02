using FluentValidation;
using Grimorio.Application.Features.Scheduling.Commands;

namespace Grimorio.Application.Features.Scheduling.Validators;

public class CreateWorkAreaCommandValidator : AbstractValidator<CreateWorkAreaCommand>
{
    public CreateWorkAreaCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Color).NotEmpty();
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public class UpdateWorkAreaCommandValidator : AbstractValidator<UpdateWorkAreaCommand>
{
    public UpdateWorkAreaCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Color).NotEmpty();
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public class DeleteWorkAreaCommandValidator : AbstractValidator<DeleteWorkAreaCommand>
{
    public DeleteWorkAreaCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class CreateWorkRoleCommandValidator : AbstractValidator<CreateWorkRoleCommand>
{
    public CreateWorkRoleCommandValidator()
    {
        RuleFor(x => x.WorkAreaId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class UpdateWorkRoleCommandValidator : AbstractValidator<UpdateWorkRoleCommand>
{
    public UpdateWorkRoleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class DeleteWorkRoleCommandValidator : AbstractValidator<DeleteWorkRoleCommand>
{
    public DeleteWorkRoleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class AssignWorkRolesToEmployeeCommandValidator : AbstractValidator<AssignWorkRolesToEmployeeCommand>
{
    public AssignWorkRolesToEmployeeCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.WorkRoleIds).NotEmpty();
        RuleForEach(x => x.WorkRoleIds).NotEmpty();
    }
}

public class RemoveWorkRoleFromEmployeeCommandValidator : AbstractValidator<RemoveWorkRoleFromEmployeeCommand>
{
    public RemoveWorkRoleFromEmployeeCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.WorkRoleId).NotEmpty();
    }
}

public class CreateShiftTemplateCommandValidator : AbstractValidator<CreateShiftTemplateCommand>
{
    public CreateShiftTemplateCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.WorkAreaId).NotEmpty();
        RuleFor(x => x.WorkRoleId).NotEmpty();
        RuleFor(x => x.DayOfWeek).IsInEnum();
        RuleFor(x => x.StartTime).LessThan(x => x.EndTime);
        RuleFor(x => x.RequiredCount).GreaterThan(0);
    }
}

public class UpdateShiftTemplateCommandValidator : AbstractValidator<UpdateShiftTemplateCommand>
{
    public UpdateShiftTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.DayOfWeek).IsInEnum();
        RuleFor(x => x.StartTime).LessThan(x => x.EndTime);
        RuleFor(x => x.RequiredCount).GreaterThan(0);
    }
}

public class DeleteShiftTemplateCommandValidator : AbstractValidator<DeleteShiftTemplateCommand>
{
    public DeleteShiftTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class CreateShiftAssignmentCommandValidator : AbstractValidator<CreateShiftAssignmentCommand>
{
    public CreateShiftAssignmentCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.WorkAreaId).NotEmpty();
        RuleFor(x => x.WorkRoleId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.StartTime).LessThan(x => x.EndTime);
    }
}

public class UpdateShiftAssignmentCommandValidator : AbstractValidator<UpdateShiftAssignmentCommand>
{
    public UpdateShiftAssignmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.StartTime).LessThan(x => x.EndTime);
    }
}

public class ApproveShiftAssignmentCommandValidator : AbstractValidator<ApproveShiftAssignmentCommand>
{
    public ApproveShiftAssignmentCommandValidator()
    {
        RuleFor(x => x.ShiftAssignmentId).NotEmpty();
        RuleFor(x => x.ApprovedBy).NotEmpty();
    }
}

public class DeleteShiftAssignmentCommandValidator : AbstractValidator<DeleteShiftAssignmentCommand>
{
    public DeleteShiftAssignmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class AddEmployeeAvailabilityCommandValidator : AbstractValidator<AddEmployeeAvailabilityCommand>
{
    public AddEmployeeAvailabilityCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.UnavailableDate).NotEmpty();
    }
}

public class RemoveEmployeeAvailabilityCommandValidator : AbstractValidator<RemoveEmployeeAvailabilityCommand>
{
    public RemoveEmployeeAvailabilityCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
    }
}

public class GenerateMonthlyShiftsCommandValidator : AbstractValidator<GenerateMonthlyShiftsCommand>
{
    public GenerateMonthlyShiftsCommandValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

public class CreateScheduleConfigurationCommandValidator : AbstractValidator<CreateScheduleConfigurationCommand>
{
    public CreateScheduleConfigurationCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.HoursPerDay).GreaterThan(0);
        RuleFor(x => x.FreeDayColor).NotEmpty();
    }
}

public class UpdateScheduleConfigurationCommandValidator : AbstractValidator<UpdateScheduleConfigurationCommand>
{
    public UpdateScheduleConfigurationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.HoursPerDay).GreaterThan(0);
        RuleFor(x => x.FreeDayColor).NotEmpty();
    }
}

public class CreateSpecialDateCommandValidator : AbstractValidator<CreateSpecialDateCommand>
{
    public CreateSpecialDateCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class UpdateSpecialDateCommandValidator : AbstractValidator<UpdateSpecialDateCommand>
{
    public UpdateSpecialDateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class DeleteSpecialDateCommandValidator : AbstractValidator<DeleteSpecialDateCommand>
{
    public DeleteSpecialDateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class CreateSpecialDateTemplateCommandValidator : AbstractValidator<CreateSpecialDateTemplateCommand>
{
    public CreateSpecialDateTemplateCommandValidator()
    {
        RuleFor(x => x.SpecialDateId).NotEmpty();
        RuleFor(x => x.WorkAreaId).NotEmpty();
        RuleFor(x => x.WorkRoleId).NotEmpty();
        RuleFor(x => x.StartTime).LessThan(x => x.EndTime);
        RuleFor(x => x.RequiredCount).GreaterThan(0);
    }
}

public class UpdateSpecialDateTemplateCommandValidator : AbstractValidator<UpdateSpecialDateTemplateCommand>
{
    public UpdateSpecialDateTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.WorkAreaId).NotEmpty();
        RuleFor(x => x.WorkRoleId).NotEmpty();
        RuleFor(x => x.StartTime).LessThan(x => x.EndTime);
        RuleFor(x => x.RequiredCount).GreaterThan(0);
    }
}

public class DeleteSpecialDateTemplateCommandValidator : AbstractValidator<DeleteSpecialDateTemplateCommand>
{
    public DeleteSpecialDateTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
