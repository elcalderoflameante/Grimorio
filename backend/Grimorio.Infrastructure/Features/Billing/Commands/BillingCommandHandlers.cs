using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Application.Features.Inventory.Commands;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.Inventory;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Grimorio.Infrastructure.Features.Billing.Commands;

// ── TaxRate ───────────────────────────────────────────────────────────────────

public class CreateTaxRateHandler : IRequestHandler<CreateTaxRateCommand, TaxRateDto>
{
    private readonly GrimorioDbContext _db;
    public CreateTaxRateHandler(GrimorioDbContext db) => _db = db;

    public async Task<TaxRateDto> Handle(CreateTaxRateCommand req, CancellationToken ct)
    {
        if (req.IsDefault)
            await ClearDefault(req.BranchId, ct);

        var entity = new TaxRate
        {
            BranchId = req.BranchId, Name = req.Name.Trim(),
            Percentage = req.Percentage, SriCode = req.SriCode.Trim(),
            IsDefault = req.IsDefault, IsActive = req.IsActive,
        };
        _db.TaxRates.Add(entity);
        await _db.SaveChangesAsync(ct);
        return BillingMapper.MapTaxRate(entity);
    }

    private async Task ClearDefault(Guid branchId, CancellationToken ct)
    {
        var current = await _db.TaxRates.FirstOrDefaultAsync(t => t.BranchId == branchId && t.IsDefault && !t.IsDeleted, ct);
        if (current != null) { current.IsDefault = false; }
    }
}

public class UpdateTaxRateHandler : IRequestHandler<UpdateTaxRateCommand, TaxRateDto>
{
    private readonly GrimorioDbContext _db;
    public UpdateTaxRateHandler(GrimorioDbContext db) => _db = db;

