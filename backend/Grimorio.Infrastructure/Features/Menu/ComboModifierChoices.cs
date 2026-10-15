using Grimorio.Application.DTOs;
using Grimorio.Domain.Entities.Menu;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Features.POS.Commands;
using Grimorio.Infrastructure.Persistence;

namespace Grimorio.Infrastructure.Features.Menu;

internal static class ComboModifierChoices
{
    public static async Task LoadAsync(GrimorioDbContext db, Guid branchId, IEnumerable<MenuItem> items, CancellationToken ct)
    {
        var combos = items.Where(i => i.IsCombo).ToList();
        if (combos.Count == 0) return;
        var loaded = await MenuRecipeExpansion.LoadMenuItemsWithRecipeAsync(db, branchId, combos.Select(i => i.Id).ToList(), ct);
        foreach (var item in combos) item.LoadedComboDefinition = loaded.Single(i => i.Id == item.Id);
    }

    public static IEnumerable<MenuItemModifierGroup> Groups(MenuItem item)
    {
        if (!item.IsCombo) return item.ModifierGroups.Where(g => !g.IsDeleted && g.IsActive);
        return (item.LoadedComboDefinition ?? item).ComboComponents.Where(c => !c.IsDeleted && c.ComponentItem != null)
            .SelectMany(component => component.ComponentItem!.ModifierGroups.Where(g => !g.IsDeleted && g.IsActive
                && component.SelectableModifierGroupIds.Contains(g.Id)).Select(group => new MenuItemModifierGroup
            {
                Id = group.Id, BranchId = group.BranchId, MenuItemId = item.Id,
                Name = $"{component.ComponentItem.Name} / {group.Name}", MinSelections = group.MinSelections,
                MaxSelections = group.MaxSelections, IsRequired = group.IsRequired, AllowDuplicates = group.AllowDuplicates,
                DisplayOrder = group.DisplayOrder, IsActive = true,
                Options = group.Options.Where(o => !o.IsDeleted && o.IsActive).Select(option => new MenuItemModifierOption
                {
                    Id = option.Id, ModifierGroupId = group.Id, BranchId = option.BranchId, Name = option.Name,
                    ArticleId = option.ArticleId, Article = option.Article, UnitId = option.UnitId, Unit = option.Unit,
                    Quantity = EffectiveQuantity(group, option, component) * component.Quantity, PriceDelta = 0, DisplayOrder = option.DisplayOrder,
                    AdditionalIngredients = option.AdditionalIngredients.Select(i => i with { Quantity = i.Quantity * component.Quantity }).ToList(),
                    ConsumptionArticles = option.ConsumptionArticles,
                }).ToList(),
            }));
    }

    private static decimal EffectiveQuantity(MenuItemModifierGroup group, MenuItemModifierOption option, MenuComboComponent component)
    {
        if (!group.QuantitySourceGroupId.HasValue) return option.Quantity;
        var source = component.ComponentItem!.ModifierGroups.FirstOrDefault(g => g.Id == group.QuantitySourceGroupId);
        var sourceIds = source?.Options.Select(o => o.Id).ToHashSet() ?? [];
        var fixedSource = component.ModifierSelections.FirstOrDefault(s => sourceIds.Contains(s.ModifierOptionId));
        return fixedSource is not null ? group.QuantityRules.Single(r => r.SourceOptionId == fixedSource.ModifierOptionId).Quantity
            : group.QuantityRules.Select(r => r.Quantity).DefaultIfEmpty(option.Quantity).Min();
    }

    public static List<OrderItemModifierSelection> Resolve(MenuComboComponent component, IEnumerable<CreateModifierSelectionDto> requested)
    {
        var child = component.ComponentItem ?? throw new InvalidOperationException("Componente no disponible.");
        var visibleIds = child.ModifierGroups.Where(g => component.SelectableModifierGroupIds.Contains(g.Id))
            .SelectMany(g => g.Options).Select(o => o.Id).ToHashSet();
        return PosMapper.BuildModifierSelections(child.BranchId, new CreateOrderItemDto
        {
            MenuItemId = child.Id, Quantity = 1,
            ModifierSelections = component.ModifierSelections.Select(s => new CreateModifierSelectionDto
                { ModifierOptionId = s.ModifierOptionId, Quantity = s.Quantity })
                .Concat(requested.Where(s => visibleIds.Contains(s.ModifierOptionId))).ToList(),
        }, child);
    }

    public static List<OrderItemModifierSelection> BuildSelections(MenuItem item, List<CreateModifierSelectionDto> requested)
    {
        var definition = item.LoadedComboDefinition ?? item;
        var visibleIds = Groups(item).SelectMany(g => g.Options).Select(o => o.Id).ToHashSet();
        if (requested.Any(s => !visibleIds.Contains(s.ModifierOptionId)))
            throw new InvalidOperationException("La opcion no corresponde a una eleccion del cliente en este combo.");
        var resolved = definition.ComboComponents.Where(c => !c.IsDeleted).SelectMany(c => Resolve(c, requested)).ToList();
        var requestedIds = requested.Select(s => s.ModifierOptionId).ToHashSet();
        foreach (var selection in resolved.Where(s => requestedIds.Contains(s.ModifierOptionId)))
        {
            // Consumption belongs to the component snapshot, not to the priced parent line.
            selection.UnitPriceDelta = 0;
            selection.ArticleId = null;
            selection.UnitId = null;
            selection.InventoryQuantity = 0;
            selection.AdditionalIngredientsJson = "[]";
        }
        return resolved.Where(s => requestedIds.Contains(s.ModifierOptionId)).ToList();
    }
}
