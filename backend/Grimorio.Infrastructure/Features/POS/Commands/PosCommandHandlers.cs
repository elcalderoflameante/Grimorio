using Grimorio.Application.DTOs;
using Grimorio.Application.Features.POS.Commands;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.POS.Commands;

// â”€â”€ Estaciones â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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

// â”€â”€ Mesas posiciÃ³n â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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

// â”€â”€ Ã“rdenes â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly GrimorioDbContext _db;
    public CreateOrderCommandHandler(GrimorioDbContext db) => _db = db;

    public async Task<OrderDto> Handle(CreateOrderCommand req, CancellationToken ct)
    {
        if (!Enum.TryParse<OrderType>(req.Type, out var orderType))
            throw new InvalidOperationException($"Type de orden no vÃ¡lido: {req.Type}");

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
            .ToListAsync(ct);

        decimal subtotal = 0;
        foreach (var itemDto in req.Items)
        {
            var menuItem = menuItems.FirstOrDefault(m => m.Id == itemDto.MenuItemId)
                ?? throw new InvalidOperationException($"Ãtem no encontrado: {itemDto.MenuItemId}");

            var totalPrice = menuItem.Price * itemDto.Quantity;
            subtotal += totalPrice;

            order.Items.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                BranchId = req.BranchId,
                MenuItemId = itemDto.MenuItemId,
                StationId = menuItem.StationId,
                Quantity = itemDto.Quantity,
                UnitPrice = menuItem.Price,
                TotalPrice = totalPrice,
                Notes = itemDto.Notes?.Trim(),
                Status = OrderItemStatus.Pending,
            });
        }

        order.Subtotal = subtotal;
        order.Total = subtotal;

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
            .FirstOrDefaultAsync(o => o.Id == req.OrderId && o.BranchId == req.BranchId && !o.IsDeleted, ct)
            ?? throw new InvalidOperationException("Order no encontrada.");

        if (order.Status != OrderStatus.Draft)
            throw new InvalidOperationException("Solo se pueden modificar Ã³rdenes en borrador.");

        // Soft-delete existing items
        foreach (var item in order.Items.Where(i => !i.IsDeleted))
            item.IsDeleted = true;

        var itemMenuIds = req.Items.Select(i => i.MenuItemId).Distinct().ToList();
        var menuItems = await _db.MenuItems
            .Where(m => itemMenuIds.Contains(m.Id) && !m.IsDeleted)
            .Include(m => m.Station)
            .ToListAsync(ct);

        decimal subtotal = 0;
        foreach (var itemDto in req.Items)
        {
            var menuItem = menuItems.FirstOrDefault(m => m.Id == itemDto.MenuItemId)
                ?? throw new InvalidOperationException($"Ãtem no encontrado: {itemDto.MenuItemId}");

            var totalPrice = menuItem.Price * itemDto.Quantity;
            subtotal += totalPrice;

            var newItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                BranchId = req.BranchId,
                OrderId = order.Id,
                MenuItemId = itemDto.MenuItemId,
                StationId = menuItem.StationId,
                Quantity = itemDto.Quantity,
                UnitPrice = menuItem.Price,
                TotalPrice = totalPrice,
                Notes = itemDto.Notes?.Trim(),
                Status = OrderItemStatus.Pending,
            };
            _db.OrderItems.Add(newItem);
        }

        order.Subtotal = subtotal;
        order.Total = subtotal;
        await _db.SaveChangesAsync(ct);

        var updated = await _db.Orders
            .Include(o => o.Table)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.MenuItem)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Station)
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
            ?? throw new InvalidOperationException("Order no encontrada.");

        if (order.Status != OrderStatus.Draft)
            throw new InvalidOperationException("La orden ya fue confirmada.");

        if (!order.Items.Any(i => !i.IsDeleted))
            throw new InvalidOperationException("La orden no tiene Ã­tems.");

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
            ?? throw new InvalidOperationException("Order no encontrada.");

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
            ?? throw new InvalidOperationException("Order no encontrada.");

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
            ?? throw new InvalidOperationException("Ãtem de orden no encontrado.");

        if (!Enum.TryParse<OrderItemStatus>(req.Status, out var status))
            throw new InvalidOperationException($"Status no vÃ¡lido: {req.Status}");

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

// â”€â”€ Mapper â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

internal static class PosMapper
{
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
        MenuItemId = i.MenuItemId,
        ItemName = i.MenuItem?.Name ?? string.Empty,
        ItemCode = i.MenuItem?.InternalCode,
        StationId = i.StationId,
        StationName = i.Station?.Name,
        Quantity = i.Quantity,
        UnitPrice = i.UnitPrice,
        TotalPrice = i.TotalPrice,
        Notes = i.Notes,
        Status = i.Status.ToString(),
    };
}

