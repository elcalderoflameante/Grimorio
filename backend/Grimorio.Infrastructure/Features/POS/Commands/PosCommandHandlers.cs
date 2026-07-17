using Grimorio.Application.DTOs;
using Grimorio.Application.Features.POS.Commands;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Features.Inventory;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Grimorio.Infrastructure.Features.POS.Commands;

// ── Estaciones ────────────────────────────────────────────────────────────────

public class CreateWorkStationCommandHandler : IRequestHandler<CreateWorkStationCommand, WorkStationDto>
{
    private readonly GrimorioDbContext _db;
    public CreateWorkStationCommandHandler(GrimorioDbContext db) => _db = db;

    public async Task<WorkStationDto> Handle(CreateWorkStationCommand req, CancellationToken ct)
    {
        if (!Enum.TryParse<StationType>(req.Type, out var stationType))
            throw new InvalidOperationException($"Type de estación no válido: {req.Type}");

        var entity = new WorkStation
        {
            Id = Guid.NewGuid(),
            BranchId = req.BranchId,
            Name = req.Name.Trim(),
            Type = stationType,
            IsActive = true,
        };
        _db.WorkStations.Add(entity);
        await _db.SaveChangesAsync(ct);
        return PosMapper.MapWorkStation(entity);
    }
}

public class UpdateWorkStationCommandHandler : IRequestHandler<UpdateWorkStationCommand, WorkStationDto>
{
    private readonly GrimorioDbContext _db;
    public UpdateWorkStationCommandHandler(GrimorioDbContext db) => _db = db;

    public async Task<WorkStationDto> Handle(UpdateWorkStationCommand req, CancellationToken ct)
    {
        var entity = await _db.WorkStations.FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("Estación no encontrada.");

        if (!Enum.TryParse<StationType>(req.Type, out var stationType))
            throw new InvalidOperationException($"Type de estación no válido: {req.Type}");

        entity.Name = req.Name.Trim();
        entity.Type = stationType;
        entity.IsActive = req.IsActive;
        await _db.SaveChangesAsync(ct);
        return PosMapper.MapWorkStation(entity);
    }
}

