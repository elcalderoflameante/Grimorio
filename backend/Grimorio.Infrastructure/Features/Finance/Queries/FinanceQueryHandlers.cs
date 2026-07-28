using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Queries;
using Grimorio.Application.Features.Finance.Queries;
using Grimorio.Domain.Entities.Finance;
using Grimorio.Infrastructure.Features.Finance.Commands;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Finance.Queries;

public class GetCostCentersHandler : IRequestHandler<GetCostCentersQuery, List<CostCenterDto>>
{
    private readonly GrimorioDbContext _db;
    public GetCostCentersHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<CostCenterDto>> Handle(GetCostCentersQuery req, CancellationToken ct)
    {
        var query = _db.CostCenters.Where(x => x.BranchId == req.BranchId);
        if (req.ActiveOnly == true) query = query.Where(x => x.IsActive);

        var list = await query
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);

        return list.Select(FinanceMapper.MapCostCenter).ToList();
    }
}

public class GetExpenseCategoriesHandler : IRequestHandler<GetExpenseCategoriesQuery, List<ExpenseCategoryDto>>
{
    private readonly GrimorioDbContext _db;
    public GetExpenseCategoriesHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<ExpenseCategoryDto>> Handle(GetExpenseCategoriesQuery req, CancellationToken ct)
    {
        var query = _db.ExpenseCategories.Where(x => x.BranchId == req.BranchId);
        if (req.ActiveOnly == true) query = query.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(req.Type) &&
            Enum.TryParse<ExpenseCategoryType>(req.Type, true, out var type))
            query = query.Where(x => x.Type == type);

        var list = await query
            .OrderBy(x => x.Type)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);

        return list.Select(FinanceMapper.MapExpenseCategory).ToList();
    }
}

public class GetExpensesHandler : IRequestHandler<GetExpensesQuery, List<ExpenseDto>>
{
    private readonly GrimorioDbContext _db;
    public GetExpensesHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<ExpenseDto>> Handle(GetExpensesQuery req, CancellationToken ct)
    {
        var query = _db.Expenses
            .Include(x => x.CostCenter)
            .Include(x => x.ExpenseCategory)
            .Include(x => x.PaymentMethodConfig)
            .Include(x => x.CashSession).ThenInclude(s => s!.CashRegister)
            .Where(x => x.BranchId == req.BranchId);

        if (!string.IsNullOrWhiteSpace(req.Status) &&
            Enum.TryParse<ExpenseStatus>(req.Status, true, out var status))
            query = query.Where(x => x.Status == status);
        if (req.CostCenterId.HasValue) query = query.Where(x => x.CostCenterId == req.CostCenterId.Value);
        if (req.ExpenseCategoryId.HasValue) query = query.Where(x => x.ExpenseCategoryId == req.ExpenseCategoryId.Value);
        if (req.CashSessionId.HasValue) query = query.Where(x => x.CashSessionId == req.CashSessionId.Value);
        if (req.FromUtc.HasValue) query = query.Where(x => x.ExpenseDate >= req.FromUtc.Value);
        if (req.ToUtc.HasValue) query = query.Where(x => x.ExpenseDate <= req.ToUtc.Value);

        var list = await query
            .OrderByDescending(x => x.ExpenseDate)
            .ThenByDescending(x => x.RegisteredAt)
            .Take(req.PageSize)
            .ToListAsync(ct);

        return list.Select(FinanceMapper.MapExpense).ToList();
    }
}

public class GetExpenseReportHandler : IRequestHandler<GetExpenseReportQuery, ExpenseReportDto>
{
    private readonly GrimorioDbContext _db;
    public GetExpenseReportHandler(GrimorioDbContext db) => _db = db;

