namespace Grimorio.Application.DTOs;

public class CostCenterDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public class UpsertCostCenterDto
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ExpenseCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public class UpsertExpenseCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = "Variable";
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ExpenseDto
{
    public Guid Id { get; set; }
    public DateTime ExpenseDate { get; set; }
    public Guid CostCenterId { get; set; }
    public string CostCenterName { get; set; } = string.Empty;
    public Guid ExpenseCategoryId { get; set; }
    public string ExpenseCategoryName { get; set; } = string.Empty;
    public string ExpenseCategoryType { get; set; } = string.Empty;
    public Guid? PaymentMethodConfigId { get; set; }
    public string? PaymentMethodName { get; set; }
    public string? PaymentMethodColor { get; set; }
    public bool IsCashPayment { get; set; }
    public Guid? CashSessionId { get; set; }
    public string? CashRegisterName { get; set; }
    public string? CashRegisterCode { get; set; }
    public string? SupplierName { get; set; }
    public string? DocumentNumber { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public string RegisteredByName { get; set; } = string.Empty;
    public DateTime? CancelledAt { get; set; }
    public string? CancelledByName { get; set; }
    public string? CancellationReason { get; set; }
}

public class CreateExpenseDto
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
}

public class CancelExpenseDto
{
    public string? Reason { get; set; }
}

public class ExpenseReportDto
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public decimal TotalExpenses { get; set; }
    public int TotalCount { get; set; }
    public decimal FixedTotal { get; set; }
    public decimal VariableTotal { get; set; }
    public decimal MixedTotal { get; set; }
    public List<ExpenseReportGroupDto> ByCostCenter { get; set; } = [];
    public List<ExpenseReportGroupDto> ByCategory { get; set; } = [];
    public List<ExpenseReportGroupDto> ByType { get; set; } = [];
}

public class ExpenseReportGroupDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public decimal Total { get; set; }
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class IncomeStatementDto
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public decimal GrossSales { get; set; }
    public decimal NetSales { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal FoodCost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal GrossMarginPercentage { get; set; }
    public decimal OperatingExpenses { get; set; }
    public decimal OperatingProfit { get; set; }
    public decimal OperatingMarginPercentage { get; set; }
    public decimal FoodCostPercentage { get; set; }
    public int TotalOrders { get; set; }
    public int ExpenseCount { get; set; }
    public int MissingCostLines { get; set; }
    public int ConversionWarningLines { get; set; }
    public List<IncomeStatementLineDto> Lines { get; set; } = [];
    public List<ExpenseReportGroupDto> ExpensesByCostCenter { get; set; } = [];
    public List<ExpenseReportGroupDto> ExpensesByCategory { get; set; } = [];
}

public class IncomeStatementLineDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal PercentageOfNetSales { get; set; }
    public bool IsSubtotal { get; set; }
}

public class CostCenterProfitabilityReportDto
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public decimal GrossSales { get; set; }
    public decimal NetSales { get; set; }
    public decimal FoodCost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal OperatingExpenses { get; set; }
    public decimal OperatingProfit { get; set; }
    public decimal FoodCostPercentage { get; set; }
    public decimal GrossMarginPercentage { get; set; }
    public decimal OperatingMarginPercentage { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalItems { get; set; }
    public int MissingCostLines { get; set; }
    public int ConversionWarningLines { get; set; }
    public List<CostCenterProfitabilityDto> Centers { get; set; } = [];
}

public class CostCenterProfitabilityDto
{
    public Guid? CostCenterId { get; set; }
    public string CostCenterName { get; set; } = string.Empty;
    public decimal GrossSales { get; set; }
    public decimal NetSales { get; set; }
    public decimal FoodCost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal OperatingExpenses { get; set; }
    public decimal OperatingProfit { get; set; }
    public decimal FoodCostPercentage { get; set; }
    public decimal GrossMarginPercentage { get; set; }
    public decimal OperatingMarginPercentage { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalItems { get; set; }
    public int MissingCostLines { get; set; }
    public int ConversionWarningLines { get; set; }
}