public class DeleteWorkStationCommandHandler : IRequestHandler<DeleteWorkStationCommand, bool>
{
    private readonly GrimorioDbContext _db;
    public DeleteWorkStationCommandHandler(GrimorioDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteWorkStationCommand req, CancellationToken ct)
    {
        var entity = await _db.WorkStations.FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("Estación no encontrada.");
        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

// ── Mesas posición ────────────────────────────────────────────────────────────

public class UpdateTablePositionCommandHandler : IRequestHandler<UpdateTablePositionCommand, bool>
{
    private readonly GrimorioDbContext _db;
    public UpdateTablePositionCommandHandler(GrimorioDbContext db) => _db = db;

    public async Task<bool> Handle(UpdateTablePositionCommand req, CancellationToken ct)
    {
        var table = await _db.RestaurantTables.FirstOrDefaultAsync(x => x.Id == req.Id && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("Mesa no encontrada.");
        table.PosX = req.PosX;
        table.PosY = req.PosY;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

// ── Órdenes ───────────────────────────────────────────────────────────────────

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly GrimorioDbContext _db;
    public CreateOrderCommandHandler(GrimorioDbContext db) => _db = db;

    public async Task<OrderDto> Handle(CreateOrderCommand req, CancellationToken ct)
    {
        if (!Enum.TryParse<OrderType>(req.Type, out var orderType))
            throw new InvalidOperationException($"Type de orden no válido: {req.Type}");

        var number = await _db.Orders
            .Where(o => o.BranchId == req.BranchId)
            .MaxAsync(o => (int?)o.Number, ct) ?? 0;
        number++;

        var order = new Order
        {
            Id = Guid.NewGuid(),
            BranchId = req.BranchId,
            Number = number,
            Type = orderType,
            Status = OrderStatus.Draft,
            TableId = req.TableId,
            WaiterId = req.WaiterId,
            CustomerName = req.CustomerName?.Trim(),
            DeliveryAddress = req.DeliveryAddress?.Trim(),
            Notes = req.Notes?.Trim(),
        };

        var itemMenuIds = req.Items.Select(i => i.MenuItemId).Distinct().ToList();
        var menuItems = await _db.MenuItems
            .Where(m => itemMenuIds.Contains(m.Id) && !m.IsDeleted)
            .Include(m => m.Station)
            .Include(m => m.TaxRate)
            .ToListAsync(ct);

        decimal subtotal = 0, discountTotal = 0;
        decimal base15 = 0, base0 = 0, baseExempt = 0, iva15 = 0, ice = 0;
        foreach (var itemDto in req.Items)
        {
            var menuItem = menuItems.FirstOrDefault(m => m.Id == itemDto.MenuItemId)
                ?? throw new InvalidOperationException($"Ítem no encontrado: {itemDto.MenuItemId}");

            var (discountAmt, taxableBase, taxAmt, totalPrice) = PosMapper.CalcItem(menuItem.Price, itemDto.Quantity, itemDto.DiscountPct, menuItem.TaxRate?.Percentage);
            subtotal += menuItem.Price * itemDto.Quantity;
            discountTotal += discountAmt;
            PosMapper.ClassifyTax(menuItem.TaxRate?.SriCode, taxableBase, taxAmt, ref base15, ref base0, ref baseExempt, ref iva15, ref ice);

            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                BranchId = req.BranchId,
                MenuItemId = itemDto.MenuItemId,
                StationId = menuItem.StationId,
                Quantity = itemDto.Quantity,
                UnitPrice = menuItem.Price,
                DiscountPct = itemDto.DiscountPct,
                DiscountAmount = discountAmt,
                TaxRateId = menuItem.TaxRateId,
                TaxAmount = taxAmt,
                TotalPrice = totalPrice,
                Notes = itemDto.Notes?.Trim(),
                Status = OrderItemStatus.Pending,
            };
            foreach (var choice in itemDto.IngredientChoices)
                orderItem.IngredientChoices.Add(new OrderItemIngredientChoice
                {
                    BranchId = req.BranchId,
                    RecipeIngredientId = choice.RecipeIngredientId,
                    ChosenArticleId = choice.ChosenArticleId,
                });
            order.Items.Add(orderItem);
        }

        order.Subtotal = subtotal;
        order.DiscountTotal = discountTotal;
        order.TaxableBase15 = base15;
        order.TaxableBase0 = base0;
        order.TaxableBaseExempt = baseExempt;
        order.Iva15 = iva15;
        order.Ice = ice;
        order.TaxAmount = iva15 + ice;
        // Precio inclusivo: Total = suma bruta - descuentos (IVA ya incluido)
        order.Total = subtotal - discountTotal;

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        return await LoadOrderDto(order.Id, ct);
    }

    private async Task<OrderDto> LoadOrderDto(Guid id, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Table)
            .Include(o => o.Items).ThenInclude(i => i.MenuItem)
            .Include(o => o.Items).ThenInclude(i => i.Station)
            .Include(o => o.Items).ThenInclude(i => i.IngredientChoices)
                .ThenInclude(c => c.ChosenArticle)
            .FirstAsync(o => o.Id == id, ct);
        return PosMapper.MapOrder(order);
    }
}

public class CreateDirectSaleCommandHandler : IRequestHandler<CreateDirectSaleCommand, OrderDto>
{
    private readonly GrimorioDbContext _db;
    public CreateDirectSaleCommandHandler(GrimorioDbContext db) => _db = db;

    public async Task<OrderDto> Handle(CreateDirectSaleCommand req, CancellationToken ct)
    {
        if (req.Items.Count == 0)
            throw new InvalidOperationException("La venta directa no tiene items.");

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var number = await _db.Orders
            .Where(o => o.BranchId == req.BranchId)
            .MaxAsync(o => (int?)o.Number, ct) ?? 0;
        number++;

        var order = new Order
        {
            Id = Guid.NewGuid(),
            BranchId = req.BranchId,
            Number = number,
            Type = OrderType.Takeout,
            Status = OrderStatus.Confirmed,
            ConfirmedAt = DateTime.UtcNow,
            WaiterId = req.CashierId,
            CustomerName = req.CustomerName?.Trim(),
            Notes = req.Notes?.Trim(),
        };

        var itemMenuIds = req.Items.Select(i => i.MenuItemId).Distinct().ToList();
        var menuItems = await _db.MenuItems
            .Where(m => itemMenuIds.Contains(m.Id) && !m.IsDeleted)
            .Include(m => m.Station)
            .Include(m => m.TaxRate)
            .ToListAsync(ct);

        decimal subtotal = 0, discountTotal = 0;
        decimal base15 = 0, base0 = 0, baseExempt = 0, iva15 = 0, ice = 0;
        foreach (var itemDto in req.Items)
        {
            var menuItem = menuItems.FirstOrDefault(m => m.Id == itemDto.MenuItemId)
                ?? throw new InvalidOperationException($"Item no encontrado: {itemDto.MenuItemId}");

            var (discountAmt, taxableBase, taxAmt, totalPrice) = PosMapper.CalcItem(menuItem.Price, itemDto.Quantity, itemDto.DiscountPct, menuItem.TaxRate?.Percentage);
            subtotal += menuItem.Price * itemDto.Quantity;
            discountTotal += discountAmt;
            PosMapper.ClassifyTax(menuItem.TaxRate?.SriCode, taxableBase, taxAmt, ref base15, ref base0, ref baseExempt, ref iva15, ref ice);

            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                BranchId = req.BranchId,
                MenuItemId = itemDto.MenuItemId,
                StationId = menuItem.StationId,
                Quantity = itemDto.Quantity,
                UnitPrice = menuItem.Price,
                DiscountPct = itemDto.DiscountPct,
                DiscountAmount = discountAmt,
                TaxRateId = menuItem.TaxRateId,
                TaxAmount = taxAmt,
                TotalPrice = totalPrice,
                Notes = itemDto.Notes?.Trim(),
                Status = OrderItemStatus.Pending,
            };

            foreach (var choice in itemDto.IngredientChoices)
                orderItem.IngredientChoices.Add(new OrderItemIngredientChoice
                {
                    BranchId = req.BranchId,
                    RecipeIngredientId = choice.RecipeIngredientId,
                    ChosenArticleId = choice.ChosenArticleId,
                });

            order.Items.Add(orderItem);
        }

        order.Subtotal = subtotal;
        order.DiscountTotal = discountTotal;
        order.TaxableBase15 = base15;
        order.TaxableBase0 = base0;
        order.TaxableBaseExempt = baseExempt;
        order.Iva15 = iva15;
        order.Ice = ice;
        order.TaxAmount = iva15 + ice;
        order.Total = subtotal - discountTotal;

        _db.Orders.Add(order);
        await StockReservationService.ReserveOrderItemsAsync(_db, req.BranchId, order.Id, order.Items.ToList(), ct);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var created = await _db.Orders
            .Include(o => o.Table)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.MenuItem)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Station)
            .Include(o => o.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.IngredientChoices.Where(c => !c.IsDeleted))
                    .ThenInclude(c => c.ChosenArticle)
            .FirstAsync(o => o.Id == order.Id, ct);
        return PosMapper.MapOrder(created);
    }
}

