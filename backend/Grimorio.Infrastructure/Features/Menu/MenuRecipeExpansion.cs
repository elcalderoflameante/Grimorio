using Grimorio.Domain.Entities.Inventory;
using Grimorio.Domain.Entities.Menu;
using Grimorio.Domain.Entities.POS;
using System.Text.Json;
using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Menu;

internal sealed record MenuRecipeRequirement(
    Guid? RecipeIngredientId,
    Guid ArticleId,
    string ArticleName,
    string? InternalCode,
    Guid UnitId,
    string UnitSymbol,
    Guid BaseUnitId,
    string BaseUnitSymbol,
    decimal Quantity,
    decimal BaseQuantity,
    string? SourceName);

internal static class MenuRecipeExpansion
{
    public static async Task<List<MenuItem>> LoadMenuItemsWithRecipeAsync(
        GrimorioDbContext db,
        Guid branchId,
        IReadOnlyCollection<Guid> menuItemIds,
        CancellationToken ct)
    {
        var items = await db.MenuItems
            .AsNoTracking()
            .Include(x => x.Station)
            .Include(x => x.ComboComponents.Where(c => !c.IsDeleted)).ThenInclude(c => c.ComponentItem)
            .Include(x => x.ModifierGroups.Where(g => !g.IsDeleted && g.IsActive))
                .ThenInclude(g => g.Options.Where(o => !o.IsDeleted && o.IsActive))
                    .ThenInclude(o => o.Unit)
            .Include(x => x.Category)
            .Include(x => x.TaxRate)
            .Include(x => x.Recipe.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.Article)
                    .ThenInclude(a => a!.BaseUnit)
            .Include(x => x.Recipe.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.SubRecipe)
                    .ThenInclude(s => s!.OutputUnit)
            .Include(x => x.Recipe.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.SubRecipe)
                    .ThenInclude(s => s!.Ingredients.Where(i => !i.IsDeleted))
                        .ThenInclude(i => i.Article)
                            .ThenInclude(a => a!.BaseUnit)
            .Include(x => x.Recipe.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.SubRecipe)
                    .ThenInclude(s => s!.Ingredients.Where(i => !i.IsDeleted))
                        .ThenInclude(i => i.Unit)
            .Include(x => x.Recipe.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.Unit)
            .Where(x => x.BranchId == branchId && menuItemIds.Contains(x.Id) && !x.IsDeleted)
            .AsSplitQuery()
            .ToListAsync(ct);
        var options = items.SelectMany(x => x.ModifierGroups).SelectMany(g => g.Options).ToList();
        var optionArticleIds = options.SelectMany(ModifierIngredients.ForOption).Select(x => x.ArticleId).Distinct().ToList();
        if (optionArticleIds.Count > 0)
        {
            var articles = await db.InventoryArticles.AsNoTracking().Include(x => x.BaseUnit)
                .Where(x => x.BranchId == branchId && !x.IsDeleted && optionArticleIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, ct);
            foreach (var option in options) option.ConsumptionArticles = articles;
        }
        var components = items.Where(x => x.IsCombo).SelectMany(x => x.ComboComponents).ToList();
        if (components.Any(x => x.ComponentItem is null || x.ComponentItem.IsCombo))
            throw new InvalidOperationException("El combo contiene un plato eliminado o un combo anidado.");
        if (components.Count > 0)
        {
            var children = await LoadMenuItemsWithRecipeAsync(db, branchId, components.Select(x => x.ComponentItemId).Distinct().ToList(), ct);
            var byId = children.ToDictionary(x => x.Id);
            foreach (var component in components) component.ComponentItem = byId.GetValueOrDefault(component.ComponentItemId);
        }
        return items;
    }

