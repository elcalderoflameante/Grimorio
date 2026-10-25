using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Inventory.Queries;
using Grimorio.Domain.Entities.Menu;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Inventory.Queries;

public class GetArticleMenuUsageHandler(GrimorioDbContext db)
    : IRequestHandler<GetArticleMenuUsageQuery, List<ArticleMenuUsageDto>?>
{
    public async Task<List<ArticleMenuUsageDto>?> Handle(GetArticleMenuUsageQuery req, CancellationToken ct)
    {
        if (!await db.InventoryArticles.AnyAsync(a => a.Id == req.ArticleId && a.BranchId == req.BranchId, ct))
            return null;

        var items = await db.MenuItems.AsNoTracking()
            .Where(i => i.BranchId == req.BranchId)
            .Select(i => new { i.Id, i.Name, i.IsActive }).ToListAsync(ct);
        var itemIds = items.Select(i => i.Id).ToList();
        var recipes = await db.RecipeIngredients.AsNoTracking()
            .Where(r => r.BranchId == req.BranchId && itemIds.Contains(r.MenuItemId)
                && ((r.Type == RecipeIngredientType.Article && r.ArticleId == req.ArticleId)
                    || (r.Type == RecipeIngredientType.SubRecipe && r.SubRecipe != null
                        && r.SubRecipe.BranchId == req.BranchId && r.SubRecipe.Ingredients.Any(i =>
                            i.BranchId == req.BranchId && i.ArticleId == req.ArticleId))))
            .Select(r => new { r.MenuItemId, r.Type, SubRecipeName = r.SubRecipe == null ? null : r.SubRecipe.Name })
            .ToListAsync(ct);
        var usages = new Dictionary<Guid, HashSet<string>>();
        void Add(Guid id, string usage)
        {
            if (!usages.TryGetValue(id, out var entries)) usages[id] = entries = [];
            entries.Add(usage);
        }
        foreach (var r in recipes)
            Add(r.MenuItemId, r.Type == RecipeIngredientType.Article ? "Receta" : $"Subreceta: {r.SubRecipeName}");

        // Additional modifier ingredients are stored as JSON, so inspect them after the branch-scoped query.
        var options = await db.MenuItemModifierOptions.AsNoTracking()
            .Where(o => o.BranchId == req.BranchId && o.ModifierGroup != null
                && o.ModifierGroup.BranchId == req.BranchId && itemIds.Contains(o.ModifierGroup.MenuItemId))
            .Select(o => new { o.ModifierGroup!.MenuItemId, GroupName = o.ModifierGroup.Name,
                o.Name, o.ArticleId, o.AdditionalIngredientsJson }).ToListAsync(ct);
        foreach (var o in options)
        {
            var extra = new MenuItemModifierOption { AdditionalIngredientsJson = o.AdditionalIngredientsJson };
            if (o.ArticleId == req.ArticleId || extra.AdditionalIngredients.Any(i => i.ArticleId == req.ArticleId))
                Add(o.MenuItemId, $"Opcion: {o.GroupName} / {o.Name}");
        }

        var components = await db.MenuComboComponents.AsNoTracking()
            .Where(c => c.BranchId == req.BranchId && itemIds.Contains(c.ComboId) && itemIds.Contains(c.ComponentItemId))
            .Select(c => new { c.ComboId, c.ComponentItemId }).ToListAsync(ct);
        var names = items.ToDictionary(i => i.Id, i => i.Name);
        // Fixed-point propagation also handles nested combos without recursing through possible cycles.
        bool changed;
        do
        {
            changed = false;
            foreach (var c in components)
            {
                if (!usages.ContainsKey(c.ComponentItemId)) continue;
                var count = usages.GetValueOrDefault(c.ComboId)?.Count ?? 0;
                Add(c.ComboId, $"Componente: {names[c.ComponentItemId]}");
                changed |= usages[c.ComboId].Count != count;
            }
        } while (changed);

        return items.Where(i => usages.ContainsKey(i.Id)).OrderBy(i => i.Name)
            .Select(i => new ArticleMenuUsageDto { MenuItemId = i.Id, MenuItemName = i.Name,
                IsActive = i.IsActive, Usages = usages[i.Id].Order().ToList() }).ToList();
    }
}