public class UpdateOrderItemsCommandHandler : IRequestHandler<UpdateOrderItemsCommand, OrderDto>
{
    private readonly GrimorioDbContext _db;
    public UpdateOrderItemsCommandHandler(GrimorioDbContext db) => _db = db;

    public async Task<OrderDto> Handle(UpdateOrderItemsCommand req, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == req.OrderId && o.BranchId == req.BranchId && !o.IsDeleted, ct)
            ?? throw new InvalidOperationException("Orden no encontrada.");

        var paidAmount = order.Payments.Where(p => !p.IsDeleted).Sum(p => p.OrderAmount);
        var isFullyPaid = order.PaidAt.HasValue || paidAmount >= order.Total - 0.01m;
        if (isFullyPaid)
            throw new InvalidOperationException("No se pueden agregar ítems a una orden que ya está pagada.");

        if (order.Status == OrderStatus.Draft)
        {
            // Draft: reemplazar todos los ítems existentes
            foreach (var item in order.Items.Where(i => !i.IsDeleted))
                item.IsDeleted = true;
        }
        else if (order.Status != OrderStatus.Confirmed && order.Status != OrderStatus.InPreparation)
        {
            throw new InvalidOperationException("No se pueden agregar ítems en el estado actual de la orden.");
        }

        var itemMenuIds = req.Items.Select(i => i.MenuItemId).Distinct().ToList();
        var menuItems = await _db.MenuItems
            .Where(m => itemMenuIds.Contains(m.Id) && !m.IsDeleted)
            .Include(m => m.Station)
            .Include(m => m.TaxRate)
            .ToListAsync(ct);

        decimal addedSubtotal = 0, addedDiscount = 0;
        decimal addedBase15 = 0, addedBase0 = 0, addedBaseExempt = 0, addedIva15 = 0, addedIce = 0;
        var newItems = new List<OrderItem>();
        foreach (var itemDto in req.Items)
        {
            var menuItem = menuItems.FirstOrDefault(m => m.Id == itemDto.MenuItemId)
                ?? throw new InvalidOperationException($"Ítem no encontrado: {itemDto.MenuItemId}");

            var (discountAmt, taxableBase, taxAmt, totalPrice) = PosMapper.CalcItem(menuItem.Price, itemDto.Quantity, itemDto.DiscountPct, menuItem.TaxRate?.Percentage);
            addedSubtotal += menuItem.Price * itemDto.Quantity;
            addedDiscount += discountAmt;
            PosMapper.ClassifyTax(menuItem.TaxRate?.SriCode, taxableBase, taxAmt, ref addedBase15, ref addedBase0, ref addedBaseExempt, ref addedIva15, ref addedIce);

            var newItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                BranchId = req.BranchId,
                OrderId = order.Id,
                MenuItemId = itemDto.MenuItemId,
                StationId = menuItem.StationId,
                Quantity = itemDto.Quantity,
                UnitPrice = menuItem.Price,
                DiscountPct = itemDto.DiscountPct,
                DiscountAmount = discountAmt,
                TaxRateId = menuItem.TaxRateId,
                TaxAmount = taxAmt,
                TotalPrice = totalPrice,
                Notes = itemDto.Notes?.Trim(),
                Status = OrderItemStatus.Pending,
            };
            foreach (var choice in itemDto.IngredientChoices)
                newItem.IngredientChoices.Add(new OrderItemIngredientChoice
                {
                    BranchId = req.BranchId,
                    RecipeIngredientId = choice.RecipeIngredientId,
                    ChosenArticleId = choice.ChosenArticleId,
                });
            _db.OrderItems.Add(newItem);
            newItems.Add(newItem);
        }

        if (order.Status == OrderStatus.Draft)
        {
            order.Subtotal = addedSubtotal;
            order.DiscountTotal = addedDiscount;
            order.TaxableBase15 = addedBase15;
            order.TaxableBase0 = addedBase0;
            order.TaxableBaseExempt = addedBaseExempt;
            order.Iva15 = addedIva15;
            order.Ice = addedIce;
            order.TaxAmount = addedIva15 + addedIce;
            order.Total = addedSubtotal - addedDiscount;
        }
        else
        {
            order.Subtotal += addedSubtotal;
            order.DiscountTotal += addedDiscount;
            order.TaxableBase15 += addedBase15;
            order.TaxableBase0 += addedBase0;
            order.TaxableBaseExempt += addedBaseExempt;
            order.Iva15 += addedIva15;
            order.Ice += addedIce;
            order.TaxAmount += addedIva15 + addedIce;
            order.Total += addedSubtotal - addedDiscount;

            await StockReservationService.ReserveOrderItemsAsync(_db, req.BranchId, order.Id, newItems, ct);
        }

        await _db.SaveChangesAsync(ct);

        var updated = await _db.Orders
            .Include(o => o.Table)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.MenuItem)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Station)
            .Include(o => o.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.IngredientChoices.Where(c => !c.IsDeleted))
                    .ThenInclude(c => c.ChosenArticle)
            .FirstAsync(o => o.Id == order.Id, ct);
        return PosMapper.MapOrder(updated);
    }
}

