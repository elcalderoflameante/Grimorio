using Grimorio.SharedKernel;

namespace Grimorio.Domain.Entities.Finance;

public enum ExpenseCategoryType
{
    Fixed = 1,
    Variable = 2,
    Mixed = 3,
}

public class CostCenter : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ExpenseCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ExpenseCategoryType Type { get; set; } = ExpenseCategoryType.Variable;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum ExpenseStatus
{
    Registered = 1,
    Cancelled = 2,
}

public class Expense : BaseEntity
{
    public DateTime ExpenseDate { get; set; }
    public Guid CostCenterId { get; set; }
    public Guid ExpenseCategoryId { get; set; }
    public Guid? PaymentMethodConfigId { get; set; }
    public Guid? CashSessionId { get; set; }
    public string? SupplierName { get; set; }
    public string? DocumentNumber { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public ExpenseStatus Status { get; set; } = ExpenseStatus.Registered;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public Guid RegisteredBy { get; set; }
    public string RegisteredByName { get; set; } = string.Empty;
    public DateTime? CancelledAt { get; set; }
    public Guid? CancelledBy { get; set; }
    public string? CancelledByName { get; set; }
    public string? CancellationReason { get; set; }

    public virtual CostCenter? CostCenter { get; set; }
    public virtual ExpenseCategory? ExpenseCategory { get; set; }
    public virtual Billing.PaymentMethodConfig? PaymentMethodConfig { get; set; }
    public virtual Billing.CashSession? CashSession { get; set; }
}
