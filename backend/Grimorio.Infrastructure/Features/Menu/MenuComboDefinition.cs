using Grimorio.Application.DTOs;
using Grimorio.Domain.Entities.Menu;
using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Grimorio.Infrastructure.Features.POS.Commands;
using Grimorio.Domain.Entities.POS;

namespace Grimorio.Infrastructure.Features.Menu;

internal static class MenuComboDefinition
{
    public static async Task ApplyAsync(GrimorioDbContext db, MenuItem item, bool isCombo,
        List<UpsertMenuComboComponentDto>? components, CancellationToken ct)
    {
        if (components is null && item.IsCombo == isCombo)
        {
            if (isCombo) item.StationId = null;
            return;
        }
        if (item.IsCombo != isCombo && await db.OrderItems.AnyAsync(x => x.MenuItemId == item.Id, ct))
            throw new InvalidOperationException("Un item que ya tiene pedidos no puede cambiar de plato a combo o viceversa. Crea un item nuevo.");
        var requested = components ?? [];
        if (!isCombo && requested.Count > 0)
            throw new InvalidOperationException("Un plato individual no puede tener componentes de combo.");
        if (isCombo)
        {
            if (!await db.MenuCategories.AnyAsync(x => x.Id == item.MenuCategoryId && x.BranchId == item.BranchId && !x.IsDeleted, ct))
                throw new InvalidOperationException("La categoria del combo no pertenece a esta sucursal.");
            if (requested.Count is < 1 or > 20 || requested.Any(x => x.Quantity is < 1 or > 100)
                || requested.Select(x => x.ComponentItemId).Distinct().Count() != requested.Count
                || requested.Any(x => x.ComponentItemId == item.Id))
                throw new InvalidOperationException("Selecciona entre 1 y 20 componentes distintos, con cantidades de 1 a 100. Un combo no puede incluirse a si mismo.");
            if (await db.RecipeIngredients.AnyAsync(x => x.MenuItemId == item.Id && !x.IsDeleted, ct))
                throw new InvalidOperationException("Retira la receta propia antes de convertir el plato en combo; se utilizaran las recetas de sus componentes.");
            if (await db.MenuItemModifierGroups.AnyAsync(x => x.MenuItemId == item.Id && x.IsActive && !x.IsDeleted, ct))
                throw new InvalidOperationException("Retira los modificadores propios antes de convertir el plato en combo.");
            if (await db.MenuComboComponents.AnyAsync(x => x.ComponentItemId == item.Id && !x.IsDeleted, ct))
                throw new InvalidOperationException("Este plato forma parte de otro combo. No se permiten combos anidados.");
        }
        var ids = requested.Select(x => x.ComponentItemId).ToList();
        var children = await db.MenuItems.Include(x => x.Station)
            .Include(x => x.ModifierGroups.Where(g => !g.IsDeleted && g.IsActive))
                .ThenInclude(g => g.Options.Where(o => !o.IsDeleted && o.IsActive))
            .Where(x => ids.Contains(x.Id) && x.BranchId == item.BranchId && !x.IsDeleted).ToDictionaryAsync(x => x.Id, ct);
        foreach (var component in requested)
        {
            if (!children.TryGetValue(component.ComponentItemId, out var child) || child.IsCombo || !child.IsActive
                || !child.AvailableForSale || child.Station is null || !child.Station.IsActive || child.Station.BranchId != item.BranchId)
                throw new InvalidOperationException("Cada componente debe ser un plato activo y disponible de la sucursal, con una estacion activa asignada.");
            ResolveSelections(child, component.ModifierSelections, component.SelectableModifierGroupIds);
        }
        var existing = await db.MenuComboComponents.Where(x => x.ComboId == item.Id && !x.IsDeleted).ToListAsync(ct);
        foreach (var old in existing) old.IsDeleted = !ids.Contains(old.ComponentItemId);
        foreach (var component in requested)
        {
            var row = existing.FirstOrDefault(x => x.ComponentItemId == component.ComponentItemId);
            if (row is null)
            {
                row = new MenuComboComponent { BranchId = item.BranchId, ComboId = item.Id, ComponentItemId = component.ComponentItemId };
                item.ComboComponents.Add(row);
                db.MenuComboComponents.Add(row);
            }
            row.Quantity = component.Quantity;
            row.ModifierSelections = component.ModifierSelections;
            row.SelectableModifierGroupIds = component.SelectableModifierGroupIds;
            row.ComponentItem = children[component.ComponentItemId];
        }
        item.IsCombo = isCombo;
        if (isCombo) item.StationId = null;
    }

    public static List<MenuComboComponentDto> Map(MenuItem item) => (item.LoadedComboDefinition ?? item).ComboComponents.Where(x => !x.IsDeleted)
        .Select(x => new MenuComboComponentDto
        {
            ComponentItemId = x.ComponentItemId, Quantity = x.Quantity,
            ModifierSelections = x.ModifierSelections,
            SelectableModifierGroupIds = x.SelectableModifierGroupIds,
            Name = x.ComponentItem?.Name ?? string.Empty, GeneralName = x.ComponentItem?.GeneralName ?? string.Empty,
            StationId = x.ComponentItem?.StationId, StationName = x.ComponentItem?.Station?.Name,
        }).ToList();

    internal static List<OrderItemModifierSelection> ResolveSelections(MenuItem item, List<FixedComboModifier> selections,
        List<Guid>? selectableGroups = null)
    {
        var pending = (selectableGroups ?? []).ToHashSet();
        if (pending.Any(id => !item.ModifierGroups.Any(g => g.Id == id && g.IsActive && !g.IsDeleted))
            || item.ModifierGroups.Where(g => pending.Contains(g.Id)).SelectMany(g => g.Options)
                .Any(o => selections.Any(s => s.ModifierOptionId == o.Id)))
            throw new InvalidOperationException("Un grupo debe estar fijado o a eleccion del cliente, no ambos.");
        if (selections.Count > 100 || selections.Any(x => x.Quantity is < 1 or > 100))
            throw new InvalidOperationException("Revisa las cantidades de las opciones fijadas del combo.");
        return PosMapper.BuildModifierSelections(item.BranchId, new CreateOrderItemDto
        {
            MenuItemId = item.Id, Quantity = 1,
            ModifierSelections = selections.Select(x => new CreateModifierSelectionDto
                { ModifierOptionId = x.ModifierOptionId, Quantity = x.Quantity }).ToList(),
        }, item, pending);
    }
}
