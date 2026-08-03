using Grimorio.Domain.Entities.Inventory;
using Grimorio.Domain.Entities.Menu;
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
        return await db.MenuItems
            .AsNoTracking()
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
    }

    public static List<MenuRecipeRequirement> Expand(
        MenuItem item,
        decimal itemQuantity,
        IReadOnlyCollection<UnitConversionInfo> conversions)
    {
        var result = new List<MenuRecipeRequirement>();

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
