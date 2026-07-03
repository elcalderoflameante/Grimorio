using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Queries;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.Purchases;
using Grimorio.Infrastructure.Features.Billing.Commands;
using Grimorio.Infrastructure.Persistence;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Billing.Queries;

public class GetTaxRatesHandler : IRequestHandler<GetTaxRatesQuery, List<TaxRateDto>>
{
    private readonly GrimorioDbContext _db;
    public GetTaxRatesHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<TaxRateDto>> Handle(GetTaxRatesQuery req, CancellationToken ct)
    {
        var query = _db.TaxRates.Where(t => t.BranchId == req.BranchId && !t.IsDeleted);
        if (req.ActiveOnly) query = query.Where(t => t.IsActive);
        var list = await query.OrderBy(t => t.Percentage).ToListAsync(ct);
        return list.Select(t => new TaxRateDto
        {
            Id = t.Id, Name = t.Name, Percentage = t.Percentage,
            SriCode = t.SriCode, IsDefault = t.IsDefault, IsActive = t.IsActive,
        }).ToList();
    }
}

public class GetSmtpConfigHandler : IRequestHandler<GetSmtpConfigQuery, SmtpConfigDto?>
{
    private readonly GrimorioDbContext _db;
    public GetSmtpConfigHandler(GrimorioDbContext db) => _db = db;

    public async Task<SmtpConfigDto?> Handle(GetSmtpConfigQuery req, CancellationToken ct)
    {
        var cfg = await _db.SmtpConfigs.FirstOrDefaultAsync(c => c.BranchId == req.BranchId && !c.IsDeleted, ct);
        if (cfg == null) return null;
        return new SmtpConfigDto
        {
            Host = cfg.Host, Port = cfg.Port, Username = cfg.Username,
            FromEmail = cfg.FromEmail, FromName = cfg.FromName,
            EnableSsl = cfg.EnableSsl, IsActive = cfg.IsActive,
            HasPassword = !string.IsNullOrEmpty(cfg.PasswordEncrypted),
        };
    }
}

public class GetBranchTaxConfigHandler : IRequestHandler<GetBranchTaxConfigQuery, BranchTaxConfigDto?>
{
    private readonly GrimorioDbContext _db;
    public GetBranchTaxConfigHandler(GrimorioDbContext db) => _db = db;

    public async Task<BranchTaxConfigDto?> Handle(GetBranchTaxConfigQuery req, CancellationToken ct)
    {
        var cfg = await _db.BranchTaxConfigs.FirstOrDefaultAsync(c => c.BranchId == req.BranchId && !c.IsDeleted, ct);
        if (cfg == null) return null;
        return BillingMapper.MapBranchTaxConfig(cfg);
    }
}

public class GetPaymentMethodsHandler : IRequestHandler<GetPaymentMethodsQuery, List<PaymentMethodConfigDto>>
{
    private readonly GrimorioDbContext _db;
    public GetPaymentMethodsHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<PaymentMethodConfigDto>> Handle(GetPaymentMethodsQuery req, CancellationToken ct)
    {
        var query = _db.PaymentMethodConfigs.Where(m => !m.IsDeleted);
        if (req.ActiveOnly) query = query.Where(m => m.IsActive);
        var list = await query.OrderBy(m => m.SortOrder).ThenBy(m => m.Name).ToListAsync(ct);
        return list.Select(BillingMapper.MapPaymentMethod).ToList();
    }
}

public class GetCardBanksHandler : IRequestHandler<GetCardBanksQuery, List<CardBankDto>>
{
    private readonly GrimorioDbContext _db;
    public GetCardBanksHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<CardBankDto>> Handle(GetCardBanksQuery req, CancellationToken ct)
    {
        var query = _db.CardBanks.Where(b => b.BranchId == req.BranchId && !b.IsDeleted);
        if (req.ActiveOnly) query = query.Where(b => b.IsActive);
        var list = await query.OrderBy(b => b.SortOrder).ThenBy(b => b.Name).ToListAsync(ct);
        return list.Select(BillingMapper.MapCardBank).ToList();
    }
}