public class ConfirmOrderCommandHandler : IRequestHandler<ConfirmOrderCommand, OrderDto>
{
    private readonly GrimorioDbContext _db;
    public ConfirmOrderCommandHandler(GrimorioDbContext db) => _db = db;

    public async Task<OrderDto> Handle(ConfirmOrderCommand req, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Table)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.MenuItem)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Station)
            .Include(o => o.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.IngredientChoices.Where(c => !c.IsDeleted))
                    .ThenInclude(c => c.ChosenArticle)
            .FirstOrDefaultAsync(o => o.Id == req.OrderId && o.BranchId == req.BranchId && !o.IsDeleted, ct)
            ?? throw new InvalidOperationException("Orden no encontrada.");

        if (order.Status != OrderStatus.Draft)
            throw new InvalidOperationException("La orden ya fue confirmada.");

        if (!order.Items.Any(i => !i.IsDeleted))
            throw new InvalidOperationException("La orden no tiene ítems.");

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        order.Status = OrderStatus.Confirmed;
        order.ConfirmedAt = DateTime.UtcNow;
        await StockReservationService.ReserveOrderItemsAsync(_db, req.BranchId, order.Id, order.Items.ToList(), ct);
        await _db.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        return PosMapper.MapOrder(order);
    }
}

public class DeliverOrderCommandHandler : IRequestHandler<DeliverOrderCommand, OrderDto>
{
    private readonly GrimorioDbContext _db;
    public DeliverOrderCommandHandler(GrimorioDbContext db) => _db = db;

    public async Task<OrderDto> Handle(DeliverOrderCommand req, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Table)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.MenuItem)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Station)
            .FirstOrDefaultAsync(o => o.Id == req.OrderId && o.BranchId == req.BranchId && !o.IsDeleted, ct)
            ?? throw new InvalidOperationException("Orden no encontrada.");

        order.Status = OrderStatus.Delivered;
        order.DeliveredAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return PosMapper.MapOrder(order);
    }
}

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, OrderDto>
{
    private readonly GrimorioDbContext _db;
    public CancelOrderCommandHandler(GrimorioDbContext db) => _db = db;

    public async Task<OrderDto> Handle(CancelOrderCommand req, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Table)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.MenuItem)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Station)
            .Include(o => o.Payments.Where(p => !p.IsDeleted))
            .FirstOrDefaultAsync(o => o.Id == req.OrderId && o.BranchId == req.BranchId && !o.IsDeleted, ct)
            ?? throw new InvalidOperationException("Orden no encontrada.");

        if (order.Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("La orden ya esta cancelada.");

        if (order.Payments.Any(p => !p.IsDeleted))
            throw new InvalidOperationException("No se puede cancelar una orden con pagos registrados.");

        var activeItems = order.Items.Where(i => !i.IsDeleted && i.Status != OrderItemStatus.Cancelled).ToList();
        if (activeItems.Any(i => i.Status != OrderItemStatus.Pending))
            throw new InvalidOperationException("Solo se puede cancelar toda la orden si ningun plato ha empezado a prepararse.");

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        order.Status = OrderStatus.Cancelled;
        await StockReservationService.ReleaseOrderReservationsAsync(_db, req.BranchId, order.Id, ct);
        await _db.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        return PosMapper.MapOrder(order);
    }
}

public class CancelOrderItemCommandHandler : IRequestHandler<CancelOrderItemCommand, OrderDto>
{
    private readonly GrimorioDbContext _db;
    public CancelOrderItemCommandHandler(GrimorioDbContext db) => _db = db;

