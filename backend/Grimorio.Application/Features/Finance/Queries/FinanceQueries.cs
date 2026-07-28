using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.Finance.Queries;

public class GetCostCentersQuery : IRequest<List<CostCenterDto>>
{
    public Guid BranchId { get; set; }
    public bool? ActiveOnly { get; set; }
}

public class GetExpenseCategoriesQuery : IRequest<List<ExpenseCategoryDto>>
{
    public Guid BranchId { get; set; }
    public bool? ActiveOnly { get; set; }
    public string? Type { get; set; }
}

public class GetExpensesQuery : IRequest<List<ExpenseDto>>
{
    public Guid BranchId { get; set; }
    public string? Status { get; set; }
    public Guid? CostCenterId { get; set; }
    public Guid? ExpenseCategoryId { get; set; }
    public Guid? CashSessionId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int PageSize { get; set; } = 100;
}

public class GetExpenseReportQuery : IRequest<ExpenseReportDto>
{
    public Guid BranchId { get; set; }
    public Guid? CostCenterId { get; set; }
    public Guid? ExpenseCategoryId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}

public class GetIncomeStatementQuery : IRequest<IncomeStatementDto>
{
    public Guid BranchId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}

public class GetCostCenterProfitabilityQuery : IRequest<CostCenterProfitabilityReportDto>
{
    public Guid BranchId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
