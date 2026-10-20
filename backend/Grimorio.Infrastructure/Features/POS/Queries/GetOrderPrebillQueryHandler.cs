using Grimorio.Application.DTOs;
using Grimorio.Application.Features.POS.Queries;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Services.Sri;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.POS.Queries;

public class GetOrderPrebillQueryHandler(GrimorioDbContext db) : IRequestHandler<GetOrderPrebillQuery, OrderPrebillDto?>
{
    public async Task<OrderPrebillDto?> Handle(GetOrderPrebillQuery request, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking()
            .Include(o => o.Table)
            .Include(o => o.Customer)
            .Include(o => o.Payments.Where(p => !p.IsDeleted))
            .Include(o => o.Items.Where(i => !i.IsDeleted && i.Status != OrderItemStatus.Cancelled)).ThenInclude(i => i.MenuItem)
            .Include(o => o.Items.Where(i => !i.IsDeleted && i.Status != OrderItemStatus.Cancelled))
                .ThenInclude(i => i.ModifierSelections.Where(s => !s.IsDeleted))
            .AsSplitQuery()
            .SingleOrDefaultAsync(o => o.Id == request.OrderId && o.BranchId == request.BranchId && !o.IsDeleted, ct);
        if (order is null) return null;
        var paid = order.Payments.Sum(p => p.OrderAmount);
        var pending = Math.Max(0, order.Total - paid);
        if (order.Status is OrderStatus.Draft or OrderStatus.Cancelled || order.PaidAt.HasValue || pending <= 0 || order.Items.Count == 0)
            throw new InvalidOperationException("La precuenta solo esta disponible para pedidos confirmados con saldo pendiente.");

        var branch = await db.Branches.AsNoTracking().SingleOrDefaultAsync(b => b.Id == request.BranchId && !b.IsDeleted, ct);
        var tax = await db.BranchTaxConfigs.AsNoTracking().SingleOrDefaultAsync(c => c.BranchId == request.BranchId && !c.IsDeleted, ct);
        var waiter = order.WaiterId.HasValue ? await db.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == order.WaiterId && u.BranchId == request.BranchId && !u.IsDeleted, ct) : null;

        return new OrderPrebillDto
        {
            OrderId = order.Id, OrderNumber = order.Number, TableCode = order.Table?.Code,
            WaiterName = waiter is null ? null : $"{waiter.FirstName} {waiter.LastName}".Trim(), PrintedAt = DateTime.UtcNow,
            Issuer = new ThermalReceiptIssuerDto
            {
                BusinessName = tax?.RazonSocial ?? branch?.Name ?? string.Empty,
                TradeName = tax?.NombreComercial, Ruc = tax?.Ruc ?? branch?.IdentificationNumber ?? string.Empty,
                Address = tax?.Direccion ?? branch?.Address ?? string.Empty, Phone = branch?.Phone, Email = branch?.Email,
                EstablishmentCode = tax?.CodigoEstablecimiento ?? "001", EmissionPoint = tax?.PuntoEmision ?? "001",
                Environment = tax?.Ambiente ?? "1", AccountingRequired = tax?.ObligadoContabilidad ?? false,
            },
            Customer = order.Customer is null ? null : new ThermalReceiptCustomerDto
            {
                Name = order.Customer.Name, TaxId = order.Customer.TaxId, Address = order.Customer.Address,
                Email = order.Customer.Email, Phone = order.Customer.Phone,
            },
            Items = order.Items.OrderBy(i => i.CreatedAt).ThenBy(i => i.Id).Select(i => new OrderPrebillItemDto
            {
                Name = SriInvoiceLineFactory.BuildDescription(i.MenuItem) ?? "Item", Quantity = i.Quantity,
                UnitPrice = i.UnitPrice, Total = i.TotalPrice,
                ModifierSelections = i.ModifierSelections.OrderBy(s => s.CreatedAt).ThenBy(s => s.Id).Select(s => new ModifierSelectionDto
                {
                    ModifierGroupId = s.ModifierGroupId, ModifierOptionId = s.ModifierOptionId, GroupName = s.GroupName,
                    OptionName = s.OptionName, Quantity = s.Quantity, UnitPriceDelta = s.UnitPriceDelta,
                    TotalPriceDelta = s.Quantity * s.UnitPriceDelta,
                }).ToList(),
            }).ToList(),
            Subtotal = order.Subtotal, Discount = order.DiscountTotal, Tax = order.TaxAmount,
            Total = order.Total, PaidAmount = paid, PendingAmount = pending,
        };
    }
}