    public async Task<OrderDto> Handle(CancelOrderItemCommand req, CancellationToken ct)
    {
        var item = await _db.OrderItems
            .Include(i => i.Order)!.ThenInclude(o => o!.Payments.Where(p => !p.IsDeleted))
                .ThenInclude(p => p.Items)
            .Include(i => i.Order)!.ThenInclude(o => o!.Table)
            .Include(i => i.Order)!.ThenInclude(o => o!.Items.Where(oi => !oi.IsDeleted))
                .ThenInclude(oi => oi.TaxRate)
            .Include(i => i.Order)!.ThenInclude(o => o!.Items.Where(oi => !oi.IsDeleted))
                .ThenInclude(oi => oi.MenuItem)
            .Include(i => i.Order)!.ThenInclude(o => o!.Items.Where(oi => !oi.IsDeleted))
                .ThenInclude(oi => oi.Station)
            .Include(i => i.MenuItem)
            .Include(i => i.Station)
            .FirstOrDefaultAsync(i => i.Id == req.OrderItemId && i.BranchId == req.BranchId && !i.IsDeleted, ct)
            ?? throw new InvalidOperationException("Item de orden no encontrado.");

        var order = item.Order ?? throw new InvalidOperationException("Orden no encontrada.");
        if (order.Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("La orden ya esta cancelada.");

        if (item.Status == OrderItemStatus.Cancelled)
            throw new InvalidOperationException("El item ya esta cancelado.");

        if (item.Status != OrderItemStatus.Pending)
            throw new InvalidOperationException("Solo se puede cancelar un plato que aun no empezo a prepararse.");

        var payments = order.Payments.Where(p => !p.IsDeleted).ToList();
        if (payments.Any(p => p.Items.Count == 0))
            throw new InvalidOperationException("No se puede cancelar un item cuando existe un cobro sin detalle por item.");

        var paidQuantity = payments
            .SelectMany(p => p.Items.Where(pi => !pi.IsDeleted && pi.OrderItemId == item.Id))
            .Sum(pi => pi.Quantity);
        if (paidQuantity > 0)
            throw new InvalidOperationException("No se puede cancelar un item que ya fue cobrado.");

        var activeItemsBeforeCancel = order.Items
            .Where(i => !i.IsDeleted && i.Status != OrderItemStatus.Cancelled)
            .ToList();
        if (activeItemsBeforeCancel.Count == 1 && payments.Count > 0)
            throw new InvalidOperationException("No se puede cancelar el ultimo item activo de una orden con pagos registrados.");

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        item.Status = OrderItemStatus.Cancelled;
        await StockReservationService.ReleaseOrderItemReservationsAsync(_db, req.BranchId, item.Id, ct);

        PosMapper.RecalculateOrderTotals(order);

        var activeItems = order.Items.Where(i => !i.IsDeleted && i.Status != OrderItemStatus.Cancelled).ToList();
        if (activeItems.Count == 0)
        {
            order.Status = OrderStatus.Cancelled;
        }
        else if (activeItems.Any(i => i.Status == OrderItemStatus.InPreparation))
        {
            order.Status = OrderStatus.InPreparation;
        }
        else if (activeItems.All(i => i.Status == OrderItemStatus.Ready))
        {
            order.Status = OrderStatus.Ready;
        }
        else
        {
            order.Status = OrderStatus.Confirmed;
        }

        var paidAmount = payments.Sum(p => p.OrderAmount);
        if (paidAmount > order.Total + 0.01m)
            throw new InvalidOperationException("No se puede cancelar el item porque los pagos registrados superarian el nuevo total de la orden.");

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return PosMapper.MapOrder(order);
    }
}

public class UpdateOrderItemNotesCommandHandler : IRequestHandler<UpdateOrderItemNotesCommand, OrderItemDto>
{
    private readonly GrimorioDbContext _db;
    public UpdateOrderItemNotesCommandHandler(GrimorioDbContext db) => _db = db;

    public async Task<OrderItemDto> Handle(UpdateOrderItemNotesCommand req, CancellationToken ct)
    {
        var item = await _db.OrderItems
            .Include(i => i.MenuItem)
            .Include(i => i.Station)
            .Include(i => i.IngredientChoices.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.ChosenArticle)
            .FirstOrDefaultAsync(i => i.Id == req.OrderItemId && i.BranchId == req.BranchId && !i.IsDeleted, ct)
            ?? throw new InvalidOperationException("Item de orden no encontrado.");

        if (item.Status != OrderItemStatus.Pending)
            throw new InvalidOperationException("Solo se puede editar la observacion de un plato que aun no empezo a prepararse.");

        item.Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim();
        await _db.SaveChangesAsync(ct);

        return PosMapper.MapOrderItem(item);
    }
}

public class SetOrderItemStatusCommandHandler : IRequestHandler<SetOrderItemStatusCommand, OrderItemDto>
{
    private readonly GrimorioDbContext _db;
    public SetOrderItemStatusCommandHandler(GrimorioDbContext db) => _db = db;

    public async Task<OrderItemDto> Handle(SetOrderItemStatusCommand req, CancellationToken ct)
    {
        var item = await _db.OrderItems
            .Include(i => i.MenuItem)
            .Include(i => i.Station)
            .FirstOrDefaultAsync(i => i.Id == req.OrderItemId && i.BranchId == req.BranchId && !i.IsDeleted, ct)
            ?? throw new InvalidOperationException("Ítem de orden no encontrado.");

        if (!Enum.TryParse<OrderItemStatus>(req.Status, out var status))
            throw new InvalidOperationException($"Status no válido: {req.Status}");

        item.Status = status;

        // Update parent order state if needed
        var order = await _db.Orders
            .Include(o => o.Items.Where(i => !i.IsDeleted))
            .FirstOrDefaultAsync(o => o.Id == item.OrderId && !o.IsDeleted, ct);

        if (order != null && order.Status == OrderStatus.Confirmed)
        {
            var allItems = order.Items.Where(i => !i.IsDeleted).ToList();
            if (allItems.Any(i => i.Status == OrderItemStatus.InPreparation))
                order.Status = OrderStatus.InPreparation;
            else if (allItems.All(i => i.Status == OrderItemStatus.Ready || i.Status == OrderItemStatus.Cancelled))
                order.Status = OrderStatus.Ready;
        }

        await _db.SaveChangesAsync(ct);
        return PosMapper.MapOrderItem(item);
    }
}

// ── Mapper ────────────────────────────────────────────────────────────────────

public class ProcessAlexaKitchenCommandHandler
    : IRequestHandler<ProcessAlexaKitchenCommand, AlexaKitchenCommandResultDto>
{
    private static readonly string[] PreparationWords =
        ["preparando", "preparacion", "prepara", "preparar", "proceso", "haciendo"];

    private static readonly string[] ReadyWords =
        ["listo", "lista", "completo", "completado", "terminado", "terminada"];

    private static readonly string[] WholeOrderWords =
        ["todo", "toda", "todos", "todas", "pedido", "orden", "comanda"];

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "el", "la", "los", "las", "un", "una", "unos", "unas", "de", "del", "en",
        "para", "por", "favor", "porfa", "item", "plato", "producto", "mesa",
        "pedido", "orden", "comanda", "toda", "todo", "todos", "todas"
    };

