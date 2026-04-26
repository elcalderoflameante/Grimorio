using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Purchases.Commands;
using Grimorio.Application.Features.Inventory.Commands;
using Grimorio.Domain.Entities.Purchases;
using Grimorio.Domain.Entities.Inventory;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Purchases.Commands;

// ── Suppliers ─────────────────────────────────────────────────────────────────

public class CreateSupplierHandler : IRequestHandler<CreateSupplierCommand, SupplierDto>
{
    private readonly GrimorioDbContext _db;
    public CreateSupplierHandler(GrimorioDbContext db) => _db = db;

    public async Task<SupplierDto> Handle(CreateSupplierCommand req, CancellationToken ct)
    {
        var entity = new Supplier
        {
            Id = Guid.NewGuid(), BranchId = req.BranchId,
            Name = req.Name.Trim(), TaxId = req.TaxId?.Trim(),
            Phone = req.Phone?.Trim(), Email = req.Email?.Trim(),
            Address = req.Address?.Trim(), ContactName = req.ContactName?.Trim(),
        };
        _db.Suppliers.Add(entity);
        await _db.SaveChangesAsync(ct);
        return PurchasesMapper.MapSupplier(entity, 0);
    }
}

public class UpdateSupplierHandler : IRequestHandler<UpdateSupplierCommand, SupplierDto>
{
    private readonly GrimorioDbContext _db;
    public UpdateSupplierHandler(GrimorioDbContext db) => _db = db;

    public async Task<SupplierDto> Handle(UpdateSupplierCommand req, CancellationToken ct)
    {
        var entity = await _db.Suppliers.FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Supplier no encontrado.");
        entity.Name = req.Name.Trim(); entity.TaxId = req.TaxId?.Trim();
        entity.Phone = req.Phone?.Trim(); entity.Email = req.Email?.Trim();
        entity.Address = req.Address?.Trim(); entity.ContactName = req.ContactName?.Trim();
        entity.IsActive = req.IsActive;
        await _db.SaveChangesAsync(ct);
        return PurchasesMapper.MapSupplier(entity, 0);
    }
}

public class DeleteSupplierHandler : IRequestHandler<DeleteSupplierCommand, bool>
{
    private readonly GrimorioDbContext _db;
    public DeleteSupplierHandler(GrimorioDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteSupplierCommand req, CancellationToken ct)
    {
        var entity = await _db.Suppliers.FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Supplier no encontrado.");
        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

// ── Purchase orders ────────────────────────────────────────────────────────────

public class CreatePurchaseOrderHandler : IRequestHandler<CreatePurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly GrimorioDbContext _db;
    public CreatePurchaseOrderHandler(GrimorioDbContext db) => _db = db;

    public async Task<PurchaseOrderDto> Handle(CreatePurchaseOrderCommand req, CancellationToken ct)
    {
        var orderNumber = await GenerateOrderNumber(req.BranchId, ct);
        var order = new PurchaseOrder
        {
            Id = Guid.NewGuid(), BranchId = req.BranchId,
            SupplierId = req.SupplierId, OrderNumber = orderNumber,
            Status = PurchaseOrderStatus.Draft,
            IssuedAt = DateTime.UtcNow, ExpectedAt = req.ExpectedAt,
            Notes = req.Notes?.Trim(),
            DestinationWarehouseId = req.DestinationWarehouseId,
        };

        BuildItems(order, req.Items, req.BranchId);
        _db.PurchaseOrders.Add(order);
        await _db.SaveChangesAsync(ct);
        return await LoadDto(order.Id, ct);
    }

    private async Task<string> GenerateOrderNumber(Guid branchId, CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var count = await _db.PurchaseOrders.CountAsync(x => x.BranchId == branchId && !x.IsDeleted, ct);
        return $"OC-{year}-{(count + 1):D4}";
    }

    private static void BuildItems(PurchaseOrder order, List<PurchaseOrderItemInputDto> items, Guid branchId)
    {
        decimal subtotal = 0;
        foreach (var item in items)
        {
            var total = item.UnitPrice * item.QuantityOrdered;
            subtotal += total;
            order.Items.Add(new PurchaseOrderItem
            {
                Id = Guid.NewGuid(), BranchId = branchId,
                ArticleId = item.ArticleId, UnitId = item.UnitId,
                QuantityOrdered = item.QuantityOrdered, UnitPrice = item.UnitPrice,
                TotalPrice = total, Notes = item.Notes?.Trim(),
            });
        }
        order.Subtotal = subtotal;
        order.Total = subtotal;
    }

    private async Task<PurchaseOrderDto> LoadDto(Guid id, CancellationToken ct)
    {
        var order = await _db.PurchaseOrders
            .Include(o => o.Supplier)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Article)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Unit)
            .FirstAsync(o => o.Id == id, ct);