public class GetCashRegistersHandler : IRequestHandler<GetCashRegistersQuery, List<CashRegisterDto>>
{
    private readonly GrimorioDbContext _db;
    public GetCashRegistersHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<CashRegisterDto>> Handle(GetCashRegistersQuery req, CancellationToken ct)
    {
        var query = _db.CashRegisters.Where(r => r.BranchId == req.BranchId && !r.IsDeleted);
        if (req.ActiveOnly) query = query.Where(r => r.IsActive);

        var registers = await query.OrderBy(r => r.Code).ThenBy(r => r.Name).ToListAsync(ct);
        var registerIds = registers.Select(r => r.Id).ToList();
        var openRegisterIds = await _db.CashSessions
            .Where(s => registerIds.Contains(s.CashRegisterId) && s.Status == CashSessionStatus.Open && !s.IsDeleted)
            .Select(s => s.CashRegisterId)
            .ToListAsync(ct);
        var openSet = openRegisterIds.ToHashSet();

        return registers.Select(r => BillingMapper.MapCashRegister(r, openSet.Contains(r.Id))).ToList();
    }
}

public class GetCustomersHandler : IRequestHandler<GetCustomersQuery, List<CustomerDto>>
{
    private readonly GrimorioDbContext _db;
    public GetCustomersHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<CustomerDto>> Handle(GetCustomersQuery req, CancellationToken ct)
    {
        var query = _db.Customers.Where(c => c.BranchId == req.BranchId && !c.IsDeleted);
        if (req.ActiveOnly == true) query = query.Where(c => c.IsActive);
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(s) ||
                (c.TaxId != null && c.TaxId.Contains(s)));
        }
        var list = await query.OrderBy(c => c.Name).ToListAsync(ct);
        return list.Select(BillingMapper.MapCustomer).ToList();
    }
}

public class GetActiveCashSessionHandler : IRequestHandler<GetActiveCashSessionQuery, CashSessionDto?>
{
    private readonly GrimorioDbContext _db;
    public GetActiveCashSessionHandler(GrimorioDbContext db) => _db = db;

    public async Task<CashSessionDto?> Handle(GetActiveCashSessionQuery req, CancellationToken ct)
    {
        var session = await _db.CashSessions
            .Include(s => s.CashRegister)
            .FirstOrDefaultAsync(s => s.BranchId == req.BranchId
                && s.OpenedBy == req.UserId
                && s.Status == CashSessionStatus.Open
                && !s.IsDeleted, ct);
        if (session == null) return null;

        var payments = await _db.OrderPayments
            .Include(p => p.Lines).ThenInclude(l => l.Config)
            .Include(p => p.Items).ThenInclude(i => i.OrderItem).ThenInclude(i => i!.MenuItem)
            .Where(p => p.BranchId == req.BranchId && !p.IsDeleted
                && p.CashSessionId == session.Id)
            .ToListAsync(ct);

        return BillingMapper.MapSession(session, payments);
    }
}

public class GetCashSessionsHandler : IRequestHandler<GetCashSessionsQuery, List<CashSessionDto>>
{
    private readonly GrimorioDbContext _db;
    public GetCashSessionsHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<CashSessionDto>> Handle(GetCashSessionsQuery req, CancellationToken ct)
    {
        var query = _db.CashSessions
            .Include(s => s.CashRegister)
            .Include(s => s.Payments.Where(p => !p.IsDeleted))
                .ThenInclude(p => p.Lines)
                .ThenInclude(l => l.Config)
            .Where(s => s.BranchId == req.BranchId && !s.IsDeleted);

        if (req.FromUtc.HasValue) query = query.Where(s => s.OpenedAt >= req.FromUtc.Value);
        if (req.ToUtc.HasValue) query = query.Where(s => s.OpenedAt <= req.ToUtc.Value);

        var sessions = await query
            .OrderByDescending(s => s.OpenedAt)
            .Take(req.PageSize)
            .ToListAsync(ct);

        return sessions.Select(s => BillingMapper.MapSession(s)).ToList();
    }
}

public class GetCashSessionDetailHandler : IRequestHandler<GetCashSessionDetailQuery, CashSessionDto?>
{
    private readonly GrimorioDbContext _db;
    public GetCashSessionDetailHandler(GrimorioDbContext db) => _db = db;