    private readonly GrimorioDbContext _db;

    public ProcessAlexaKitchenCommandHandler(GrimorioDbContext db) => _db = db;

    public async Task<AlexaKitchenCommandResultDto> Handle(
        ProcessAlexaKitchenCommand req,
        CancellationToken ct)
    {
        var rawText = NormalizeText(req.RawText ?? string.Empty);
        var actionText = NormalizeText(req.Action ?? rawText);
        var targetStatus = ResolveStatus(actionText);
        if (targetStatus == null)
            return Fail("No entendi si quieres marcar preparando o listo.");

        var tableCode = NormalizeText(req.TableCode ?? ExtractTableCode(rawText) ?? string.Empty);
        var orderNumber = req.OrderNumber ?? ExtractOrderNumber(rawText);
        if (string.IsNullOrWhiteSpace(tableCode) && orderNumber == null)
            return Fail("No escuche la mesa o el numero de pedido.");

        var items = await _db.OrderItems
            .Where(i =>
                i.BranchId == req.BranchId &&
                !i.IsDeleted &&
                i.Status != OrderItemStatus.Cancelled &&
                i.Status != OrderItemStatus.Ready)
            .Include(i => i.MenuItem)
            .Include(i => i.Station)
            .Include(i => i.Order).ThenInclude(o => o!.Table)
            .Include(i => i.Order).ThenInclude(o => o!.Items.Where(oi => !oi.IsDeleted))
            .Include(i => i.IngredientChoices.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.ChosenArticle)
            .Where(i => i.Order != null &&
                i.Order.PaidAt == null &&
                i.Order.Status != OrderStatus.Cancelled &&
                i.Order.Status != OrderStatus.Delivered &&
                i.Order.Status != OrderStatus.Draft)
            .AsSplitQuery()
            .ToListAsync(ct);

        var candidates = items
            .Where(i => MatchesTarget(i, tableCode, orderNumber))
            .Where(i => CanAdvanceTo(i.Status, targetStatus.Value))
            .ToList();

        if (candidates.Count == 0)
            return Fail("No encontre esa mesa o pedido con platos pendientes.");

        var itemText = NormalizeText(req.ItemText ?? ExtractItemText(rawText));
        var isWholeOrder = req.AllItems ||
            string.IsNullOrWhiteSpace(itemText) ||
            WholeOrderWords.Any(w => Regex.IsMatch(itemText, $@"\b{Regex.Escape(w)}\b"));

        var selected = isWholeOrder ? candidates : MatchItems(candidates, itemText);
        if (selected.Count == 0)
            return Fail("No encontre ese plato en la mesa o pedido.");

        foreach (var item in selected)
            item.Status = targetStatus.Value;

        UpdateOrdersStatus(selected.Select(i => i.Order!).DistinctBy(o => o.Id));
        await _db.SaveChangesAsync(ct);

        var itemDtos = selected.Select(PosMapper.MapOrderItem).ToList();
        var label = BuildSuccessLabel(selected);
        var statusLabel = targetStatus == OrderItemStatus.InPreparation
            ? "en preparacion"
            : "listo";

        return new AlexaKitchenCommandResultDto
        {
            Success = true,
            Status = targetStatus.ToString(),
            UpdatedCount = selected.Count,
            Items = itemDtos,
            Message = $"Oido chef, {label} {statusLabel}.",
        };
    }

    private static AlexaKitchenCommandResultDto Fail(string message) => new()
    {
        Success = false,
        Message = message,
    };

    private static OrderItemStatus? ResolveStatus(string text)
    {
        if (PreparationWords.Any(text.Contains)) return OrderItemStatus.InPreparation;
        if (ReadyWords.Any(text.Contains)) return OrderItemStatus.Ready;
        return null;
    }

    private static bool CanAdvanceTo(OrderItemStatus current, OrderItemStatus target)
    {
        if (target == OrderItemStatus.InPreparation) return current == OrderItemStatus.Pending;
        if (target == OrderItemStatus.Ready)
            return current is OrderItemStatus.Pending or OrderItemStatus.InPreparation;
        return false;
    }

    private static bool MatchesTarget(OrderItem item, string tableCode, int? orderNumber)
    {
        var tableMatches = !string.IsNullOrWhiteSpace(tableCode) &&
            MatchesTable(NormalizeText(item.Order?.Table?.Code ?? string.Empty), tableCode);
        var orderMatches = orderNumber.HasValue && item.Order?.Number == orderNumber.Value;
        return tableMatches || orderMatches;
    }

    private static bool MatchesTable(string orderTable, string spokenTable)
    {
        if (orderTable == spokenTable) return true;
        if (orderTable == $"mesa {spokenTable}" || orderTable == $"m {spokenTable}") return true;

        var orderDigits = Regex.Match(orderTable, @"\d+").Value;
        var spokenDigits = Regex.Match(spokenTable, @"\d+").Value;
        return !string.IsNullOrWhiteSpace(orderDigits) &&
            !string.IsNullOrWhiteSpace(spokenDigits) &&
            orderDigits == spokenDigits;
    }