        var warehouse = order.DestinationWarehouseId.HasValue
            ? await _db.Warehouses.FindAsync([order.DestinationWarehouseId.Value], ct)
            : null;

        return PurchasesMapper.MapOrder(order, warehouse?.Name);
    }
}

public class UpdatePurchaseOrderHandler : IRequestHandler<UpdatePurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly GrimorioDbContext _db;
    public UpdatePurchaseOrderHandler(GrimorioDbContext db) => _db = db;

    public async Task<PurchaseOrderDto> Handle(UpdatePurchaseOrderCommand req, CancellationToken ct)
    {
        var order = await _db.PurchaseOrders
            .Include(o => o.Items.Where(i => !i.IsDeleted))
            .FirstOrDefaultAsync(o => o.Id == req.Id && o.BranchId == req.BranchId && !o.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Order no encontrada.");

        if (order.Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException("Solo se pueden modificar órdenes en borrador.");

        order.SupplierId = req.SupplierId;
        order.ExpectedAt = req.ExpectedAt;
        order.Notes = req.Notes?.Trim();
        order.DestinationWarehouseId = req.DestinationWarehouseId;

        foreach (var item in order.Items) item.IsDeleted = true;

        decimal subtotal = 0;
        foreach (var item in req.Items)
        {
            var total = item.UnitPrice * item.QuantityOrdered;
            subtotal += total;
            _db.PurchaseOrderItems.Add(new PurchaseOrderItem
            {
                Id = Guid.NewGuid(), BranchId = req.BranchId, PurchaseOrderId = order.Id,
                ArticleId = item.ArticleId, UnitId = item.UnitId,
                QuantityOrdered = item.QuantityOrdered, UnitPrice = item.UnitPrice,
                TotalPrice = total, Notes = item.Notes?.Trim(),
            });
        }
        order.Subtotal = subtotal;
        order.Total = subtotal;
        await _db.SaveChangesAsync(ct);

        var updated = await _db.PurchaseOrders
            .Include(o => o.Supplier)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Article)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Unit)
            .FirstAsync(o => o.Id == order.Id, ct);

        var warehouse = updated.DestinationWarehouseId.HasValue
            ? await _db.Warehouses.FindAsync([updated.DestinationWarehouseId.Value], ct)
            : null;

        return PurchasesMapper.MapOrder(updated, warehouse?.Name);
    }
}

public class SendPurchaseOrderHandler : IRequestHandler<SendPurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly GrimorioDbContext _db;
    public SendPurchaseOrderHandler(GrimorioDbContext db) => _db = db;

    public async Task<PurchaseOrderDto> Handle(SendPurchaseOrderCommand req, CancellationToken ct)
    {
        var order = await LoadWithItems(req.Id, req.BranchId, ct);
        if (order.Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException("La orden ya fue enviada.");
        if (!order.Items.Any(i => !i.IsDeleted))
            throw new InvalidOperationException("La orden no tiene ítems.");

        order.Status = PurchaseOrderStatus.Sent;
        await _db.SaveChangesAsync(ct);

        var warehouse = order.DestinationWarehouseId.HasValue ? await _db.Warehouses.FindAsync([order.DestinationWarehouseId.Value], ct) : null;
        return PurchasesMapper.MapOrder(order, warehouse?.Name);
    }

    private async Task<PurchaseOrder> LoadWithItems(Guid id, Guid branchId, CancellationToken ct) =>
        await _db.PurchaseOrders
            .Include(o => o.Supplier)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Article)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Unit)
            .FirstOrDefaultAsync(o => o.Id == id && o.BranchId == branchId && !o.IsDeleted, ct)
        ?? throw new KeyNotFoundException("Order no encontrada.");
}