    public async Task<CashSessionDto?> Handle(GetCashSessionDetailQuery req, CancellationToken ct)
    {
        var session = await _db.CashSessions
            .Include(s => s.CashRegister)
            .FirstOrDefaultAsync(s => s.Id == req.Id && s.BranchId == req.BranchId && !s.IsDeleted, ct);
        if (session == null) return null;

        var payments = await _db.OrderPayments
            .Include(p => p.Lines).ThenInclude(l => l.Config)
            .Include(p => p.Items).ThenInclude(i => i.OrderItem).ThenInclude(i => i!.MenuItem)
            .Where(p => p.BranchId == req.BranchId && !p.IsDeleted
                && p.CashSessionId == session.Id)
            .ToListAsync(ct);

        return BillingMapper.MapSession(session, payments);
    }
}

public class GetOrderPaymentsHandler : IRequestHandler<GetOrderPaymentsQuery, List<OrderPaymentDto>>
{
    private readonly GrimorioDbContext _db;
    public GetOrderPaymentsHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<OrderPaymentDto>> Handle(GetOrderPaymentsQuery req, CancellationToken ct)
    {
        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == req.OrderId && o.BranchId == req.BranchId && !o.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Orden no encontrada.");

        var payments = await _db.OrderPayments
            .Include(p => p.Lines).ThenInclude(l => l.Config)
            .Include(p => p.Items).ThenInclude(i => i.OrderItem).ThenInclude(i => i!.MenuItem)
            .Include(p => p.Customer)
            .Include(p => p.CashSession).ThenInclude(s => s!.CashRegister)
            .Where(p => p.OrderId == req.OrderId && !p.IsDeleted)
            .OrderBy(p => p.PaidAt)
            .ToListAsync(ct);

        return payments.Select(p => BillingMapper.MapPayment(p, order.Number, p.Customer)).ToList();
    }
}

public class GetSalesHandler : IRequestHandler<GetSalesQuery, List<OrderPaymentDto>>
{
    private readonly GrimorioDbContext _db;
    public GetSalesHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<OrderPaymentDto>> Handle(GetSalesQuery req, CancellationToken ct)
    {
        var query = _db.OrderPayments
            .Include(p => p.Lines).ThenInclude(l => l.Config)
            .Include(p => p.Items).ThenInclude(i => i.OrderItem).ThenInclude(i => i!.MenuItem)
            .Include(p => p.Customer)
            .Include(p => p.CashSession).ThenInclude(s => s!.CashRegister)
            .Include(p => p.Order).ThenInclude(o => o!.Table)
            .Where(p => p.BranchId == req.BranchId && !p.IsDeleted);

        if (req.FromUtc.HasValue) query = query.Where(p => p.PaidAt >= req.FromUtc.Value);
        if (req.ToUtc.HasValue) query = query.Where(p => p.PaidAt <= req.ToUtc.Value);

        var payments = await query
            .OrderByDescending(p => p.PaidAt)
            .Take(req.PageSize)
            .ToListAsync(ct);

        var paymentIds = payments.Select(p => p.Id).ToList();
        var elDocs = await _db.ElectronicDocuments
            .Where(d => paymentIds.Contains(d.OrderPaymentId) && !d.IsDeleted)
            .ToListAsync(ct);
        var elDocByPayment = elDocs
            .GroupBy(d => d.OrderPaymentId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.CreatedAt).First());

        return payments.Select(p => BillingMapper.MapPayment(
            p,
            p.Order?.Number ?? 0,
            p.Customer,
            p.Order?.Table?.Code,
            p.Order?.Type.ToString(),
            elDocByPayment.GetValueOrDefault(p.Id)
        )).ToList();
    }
}

// ── SRI Certificado ───────────────────────────────────────────────────────────

public class GetSalesProfitabilityHandler : IRequestHandler<GetSalesProfitabilityQuery, SalesProfitabilityReportDto>
{
    private readonly GrimorioDbContext _db;
    public GetSalesProfitabilityHandler(GrimorioDbContext db) => _db = db;

