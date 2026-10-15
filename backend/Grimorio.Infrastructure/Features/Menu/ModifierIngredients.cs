using System.Text.Json;
using Grimorio.Domain.Entities.Menu;
using Grimorio.Domain.Entities.POS;

namespace Grimorio.Infrastructure.Features.Menu;

internal static class ModifierIngredients
{
    public static IEnumerable<ModifierIngredient> ForOption(MenuItemModifierOption option) =>
        Enumerate(option.ArticleId, option.UnitId, option.Quantity, option.AdditionalIngredients);

    public static IEnumerable<ModifierIngredient> ForSelection(OrderItemModifierSelection selection) =>
        Enumerate(selection.ArticleId, selection.UnitId, selection.InventoryQuantity,
            JsonSerializer.Deserialize<List<ModifierIngredient>>(selection.AdditionalIngredientsJson) ?? []);

    private static IEnumerable<ModifierIngredient> Enumerate(Guid? articleId, Guid? unitId, decimal quantity,
        IEnumerable<ModifierIngredient> additional)
    {
        if (articleId.HasValue)
        {
            if (!unitId.HasValue || quantity <= 0)
                throw new InvalidOperationException("El consumo de la opcion requiere unidad y cantidad mayor a cero.");
            yield return new ModifierIngredient(articleId.Value, unitId.Value, quantity);
        }
        foreach (var ingredient in additional)
        {
            if (ingredient.ArticleId == Guid.Empty || ingredient.UnitId == Guid.Empty || ingredient.Quantity <= 0)
                throw new InvalidOperationException("Cada consumo adicional requiere articulo, unidad y cantidad mayor a cero.");
            yield return ingredient;
        }
    }
}
