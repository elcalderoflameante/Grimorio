using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.Finance.Commands;

public class CreateCostCenterCommand : IRequest<CostCenterDto>
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateCostCenterCommand : IRequest<CostCenterDto>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public class DeleteCostCenterCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
}

public class CreateExpenseCategoryCommand : IRequest<ExpenseCategoryDto>
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = "Variable";
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateExpenseCategoryCommand : IRequest<ExpenseCategoryDto>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = "Variable";
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public class DeleteExpenseCategoryCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
}

public class CreateExpenseCommand : IRequest<ExpenseDto>
{
    public Guid BranchId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
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

public class CancelExpenseCommand : IRequest<ExpenseDto>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Reason { get; set; }
}