    public async Task<SalesProfitabilityReportDto> Handle(GetSalesProfitabilityQuery req, CancellationToken ct)
    {
        var query = _db.OrderPayments
            .AsNoTracking()
            .Include(p => p.CashSession).ThenInclude(s => s!.CashRegister)
            .Include(p => p.Order)!.ThenInclude(o => o!.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.TaxRate)
            .Include(p => p.Order)!.ThenInclude(o => o!.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.MenuItem)!.ThenInclude(m => m!.Category)
            .Include(p => p.Order)!.ThenInclude(o => o!.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.MenuItem)!.ThenInclude(m => m!.TaxRate)
            .Include(p => p.Order)!.ThenInclude(o => o!.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.MenuItem)!.ThenInclude(m => m!.Recipe.Where(r => !r.IsDeleted))
                    .ThenInclude(r => r.Article)!.ThenInclude(a => a!.BaseUnit)
            .Include(p => p.Order)!.ThenInclude(o => o!.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.MenuItem)!.ThenInclude(m => m!.Recipe.Where(r => !r.IsDeleted))
                    .ThenInclude(r => r.Unit)
            .Include(p => p.Order)!.ThenInclude(o => o!.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.IngredientChoices.Where(c => !c.IsDeleted))
            .Where(p => p.BranchId == req.BranchId && !p.IsDeleted && p.Order != null);

        if (req.FromUtc.HasValue) query = query.Where(p => p.PaidAt >= req.FromUtc.Value);
        if (req.ToUtc.HasValue) query = query.Where(p => p.PaidAt <= req.ToUtc.Value);
        if (req.CashRegisterId.HasValue)
            query = query.Where(p => p.CashSession != null && p.CashSession.CashRegisterId == req.CashRegisterId.Value);

        var payments = await query.AsSplitQuery().OrderBy(p => p.PaidAt).ToListAsync(ct);

        var articleIds = payments
            .SelectMany(p => p.Order?.Items ?? [])
            .SelectMany(i => i.MenuItem?.Recipe ?? [])
            .Where(r => !r.IsDeleted)
            .Select(r => r.ArticleId)
            .Concat(payments
                .SelectMany(p => p.Order?.Items ?? [])
                .SelectMany(i => i.IngredientChoices.Where(c => !c.IsDeleted).Select(c => c.ChosenArticleId)))
            .Distinct()
            .ToList();

        var conversions = await _db.UnitConversions
            .AsNoTracking()
            .Where(x => x.BranchId == req.BranchId && !x.IsDeleted)
            .Select(x => new SalesUnitConversionInfo(x.OriginUnitId, x.DestinationUnitId, x.Factor))
            .ToListAsync(ct);

        var baseUnitByArticle = await _db.InventoryArticles
            .AsNoTracking()
            .Where(a => a.BranchId == req.BranchId && articleIds.Contains(a.Id))
            .Select(a => new { a.Id, a.BaseUnitId })
            .ToDictionaryAsync(a => a.Id, a => a.BaseUnitId, ct);

        var unitCosts = await LoadUnitCosts(req.BranchId, articleIds, conversions, ct);
        var rows = new List<SalesProfitabilityLine>();

        foreach (var payment in payments)
        {
            var order = payment.Order;
            if (order is null) continue;

            var ratio = order.Total > 0 ? payment.OrderAmount / order.Total : 1m;
            if (ratio <= 0) continue;
            if (ratio > 1m) ratio = 1m;

            foreach (var item in order.Items.Where(i => !i.IsDeleted && i.Status != Domain.Entities.POS.OrderItemStatus.Cancelled))
            {
                var gross = (item.UnitPrice * item.Quantity - item.DiscountAmount) * ratio;
                var taxPct = item.TaxRate?.Percentage ?? item.MenuItem?.TaxRate?.Percentage ?? 0m;
                var net = taxPct > 0 ? gross / (1m + taxPct / 100m) : gross;
                var recipeCost = CalculateRecipeUnitCost(item, baseUnitByArticle, unitCosts, conversions, out var missingCosts, out var conversionWarnings);
                var totalCost = recipeCost * item.Quantity * ratio;

                rows.Add(new SalesProfitabilityLine(
                    item.MenuItemId,
                    item.MenuItem?.Name ?? item.MenuItemId.ToString(),
                    item.MenuItem?.InternalCode,
                    item.MenuItem?.Category?.Name ?? string.Empty,
                    item.Quantity * ratio,
                    gross,
                    net,
                    gross - net,
                    recipeCost,
                    totalCost,
                    missingCosts,
                    conversionWarnings,
                    payment.OrderId,
                    payment.CashSession?.CashRegisterId,
                    payment.CashSession?.CashRegister?.Name ?? "Sin caja"));
            }
        }

        return BuildReport(req, payments, rows);
    }

