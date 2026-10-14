using System.Text.Json;
using Grimorio.Application.DTOs;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Features.Menu;
using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.POS;

internal static class ComboPreparation
{
    public static async Task InitializeAsync(GrimorioDbContext db, Guid branchId,
        IReadOnlyCollection<OrderItem> orderItems, CancellationToken ct)
    {
        var ids = orderItems.Select(x => x.MenuItemId).Distinct().ToList();
        var comboIds = await db.MenuItems.Where(x => x.BranchId == branchId && ids.Contains(x.Id) && x.IsCombo)
            .Select(x => x.Id).ToListAsync(ct);
        if (comboIds.Count == 0) return;
        var combos = await MenuRecipeExpansion.LoadMenuItemsWithRecipeAsync(db, branchId, comboIds, ct);
        var conversions = await db.UnitConversions.Where(x => x.BranchId == branchId && !x.IsDeleted)
            .Select(x => new UnitConversionInfo(x.OriginUnitId, x.DestinationUnitId, x.Factor)).ToListAsync(ct);
        foreach (var orderItem in orderItems.Where(x => comboIds.Contains(x.MenuItemId)))
        {
            if (orderItem.Quantity <= 0) throw new InvalidOperationException("La cantidad del combo debe ser mayor a cero.");
            if (orderItem.PreparationTasks.Count > 0) continue;
            var combo = combos.Single(x => x.Id == orderItem.MenuItemId);
            if (!combo.IsActive || !combo.AvailableForSale || combo.ComboComponents.Count == 0)
                throw new InvalidOperationException("El combo no esta disponible o no tiene componentes.");
            orderItem.StationId = null;
            foreach (var component in combo.ComboComponents)
            {
                var child = component.ComponentItem;
                if (child is null || child.BranchId != branchId || child.IsCombo || !child.IsActive || !child.AvailableForSale
                    || child.Station is null || child.Station.IsDeleted || !child.Station.IsActive || child.Station.BranchId != branchId
                    || child.ModifierGroups.Any(x => x.IsRequired || x.MinSelections > 0))
                    throw new InvalidOperationException("El combo tiene componentes no disponibles, sin estacion activa o con opciones obligatorias.");
                var requirements = MenuRecipeExpansion.Expand(child, 1, conversions);
                if (requirements.Any(x => x.BaseQuantity <= 0))
                    throw new InvalidOperationException($"Revisa las conversiones de unidades de {child.Name} antes de vender el combo.");
                orderItem.PreparationTasks.Add(new OrderItemPreparationTask
                {
                    BranchId = branchId, OrderItemId = orderItem.Id, ComponentItemId = child.Id,
                    StationId = child.StationId!.Value, ComponentName = child.Name, ComboName = combo.Name,
                    QuantityPerCombo = component.Quantity, RecipeSnapshot = JsonSerializer.Serialize(requirements),
                });
            }
        }
    }

    public static OrderItemDto MapTask(OrderItemPreparationTask task, OrderItemDto parent) => new()
    {
        Id = task.Id, OrderId = parent.OrderId, MenuItemId = task.ComponentItemId,
        ItemName = $"{task.ComponentName} - {task.ComboName}", StationId = task.StationId,
        Quantity = checked(parent.Quantity * task.QuantityPerCombo),
        Status = parent.Status == "Cancelled" ? "Cancelled" : task.Status.ToString(),
        Notes = parent.Notes, IsTakeout = parent.IsTakeout,
    };

    public static async Task ValidateStationsAsync(GrimorioDbContext db, Guid branchId,
        IReadOnlyCollection<OrderItem> items, CancellationToken ct)
    {
        var comboIds = items.Where(i => i.PreparationTasks.Count > 0).Select(i => i.MenuItemId).Distinct().ToList();
        var componentIds = items.SelectMany(i => i.PreparationTasks).Select(t => t.ComponentItemId).Distinct().ToList();
        var requiredIds = comboIds.Concat(componentIds).Distinct().ToList();
        if (requiredIds.Count > 0 && await db.MenuItems.CountAsync(i => requiredIds.Contains(i.Id)
            && i.BranchId == branchId && i.IsActive && i.AvailableForSale && !i.IsDeleted, ct) != requiredIds.Count)
            throw new InvalidOperationException("El combo o uno de sus componentes ya no esta disponible. Revisa el pedido antes de confirmarlo.");
        var stationIds = items.SelectMany(i => i.PreparationTasks).Select(t => t.StationId).Distinct().ToList();
        if (stationIds.Count > 0 && await db.WorkStations.CountAsync(s => stationIds.Contains(s.Id)
            && s.BranchId == branchId && s.IsActive && !s.IsDeleted, ct) != stationIds.Count)
            throw new InvalidOperationException("Una estacion del combo ya no esta activa. Revisa el pedido antes de enviarlo a preparar.");
    }

    public static void RecalculateItem(OrderItem item)
    {
        var tasks = item.PreparationTasks.Where(x => !x.IsDeleted).ToList();
        if (tasks.Count == 0 || item.Status == OrderItemStatus.Cancelled) return;
        item.Status = tasks.All(x => x.Status == OrderItemStatus.Ready) ? OrderItemStatus.Ready
            : tasks.Any(x => x.Status is OrderItemStatus.InPreparation or OrderItemStatus.Ready) ? OrderItemStatus.InPreparation
            : OrderItemStatus.Pending;
    }

    public static IQueryable<OrderItemPreparationTask> VoiceTasks(GrimorioDbContext db, Guid branchId) =>
        db.OrderItemPreparationTasks.Where(t => t.BranchId == branchId && !t.IsDeleted
            && !t.OrderItem!.IsDeleted && t.OrderItem.Status != OrderItemStatus.Cancelled
            && !t.OrderItem.Order!.IsDeleted && t.OrderItem.Order.Status != OrderStatus.Draft
            && t.OrderItem.Order.Status != OrderStatus.Cancelled && t.OrderItem.Order.Status != OrderStatus.Delivered)
            .Include(t => t.Station)
            .Include(t => t.OrderItem)!.ThenInclude(i => i!.Order)!.ThenInclude(o => o!.Table)
            .Include(t => t.OrderItem)!.ThenInclude(i => i!.Order)!.ThenInclude(o => o!.Items.Where(i => !i.IsDeleted));

    public static OrderItem VoiceItem(OrderItemPreparationTask task) => new()
    {
        Id = task.Id, BranchId = task.BranchId, OrderId = task.OrderItem!.OrderId, Order = task.OrderItem.Order,
        MenuItemId = task.ComponentItemId, MenuItem = new Domain.Entities.Menu.MenuItem { Name = $"{task.ComponentName} - {task.ComboName}" },
        StationId = task.StationId, Station = task.Station, Quantity = checked(task.QuantityPerCombo * task.OrderItem.Quantity),
        Status = task.Status, Notes = task.OrderItem.Notes, IsTakeout = task.OrderItem.IsTakeout,
    };
}
