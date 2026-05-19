using Grimorio.Application.DTOs;
using Grimorio.Application.Features.POS.Commands;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
            .FirstOrDefaultAsync(o => o.Id == req.OrderId && o.BranchId == req.BranchId && !o.IsDeleted, ct)
            ?? throw new InvalidOperationException("Orden no encontrada.");

        if (order.Status != OrderStatus.Draft)
            throw new InvalidOperationException("La orden ya fue confirmada.");

        if (!order.Items.Any(i => !i.IsDeleted))
            throw new InvalidOperationException("La orden no tiene ítems.");

        order.Status = OrderStatus.Confirmed;
        order.ConfirmedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
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
            .FirstOrDefaultAsync(o => o.Id == req.OrderId && o.BranchId == req.BranchId && !o.IsDeleted, ct)
            ?? throw new InvalidOperationException("Orden no encontrada.");

        order.Status = OrderStatus.Cancelled;
        await _db.SaveChangesAsync(ct);
        return PosMapper.MapOrder(order);
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
        if (sriCode == "10")
        {
            base15 += taxableBase;
            iva15 += taxAmt;
        }
        else if (sriCode == "0" || sriCode == "8")
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

    public static OrderDto MapOrder(Order o) => new()
    {
        Id = o.Id,
        Number = o.Number,
        Type = o.Type.ToString(),
        Status = o.Status.ToString(),
        TableId = o.TableId,
        TableCode = o.Table?.Code,
        TableName = o.Table?.Name,
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