    private static SalesProfitabilityReportDto BuildReport(
        GetSalesProfitabilityQuery req,
        List<OrderPayment> payments,
        List<SalesProfitabilityLine> rows)
    {
        var report = new SalesProfitabilityReportDto
        {
            FromUtc = req.FromUtc,
            ToUtc = req.ToUtc,
            CashRegisterId = req.CashRegisterId,
            CashRegisterName = req.CashRegisterId.HasValue
                ? payments.Select(p => p.CashSession?.CashRegister?.Name).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))
                : null,
            GrossSales = Round2(rows.Sum(r => r.GrossSales)),
            NetSales = Round2(rows.Sum(r => r.NetSales)),
            TaxAmount = Round2(rows.Sum(r => r.TaxAmount)),
            FoodCost = Round2(rows.Sum(r => r.TotalFoodCost)),
            TotalOrders = rows.Select(r => r.OrderId).Distinct().Count(),
            TotalItems = Round2(rows.Sum(r => r.Quantity)),
            MissingCostLines = rows.Count(r => r.HasMissingCosts),
            ConversionWarningLines = rows.Count(r => r.HasConversionWarnings),
        };

        report.GrossProfit = Round2(report.NetSales - report.FoodCost);
        report.FoodCostPercentage = report.NetSales > 0 ? Round2(report.FoodCost / report.NetSales * 100m) : 0m;
        report.GrossMarginPercentage = report.NetSales > 0 ? Round2(report.GrossProfit / report.NetSales * 100m) : 0m;

        report.Items = rows
            .GroupBy(r => new { r.MenuItemId, r.MenuItemName, r.InternalCode, r.CategoryName })
            .Select(g =>
            {
                var net = g.Sum(r => r.NetSales);
                var cost = g.Sum(r => r.TotalFoodCost);
                var profit = net - cost;
                return new SalesProfitabilityItemDto
                {
                    MenuItemId = g.Key.MenuItemId,
                    MenuItemName = g.Key.MenuItemName,
                    InternalCode = g.Key.InternalCode,
                    CategoryName = g.Key.CategoryName,
                    Quantity = Round2(g.Sum(r => r.Quantity)),
                    GrossSales = Round2(g.Sum(r => r.GrossSales)),
                    NetSales = Round2(net),
                    TaxAmount = Round2(g.Sum(r => r.TaxAmount)),
                    UnitRecipeCost = Round2(g.Average(r => r.UnitRecipeCost)),
                    TotalFoodCost = Round2(cost),
                    GrossProfit = Round2(profit),
                    FoodCostPercentage = net > 0 ? Round2(cost / net * 100m) : 0m,
                    GrossMarginPercentage = net > 0 ? Round2(profit / net * 100m) : 0m,
                    HasMissingCosts = g.Any(r => r.HasMissingCosts),
                    HasConversionWarnings = g.Any(r => r.HasConversionWarnings),
                };
            })
            .OrderByDescending(i => i.NetSales)
            .ToList();

        report.CashRegisters = rows
            .GroupBy(r => new { r.CashRegisterId, r.CashRegisterName })
            .Select(g =>
            {
                var net = g.Sum(r => r.NetSales);
                var cost = g.Sum(r => r.TotalFoodCost);
                return new SalesProfitabilityCashRegisterDto
                {
                    CashRegisterId = g.Key.CashRegisterId,
                    CashRegisterName = g.Key.CashRegisterName,
                    GrossSales = Round2(g.Sum(r => r.GrossSales)),
                    NetSales = Round2(net),
                    FoodCost = Round2(cost),
                    GrossProfit = Round2(net - cost),
                    FoodCostPercentage = net > 0 ? Round2(cost / net * 100m) : 0m,
                    TotalOrders = g.Select(r => r.OrderId).Distinct().Count(),
                };
            })
            .OrderByDescending(c => c.NetSales)
            .ToList();

        return report;
    }

    private async Task<Dictionary<Guid, SalesArticleUnitCost>> LoadUnitCosts(
        Guid branchId,
        List<Guid> articleIds,
        IReadOnlyCollection<SalesUnitConversionInfo> conversions,
        CancellationToken ct)
    {
        if (articleIds.Count == 0) return [];

        var inputs = await _db.PurchaseItems
            .AsNoTracking()
            .Include(x => x.Purchase)
            .Include(x => x.Article)
            .Where(x => x.BranchId == branchId
                && !x.IsDeleted
                && articleIds.Contains(x.ArticleId)
                && x.Purchase != null
                && !x.Purchase.IsDeleted
                && x.Purchase.Status == PurchaseStatus.Registrada)
            .Select(x => new SalesPurchaseCostInput(
                x.ArticleId,
                x.UnitId,
                x.Article != null ? x.Article.BaseUnitId : Guid.Empty,
                x.Quantity,
                x.UnitPrice,
                x.DiscountAmount))
            .ToListAsync(ct);

        return inputs
            .Select(x =>
            {
                var baseQty = ConvertQuantity(x.Quantity, x.UnitId, x.ArticleBaseUnitId, conversions);
                var netCost = x.UnitPrice * x.Quantity - x.DiscountAmount;
                return new SalesArticleCostSample(x.ArticleId, baseQty, netCost);
            })
            .Where(x => x.BaseQuantity > 0 && x.NetCost >= 0)
            .GroupBy(x => x.ArticleId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var qty = g.Sum(x => x.BaseQuantity);
                    return new SalesArticleUnitCost(qty > 0 ? g.Sum(x => x.NetCost) / qty : 0m);
                });
    }

    private static decimal CalculateRecipeUnitCost(
        Domain.Entities.POS.OrderItem item,
        IReadOnlyDictionary<Guid, Guid> baseUnitByArticle,
        IReadOnlyDictionary<Guid, SalesArticleUnitCost> unitCosts,
        IReadOnlyCollection<SalesUnitConversionInfo> conversions,
        out bool missingCosts,
        out bool conversionWarnings)
    {
        missingCosts = false;
        conversionWarnings = false;
        var total = 0m;
        var choices = item.IngredientChoices
            .Where(c => !c.IsDeleted)
            .ToDictionary(c => c.RecipeIngredientId, c => c.ChosenArticleId);

        foreach (var ingredient in item.MenuItem?.Recipe.Where(r => !r.IsDeleted) ?? [])
        {
            var articleId = choices.GetValueOrDefault(ingredient.Id, ingredient.ArticleId);
            var baseUnitId = baseUnitByArticle.GetValueOrDefault(articleId, ingredient.Article?.BaseUnitId ?? Guid.Empty);
            var baseQty = ConvertQuantity(ingredient.Quantity, ingredient.UnitId, baseUnitId, conversions);

            if (baseQty <= 0 && ingredient.UnitId != baseUnitId)
            {
                conversionWarnings = true;
                continue;
            }

            if (!unitCosts.TryGetValue(articleId, out var cost) || cost.Average <= 0)
            {
                missingCosts = true;
                continue;
            }

            total += baseQty * cost.Average;
        }

        return total;
    }

    private static decimal ConvertQuantity(decimal quantity, Guid originUnitId, Guid destinationUnitId, IEnumerable<SalesUnitConversionInfo> conversions)
    {
        if (originUnitId == Guid.Empty || destinationUnitId == Guid.Empty) return 0m;
        if (originUnitId == destinationUnitId) return quantity;

        var direct = conversions.FirstOrDefault(x => x.OriginUnitId == originUnitId && x.DestinationUnitId == destinationUnitId);
        if (direct is not null) return quantity * direct.Factor;

        var reverse = conversions.FirstOrDefault(x => x.OriginUnitId == destinationUnitId && x.DestinationUnitId == originUnitId);
        if (reverse is not null && reverse.Factor != 0) return quantity / reverse.Factor;

        return 0m;
    }

    private static decimal Round2(decimal value) => Math.Round(value, 2);

    private sealed record SalesUnitConversionInfo(Guid OriginUnitId, Guid DestinationUnitId, decimal Factor);
    private sealed record SalesPurchaseCostInput(Guid ArticleId, Guid UnitId, Guid ArticleBaseUnitId, decimal Quantity, decimal UnitPrice, decimal DiscountAmount);
    private sealed record SalesArticleCostSample(Guid ArticleId, decimal BaseQuantity, decimal NetCost);
    private sealed record SalesArticleUnitCost(decimal Average);
    private sealed record SalesProfitabilityLine(
        Guid MenuItemId,
        string MenuItemName,
        string? InternalCode,
        string CategoryName,
        decimal Quantity,
        decimal GrossSales,
        decimal NetSales,
        decimal TaxAmount,
        decimal UnitRecipeCost,
        decimal TotalFoodCost,
        bool HasMissingCosts,
        bool HasConversionWarnings,
        Guid OrderId,
        Guid? CashRegisterId,
        string CashRegisterName);
}

