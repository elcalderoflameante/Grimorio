using Grimorio.Domain.Entities.Inventory;
using Grimorio.Domain.Entities.Menu;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Inventory;

internal static class StockReservationService
{
    public static async Task ReserveOrderItemsAsync(
        GrimorioDbContext db,
        Guid branchId,
        Guid orderId,
        IReadOnlyCollection<OrderItem> orderItems,
        CancellationToken ct)
    {
        var items = orderItems.Where(i => !i.IsDeleted).ToList();
        if (items.Count == 0) return;

        var orderItemIds = items.Select(i => i.Id).ToList();
        var alreadyReserved = await db.StockReservations
            .AnyAsync(r => r.BranchId == branchId
                && orderItemIds.Contains(r.OrderItemId)
                && r.Status == StockReservationStatus.Active
                && !r.IsDeleted, ct);
        if (alreadyReserved) return;

        var menuItemIds = items.Select(i => i.MenuItemId).Distinct().ToList();
        var recipes = await db.RecipeIngredients
            .AsNoTracking()
            .Include(r => r.Article)
            .Where(r => r.BranchId == branchId && menuItemIds.Contains(r.MenuItemId) && !r.IsDeleted)
            .ToListAsync(ct);

        if (recipes.Count == 0) return;

        var choiceArticleIds = items
            .SelectMany(i => i.IngredientChoices.Where(c => !c.IsDeleted).Select(c => c.ChosenArticleId))
            .ToList();
        var articleIdsForRequirements = recipes.Select(r => r.ArticleId)
            .Concat(choiceArticleIds)
            .Distinct()
            .ToList();
        var articlesById = await db.InventoryArticles
            .AsNoTracking()
            .Where(a => a.BranchId == branchId && articleIdsForRequirements.Contains(a.Id) && !a.IsDeleted)
            .ToDictionaryAsync(a => a.Id, ct);

        var conversions = await db.UnitConversions
            .AsNoTracking()
            .Where(c => c.BranchId == branchId && !c.IsDeleted)
            .ToListAsync(ct);

        var requirements = BuildRequirements(items, recipes, articlesById, conversions)
            .GroupBy(r => new { r.OrderItemId, r.ArticleId, r.BaseUnitId, r.ArticleName })
            .Select(g => new StockRequirement(
                g.Key.OrderItemId,
                g.Key.ArticleId,
                g.Key.BaseUnitId,
                g.Sum(r => r.BaseQuantity),
                g.Key.ArticleName))
            .ToList();
        if (requirements.Count == 0) return;

        var articleIds = requirements.Select(r => r.ArticleId).Distinct().ToList();
        var stockRows = await db.WarehouseStock
            .AsNoTracking()
            .Where(s => s.BranchId == branchId && articleIds.Contains(s.ArticleId) && !s.IsDeleted)
            .Select(s => new AvailableStock(s.ArticleId, s.WarehouseId, s.Quantity))
            .ToListAsync(ct);

        var activeReservations = await db.StockReservations
            .AsNoTracking()
            .Where(r => r.BranchId == branchId
                && articleIds.Contains(r.ArticleId)
                && r.Status == StockReservationStatus.Active
                && !r.IsDeleted)
            .GroupBy(r => new { r.ArticleId, r.WarehouseId })
            .Select(g => new { g.Key.ArticleId, g.Key.WarehouseId, Quantity = g.Sum(r => r.BaseQuantity) })
            .ToListAsync(ct);

        var reservedByArticleWarehouse = activeReservations.ToDictionary(
            x => (x.ArticleId, x.WarehouseId),
            x => x.Quantity);

        foreach (var requirement in requirements)
        {
            var availableStocks = stockRows
                .Where(s => s.ArticleId == requirement.ArticleId)
                .Select(s => new AvailableStock(
                    s.ArticleId,
                    s.WarehouseId,
                    s.Quantity - reservedByArticleWarehouse.GetValueOrDefault((s.ArticleId, s.WarehouseId))))
                .Where(s => s.Quantity > 0)
                .OrderByDescending(s => s.Quantity)
                .ToList();

            var totalAvailable = availableStocks.Sum(s => s.Quantity);
            if (totalAvailable + 0.0001m < requirement.BaseQuantity)
                throw new InvalidOperationException($"Stock insuficiente para {requirement.ArticleName}. Disponible: {Math.Floor(totalAvailable)}, requerido: {Math.Ceiling(requirement.BaseQuantity)}.");

            var remaining = requirement.BaseQuantity;
            foreach (var stock in availableStocks)
            {
                if (remaining <= 0) break;

                var reservedQuantity = Math.Min(remaining, stock.Quantity);
                db.StockReservations.Add(new StockReservation
                {
                    Id = Guid.NewGuid(),
                    BranchId = branchId,
                    OrderId = orderId,
                    OrderItemId = requirement.OrderItemId,
                    ArticleId = requirement.ArticleId,
                    WarehouseId = stock.WarehouseId,
                    Quantity = reservedQuantity,
                    UnitId = requirement.BaseUnitId,
                    BaseQuantity = reservedQuantity,
                    Status = StockReservationStatus.Active,
                    ReservedAt = DateTime.UtcNow,
                });

                reservedByArticleWarehouse[(stock.ArticleId, stock.WarehouseId)] =
                    reservedByArticleWarehouse.GetValueOrDefault((stock.ArticleId, stock.WarehouseId)) + reservedQuantity;
                remaining -= reservedQuantity;
            }
        }
    }

