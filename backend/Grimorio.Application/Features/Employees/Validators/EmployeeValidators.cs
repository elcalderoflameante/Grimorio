using FluentValidation;
using Grimorio.Application.Features.Employees.Commands;
using Grimorio.Application.Features.Employees.Queries;

namespace Grimorio.Application.Features.Employees.Validators;

public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.PositionId).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Phone).NotEmpty();
        RuleFor(x => x.IdentificationNumber).NotEmpty();
        RuleFor(x => x.HireDate).NotEmpty();
        RuleFor(x => x.ContractType).IsInEnum();
        RuleFor(x => x.WeeklyMinHours).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WeeklyMaxHours).GreaterThanOrEqualTo(x => x.WeeklyMinHours);
        RuleFor(x => x.BaseSalary).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FreeDaysPerMonth).InclusiveBetween(0, 31);
    }
}

public class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.PositionId).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Phone).NotEmpty();
        RuleFor(x => x.IdentificationNumber).NotEmpty();
        RuleFor(x => x.ContractType).IsInEnum();
        RuleFor(x => x.WeeklyMinHours).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WeeklyMaxHours).GreaterThanOrEqualTo(x => x.WeeklyMinHours);
        RuleFor(x => x.BaseSalary).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FreeDaysPerMonth).InclusiveBetween(0, 31);
    }
}

public class DeleteEmployeeCommandValidator : AbstractValidator<DeleteEmployeeCommand>
{
    public DeleteEmployeeCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
    }
}

public class GetEmployeeQueryValidator : AbstractValidator<GetEmployeeQuery>
{
    public GetEmployeeQueryValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
    }
}

public class GetEmployeesQueryValidator : AbstractValidator<GetEmployeesQuery>
{
    public GetEmployeesQueryValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