public class ReceivePurchaseOrderHandler : IRequestHandler<ReceivePurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly GrimorioDbContext _db;
    private readonly IMediator _mediator;
    public ReceivePurchaseOrderHandler(GrimorioDbContext db, IMediator mediator) { _db = db; _mediator = mediator; }

    public async Task<PurchaseOrderDto> Handle(ReceivePurchaseOrderCommand req, CancellationToken ct)
    {
        var order = await _db.PurchaseOrders
            .Include(o => o.Supplier)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Article)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Unit)
            .FirstOrDefaultAsync(o => o.Id == req.Id && o.BranchId == req.BranchId && !o.IsDeleted, ct)
        ?? throw new KeyNotFoundException("Order no encontrada.");

        if (order.Status == PurchaseOrderStatus.Received)
            throw new InvalidOperationException("La orden ya fue recibida.");
        if (order.Status == PurchaseOrderStatus.Cancelled)
            throw new InvalidOperationException("La orden está cancelada.");

        foreach (var reception in req.Items)
        {
            var item = order.Items.FirstOrDefault(i => i.Id == reception.PurchaseOrderItemId && !i.IsDeleted);
            if (item == null || reception.QuantityReceived <= 0) continue;

            item.QuantityReceived = reception.QuantityReceived;

            await _mediator.Send(new RegisterMovementCommand
            {
                BranchId = req.BranchId,
                ArticleId = item.ArticleId,
                WarehouseId = req.WarehouseId,
                Type = MovementType.PurchaseEntry,
                Quantity = reception.QuantityReceived,
                UnitId = item.UnitId,
                Reference = $"Compra {order.OrderNumber}",
            }, ct);
        }

        order.Status = PurchaseOrderStatus.Received;
        order.ReceivedAt = DateTime.UtcNow;
        order.DestinationWarehouseId = req.WarehouseId;
        await _db.SaveChangesAsync(ct);

        var warehouse = await _db.Warehouses.FindAsync([req.WarehouseId], ct);
        return PurchasesMapper.MapOrder(order, warehouse?.Name);
    }
}

public class CancelPurchaseOrderHandler : IRequestHandler<CancelPurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly GrimorioDbContext _db;
    public CancelPurchaseOrderHandler(GrimorioDbContext db) => _db = db;

    public async Task<PurchaseOrderDto> Handle(CancelPurchaseOrderCommand req, CancellationToken ct)
    {
        var order = await _db.PurchaseOrders
            .Include(o => o.Supplier)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Article)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Unit)
            .FirstOrDefaultAsync(o => o.Id == req.Id && o.BranchId == req.BranchId && !o.IsDeleted, ct)
        ?? throw new KeyNotFoundException("Order no encontrada.");

        if (order.Status == PurchaseOrderStatus.Received)
            throw new InvalidOperationException("No se puede cancelar una orden ya recibida.");

        order.Status = PurchaseOrderStatus.Cancelled;
        await _db.SaveChangesAsync(ct);
        return PurchasesMapper.MapOrder(order, null);
    }
}

public class DeletePurchaseOrderHandler : IRequestHandler<DeletePurchaseOrderCommand, bool>
{
    private readonly GrimorioDbContext _db;
    public DeletePurchaseOrderHandler(GrimorioDbContext db) => _db = db;

    public async Task<bool> Handle(DeletePurchaseOrderCommand req, CancellationToken ct)
    {
        var order = await _db.PurchaseOrders.FirstOrDefaultAsync(o => o.Id == req.Id && o.BranchId == req.BranchId && !o.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Order no encontrada.");
        if (order.Status == PurchaseOrderStatus.Received)
            throw new InvalidOperationException("No se puede eliminar una orden recibida.");
        order.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

// ── Mapper ────────────────────────────────────────────────────────────────────

internal static class PurchasesMapper
{
    internal static SupplierDto MapSupplier(Supplier p, int totalOrders) => new()
    {
        Id = p.Id, Name = p.Name, TaxId = p.TaxId,
        Phone = p.Phone, Email = p.Email, Address = p.Address,
        ContactName = p.ContactName, IsActive = p.IsActive, TotalOrders = totalOrders,
    };

    internal static PurchaseOrderDto MapOrder(PurchaseOrder o, string? warehouseName) => new()
    {
        Id = o.Id, OrderNumber = o.OrderNumber, Status = o.Status.ToString(),
        SupplierId = o.SupplierId, SupplierName = o.Supplier?.Name ?? string.Empty,
        IssuedAt = o.IssuedAt, ExpectedAt = o.ExpectedAt,
        ReceivedAt = o.ReceivedAt, Notes = o.Notes,
        Subtotal = o.Subtotal, Total = o.Total,
        DestinationWarehouseId = o.DestinationWarehouseId, WarehouseName = warehouseName,
        TotalItems = o.Items.Count(i => !i.IsDeleted),
        Items = o.Items.Where(i => !i.IsDeleted).Select(i => new PurchaseOrderItemDto
        {
            Id = i.Id, ArticleId = i.ArticleId,
            ArticleName = i.Article?.Name ?? string.Empty,
            InternalCode = i.Article?.InternalCode,
            UnitId = i.UnitId, UnitSymbol = i.Unit?.Symbol ?? string.Empty,
            QuantityOrdered = i.QuantityOrdered, QuantityReceived = i.QuantityReceived,
            UnitPrice = i.UnitPrice, TotalPrice = i.TotalPrice,
            Notes = i.Notes,
        }).ToList(),
    };
}