    private static List<OrderItem> MatchItems(List<OrderItem> candidates, string itemText)
    {
        var spokenTokens = Tokens(itemText);
        if (spokenTokens.Count == 0) return [];

        var scored = candidates
            .Select(item => new { Item = item, Score = ScoreItem(item, itemText, spokenTokens) })
            .Where(x => x.Score >= 1.5)
            .OrderByDescending(x => x.Score)
            .ToList();

        if (scored.Count == 0) return [];
        if (scored.Count > 1 && scored[0].Score - scored[1].Score < 0.75) return [];

        return [scored[0].Item];
    }

    private static double ScoreItem(OrderItem item, string spokenText, List<string> spokenTokens)
    {
        var itemName = NormalizeText(item.MenuItem?.Name ?? string.Empty);
        var choiceNames = NormalizeText(string.Join(" ",
            item.IngredientChoices
                .Where(c => !c.IsDeleted)
                .Select(c => c.ChosenArticle?.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))));
        var searchable = $"{itemName} {choiceNames}".Trim();
        var itemTokens = Tokens(searchable);
        var score = 0.0;

        if (itemName == spokenText) score += 8;
        if (searchable.Contains(spokenText) || spokenText.Contains(itemName)) score += 5;

        foreach (var token in spokenTokens)
        {
            if (itemTokens.Contains(token)) score += 2;
            else if (searchable.Contains(token)) score += 1;
        }

        if (itemTokens.Count > 0)
        {
            var matched = spokenTokens.Count(itemTokens.Contains);
            score += (double)matched / itemTokens.Count;
        }

        return score;
    }

    private static void UpdateOrdersStatus(IEnumerable<Order> orders)
    {
        foreach (var order in orders)
        {
            var activeItems = order.Items
                .Where(i => !i.IsDeleted && i.Status != OrderItemStatus.Cancelled)
                .ToList();

            if (activeItems.Count == 0) continue;
            if (activeItems.All(i => i.Status == OrderItemStatus.Ready))
                order.Status = OrderStatus.Ready;
            else if (activeItems.Any(i => i.Status == OrderItemStatus.InPreparation))
                order.Status = OrderStatus.InPreparation;
            else if (order.Status is OrderStatus.InPreparation or OrderStatus.Ready)
                order.Status = OrderStatus.Confirmed;
        }
    }

    private static string BuildSuccessLabel(List<OrderItem> items)
    {
        if (items.Count > 1)
        {
            var table = items.First().Order?.Table?.Code;
            return !string.IsNullOrWhiteSpace(table)
                ? $"pedido de {table}"
                : $"pedido #{items.First().Order?.Number}";
        }

        return items[0].MenuItem?.Name ?? "plato";
    }

    private static string? ExtractTableCode(string text)
    {
        var match = Regex.Match(text, @"\bmesas?\s*([a-z0-9]+)\b");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static int? ExtractOrderNumber(string text)
    {
        var match = Regex.Match(text, @"\b(?:pedido|orden|comanda)\s*(\d+)\b");
        return match.Success && int.TryParse(match.Groups[1].Value, out var value)
            ? value
            : null;
    }

    private static string ExtractItemText(string text)
    {
        var cleaned = Regex.Replace(text, @"\bmesas?\s*[a-z0-9]+\b", " ");
        cleaned = Regex.Replace(cleaned, @"\b(?:pedido|orden|comanda)\s*\d+\b", " ");

        return string.Join(' ', cleaned
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(word =>
                !StopWords.Contains(word) &&
                !PreparationWords.Contains(word) &&
                !ReadyWords.Contains(word)));
    }

    private static List<string> Tokens(string text) =>
        NormalizeText(text)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Length > 1 && !StopWords.Contains(word))
            .Distinct()
            .ToList();

    private static string NormalizeText(string value)
    {
        var normalized = NormalizeNumbers(value)
            .ToLowerInvariant()
            .Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }

        return Regex.Replace(
                Regex.Replace(builder.ToString().Normalize(NormalizationForm.FormC), @"[^a-z0-9\s]", " "),
                @"\s+",
                " ")
            .Trim();
    }

    private static string NormalizeNumbers(string text)
    {
        var map = new Dictionary<string, string>
        {
            ["cero"] = "0",
            ["uno"] = "1",
            ["dos"] = "2",
            ["tres"] = "3",
            ["cuatro"] = "4",
            ["cinco"] = "5",
            ["seis"] = "6",
            ["siete"] = "7",
            ["ocho"] = "8",
            ["nueve"] = "9",
            ["diez"] = "10",
            ["once"] = "11",
            ["doce"] = "12",
            ["trece"] = "13",
            ["catorce"] = "14",
            ["quince"] = "15",
            ["dieciseis"] = "16",
            ["diecisiete"] = "17",
            ["dieciocho"] = "18",
            ["diecinueve"] = "19",
            ["veinte"] = "20",
            ["veintiuno"] = "21",
            ["veintidos"] = "22",
            ["veintitres"] = "23",
            ["veinticuatro"] = "24",
            ["veinticinco"] = "25",
            ["veintiseis"] = "26",
            ["veintisiete"] = "27",
            ["veintiocho"] = "28",
            ["veintinueve"] = "29",
            ["treinta"] = "30",
        };

        var result = text.ToLowerInvariant();
        foreach (var (word, digit) in map)
            result = Regex.Replace(result, $@"\b{word}\b", digit);

        return Regex.Replace(result, @"\bveinti\s+(\d)\b", "2$1");
    }
}

