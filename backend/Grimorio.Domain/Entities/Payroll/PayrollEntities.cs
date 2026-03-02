using Grimorio.Domain.Enums;
using Grimorio.SharedKernel;

namespace Grimorio.Domain.Entities.Payroll;

public class PayrollConfiguration : BaseEntity
{
    public Guid BranchId { get; set; }
    public decimal IessEmployeeRate { get; set; } = 9.45m;
    public decimal IessEmployerRate { get; set; } = 11.45m;
    public decimal IncomeTaxRate { get; set; } = 0m;
    public decimal OvertimeRate50 { get; set; } = 50m;
    public decimal OvertimeRate100 { get; set; } = 100m;
    public decimal DecimoThirdRate { get; set; } = 8.33m;
    public decimal DecimoFourthRate { get; set; } = 8.33m;
    public decimal ReserveFundRate { get; set; } = 8.33m;
    public int MonthlyHours { get; set; } = 240;
}

public class PayrollAdvance : BaseEntity
{
    public Guid BranchId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public virtual Organization.Employee? Employee { get; set; }
}

public class EmployeeConsumption : BaseEntity
{
    public Guid BranchId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }

    public virtual Organization.Employee? Employee { get; set; }
}

public class PayrollAdjustment : BaseEntity
{
    public Guid BranchId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public PayrollAdjustmentType Type { get; set; }
    public PayrollAdjustmentCategory Category { get; set; }
    public decimal? Hours { get; set; }
    public decimal? Amount { get; set; }
    public string? Notes { get; set; }

    public virtual Organization.Employee? Employee { get; set; }
}

public class PayrollRoleHeader : BaseEntity
{
    public Guid BranchId { get; set; }
    public Guid EmployeeId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public PayrollRoleStatus Status { get; set; } = PayrollRoleStatus.Generated;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AuthorizedAt { get; set; }
    public DateTime? PaidAt { get; set; }

    public decimal TotalIncome { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }

    public virtual Organization.Employee? Employee { get; set; }
    public virtual ICollection<PayrollRoleDetail> Details { get; set; } = new List<PayrollRoleDetail>();
}

public class PayrollRoleDetail : BaseEntity
{
    public Guid PayrollRoleHeaderId { get; set; }
    public PayrollRoleDetailType Type { get; set; }
    public string Concept { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int SortOrder { get; set; }
    public string? Notes { get; set; }

    public virtual PayrollRoleHeader? PayrollRoleHeader { get; set; }
}
