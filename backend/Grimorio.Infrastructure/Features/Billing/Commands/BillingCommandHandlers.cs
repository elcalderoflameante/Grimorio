using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Billing.Commands;

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
            .Include(s => s.Payments).ThenInclude(p => p.Lines)
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
    public PayOrderHandler(GrimorioDbContext db) => _db = db;

    public async Task<OrderPaymentDto> Handle(PayOrderCommand req, CancellationToken ct)
    {
        if (req.Lines.Count == 0)
            throw new InvalidOperationException("Se requiere al menos un medio de pago.");

        if (req.OrderAmount <= 0)
            throw new InvalidOperationException("El monto a cobrar debe ser mayor a cero.");

        var order = await _db.Orders
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == req.OrderId && o.BranchId == req.BranchId && !o.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Orden no encontrada.");

        if (order.Status == Domain.Entities.POS.OrderStatus.Cancelled)
            throw new InvalidOperationException("No se puede cobrar una orden cancelada.");

        if (order.Status == Domain.Entities.POS.OrderStatus.Draft)
            throw new InvalidOperationException("La orden debe estar confirmada antes de cobrarla.");

        // Calcular saldo pendiente
        var alreadyPaid = order.Payments.Where(p => !p.IsDeleted).Sum(p => p.OrderAmount);
        var remaining = order.Total - alreadyPaid;

        if (req.OrderAmount > remaining + 0.01m)
            throw new InvalidOperationException($"El monto ({req.OrderAmount:F2}) supera el saldo pendiente ({remaining:F2}).");

        // Validar que las líneas cubren el monto a cobrar
        var totalTendered = req.Lines.Sum(l => l.AmountTendered);
        if (totalTendered < req.OrderAmount)
            throw new InvalidOperationException("Los medios de pago no cubren el monto indicado.");

        // El excedente es vuelto en efectivo
        var totalChange = totalTendered - req.OrderAmount;
        var hasCashLine = req.Lines.Any(l => l.Method.Equals("Cash", StringComparison.OrdinalIgnoreCase));
        if (totalChange > 0 && !hasCashLine)
            throw new InvalidOperationException("Hay excedente pero no se indicó línea de efectivo para dar vuelto.");

        // Factura requiere cliente con RUC/cédula
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

        // Construir líneas asignando el vuelto a la última línea de efectivo
        var paidAt = DateTime.UtcNow;
        var payment = new OrderPayment
        {
            Id = Guid.NewGuid(), BranchId = req.BranchId,
            OrderId = order.Id, CashSessionId = req.CashSessionId,
            CustomerId = req.CustomerId, DocumentType = docType,
            OrderAmount = req.OrderAmount, PaidAt = paidAt,
        };

        var remainingChange = totalChange;
        var lines = req.Lines.Select((l, idx) =>
        {
            if (!Enum.TryParse<PaymentMethod>(l.Method, out var method))
                throw new InvalidOperationException($"Método de pago inválido: {l.Method}");

            // El vuelto va a la última línea de efectivo
            var change = 0m;
            if (method == PaymentMethod.Cash && remainingChange > 0)
            {
                // Asignar todo el vuelto a la primera línea de efectivo que lo absorba
                change = Math.Min(remainingChange, l.AmountTendered);
                remainingChange -= change;
            }

            return new PaymentLine
            {
                Id = Guid.NewGuid(),
                OrderPaymentId = payment.Id,
                Method = method,
                AmountTendered = l.AmountTendered,
                Change = change,
            };
        }).ToList();

        payment.Lines = lines;
        _db.OrderPayments.Add(payment);

        // Marcar orden como pagada solo si queda saldo cero
        var newRemaining = remaining - req.OrderAmount;
        if (newRemaining <= 0.01m)
        {
            order.PaidAt = paidAt;
            order.CustomerId ??= req.CustomerId;
        }

        await _db.SaveChangesAsync(ct);

        Customer? customer = req.CustomerId.HasValue
            ? await _db.Customers.FindAsync([req.CustomerId.Value], ct)
            : null;

        return BillingMapper.MapPayment(payment, order.Number, customer);
    }
}

// ── Mapper ────────────────────────────────────────────────────────────────────

internal static class BillingMapper
{
    internal static CustomerDto MapCustomer(Customer c) => new()
    {
        Id = c.Id, Name = c.Name, TaxId = c.TaxId,
        TaxIdType = c.TaxIdType.ToString(), Address = c.Address,
        Phone = c.Phone, Email = c.Email, IsActive = c.IsActive,
    };

    internal static CashSessionDto MapSession(CashSession s)
    {
        var allLines = s.Payments.Where(p => !p.IsDeleted).SelectMany(p => p.Lines).ToList();

        var totalCash = allLines.Where(l => l.Method == PaymentMethod.Cash).Sum(l => l.AmountTendered - l.Change);
        var totalCard = allLines.Where(l => l.Method == PaymentMethod.Card).Sum(l => l.AmountTendered);
        var totalTransfer = allLines.Where(l => l.Method == PaymentMethod.Transfer).Sum(l => l.AmountTendered);
        var totalQr = allLines.Where(l => l.Method == PaymentMethod.QR).Sum(l => l.AmountTendered);
        var totalSales = totalCash + totalCard + totalTransfer + totalQr;
        var expectedCash = s.OpeningBalance + totalCash;
        var totalOrders = s.Payments.Where(p => !p.IsDeleted).Select(p => p.OrderId).Distinct().Count();

        return new CashSessionDto
        {
            Id = s.Id, OpenedByName = s.OpenedByName,
            OpeningBalance = s.OpeningBalance, OpenedAt = s.OpenedAt,
            ClosedAt = s.ClosedAt, ClosedByName = s.ClosedByName,
            ActualCash = s.ActualCash, CloseNotes = s.CloseNotes,
            Status = s.Status.ToString(),
            TotalCash = totalCash, TotalCard = totalCard,
            TotalTransfer = totalTransfer, TotalQr = totalQr,
            TotalSales = totalSales, TotalOrders = totalOrders,
            ExpectedCash = expectedCash,
            CashDifference = s.ActualCash.HasValue ? s.ActualCash.Value - expectedCash : null,
        };
    }

    internal static OrderPaymentDto MapPayment(OrderPayment p, int orderNumber, Customer? customer) => new()
    {
        Id = p.Id, OrderId = p.OrderId, OrderNumber = orderNumber,
        CustomerId = p.CustomerId, CustomerName = customer?.Name,
        CustomerTaxId = customer?.TaxId,
        DocumentType = p.DocumentType.ToString(),
        OrderAmount = p.OrderAmount, PaidAt = p.PaidAt,
        Lines = p.Lines.Select(l => new PaymentLineDto
        {
            Id = l.Id, Method = l.Method.ToString(),
            AmountTendered = l.AmountTendered, Change = l.Change,
        }).ToList(),
    };
}