    public async Task<TaxRateDto> Handle(UpdateTaxRateCommand req, CancellationToken ct)
    {
        var entity = await _db.TaxRates.FirstOrDefaultAsync(t => t.Id == req.Id && t.BranchId == req.BranchId && !t.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Tarifa de IVA no encontrada.");

        if (req.IsDefault && !entity.IsDefault)
        {
            var current = await _db.TaxRates.FirstOrDefaultAsync(t => t.BranchId == req.BranchId && t.IsDefault && !t.IsDeleted && t.Id != req.Id, ct);
            if (current != null) current.IsDefault = false;
        }

        entity.Name = req.Name.Trim(); entity.Percentage = req.Percentage;
        entity.SriCode = req.SriCode.Trim(); entity.IsDefault = req.IsDefault;
        entity.IsActive = req.IsActive;
        await _db.SaveChangesAsync(ct);
        return BillingMapper.MapTaxRate(entity);
    }
}

public class DeleteTaxRateHandler : IRequestHandler<DeleteTaxRateCommand, bool>
{
    private readonly GrimorioDbContext _db;
    public DeleteTaxRateHandler(GrimorioDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteTaxRateCommand req, CancellationToken ct)
    {
        var entity = await _db.TaxRates.FirstOrDefaultAsync(t => t.Id == req.Id && t.BranchId == req.BranchId && !t.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Tarifa de IVA no encontrada.");
        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

// ── BranchTaxConfig ───────────────────────────────────────────────────────────

public class UpsertBranchTaxConfigHandler : IRequestHandler<UpsertBranchTaxConfigCommand, BranchTaxConfigDto>
{
    private readonly GrimorioDbContext _db;
    public UpsertBranchTaxConfigHandler(GrimorioDbContext db) => _db = db;

    public async Task<BranchTaxConfigDto> Handle(UpsertBranchTaxConfigCommand req, CancellationToken ct)
    {
        var cfg = await _db.BranchTaxConfigs.FirstOrDefaultAsync(c => c.BranchId == req.BranchId && !c.IsDeleted, ct);
        if (cfg == null)
        {
            cfg = new BranchTaxConfig { BranchId = req.BranchId };
            _db.BranchTaxConfigs.Add(cfg);
        }
        cfg.Ruc = req.Ruc.Trim(); cfg.RazonSocial = req.RazonSocial.Trim();
        cfg.NombreComercial = req.NombreComercial?.Trim();
        cfg.Direccion = req.Direccion.Trim();
        cfg.CodigoEstablecimiento = req.CodigoEstablecimiento.Trim();
        cfg.PuntoEmision = req.PuntoEmision.Trim();
        cfg.Ambiente = req.Ambiente;
        await _db.SaveChangesAsync(ct);
        return new BranchTaxConfigDto
        {
            Id = cfg.Id, Ruc = cfg.Ruc, RazonSocial = cfg.RazonSocial,
            NombreComercial = cfg.NombreComercial, Direccion = cfg.Direccion,
            CodigoEstablecimiento = cfg.CodigoEstablecimiento, PuntoEmision = cfg.PuntoEmision,
            Ambiente = cfg.Ambiente,
        };
    }
}

// ── PaymentMethodConfig ───────────────────────────────────────────────────────

public class CreatePaymentMethodHandler : IRequestHandler<CreatePaymentMethodCommand, PaymentMethodConfigDto>
{
    private readonly GrimorioDbContext _db;
    public CreatePaymentMethodHandler(GrimorioDbContext db) => _db = db;

    public async Task<PaymentMethodConfigDto> Handle(CreatePaymentMethodCommand req, CancellationToken ct)
    {
        var entity = new PaymentMethodConfig
        {
            Id = Guid.NewGuid(),
            Name = req.Name.Trim(),
            Color = req.Color,
            IsCash = req.IsCash,
            IsActive = true,
            SortOrder = req.SortOrder,
        };
        _db.PaymentMethodConfigs.Add(entity);
        await _db.SaveChangesAsync(ct);
        return BillingMapper.MapPaymentMethod(entity);
    }
}

public class UpdatePaymentMethodHandler : IRequestHandler<UpdatePaymentMethodCommand, PaymentMethodConfigDto>
{
    private readonly GrimorioDbContext _db;
    public UpdatePaymentMethodHandler(GrimorioDbContext db) => _db = db;

    public async Task<PaymentMethodConfigDto> Handle(UpdatePaymentMethodCommand req, CancellationToken ct)
    {
        var entity = await _db.PaymentMethodConfigs
            .FirstOrDefaultAsync(x => x.Id == req.Id && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Método de pago no encontrado.");

        entity.Name = req.Name.Trim();
        entity.Color = req.Color;
        entity.IsCash = req.IsCash;
        entity.IsActive = req.IsActive;
        entity.SortOrder = req.SortOrder;
        await _db.SaveChangesAsync(ct);
        return BillingMapper.MapPaymentMethod(entity);
    }
}

public class DeletePaymentMethodHandler : IRequestHandler<DeletePaymentMethodCommand, bool>
{
    private readonly GrimorioDbContext _db;
    public DeletePaymentMethodHandler(GrimorioDbContext db) => _db = db;

    public async Task<bool> Handle(DeletePaymentMethodCommand req, CancellationToken ct)
    {
        var entity = await _db.PaymentMethodConfigs
            .FirstOrDefaultAsync(x => x.Id == req.Id && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Método de pago no encontrado.");

        var inUse = await _db.PaymentLines.AnyAsync(l => l.PaymentMethodConfigId == req.Id, ct);
        if (inUse)
            throw new InvalidOperationException("No se puede eliminar un medio de pago que ya tiene registros de cobro. Desactívalo en su lugar.");

        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

// ── Customers ─────────────────────────────────────────────────────────────────

public class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    private readonly GrimorioDbContext _db;
    public CreateCustomerHandler(GrimorioDbContext db) => _db = db;

    public async Task<CustomerDto> Handle(CreateCustomerCommand req, CancellationToken ct)
    {
        if (!Enum.TryParse<TaxIdType>(req.TaxIdType, out var taxIdType))
            throw new InvalidOperationException($"Tipo de identificación inválido: {req.TaxIdType}");

        var entity = new Customer
        {
            Id = Guid.NewGuid(), BranchId = req.BranchId,
            Name = req.Name.Trim(), TaxId = req.TaxId?.Trim(),
            TaxIdType = taxIdType, Address = req.Address?.Trim(),
            Phone = req.Phone?.Trim(), Email = req.Email?.Trim(),
        };
        _db.Customers.Add(entity);
        await _db.SaveChangesAsync(ct);
        return BillingMapper.MapCustomer(entity);
    }
}

public class UpdateCustomerHandler : IRequestHandler<UpdateCustomerCommand, CustomerDto>
{
    private readonly GrimorioDbContext _db;
    public UpdateCustomerHandler(GrimorioDbContext db) => _db = db;

    public async Task<CustomerDto> Handle(UpdateCustomerCommand req, CancellationToken ct)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");

        if (!Enum.TryParse<TaxIdType>(req.TaxIdType, out var taxIdType))
            throw new InvalidOperationException($"Tipo de identificación inválido: {req.TaxIdType}");

        entity.Name = req.Name.Trim();
        entity.TaxId = req.TaxId?.Trim();
        entity.TaxIdType = taxIdType;
        entity.Address = req.Address?.Trim();
        entity.Phone = req.Phone?.Trim();
        entity.Email = req.Email?.Trim();
        entity.IsActive = req.IsActive;
        await _db.SaveChangesAsync(ct);
        return BillingMapper.MapCustomer(entity);
    }
}

public class DeleteCustomerHandler : IRequestHandler<DeleteCustomerCommand, bool>
{
    private readonly GrimorioDbContext _db;
    public DeleteCustomerHandler(GrimorioDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteCustomerCommand req, CancellationToken ct)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");
        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

// ── CashSession ───────────────────────────────────────────────────────────────

public class OpenCashSessionHandler : IRequestHandler<OpenCashSessionCommand, CashSessionDto>
{
    private readonly GrimorioDbContext _db;
    public OpenCashSessionHandler(GrimorioDbContext db) => _db = db;

    public async Task<CashSessionDto> Handle(OpenCashSessionCommand req, CancellationToken ct)
    {
        var existing = await _db.CashSessions
            .FirstOrDefaultAsync(x => x.BranchId == req.BranchId && x.Status == CashSessionStatus.Open && !x.IsDeleted, ct);
        if (existing != null)
            throw new InvalidOperationException("Ya hay una sesión de caja abierta.");

        var session = new CashSession
        {
            Id = Guid.NewGuid(), BranchId = req.BranchId,
            OpenedBy = req.UserId, OpenedByName = req.UserName,
            OpeningBalance = req.OpeningBalance,
            OpenedAt = DateTime.UtcNow,
            Status = CashSessionStatus.Open,
        };
        _db.CashSessions.Add(session);
        await _db.SaveChangesAsync(ct);
        return BillingMapper.MapSession(session);
    }
}

public class CloseCashSessionHandler : IRequestHandler<CloseCashSessionCommand, CashSessionDto>
{
    private readonly GrimorioDbContext _db;
    public CloseCashSessionHandler(GrimorioDbContext db) => _db = db;

    public async Task<CashSessionDto> Handle(CloseCashSessionCommand req, CancellationToken ct)
    {
        var session = await _db.CashSessions
            .Include(s => s.Payments).ThenInclude(p => p.Lines).ThenInclude(l => l.Config)
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Sesión no encontrada.");

        if (session.Status != CashSessionStatus.Open)
            throw new InvalidOperationException("La sesión ya está cerrada.");

        session.ClosedAt = DateTime.UtcNow;
        session.ClosedBy = req.UserId;
        session.ClosedByName = req.UserName;
        session.ActualCash = req.ActualCash;
        session.CloseNotes = req.Notes?.Trim();
        session.Status = CashSessionStatus.Closed;
        await _db.SaveChangesAsync(ct);
        return BillingMapper.MapSession(session);
    }
}

// ── Payment ───────────────────────────────────────────────────────────────────

public class PayOrderHandler : IRequestHandler<PayOrderCommand, OrderPaymentDto>
{
    private readonly GrimorioDbContext _db;
    private readonly IMediator _mediator;
    private readonly ILogger<PayOrderHandler> _logger;
    public PayOrderHandler(GrimorioDbContext db, IMediator mediator, ILogger<PayOrderHandler> logger)
    { _db = db; _mediator = mediator; _logger = logger; }

    public async Task<OrderPaymentDto> Handle(PayOrderCommand req, CancellationToken ct)
    {
        if (req.Lines.Count == 0)
            throw new InvalidOperationException("Se requiere al menos un medio de pago.");

        if (req.OrderAmount <= 0)
            throw new InvalidOperationException("El monto a cobrar debe ser mayor a cero.");

        // Cargar métodos de pago usados en este cobro
        var methodIds = req.Lines.Select(l => l.MethodId).Distinct().ToList();
        var methods = await _db.PaymentMethodConfigs
            .Where(m => methodIds.Contains(m.Id) && !m.IsDeleted)
            .ToListAsync(ct);

        if (methods.Count != methodIds.Count)
            throw new InvalidOperationException("Uno o más medios de pago no existen.");

        var methodMap = methods.ToDictionary(m => m.Id);

        var order = await _db.Orders
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == req.OrderId && o.BranchId == req.BranchId && !o.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Orden no encontrada.");

        if (order.Status == Domain.Entities.POS.OrderStatus.Cancelled)
            throw new InvalidOperationException("No se puede cobrar una orden cancelada.");

        if (order.Status == Domain.Entities.POS.OrderStatus.Draft)
            throw new InvalidOperationException("La orden debe estar confirmada antes de cobrarla.");

        var alreadyPaid = order.Payments.Where(p => !p.IsDeleted).Sum(p => p.OrderAmount);
        var remaining = order.Total - alreadyPaid;

        if (req.OrderAmount > remaining + 0.01m)
            throw new InvalidOperationException($"El monto ({req.OrderAmount:F2}) supera el saldo pendiente ({remaining:F2}).");

        var totalTendered = req.Lines.Sum(l => l.AmountTendered);
        if (totalTendered < req.OrderAmount)
            throw new InvalidOperationException("Los medios de pago no cubren el monto indicado.");

        var totalChange = totalTendered - req.OrderAmount;
        var hasCashLine = req.Lines.Any(l => methodMap[l.MethodId].IsCash);
        if (totalChange > 0 && !hasCashLine)
            throw new InvalidOperationException("Hay excedente pero no se indicó línea de efectivo para dar vuelto.");

        if (!Enum.TryParse<DocumentType>(req.DocumentType, out var docType))
            throw new InvalidOperationException($"Tipo de documento inválido: {req.DocumentType}");

        if (docType == DocumentType.Factura && !req.CustomerId.HasValue)
            throw new InvalidOperationException("La factura requiere un cliente con RUC o cédula.");

        if (req.CustomerId.HasValue)
        {
            var exists = await _db.Customers.AnyAsync(
                c => c.Id == req.CustomerId.Value && c.BranchId == req.BranchId && !c.IsDeleted, ct);
            if (!exists) throw new KeyNotFoundException("Cliente no encontrado.");
        }

        var paidAt = DateTime.UtcNow;

        // Si el frontend no envió sesión, auto-asignar la activa del sucursal
        var sessionId = req.CashSessionId ?? await _db.CashSessions
            .Where(s => s.BranchId == req.BranchId && s.Status == CashSessionStatus.Open && !s.IsDeleted)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(ct);

        var payment = new OrderPayment
        {
            Id = Guid.NewGuid(), BranchId = req.BranchId,
            OrderId = order.Id, CashSessionId = sessionId,
            CustomerId = req.CustomerId, DocumentType = docType,
            OrderAmount = req.OrderAmount, PaidAt = paidAt,
        };

        var remainingChange = totalChange;
        var lines = req.Lines.Select(l =>
        {
            var method = methodMap[l.MethodId];
            var change = 0m;
            if (method.IsCash && remainingChange > 0)
            {
                change = Math.Min(remainingChange, l.AmountTendered);
                remainingChange -= change;
            }
            return new PaymentLine
            {
                Id = Guid.NewGuid(),
                OrderPaymentId = payment.Id,
                PaymentMethodConfigId = l.MethodId,
                AmountTendered = l.AmountTendered,
                Change = change,
                Config = method,
            };
        }).ToList();

        payment.Lines = lines;
        _db.OrderPayments.Add(payment);

        var newRemaining = remaining - req.OrderAmount;
        var isFullyPaid = newRemaining <= 0.01m;
        if (isFullyPaid)
        {
            order.PaidAt = paidAt;
            order.CustomerId ??= req.CustomerId;
        }

        await _db.SaveChangesAsync(ct);

        if (isFullyPaid)
            await DeductInventoryForOrderAsync(order.Id, order.Number, req.BranchId, ct);

        Customer? customer = req.CustomerId.HasValue
            ? await _db.Customers.FindAsync([req.CustomerId.Value], ct)
            : null;

        return BillingMapper.MapPayment(payment, order.Number, customer);
    }

    private async Task DeductInventoryForOrderAsync(Guid orderId, int orderNumber, Guid branchId, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.MenuItem)
                    .ThenInclude(m => m!.Recipe.Where(r => !r.IsDeleted))
            .Include(o => o.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.IngredientChoices.Where(c => !c.IsDeleted))
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null) return;

        // Bodega de respaldo: la primera activa de la sucursal
        var fallbackWarehouseId = await _db.Warehouses
            .Where(w => w.BranchId == branchId && !w.IsDeleted && w.IsActive)
            .OrderBy(w => w.CreatedAt)
            .Select(w => w.Id)
            .FirstOrDefaultAsync(ct);

        if (fallbackWarehouseId == default) return;

        foreach (var item in order.Items)
        {
            if (item.MenuItem?.Recipe == null) continue;

            foreach (var ingredient in item.MenuItem.Recipe)
            {
                // Ingrediente variable: usar el artículo elegido por el cliente
                var articleId = ingredient.ArticleId;
                if (ingredient.IsVariable)
                {
                    var choice = item.IngredientChoices
                        .FirstOrDefault(c => c.RecipeIngredientId == ingredient.Id);
                    if (choice != null) articleId = choice.ChosenArticleId;
                }

                var qty = ingredient.Quantity * item.Quantity;

                // Bodega con más stock para ese artículo, o la de respaldo
                var warehouseId = await _db.WarehouseStock
                    .Where(ws => ws.ArticleId == articleId && ws.BranchId == branchId && !ws.IsDeleted)
                    .OrderByDescending(ws => ws.Quantity)
                    .Select(ws => ws.WarehouseId)
                    .FirstOrDefaultAsync(ct);

                if (warehouseId == default) warehouseId = fallbackWarehouseId;

                try
                {
                    await _mediator.Send(new RegisterMovementCommand
                    {
                        BranchId = branchId,
                        ArticleId = articleId,
                        WarehouseId = warehouseId,
                        Type = MovementType.SaleDeduction,
                        Quantity = qty,
                        UnitId = ingredient.UnitId,
                        Reference = $"Orden #{orderNumber}",
                    }, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "No se pudo descontar inventario: artículo {ArticleId}, unidad {UnitId}, orden #{OrderNumber}",
                        articleId, ingredient.UnitId, orderNumber);
                }
            }
        }
    }
}

// ── Mapper ────────────────────────────────────────────────────────────────────

internal static class BillingMapper
{
    internal static TaxRateDto MapTaxRate(TaxRate t) => new()
    {
        Id = t.Id, Name = t.Name, Percentage = t.Percentage,
        SriCode = t.SriCode, IsDefault = t.IsDefault, IsActive = t.IsActive,
    };

    internal static PaymentMethodConfigDto MapPaymentMethod(PaymentMethodConfig m) => new()
    {
        Id = m.Id, Name = m.Name, Color = m.Color,
        IsCash = m.IsCash, IsActive = m.IsActive, SortOrder = m.SortOrder,
    };

    internal static CustomerDto MapCustomer(Customer c) => new()
    {
        Id = c.Id, Name = c.Name, TaxId = c.TaxId,
        TaxIdType = c.TaxIdType.ToString(), Address = c.Address,
        Phone = c.Phone, Email = c.Email, IsActive = c.IsActive,
    };

    internal static CashSessionDto MapSession(CashSession s, List<OrderPayment>? payments = null)
    {
        var effectivePayments = payments ?? s.Payments.Where(p => !p.IsDeleted).ToList();
        var allLines = effectivePayments.SelectMany(p => p.Lines).ToList();

        var totals = allLines
            .Where(l => l.Config != null)
            .GroupBy(l => l.Config!)
            .Select(g => new PaymentMethodTotalDto
            {
                MethodId = g.Key.Id,
                MethodName = g.Key.Name,
                MethodColor = g.Key.Color,
                IsCash = g.Key.IsCash,
                Total = g.Sum(l => l.AmountTendered - l.Change),
            })
            .OrderBy(t => t.MethodName)
            .ToList();

        var totalCash = totals.Where(t => t.IsCash).Sum(t => t.Total);
        var totalSales = totals.Sum(t => t.Total);
        var expectedCash = s.OpeningBalance + totalCash;
        var totalOrders = effectivePayments.Select(p => p.OrderId).Distinct().Count();

        return new CashSessionDto
        {
            Id = s.Id, OpenedByName = s.OpenedByName,
            OpeningBalance = s.OpeningBalance, OpenedAt = s.OpenedAt,
            ClosedAt = s.ClosedAt, ClosedByName = s.ClosedByName,
            ActualCash = s.ActualCash, CloseNotes = s.CloseNotes,
            Status = s.Status.ToString(),
            Totals = totals,
            TotalSales = totalSales, TotalOrders = totalOrders,
            ExpectedCash = expectedCash,
            CashDifference = s.ActualCash.HasValue ? s.ActualCash.Value - expectedCash : null,
        };
    }

    internal static OrderPaymentDto MapPayment(
        OrderPayment p, int orderNumber, Customer? customer,
        string? tableCode = null, string? tableName = null, string? orderType = null) => new()
    {
        Id = p.Id, OrderId = p.OrderId, OrderNumber = orderNumber,
        OrderType = orderType,
        CustomerId = p.CustomerId, CustomerName = customer?.Name,
        CustomerTaxId = customer?.TaxId,
        TableCode = tableCode, TableName = tableName,
        DocumentType = p.DocumentType.ToString(),
        OrderAmount = p.OrderAmount, PaidAt = p.PaidAt,
        Lines = p.Lines.Select(l => new PaymentLineDto
        {
            Id = l.Id,
            MethodId = l.PaymentMethodConfigId,
            MethodName = l.Config?.Name ?? string.Empty,
            MethodColor = l.Config?.Color ?? "#1677ff",
            IsCash = l.Config?.IsCash ?? false,
            AmountTendered = l.AmountTendered,
            Change = l.Change,
        }).ToList(),
    };
}
