using Grimorio.Domain.Enums;

namespace Grimorio.Application.DTOs;

public class PayrollConfigurationDto
{
    public Guid Id { get; set; }
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

public class CreatePayrollConfigurationDto
{
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

public class UpdatePayrollConfigurationDto : CreatePayrollConfigurationDto
{
    public Guid Id { get; set; }
}

public class PayrollAdvanceDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class CreatePayrollAdvanceDto
{
    public Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class EmployeeConsumptionDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}

public class CreateEmployeeConsumptionDto
{
    public Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}

public class PayrollAdjustmentDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public PayrollAdjustmentType Type { get; set; }
    public PayrollAdjustmentCategory Category { get; set; }
    public decimal? Hours { get; set; }
    public decimal? Amount { get; set; }
    public string? Notes { get; set; }
}

public class CreatePayrollAdjustmentDto
{
    public Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public PayrollAdjustmentType Type { get; set; }
    public PayrollAdjustmentCategory Category { get; set; }
    public decimal? Hours { get; set; }
    public decimal? Amount { get; set; }
    public string? Notes { get; set; }
}

public class EmployeePayrollSummaryDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public decimal BaseSalary { get; set; }
    public decimal IessEmployee { get; set; }
    public decimal IessEmployer { get; set; }
    public decimal IncomeTax { get; set; }
    public decimal DecimoThird { get; set; }
    public decimal DecimoFourth { get; set; }
    public decimal ReserveFund { get; set; }
    public decimal Overtime50 { get; set; }
    public decimal Overtime100 { get; set; }
    public decimal OtherIncome { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal Advances { get; set; }
    public decimal Consumptions { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
}

public class PayrollRoleDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public PayrollRoleStatus Status { get; set; }
    public DateTime GeneratedAt { get; set; }
    public DateTime? AuthorizedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
}

public class PayrollRoleDetailDto
{
    public Guid Id { get; set; }
    public Guid PayrollRoleHeaderId { get; set; }
    public PayrollRoleDetailType Type { get; set; }
    public string Concept { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int SortOrder { get; set; }
    public string? Notes { get; set; }
}

public class PayrollRoleFullDto
{
    public PayrollRoleDto Header { get; set; } = new();
    public List<PayrollRoleDetailDto> Details { get; set; } = new();
}

public class GeneratePayrollRolesResultDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int GeneratedCount { get; set; }
    public int UpdatedCount { get; set; }
}

public class UpdatePayrollRoleStatusDto
{
    public PayrollRoleStatus Status { get; set; }
}
