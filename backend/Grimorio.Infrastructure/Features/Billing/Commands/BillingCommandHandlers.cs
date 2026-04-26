using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.POS;
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
            .Include(s => s.Payments)
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
        var order = await _db.Orders
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == req.OrderId && o.BranchId == req.BranchId && !o.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Orden no encontrada.");

        if (order.Payment != null)
            throw new InvalidOperationException("La orden ya fue cobrada.");

        if (order.Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("No se puede cobrar una orden cancelada.");

        if (!Enum.TryParse<PaymentMethod>(req.Method, out var method))
            throw new InvalidOperationException($"Método de pago inválido: {req.Method}");

        var change = method == PaymentMethod.Cash
            ? Math.Max(0, req.AmountPaid - order.Total)
            : 0m;

        // Validate customer if provided
        if (req.CustomerId.HasValue)
        {
            var customerExists = await _db.Customers.AnyAsync(
                c => c.Id == req.CustomerId.Value && c.BranchId == req.BranchId && !c.IsDeleted, ct);
            if (!customerExists)
                throw new KeyNotFoundException("Cliente no encontrado.");
        }

        var payment = new OrderPayment
        {
            Id = Guid.NewGuid(), BranchId = req.BranchId,
            OrderId = order.Id, CashSessionId = req.CashSessionId,
            CustomerId = req.CustomerId, Method = method,
            AmountPaid = req.AmountPaid, Change = change,
            OrderTotal = order.Total, PaidAt = DateTime.UtcNow,
        };

        // Update order
        order.PaidAt = payment.PaidAt;
        order.CustomerId = req.CustomerId;
        if (order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Ready)
            order.Status = OrderStatus.Delivered; // keep delivered

        _db.OrderPayments.Add(payment);
        await _db.SaveChangesAsync(ct);

        // Load customer for response
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
        var totalCash = s.Payments.Where(p => p.Method == PaymentMethod.Cash).Sum(p => p.OrderTotal);
        var totalCard = s.Payments.Where(p => p.Method == PaymentMethod.Card).Sum(p => p.OrderTotal);
        var totalTransfer = s.Payments.Where(p => p.Method == PaymentMethod.Transfer).Sum(p => p.OrderTotal);
        var totalQr = s.Payments.Where(p => p.Method == PaymentMethod.QR).Sum(p => p.OrderTotal);
        var totalSales = totalCash + totalCard + totalTransfer + totalQr;
        var totalChangeGiven = s.Payments.Where(p => p.Method == PaymentMethod.Cash).Sum(p => p.Change);
        var expectedCash = s.OpeningBalance + totalCash - totalChangeGiven;

        return new CashSessionDto
        {
            Id = s.Id, OpenedByName = s.OpenedByName,
            OpeningBalance = s.OpeningBalance, OpenedAt = s.OpenedAt,
            ClosedAt = s.ClosedAt, ClosedByName = s.ClosedByName,
            ActualCash = s.ActualCash, CloseNotes = s.CloseNotes,
            Status = s.Status.ToString(),
            TotalCash = totalCash, TotalCard = totalCard,
            TotalTransfer = totalTransfer, TotalQr = totalQr,
            TotalSales = totalSales, TotalOrders = s.Payments.Count,
            ExpectedCash = expectedCash,
            CashDifference = s.ActualCash.HasValue ? s.ActualCash.Value - expectedCash : null,
        };
    }

    internal static OrderPaymentDto MapPayment(OrderPayment p, int orderNumber, Customer? customer) => new()
    {
        Id = p.Id, OrderId = p.OrderId, OrderNumber = orderNumber,
        CustomerId = p.CustomerId, CustomerName = customer?.Name,
        CustomerTaxId = customer?.TaxId,
        Method = p.Method.ToString(), AmountPaid = p.AmountPaid,
        Change = p.Change, OrderTotal = p.OrderTotal, PaidAt = p.PaidAt,
    };
}
