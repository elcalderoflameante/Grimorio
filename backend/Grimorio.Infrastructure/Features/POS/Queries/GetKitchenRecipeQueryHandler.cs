using Grimorio.Application.DTOs;
using Grimorio.Application.Features.POS.Queries;
using Grimorio.Domain.Entities.Menu;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Features.Menu;
using Grimorio.Infrastructure.Features.Menu.Commands;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.POS.Queries;

public class GetKitchenRecipeQueryHandler(GrimorioDbContext db)
    : IRequestHandler<GetKitchenRecipeQuery, KitchenRecipeDto?>
{
    public async Task<KitchenRecipeDto?> Handle(GetKitchenRecipeQuery req, CancellationToken ct)
    {
        var menuItemId = await db.OrderItems.AsNoTracking()
            .Where(i => i.Id == req.OrderItemId && i.BranchId == req.BranchId && !i.IsDeleted
                && i.Status != OrderItemStatus.Cancelled && i.Order != null
                && i.Order.BranchId == req.BranchId && !i.Order.IsDeleted
                && i.Order.Status != OrderStatus.Draft && i.Order.Status != OrderStatus.Cancelled)
            .Select(i => (Guid?)i.MenuItemId).SingleOrDefaultAsync(ct);

        // Combo rows in KDS carry preparation-task IDs.
        menuItemId ??= await db.OrderItemPreparationTasks.IgnoreAutoIncludes().AsNoTracking()
            .Where(t => t.Id == req.OrderItemId && t.BranchId == req.BranchId && !t.IsDeleted
                && t.Status != OrderItemStatus.Cancelled && t.OrderItem != null
                && t.OrderItem.BranchId == req.BranchId && !t.OrderItem.IsDeleted
                && t.OrderItem.Status != OrderItemStatus.Cancelled && t.OrderItem.Order != null
                && t.OrderItem.Order.BranchId == req.BranchId && !t.OrderItem.Order.IsDeleted
                && t.OrderItem.Order.Status != OrderStatus.Draft && t.OrderItem.Order.Status != OrderStatus.Cancelled)
            .Select(t => (Guid?)t.ComponentItemId).SingleOrDefaultAsync(ct);
        if (!menuItemId.HasValue) return null;

        var menuItem = await db.MenuItems.IgnoreAutoIncludes().AsNoTracking()
            .Where(m => m.Id == menuItemId.Value && m.BranchId == req.BranchId && !m.IsDeleted)
            .Include(m => m.Recipe.Where(r => !r.IsDeleted && r.BranchId == req.BranchId)).ThenInclude(r => r.Article)
            .Include(m => m.Recipe.Where(r => !r.IsDeleted && r.BranchId == req.BranchId)).ThenInclude(r => r.Unit)
            .Include(m => m.Recipe.Where(r => !r.IsDeleted && r.BranchId == req.BranchId)).ThenInclude(r => r.SubRecipe)
                .ThenInclude(s => s!.Ingredients.Where(i => !i.IsDeleted && i.BranchId == req.BranchId)).ThenInclude(i => i.Article)
            .Include(m => m.Recipe.Where(r => !r.IsDeleted && r.BranchId == req.BranchId)).ThenInclude(r => r.SubRecipe)
                .ThenInclude(s => s!.Ingredients.Where(i => !i.IsDeleted && i.BranchId == req.BranchId)).ThenInclude(i => i.Unit)
            .Include(m => m.Preparation)!.ThenInclude(p => p!.Steps.Where(s => !s.IsDeleted && s.BranchId == req.BranchId))
            .AsSplitQuery().SingleOrDefaultAsync(ct);
        if (menuItem is null) return null;

        var conversions = await db.UnitConversions.AsNoTracking()
            .Where(c => c.BranchId == req.BranchId && !c.IsDeleted)
            .Select(c => new UnitConversionInfo(c.OriginUnitId, c.DestinationUnitId, c.Factor)).ToListAsync(ct);
        return new KitchenRecipeDto
        {
            MenuItemId = menuItem.Id,
            Name = MenuItemNames.Format(menuItem.Name, menuItem.GeneralName),
            Description = menuItem.Description,
            ImageUrl = menuItem.ImageUrl,
            Ingredients = menuItem.Recipe.Select(r => MapIngredient(r, req.BranchId, conversions))
                .OrderBy(i => i.Name).ToList(),
            Preparation = menuItem.Preparation is { IsDeleted: false } preparation && preparation.BranchId == req.BranchId
                ? MenuMapper.MapPreparation(preparation) : null,
        };
    }

    private static KitchenRecipeIngredientDto MapIngredient(RecipeIngredient ingredient, Guid branchId,
        List<UnitConversionInfo> conversions)
    {
        var dto = new KitchenRecipeIngredientDto
        {
            Quantity = ingredient.Quantity,
            Unit = ingredient.Unit is { IsDeleted: false } unit && unit.BranchId == branchId ? unit.Symbol : string.Empty,
            Notes = ingredient.Notes,
        };
        if (ingredient.Type != RecipeIngredientType.SubRecipe)
        {
            dto.Name = ingredient.Article is { IsDeleted: false } article && article.BranchId == branchId
                ? article.Name : "Ingrediente no disponible";
            return dto;
        }
        var subRecipe = ingredient.SubRecipe;
        if (subRecipe is null || subRecipe.IsDeleted || subRecipe.BranchId != branchId)
        {
            dto.Name = "Subreceta no disponible";
            return dto;
        }
        dto.Name = subRecipe.Name;
        var output = MenuRecipeExpansion.ConvertQuantity(ingredient.Quantity, ingredient.UnitId, subRecipe.OutputUnitId, conversions);
        decimal? factor = output > 0 && subRecipe.OutputQuantity > 0 ? output / subRecipe.OutputQuantity : null;
        dto.Ingredients = subRecipe.Ingredients
            .Where(i => !i.IsDeleted && i.BranchId == branchId && i.Article is { IsDeleted: false } && i.Article.BranchId == branchId)
            .OrderBy(i => i.Article!.Name)
            .Select(i => new KitchenRecipeIngredientDto
            {
                Name = i.Article!.Name,
                Quantity = factor.HasValue ? i.Quantity * factor.Value : null,
                Unit = i.Unit is { IsDeleted: false } childUnit && childUnit.BranchId == branchId ? childUnit.Symbol : string.Empty,
                Notes = i.Notes,
            }).ToList();
        return dto;
    }
}