public class GetSriCertificateStatusHandler : IRequestHandler<GetSriCertificateStatusQuery, SriCertificateStatusDto>
{
    private readonly GrimorioDbContext _db;
    public GetSriCertificateStatusHandler(GrimorioDbContext db) => _db = db;

    public async Task<SriCertificateStatusDto> Handle(GetSriCertificateStatusQuery req, CancellationToken ct)
    {
        var cert = await _db.SriCertificates
            .FirstOrDefaultAsync(x => x.BranchId == req.BranchId && !x.IsDeleted, ct);
        if (cert == null)
            return new SriCertificateStatusDto { HasCertificate = false };

        return new SriCertificateStatusDto
        {
            HasCertificate = true,
            FileName = cert.FileName,
            ExpiresAt = cert.ExpiresAt,
            UploadedAt = cert.CreatedAt.ToString("o"),
        };
    }
}

// ── Documentos Electrónicos ───────────────────────────────────────────────────

public class GetElectronicDocumentsHandler : IRequestHandler<GetElectronicDocumentsQuery, List<ElectronicDocumentDto>>
{
    private readonly GrimorioDbContext _db;
    public GetElectronicDocumentsHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<ElectronicDocumentDto>> Handle(GetElectronicDocumentsQuery req, CancellationToken ct)
    {
        var query = _db.ElectronicDocuments.Where(d => d.BranchId == req.BranchId && !d.IsDeleted);
        if (req.FromUtc.HasValue) query = query.Where(d => d.CreatedAt >= req.FromUtc.Value);
        if (req.ToUtc.HasValue) query = query.Where(d => d.CreatedAt <= req.ToUtc.Value);
        if (!string.IsNullOrEmpty(req.Status) && Enum.TryParse<ElectronicDocumentStatus>(req.Status, true, out var statusEnum))
            query = query.Where(d => d.Status == statusEnum);

        var docs = await query
            .OrderByDescending(d => d.CreatedAt)
            .Take(req.PageSize)
            .ToListAsync(ct);

        return docs.Select(MapDoc).ToList();
    }