    public static async Task ReleaseOrderReservationsAsync(
        GrimorioDbContext db,
        Guid branchId,
        Guid orderId,
        CancellationToken ct)
    {
        var reservations = await db.StockReservations
            .Where(r => r.BranchId == branchId
                && r.OrderId == orderId
                && r.Status == StockReservationStatus.Active
                && !r.IsDeleted)
            .ToListAsync(ct);

        foreach (var reservation in reservations)
        {
            reservation.Status = StockReservationStatus.Released;
            reservation.ReleasedAt = DateTime.UtcNow;
        }
    }

    public static async Task ReleaseOrderItemReservationsAsync(
        GrimorioDbContext db,
        Guid branchId,
        Guid orderItemId,
        CancellationToken ct)
    {
        var reservations = await db.StockReservations
            .Where(r => r.BranchId == branchId
                && r.OrderItemId == orderItemId
                && r.Status == StockReservationStatus.Active
                && !r.IsDeleted)
            .ToListAsync(ct);

        foreach (var reservation in reservations)
        {
            reservation.Status = StockReservationStatus.Released;
            reservation.ReleasedAt = DateTime.UtcNow;
        }
    }

    private static List<StockRequirement> BuildRequirements(
        IReadOnlyCollection<OrderItem> items,
        IReadOnlyCollection<RecipeIngredient> recipes,
        IReadOnlyDictionary<Guid, InventoryArticle> articlesById,
        IReadOnlyCollection<UnitConversion> conversions)
    {
        var result = new List<StockRequirement>();
        var recipesByMenuItem = recipes.GroupBy(r => r.MenuItemId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var item in items)
        {
            if (!recipesByMenuItem.TryGetValue(item.MenuItemId, out var recipe)) continue;

            foreach (var ingredient in recipe)
            {
                var articleId = ingredient.ArticleId;
                if (ingredient.IsVariable)
                {
                    var choice = item.IngredientChoices
                        .FirstOrDefault(c => c.RecipeIngredientId == ingredient.Id && !c.IsDeleted);
                    if (choice != null) articleId = choice.ChosenArticleId;
                }

                articlesById.TryGetValue(articleId, out var article);
                var baseUnitId = article?.BaseUnitId ?? Guid.Empty;
                if (baseUnitId == Guid.Empty) continue;

                var baseQuantity = ConvertQuantity(
                    ingredient.Quantity * item.Quantity,
                    ingredient.UnitId,
                    baseUnitId,
                    conversions,
                    article?.Name ?? "artículo");

                result.Add(new StockRequirement(
                    item.Id,
                    articleId,
                    baseUnitId,
                    baseQuantity,
                    article?.Name ?? "artículo"));
            }
        }

        return result;
    }

    private static decimal ConvertQuantity(
        decimal quantity,
        Guid originUnitId,
        Guid destinationUnitId,
        IReadOnlyCollection<UnitConversion> conversions,
        string articleName)
    {
        if (originUnitId == destinationUnitId) return quantity;

        var direct = conversions.FirstOrDefault(c => c.OriginUnitId == originUnitId && c.DestinationUnitId == destinationUnitId);
        if (direct != null) return quantity * direct.Factor;

        var reverse = conversions.FirstOrDefault(c => c.OriginUnitId == destinationUnitId && c.DestinationUnitId == originUnitId);
        if (reverse != null) return quantity / reverse.Factor;

        throw new InvalidOperationException($"No existe conversión de unidad para reservar {articleName}.");
    }

    private sealed record StockRequirement(
        Guid OrderItemId,
        Guid ArticleId,
        Guid BaseUnitId,
        decimal BaseQuantity,
        string ArticleName);

    private sealed record AvailableStock(Guid ArticleId, Guid WarehouseId, decimal Quantity);
}