    public static List<MenuRecipeRequirement> Expand(
        MenuItem item,
        decimal itemQuantity,
        IReadOnlyCollection<UnitConversionInfo> conversions)
    {
        var result = new List<MenuRecipeRequirement>();

        if (item.IsCombo)
        {
            foreach (var component in item.ComboComponents.Where(x => !x.IsDeleted))
            {
                if (component.ComponentItem is null || component.ComponentItem.IsCombo)
                    throw new InvalidOperationException("Componente de combo no valido.");
                result.AddRange(ExpandComponent(component, itemQuantity * component.Quantity, conversions)
                    .Select(x => x with { SourceName = component.ComponentItem.Name }));
            }
            return result;
        }

        foreach (var ingredient in item.Recipe.Where(r => !r.IsDeleted))
        {
            if (ingredient.Type == RecipeIngredientType.SubRecipe)
            {
                AddSubRecipeRequirements(result, ingredient, itemQuantity, conversions);
                continue;
            }

            if (ingredient.ArticleId is null || ingredient.Article is null) continue;
            var article = ingredient.Article;
            var baseQuantity = ConvertQuantity(
                ingredient.Quantity * itemQuantity,
                ingredient.UnitId,
                article.BaseUnitId,
                conversions);

            result.Add(new MenuRecipeRequirement(
                ingredient.Id,
                article.Id,
                article.Name,
                article.InternalCode,
                ingredient.UnitId,
                ingredient.Unit?.Symbol ?? string.Empty,
                article.BaseUnitId,
                article.BaseUnit?.Symbol ?? string.Empty,
                ingredient.Quantity * itemQuantity,
                baseQuantity,
                null));
        }

        return result;
    }

    public static List<MenuRecipeRequirement> ExpandComponent(MenuComboComponent component, decimal quantity,
        IReadOnlyCollection<UnitConversionInfo> conversions)
    {
        var child = component.ComponentItem ?? throw new InvalidOperationException("Componente de combo no disponible.");
        if (!child.IsActive || !child.AvailableForSale || child.IsDeleted || child.Station is null
            || !child.Station.IsActive || child.Station.IsDeleted || child.Station.BranchId != child.BranchId)
            throw new InvalidOperationException($"El componente {child.Name} o su estacion no esta disponible.");
        var selections = MenuComboDefinition.ResolveSelections(child, component.ModifierSelections);
        var requirements = Expand(child, quantity, conversions);
        var options = child.ModifierGroups.SelectMany(g => g.Options).ToDictionary(o => o.Id);
        foreach (var selection in selections)
        {
            var option = options[selection.ModifierOptionId];
            foreach (var ingredient in ModifierIngredients.ForOption(option))
            {
                if (!option.ConsumptionArticles.TryGetValue(ingredient.ArticleId, out var article) || !article.IsActive)
                    throw new InvalidOperationException($"Revisa el articulo de inventario de {child.Name}: {selection.OptionName}.");
                var consumed = ingredient.Quantity * selection.Quantity * quantity;
                var baseQuantity = ConvertQuantity(consumed, ingredient.UnitId, article.BaseUnitId, conversions);
                if (baseQuantity <= 0)
                    throw new InvalidOperationException($"No existe conversion de unidad para {article.Name}.");
                requirements.Add(new MenuRecipeRequirement(null, article.Id, article.Name, article.InternalCode,
                    article.BaseUnitId, article.BaseUnit?.Symbol ?? "", article.BaseUnitId,
                    article.BaseUnit?.Symbol ?? "", baseQuantity, baseQuantity, selection.OptionName));
            }
        }
        return requirements;
    }

    public static List<MenuRecipeRequirement> ExpandForDisplay(MenuItem item, decimal quantity,
        IReadOnlyCollection<UnitConversionInfo> conversions)
    {
        try { return Expand(item, quantity, conversions); }
        catch (InvalidOperationException ex) when (item.IsCombo)
        {
            return [new MenuRecipeRequirement(null, Guid.Empty, "Revisar opciones del combo", null,
                Guid.Empty, "", Guid.Empty, "", 0, 0, ex.Message)];
        }
    }