    private static ElectronicDocumentDto MapDoc(ElectronicDocument d) => new()
    {
        Id = d.Id, OrderPaymentId = d.OrderPaymentId,
        ClaveAcceso = d.ClaveAcceso, NumeroFactura = d.NumeroFactura,
        Secuencial = d.Secuencial, Environment = d.Environment,
        Status = d.Status.ToString(),
        TotalSinImpuestos = d.TotalSinImpuestos, TotalDescuento = d.TotalDescuento,
        TotalIva = d.TotalIva, ImporteTotal = d.ImporteTotal,
        NumeroAutorizacion = d.NumeroAutorizacion, FechaAutorizacion = d.FechaAutorizacion,
        ErrorMessage = d.ErrorMessage, SentAt = d.SentAt, RetryCount = d.RetryCount,
        CreatedAt = d.CreatedAt,
        HasRide = d.RidePdf != null && d.RidePdf.Length > 0,
        HasXml = !string.IsNullOrEmpty(d.XmlAuthorized ?? d.XmlSigned),
        HasXmlResponse = !string.IsNullOrEmpty(d.XmlResponseSri),
    };
}

public class GetElectronicDocumentBytesHandler : IRequestHandler<GetElectronicDocumentBytesQuery, ElectronicDocumentBytesDto?>
{
    private readonly GrimorioDbContext _db;
    public GetElectronicDocumentBytesHandler(GrimorioDbContext db) => _db = db;