    public async Task<ExpenseReportDto> Handle(GetExpenseReportQuery req, CancellationToken ct)
    {
        var query = _db.Expenses
            .Include(x => x.CostCenter)
            .Include(x => x.ExpenseCategory)
            .Where(x => x.BranchId == req.BranchId && x.Status == ExpenseStatus.Registered);

        if (req.CostCenterId.HasValue) query = query.Where(x => x.CostCenterId == req.CostCenterId.Value);
        if (req.ExpenseCategoryId.HasValue) query = query.Where(x => x.ExpenseCategoryId == req.ExpenseCategoryId.Value);
        if (req.FromUtc.HasValue) query = query.Where(x => x.ExpenseDate >= req.FromUtc.Value);
        if (req.ToUtc.HasValue) query = query.Where(x => x.ExpenseDate <= req.ToUtc.Value);

        var expenses = await query.ToListAsync(ct);
        var total = expenses.Sum(x => x.Amount);

        static decimal Percent(decimal value, decimal total) =>
            total <= 0 ? 0 : Math.Round(value / total * 100m, 2);

        var byCostCenter = expenses
            .GroupBy(x => new { x.CostCenterId, Name = x.CostCenter?.Name ?? string.Empty })
            .Select(g =>
            {
                var groupTotal = g.Sum(x => x.Amount);
                return new ExpenseReportGroupDto
                {
                    Id = g.Key.CostCenterId,
                    Name = g.Key.Name,
                    Total = groupTotal,
                    Count = g.Count(),
                    Percentage = Percent(groupTotal, total),
                };
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        var byCategory = expenses
            .GroupBy(x => new
            {
                x.ExpenseCategoryId,
                Name = x.ExpenseCategory?.Name ?? string.Empty,
                Type = x.ExpenseCategory?.Type.ToString() ?? string.Empty,
            })
            .Select(g =>
            {
                var groupTotal = g.Sum(x => x.Amount);
                return new ExpenseReportGroupDto
                {
                    Id = g.Key.ExpenseCategoryId,
                    Name = g.Key.Name,
                    Type = g.Key.Type,
                    Total = groupTotal,
                    Count = g.Count(),
                    Percentage = Percent(groupTotal, total),
                };
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        var byType = expenses
            .GroupBy(x => x.ExpenseCategory?.Type.ToString() ?? string.Empty)
            .Select(g =>
            {
                var groupTotal = g.Sum(x => x.Amount);
                return new ExpenseReportGroupDto
                {
                    Name = g.Key,
                    Type = g.Key,
                    Total = groupTotal,
                    Count = g.Count(),
                    Percentage = Percent(groupTotal, total),
                };
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        return new ExpenseReportDto
        {
            FromUtc = req.FromUtc,
            ToUtc = req.ToUtc,
            TotalExpenses = total,
            TotalCount = expenses.Count,
            FixedTotal = expenses.Where(x => x.ExpenseCategory?.Type == ExpenseCategoryType.Fixed).Sum(x => x.Amount),
            VariableTotal = expenses.Where(x => x.ExpenseCategory?.Type == ExpenseCategoryType.Variable).Sum(x => x.Amount),
            MixedTotal = expenses.Where(x => x.ExpenseCategory?.Type == ExpenseCategoryType.Mixed).Sum(x => x.Amount),
            ByCostCenter = byCostCenter,
            ByCategory = byCategory,
            ByType = byType,
        };
    }
}

public class GetIncomeStatementHandler : IRequestHandler<GetIncomeStatementQuery, IncomeStatementDto>
{
    private readonly IMediator _mediator;

    public GetIncomeStatementHandler(IMediator mediator) => _mediator = mediator;

    public async Task<IncomeStatementDto> Handle(GetIncomeStatementQuery req, CancellationToken ct)
    {
        var sales = await _mediator.Send(new GetSalesProfitabilityQuery
        {
            BranchId = req.BranchId,
            FromUtc = req.FromUtc,
            ToUtc = req.ToUtc,
        }, ct);

        var expenseReport = await _mediator.Send(new GetExpenseReportQuery
        {
            BranchId = req.BranchId,
            FromUtc = req.FromUtc,
            ToUtc = req.ToUtc,
        }, ct);

        static decimal Round2(decimal value) => Math.Round(value, 2);
        static decimal Percent(decimal value, decimal netSales) =>
            netSales <= 0 ? 0 : Round2(value / netSales * 100m);

        var grossProfit = Round2(sales.NetSales - sales.FoodCost);
        var operatingProfit = Round2(grossProfit - expenseReport.TotalExpenses);

        var lines = new List<IncomeStatementLineDto>
        {
            new()
            {
                Key = "net-sales",
                Label = "Ventas netas",
                Amount = sales.NetSales,
                PercentageOfNetSales = Percent(sales.NetSales, sales.NetSales),
                IsSubtotal = true,
            },
            new()
            {
                Key = "food-cost",
                Label = "Costo de alimentos / recetas",
                Amount = -sales.FoodCost,
                PercentageOfNetSales = -Percent(sales.FoodCost, sales.NetSales),
            },
            new()
            {
                Key = "gross-profit",
                Label = "Utilidad bruta",
                Amount = grossProfit,
                PercentageOfNetSales = Percent(grossProfit, sales.NetSales),
                IsSubtotal = true,
            },
            new()
            {
                Key = "operating-expenses",
                Label = "Gastos operativos",
                Amount = -expenseReport.TotalExpenses,
                PercentageOfNetSales = -Percent(expenseReport.TotalExpenses, sales.NetSales),
            },
            new()
            {
                Key = "operating-profit",
                Label = "Utilidad operativa",
                Amount = operatingProfit,
                PercentageOfNetSales = Percent(operatingProfit, sales.NetSales),
                IsSubtotal = true,
            },
        };

        return new IncomeStatementDto
        {
            FromUtc = req.FromUtc,
            ToUtc = req.ToUtc,
            GrossSales = sales.GrossSales,
            NetSales = sales.NetSales,
            TaxAmount = sales.TaxAmount,
            FoodCost = sales.FoodCost,
            GrossProfit = grossProfit,
            GrossMarginPercentage = sales.NetSales > 0 ? Round2(grossProfit / sales.NetSales * 100m) : 0m,
            OperatingExpenses = expenseReport.TotalExpenses,
            OperatingProfit = operatingProfit,
            OperatingMarginPercentage = sales.NetSales > 0 ? Round2(operatingProfit / sales.NetSales * 100m) : 0m,
            FoodCostPercentage = sales.FoodCostPercentage,
            TotalOrders = sales.TotalOrders,
            ExpenseCount = expenseReport.TotalCount,
            MissingCostLines = sales.MissingCostLines,
            ConversionWarningLines = sales.ConversionWarningLines,
            Lines = lines,
            ExpensesByCostCenter = expenseReport.ByCostCenter,
            ExpensesByCategory = expenseReport.ByCategory,
        };
    }
}

public class GetCostCenterProfitabilityHandler : IRequestHandler<GetCostCenterProfitabilityQuery, CostCenterProfitabilityReportDto>
{
    private readonly IMediator _mediator;

    public GetCostCenterProfitabilityHandler(IMediator mediator) => _mediator = mediator;

    public async Task<CostCenterProfitabilityReportDto> Handle(GetCostCenterProfitabilityQuery req, CancellationToken ct)
    {
        var sales = await _mediator.Send(new GetSalesProfitabilityQuery
        {
            BranchId = req.BranchId,
            FromUtc = req.FromUtc,
            ToUtc = req.ToUtc,
        }, ct);

        var expenses = await _mediator.Send(new GetExpenseReportQuery
        {
            BranchId = req.BranchId,
            FromUtc = req.FromUtc,
            ToUtc = req.ToUtc,
        }, ct);

        static decimal Round2(decimal value) => Math.Round(value, 2);
        static decimal Percent(decimal value, decimal netSales) =>
            netSales <= 0 ? 0 : Round2(value / netSales * 100m);

        var salesByCenter = sales.Items
            .GroupBy(x => new
            {
                x.CostCenterId,
                CostCenterName = string.IsNullOrWhiteSpace(x.CostCenterName) ? "Sin centro asignado" : x.CostCenterName,
            })
            .Select(g => new
            {
                g.Key.CostCenterId,
                g.Key.CostCenterName,
                GrossSales = g.Sum(x => x.GrossSales),
                NetSales = g.Sum(x => x.NetSales),
                FoodCost = g.Sum(x => x.TotalFoodCost),
                TotalItems = g.Sum(x => x.Quantity),
                MissingCostLines = g.Count(x => x.HasMissingCosts),
                ConversionWarningLines = g.Count(x => x.HasConversionWarnings),
            })
            .ToList();

        var centerIds = salesByCenter.Select(x => x.CostCenterId)
            .Concat(expenses.ByCostCenter.Select(x => x.Id))
            .Distinct()
            .ToList();

        var centers = centerIds
            .Select(id =>
            {
                var salesGroup = salesByCenter.FirstOrDefault(x => x.CostCenterId == id);
                var expenseGroup = expenses.ByCostCenter.FirstOrDefault(x => x.Id == id);
                var netSales = salesGroup?.NetSales ?? 0m;
                var foodCost = salesGroup?.FoodCost ?? 0m;
                var grossProfit = netSales - foodCost;
                var operatingExpenses = expenseGroup?.Total ?? 0m;
                var operatingProfit = grossProfit - operatingExpenses;

                return new CostCenterProfitabilityDto
                {
                    CostCenterId = id,
                    CostCenterName = salesGroup?.CostCenterName
                        ?? (string.IsNullOrWhiteSpace(expenseGroup?.Name) ? "Sin centro asignado" : expenseGroup.Name),
                    GrossSales = Round2(salesGroup?.GrossSales ?? 0m),
                    NetSales = Round2(netSales),
                    FoodCost = Round2(foodCost),
                    GrossProfit = Round2(grossProfit),
                    OperatingExpenses = Round2(operatingExpenses),
                    OperatingProfit = Round2(operatingProfit),
                    FoodCostPercentage = Percent(foodCost, netSales),
                    GrossMarginPercentage = Percent(grossProfit, netSales),
                    OperatingMarginPercentage = Percent(operatingProfit, netSales),
                    TotalItems = Round2(salesGroup?.TotalItems ?? 0m),
                    MissingCostLines = salesGroup?.MissingCostLines ?? 0,
                    ConversionWarningLines = salesGroup?.ConversionWarningLines ?? 0,
                };
            })
            .OrderByDescending(x => x.NetSales)
            .ThenByDescending(x => x.OperatingExpenses)
            .ToList();

        var totalGrossProfit = Round2(sales.NetSales - sales.FoodCost);
        var totalOperatingProfit = Round2(totalGrossProfit - expenses.TotalExpenses);

        return new CostCenterProfitabilityReportDto
        {
            FromUtc = req.FromUtc,
            ToUtc = req.ToUtc,
            GrossSales = sales.GrossSales,
            NetSales = sales.NetSales,
            FoodCost = sales.FoodCost,
            GrossProfit = totalGrossProfit,
            OperatingExpenses = expenses.TotalExpenses,
            OperatingProfit = totalOperatingProfit,
            FoodCostPercentage = sales.FoodCostPercentage,
            GrossMarginPercentage = sales.GrossMarginPercentage,
            OperatingMarginPercentage = Percent(totalOperatingProfit, sales.NetSales),
            TotalOrders = sales.TotalOrders,
            TotalItems = sales.TotalItems,
            MissingCostLines = sales.MissingCostLines,
            ConversionWarningLines = sales.ConversionWarningLines,
            Centers = centers,
        };
    }
}