internal static class PosMapper
{
    // El precio ingresado en menú es precio final al público (incluye IVA).
    // Este método extrae la base imponible y el IVA del precio inclusivo.
    public static (decimal discountAmount, decimal taxableBase, decimal taxAmount, decimal totalPrice) CalcItem(
        decimal unitPrice, int quantity, decimal discountPct, decimal? taxPct)
    {
        var gross = unitPrice * quantity;
        var discountAmount = Math.Round(gross * (discountPct / 100m), 2);
        var netInclusive = gross - discountAmount;
        decimal taxableBase, taxAmount;
        if (taxPct.HasValue && taxPct.Value > 0)
        {
            taxableBase = Math.Round(netInclusive / (1m + taxPct.Value / 100m), 2);
            taxAmount = Math.Round(netInclusive - taxableBase, 2);
        }
        else
        {
            taxableBase = netInclusive;
            taxAmount = 0m;
        }
        return (discountAmount, taxableBase, taxAmount, netInclusive);
    }

    public static void ClassifyTax(string? sriCode, decimal taxableBase, decimal taxAmt,
        ref decimal base15, ref decimal base0, ref decimal baseExempt, ref decimal iva15, ref decimal ice)
    {
        if (sriCode is "2" or "4" or "8" or "10")
        {
            base15 += taxableBase;
            iva15 += taxAmt;
        }
        else if (sriCode == "0")
        {
            base0 += taxableBase;
        }
        else
        {
            baseExempt += taxableBase;
        }
    }

    public static WorkStationDto MapWorkStation(WorkStation e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Type = e.Type.ToString(),
        IsActive = e.IsActive,
    };

    public static void RecalculateOrderTotals(Order order)
    {
        decimal subtotal = 0, discountTotal = 0;
        decimal base15 = 0, base0 = 0, baseExempt = 0, iva15 = 0, ice = 0;

        foreach (var item in order.Items.Where(i => !i.IsDeleted && i.Status != OrderItemStatus.Cancelled))
        {
            var (discountAmt, taxableBase, taxAmt, _) = CalcItem(
                item.UnitPrice,
                item.Quantity,
                item.DiscountPct,
                item.TaxRate?.Percentage);

            subtotal += item.UnitPrice * item.Quantity;
            discountTotal += discountAmt;
            ClassifyTax(item.TaxRate?.SriCode, taxableBase, taxAmt, ref base15, ref base0, ref baseExempt, ref iva15, ref ice);
        }

        order.Subtotal = subtotal;
        order.DiscountTotal = discountTotal;
        order.TaxableBase15 = base15;
        order.TaxableBase0 = base0;
        order.TaxableBaseExempt = baseExempt;
        order.Iva15 = iva15;
        order.Ice = ice;
        order.TaxAmount = iva15 + ice;
        order.Total = subtotal - discountTotal;
    }

    public static OrderDto MapOrder(Order o) => new()
    {
        Id = o.Id,
        Number = o.Number,
        Type = o.Type.ToString(),
        Status = o.Status.ToString(),
        TableId = o.TableId,
        TableCode = o.Table?.Code,
        CustomerName = o.CustomerName,
        DeliveryAddress = o.DeliveryAddress,
        Notes = o.Notes,
        Subtotal = o.Subtotal,
        DiscountTotal = o.DiscountTotal,
        TaxableBase15 = o.TaxableBase15,
        TaxableBase0 = o.TaxableBase0,
        TaxableBaseExempt = o.TaxableBaseExempt,
        Iva15 = o.Iva15,
        Ice = o.Ice,
        TaxAmount = o.TaxAmount,
        Total = o.Total,
        CreatedAt = o.CreatedAt,
        ConfirmedAt = o.ConfirmedAt,
        DeliveredAt = o.DeliveredAt,
        PaidAt = o.PaidAt,
        TotalItems = o.Items.Count(i => !i.IsDeleted),
        Items = o.Items.Where(i => !i.IsDeleted).Select(MapOrderItem).ToList(),
    };

    public static OrderItemDto MapOrderItem(OrderItem i) => new()
    {
        Id = i.Id,
        OrderId = i.OrderId,
        MenuItemId = i.MenuItemId,
        ItemName = i.MenuItem?.Name ?? string.Empty,
        ItemCode = i.MenuItem?.InternalCode,
        StationId = i.StationId,
        StationName = i.Station?.Name,
        Quantity = i.Quantity,
        UnitPrice = i.UnitPrice,
        DiscountPct = i.DiscountPct,
        DiscountAmount = i.DiscountAmount,
        TaxRateId = i.TaxRateId,
        TaxRateName = i.TaxRate?.Name,
        TaxRatePercentage = i.TaxRate?.Percentage,
        TaxAmount = i.TaxAmount,
        TotalPrice = i.TotalPrice,
        Notes = i.Notes,
        Status = i.Status.ToString(),
        IngredientChoices = i.IngredientChoices
            .Where(c => !c.IsDeleted)
            .Select(c => new IngredientChoiceDto
            {
                RecipeIngredientId = c.RecipeIngredientId,
                ChosenArticleId = c.ChosenArticleId,
                ChosenArticleName = c.ChosenArticle?.Name ?? string.Empty,
            }).ToList(),
    };
}
