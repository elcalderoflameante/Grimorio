using FluentValidation;
using Grimorio.Application.Features.Payroll.Commands;
using Grimorio.Application.Features.Payroll.Queries;

namespace Grimorio.Application.Features.Payroll.Validators;

public class UpsertPayrollConfigurationCommandValidator : AbstractValidator<UpsertPayrollConfigurationCommand>
{
    public UpsertPayrollConfigurationCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.IessEmployeeRate).InclusiveBetween(0m, 100m);
        RuleFor(x => x.IessEmployerRate).InclusiveBetween(0m, 100m);
        RuleFor(x => x.IncomeTaxRate).InclusiveBetween(0m, 100m);
        RuleFor(x => x.OvertimeRate50).InclusiveBetween(0m, 100m);
        RuleFor(x => x.OvertimeRate100).InclusiveBetween(0m, 200m);
        RuleFor(x => x.DecimoThirdRate).InclusiveBetween(0m, 100m);
        RuleFor(x => x.DecimoFourthRate).InclusiveBetween(0m, 100m);
        RuleFor(x => x.ReserveFundRate).InclusiveBetween(0m, 100m);
        RuleFor(x => x.MonthlyHours).GreaterThan(0);
    }
}

public class CreatePayrollAdvanceCommandValidator : AbstractValidator<CreatePayrollAdvanceCommand>
{
    public CreatePayrollAdvanceCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Method).NotEmpty();
    }
}

public class CreateEmployeeConsumptionCommandValidator : AbstractValidator<CreateEmployeeConsumptionCommand>
{
    public CreateEmployeeConsumptionCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public class CreatePayrollAdjustmentCommandValidator : AbstractValidator<CreatePayrollAdjustmentCommand>
{
    public CreatePayrollAdjustmentCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Amount).GreaterThan(0).When(x => x.Amount.HasValue);
        RuleFor(x => x.Hours).GreaterThan(0).When(x => x.Hours.HasValue);
        RuleFor(x => x).Must(x => x.Amount.HasValue || x.Hours.HasValue);
    }
}

public class GetPayrollConfigurationQueryValidator : AbstractValidator<GetPayrollConfigurationQuery>
{
    public GetPayrollConfigurationQueryValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
    }
}

public class GetPayrollSummaryQueryValidator : AbstractValidator<GetPayrollSummaryQuery>
{
    public GetPayrollSummaryQueryValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

public class GetPayrollAdvancesQueryValidator : AbstractValidator<GetPayrollAdvancesQuery>
{
    public GetPayrollAdvancesQueryValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.EmployeeId).Must(id => !id.HasValue || id.Value != Guid.Empty);
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100).When(x => x.Year.HasValue);
        RuleFor(x => x.Month).InclusiveBetween(1, 12).When(x => x.Month.HasValue);
    }
}

public class GetEmployeeConsumptionsQueryValidator : AbstractValidator<GetEmployeeConsumptionsQuery>
{
    public GetEmployeeConsumptionsQueryValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.EmployeeId).Must(id => !id.HasValue || id.Value != Guid.Empty);
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100).When(x => x.Year.HasValue);
        RuleFor(x => x.Month).InclusiveBetween(1, 12).When(x => x.Month.HasValue);
    }
}

public class GetPayrollAdjustmentsQueryValidator : AbstractValidator<GetPayrollAdjustmentsQuery>
{
    public GetPayrollAdjustmentsQueryValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.EmployeeId).Must(id => !id.HasValue || id.Value != Guid.Empty);
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100).When(x => x.Year.HasValue);
        RuleFor(x => x.Month).InclusiveBetween(1, 12).When(x => x.Month.HasValue);
    }
}

public class GenerateMonthlyPayrollRolesCommandValidator : AbstractValidator<GenerateMonthlyPayrollRolesCommand>
{
    public GenerateMonthlyPayrollRolesCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

public class UpdatePayrollRoleStatusCommandValidator : AbstractValidator<UpdatePayrollRoleStatusCommand>
{
    public UpdatePayrollRoleStatusCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.PayrollRoleId).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
    }
}

public class GetPayrollRolesByEmployeeQueryValidator : AbstractValidator<GetPayrollRolesByEmployeeQuery>
{
    public GetPayrollRolesByEmployeeQueryValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
    }
}

public class GetPayrollRoleDetailQueryValidator : AbstractValidator<GetPayrollRoleDetailQuery>
{
    public GetPayrollRoleDetailQueryValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.PayrollRoleId).NotEmpty();
    }
}
