using Grimorio.Domain.Entities.Inventory;
using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Inventory;

internal sealed record InventoryAvailabilitySnapshot(
    Dictionary<Guid, decimal> Quantities,
    HashSet<Guid> NegativeStockAllowed,
    HashSet<Guid> UnavailableArticles)
{
    public bool CanFulfill(Guid articleId, decimal required) => required > 0
        && !UnavailableArticles.Contains(articleId)
        && (NegativeStockAllowed.Contains(articleId) || Quantities.GetValueOrDefault(articleId) >= required);
}

internal static class InventoryAvailability
{
    public static async Task<InventoryAvailabilitySnapshot> LoadAsync(
        GrimorioDbContext db, Guid branchId, IReadOnlyCollection<Guid> articleIds, CancellationToken ct)
    {
        if (articleIds.Count == 0) return new([], [], []);
        var articles = await db.InventoryArticles.AsNoTracking()
            .Where(a => a.BranchId == branchId && articleIds.Contains(a.Id))
            .Select(a => new { a.Id, a.IsActive, a.AllowNegativeStock }).ToListAsync(ct);
        var quantities = await db.WarehouseStock.AsNoTracking()
            .Where(s => s.BranchId == branchId && articleIds.Contains(s.ArticleId))
            .GroupBy(s => s.ArticleId)
            .Select(g => new { Id = g.Key, Quantity = g.Sum(s => s.Quantity) })
            .ToDictionaryAsync(s => s.Id, s => s.Quantity, ct);
        var reservations = await db.StockReservations.AsNoTracking()
            .Where(r => r.BranchId == branchId && articleIds.Contains(r.ArticleId) && r.Status == StockReservationStatus.Active)
            .GroupBy(r => r.ArticleId)
            .Select(g => new { Id = g.Key, Quantity = g.Sum(r => r.BaseQuantity) }).ToListAsync(ct);
        foreach (var reservation in reservations)
            quantities[reservation.Id] = quantities.GetValueOrDefault(reservation.Id) - reservation.Quantity;
        return new(quantities,
            articles.Where(a => a.IsActive && a.AllowNegativeStock).Select(a => a.Id).ToHashSet(),
            articleIds.Except(articles.Where(a => a.IsActive).Select(a => a.Id)).ToHashSet());
    }
}
