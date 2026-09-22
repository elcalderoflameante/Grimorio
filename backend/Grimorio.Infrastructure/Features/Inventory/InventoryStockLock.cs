using Grimorio.Domain.Entities.Inventory;
using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Inventory;

internal static class InventoryStockLock
{
    public static async Task<WarehouseStock?> AcquireAsync(
        GrimorioDbContext db, Guid branchId, Guid articleId, Guid warehouseId, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("El bloqueo de stock requiere una transaccion activa.");

        // The parent row also serializes first-time stock creation. NO KEY UPDATE
        // allows foreign-key checks by reservations while we wait for the stock row.
        _ = await db.InventoryArticles
            .FromSqlInterpolated($"""
                SELECT * FROM inv."InventoryArticles"
                WHERE "Id" = {articleId} AND "BranchId" = {branchId} AND NOT "IsDeleted"
                FOR NO KEY UPDATE
                """)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Articulo no encontrado.");

        var stock = await db.WarehouseStock
            .FromSqlInterpolated($"""
                SELECT * FROM inv."WarehouseStock"
                WHERE "BranchId" = {branchId} AND "ArticleId" = {articleId}
                    AND "WarehouseId" = {warehouseId} AND NOT "IsDeleted"
                FOR UPDATE
                """)
            .FirstOrDefaultAsync(ct);

        // A tracked entity can contain a value read before waiting for the lock.
        if (stock != null)
            await db.Entry(stock).ReloadAsync(ct);

        return stock;
    }
}