    public static IEnumerable<Guid> ReferencedArticleIds(MenuItem item) =>
        item.Recipe.Where(r => !r.IsDeleted).SelectMany(r => r.ArticleId.HasValue ? new[] { r.ArticleId.Value }
            : r.SubRecipe?.Ingredients.Where(i => !i.IsDeleted).Select(i => i.ArticleId) ?? [])
        .Concat(item.ModifierGroups.Where(g => !g.IsDeleted && g.IsActive).SelectMany(g => g.Options
            .Where(o => !o.IsDeleted && o.IsActive).SelectMany(ModifierIngredients.ForOption).Select(i => i.ArticleId)))
        .Concat(item.ComboComponents.Where(c => !c.IsDeleted && c.ComponentItem != null).SelectMany(c => ReferencedArticleIds(c.ComponentItem!)));

    public static List<MenuRecipeRequirement> ExpandOrderItem(OrderItem item, decimal quantity,
        IReadOnlyCollection<UnitConversionInfo> conversions, MenuItem? loadedMenuItem = null)
    {
        var tasks = item.PreparationTasks.Where(x => !x.IsDeleted).ToList();
        if (tasks.Count == 0)
            return (loadedMenuItem ?? item.MenuItem) is { } menuItem ? Expand(menuItem, quantity, conversions) : [];
        return tasks.SelectMany(task => (JsonSerializer.Deserialize<List<MenuRecipeRequirement>>(task.RecipeSnapshot) ?? [])
            .Select(x => x with { Quantity = x.Quantity * quantity * task.QuantityPerCombo,
                BaseQuantity = x.BaseQuantity * quantity * task.QuantityPerCombo, SourceName = task.ComponentName })).ToList();
    }

    public static decimal ConvertQuantity(
        decimal quantity,
        Guid originUnitId,
        Guid destinationUnitId,
        IEnumerable<UnitConversionInfo> conversions)
    {
        if (originUnitId == Guid.Empty || destinationUnitId == Guid.Empty) return 0m;
        if (originUnitId == destinationUnitId) return quantity;

        var direct = conversions.FirstOrDefault(x => x.OriginUnitId == originUnitId && x.DestinationUnitId == destinationUnitId);
        if (direct is not null) return quantity * direct.Factor;

        var reverse = conversions.FirstOrDefault(x => x.OriginUnitId == destinationUnitId && x.DestinationUnitId == originUnitId);
        if (reverse is not null && reverse.Factor != 0) return quantity / reverse.Factor;

        return 0m;
    }

    private static void AddSubRecipeRequirements(
        List<MenuRecipeRequirement> result,
        RecipeIngredient recipeIngredient,
        decimal itemQuantity,
        IReadOnlyCollection<UnitConversionInfo> conversions)
    {
        var subRecipe = recipeIngredient.SubRecipe;
        if (subRecipe is null || subRecipe.IsDeleted || !subRecipe.IsActive || subRecipe.OutputQuantity <= 0) return;

        var requestedOutput = ConvertQuantity(
            recipeIngredient.Quantity * itemQuantity,
            recipeIngredient.UnitId,
            subRecipe.OutputUnitId,
            conversions);
        if (requestedOutput <= 0) return;

        var factor = requestedOutput / subRecipe.OutputQuantity;
        foreach (var ingredient in subRecipe.Ingredients.Where(i => !i.IsDeleted))
        {
            if (ingredient.Article is null) continue;
            var article = ingredient.Article;
            var quantity = ingredient.Quantity * factor;
            var baseQuantity = ConvertQuantity(quantity, ingredient.UnitId, article.BaseUnitId, conversions);
            result.Add(new MenuRecipeRequirement(
                recipeIngredient.Id,
                article.Id,
                article.Name,
                article.InternalCode,
                ingredient.UnitId,
                ingredient.Unit?.Symbol ?? string.Empty,
                article.BaseUnitId,
                article.BaseUnit?.Symbol ?? string.Empty,
                quantity,
                baseQuantity,
                subRecipe.Name));
        }
    }
}

internal sealed record UnitConversionInfo(Guid OriginUnitId, Guid DestinationUnitId, decimal Factor);