    public async Task<ElectronicDocumentBytesDto?> Handle(GetElectronicDocumentBytesQuery req, CancellationToken ct)
    {
        var d = await _db.ElectronicDocuments
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId && !x.IsDeleted, ct);
        if (d == null) return null;
        return new ElectronicDocumentBytesDto
        {
            Id = d.Id,
            NumeroFactura = d.NumeroFactura,
            RidePdf = d.RidePdf,
            XmlSigned = d.XmlSigned,
            XmlAuthorized = d.XmlAuthorized,
            XmlResponseSri = d.XmlResponseSri,
        };
    }
}

public class GetElectronicDocumentDetailHandler : IRequestHandler<GetElectronicDocumentDetailQuery, ElectronicDocumentDto?>
{
    private readonly GrimorioDbContext _db;
    public GetElectronicDocumentDetailHandler(GrimorioDbContext db) => _db = db;

    public async Task<ElectronicDocumentDto?> Handle(GetElectronicDocumentDetailQuery req, CancellationToken ct)
    {
        var d = await _db.ElectronicDocuments
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId && !x.IsDeleted, ct);
        if (d == null) return null;
        return new ElectronicDocumentDto
        {
            Id = d.Id, OrderPaymentId = d.OrderPaymentId,
            ClaveAcceso = d.ClaveAcceso, NumeroFactura = d.NumeroFactura,
            Secuencial = d.Secuencial, Environment = d.Environment,
            Status = d.Status.ToString(),
            TotalSinImpuestos = d.TotalSinImpuestos, TotalDescuento = d.TotalDescuento,
            TotalIva = d.TotalIva, ImporteTotal = d.ImporteTotal,
            NumeroAutorizacion = d.NumeroAutorizacion, FechaAutorizacion = d.FechaAutorizacion,
            ErrorMessage = d.ErrorMessage, SentAt = d.SentAt, RetryCount = d.RetryCount,
            CreatedAt = d.CreatedAt,
            HasRide = d.RidePdf != null && d.RidePdf.Length > 0,
            HasXml = !string.IsNullOrEmpty(d.XmlAuthorized ?? d.XmlSigned),
        };
    }
}

public class GetInvoiceTemplateHandler : IRequestHandler<GetInvoiceTemplateQuery, InvoiceTemplateDto>
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly GrimorioDbContext _db;
    public GetInvoiceTemplateHandler(GrimorioDbContext db) => _db = db;

    public async Task<InvoiceTemplateDto> Handle(GetInvoiceTemplateQuery req, CancellationToken ct)
    {
        var template = await _db.InvoiceTemplates
            .FirstOrDefaultAsync(t => t.BranchId == req.BranchId && !t.IsDeleted, ct);

        if (template == null)
            return BuildDto(new InvoiceTemplate());

        return BuildDto(template);
    }

    internal static InvoiceTemplateDto BuildDto(InvoiceTemplate t)
    {
        var pdfBlocks = TryDeserialize<List<PdfBlockDto>>(t.PdfBlocksJson)
            ?? TryDeserialize<List<PdfBlockDto>>(InvoiceTemplate.DefaultPdfBlocks)
            ?? [];
        var emailBlocks = TryDeserialize<List<EmailBlockDto>>(t.EmailBlocksJson)
            ?? TryDeserialize<List<EmailBlockDto>>(InvoiceTemplate.DefaultEmailBlocks)
            ?? [];

        return new InvoiceTemplateDto
        {
            LogoBase64 = t.LogoBase64,
            PrimaryColor = t.PrimaryColor,
            AccentColor = t.AccentColor,
            PdfBlocks = pdfBlocks,
            EmailSubject = t.EmailSubject,
            EmailBlocks = emailBlocks,
        };
    }

    private static T? TryDeserialize<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        try { return JsonSerializer.Deserialize<T>(json, JsonOpts); }
        catch { return default; }
    }
}
