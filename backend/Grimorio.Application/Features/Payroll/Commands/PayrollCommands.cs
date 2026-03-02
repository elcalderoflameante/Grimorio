using MediatR;
using Grimorio.Application.DTOs;

namespace Grimorio.Application.Features.Payroll.Commands;

public class UpsertPayrollConfigurationCommand : IRequest<PayrollConfigurationDto>
{
    public Guid BranchId { get; set; }
    public decimal IessEmployeeRate { get; set; }
    public decimal IessEmployerRate { get; set; }
    public decimal IncomeTaxRate { get; set; }
    public decimal OvertimeRate50 { get; set; }
    public decimal OvertimeRate100 { get; set; }
    public decimal DecimoThirdRate { get; set; }
    public decimal DecimoFourthRate { get; set; }
    public decimal ReserveFundRate { get; set; }
    public int MonthlyHours { get; set; }
}

public class CreatePayrollAdvanceCommand : IRequest<PayrollAdvanceDto>
{
    public Guid BranchId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class DeletePayrollAdvanceCommand : IRequest<bool>
{
    public Guid BranchId { get; set; }
    public Guid PayrollAdvanceId { get; set; }
}

public class CreateEmployeeConsumptionCommand : IRequest<EmployeeConsumptionDto>
{
    public Guid BranchId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}

public class DeleteEmployeeConsumptionCommand : IRequest<bool>
{
    public Guid BranchId { get; set; }
    public Guid EmployeeConsumptionId { get; set; }
}

public class CreatePayrollAdjustmentCommand : IRequest<PayrollAdjustmentDto>
{
    public Guid BranchId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public Grimorio.Domain.Enums.PayrollAdjustmentType Type { get; set; }
    public Grimorio.Domain.Enums.PayrollAdjustmentCategory Category { get; set; }
    public decimal? Hours { get; set; }
    public decimal? Amount { get; set; }
    public string? Notes { get; set; }
}

public class DeletePayrollAdjustmentCommand : IRequest<bool>
{
    public Guid BranchId { get; set; }
    public Guid PayrollAdjustmentId { get; set; }
}

public class GenerateMonthlyPayrollRolesCommand : IRequest<GeneratePayrollRolesResultDto>
{
    public Guid BranchId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public Guid? EmployeeId { get; set; } // Optional: if provided, generate only for this employee
}

public class UpdatePayrollRoleStatusCommand : IRequest<PayrollRoleDto>
{
    public Guid BranchId { get; set; }
    public Guid PayrollRoleId { get; set; }
    public Grimorio.Domain.Enums.PayrollRoleStatus Status { get; set; }
}
