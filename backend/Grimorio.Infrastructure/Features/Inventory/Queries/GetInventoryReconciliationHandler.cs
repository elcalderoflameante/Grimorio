using System.Data;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Inventory.Queries;
using Grimorio.Domain.Entities.Inventory;
using Grimorio.Domain.Entities.POS;
using Grimorio.Domain.Entities.Purchases;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Inventory.Queries;

public class GetInventoryReconciliationHandler(GrimorioDbContext db)
    : IRequestHandler<GetInventoryReconciliationQuery, InventoryReconciliationDto>
{
    public async Task<InventoryReconciliationDto> Handle(GetInventoryReconciliationQuery req, CancellationToken ct)
    {
        // One consistent snapshot prevents a payment committed between queries from creating false alarms.
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var result = new InventoryReconciliationDto { CheckedAt = DateTime.UtcNow };
        var articles = await db.InventoryArticles.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.BranchId == req.BranchId)
            .Select(x => new { x.Id, x.Name, x.BaseUnitId }).ToDictionaryAsync(x => x.Id, ct);
        var warehouses = await db.Warehouses.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.BranchId == req.BranchId).ToDictionaryAsync(x => x.Id, x => x.Name, ct);
        var units = await db.MeasurementUnits.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.BranchId == req.BranchId).ToDictionaryAsync(x => x.Id, x => x.Symbol, ct);
        var findings = new List<InventoryReconciliationFindingDto>();
        bool InScope(Guid articleId, Guid warehouseId) =>
            (!req.ArticleId.HasValue || req.ArticleId == articleId)
            && (!req.WarehouseId.HasValue || req.WarehouseId == warehouseId);
        void Add(string code, string severity, string message, Guid articleId, Guid warehouseId,
            decimal? expected = null, decimal? actual = null, string? reference = null,
            Guid? sourceId = null, Guid? movementId = null, Guid? unitId = null)
        {
            if (!InScope(articleId, warehouseId)) return;
            articles.TryGetValue(articleId, out var article);
            findings.Add(new InventoryReconciliationFindingDto
            {
                Code = code, Severity = severity, Message = message, ArticleId = articleId,
                ArticleName = article?.Name ?? articleId.ToString(), WarehouseId = warehouseId,
                WarehouseName = warehouses.GetValueOrDefault(warehouseId) ?? warehouseId.ToString(),
                UnitSymbol = units.GetValueOrDefault(unitId ?? article?.BaseUnitId ?? Guid.Empty),
                ExpectedQuantity = expected, ActualQuantity = actual, Reference = reference,
                SourceId = sourceId, MovementId = movementId,
            });
        }

        var movements = db.StockMovements.AsNoTracking().Where(x => x.BranchId == req.BranchId);
        if (req.ArticleId.HasValue) movements = movements.Where(x => x.ArticleId == req.ArticleId);
        if (req.WarehouseId.HasValue) movements = movements.Where(x => x.WarehouseId == req.WarehouseId);
        var ledger = await movements.GroupBy(x => new { x.ArticleId, x.WarehouseId })
            .Select(g => new { g.Key.ArticleId, g.Key.WarehouseId, Quantity = g.Sum(x => x.BaseQuantity) })
            .ToListAsync(ct);
        var stocks = await db.WarehouseStock.AsNoTracking().Where(x => x.BranchId == req.BranchId
            && (!req.ArticleId.HasValue || x.ArticleId == req.ArticleId)
            && (!req.WarehouseId.HasValue || x.WarehouseId == req.WarehouseId)).ToListAsync(ct);
        var reservations = await db.StockReservations.AsNoTracking().Where(x => x.BranchId == req.BranchId
            && x.Status == StockReservationStatus.Active
            && (!req.ArticleId.HasValue || x.ArticleId == req.ArticleId)
            && (!req.WarehouseId.HasValue || x.WarehouseId == req.WarehouseId)).ToListAsync(ct);
        var ledgerByKey = ledger.ToDictionary(x => (x.ArticleId, x.WarehouseId), x => x.Quantity);
        var stockByKey = stocks.GroupBy(x => (x.ArticleId, x.WarehouseId)).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        var reservedByKey = reservations.GroupBy(x => (x.ArticleId, x.WarehouseId)).ToDictionary(g => g.Key, g => g.Sum(x => x.BaseQuantity));
        var keys = ledgerByKey.Keys.Union(stockByKey.Keys).Union(reservedByKey.Keys).ToList();
        result.CheckedBalances = keys.Count;
        foreach (var key in keys)
        {
            var balance = ledgerByKey.GetValueOrDefault(key);
            var stored = stockByKey.GetValueOrDefault(key);
            var reserved = reservedByKey.GetValueOrDefault(key);
            if (InventoryReconciliationRules.Differs(balance, stored))
                Add("BalanceMismatch", "Confirmed", "El saldo interno difiere de la suma de movimientos.", key.ArticleId, key.WarehouseId, balance, stored);
            if (balance < 0)
                Add("NegativeStock", "Review", "Saldo negativo; revisar salidas y entradas pendientes.", key.ArticleId, key.WarehouseId, 0, balance);
            if (reserved > Math.Max(0, balance))
                Add("OverReserved", "Review", "Las reservas activas superan el stock disponible en movimientos.", key.ArticleId, key.WarehouseId, Math.Max(0, balance), reserved);
        }

        var orderIds = reservations.Select(x => x.OrderId).Distinct().ToList();
        var itemIds = reservations.Select(x => x.OrderItemId).Distinct().ToList();
        var orders = await db.Orders.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.BranchId == req.BranchId && orderIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Number, x.Status, x.PaidAt, x.IsDeleted }).ToDictionaryAsync(x => x.Id, ct);
        var items = await db.OrderItems.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.BranchId == req.BranchId && itemIds.Contains(x.Id))
            .Select(x => new { x.Id, x.OrderId, x.Status, x.Quantity, x.IsDeleted }).ToDictionaryAsync(x => x.Id, ct);
        var paid = await db.OrderPaymentItems.AsNoTracking()
            .Where(x => x.BranchId == req.BranchId && itemIds.Contains(x.OrderItemId)
                && x.Payment != null && !x.Payment.IsDeleted && x.Payment.BranchId == req.BranchId)
            .GroupBy(x => x.OrderItemId).Select(g => new { Id = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.Id, x => x.Quantity, ct);
        foreach (var reservation in reservations)
        {
            orders.TryGetValue(reservation.OrderId, out var order);
            items.TryGetValue(reservation.OrderItemId, out var item);
            if (InventoryReconciliationRules.HasInvalidReservation(order is null || order.IsDeleted,
                order?.Status == OrderStatus.Cancelled, order?.PaidAt != null,
                item is null || item.IsDeleted || item.OrderId != reservation.OrderId,
                item?.Status == OrderItemStatus.Cancelled, item?.Quantity ?? 0,
                paid.GetValueOrDefault(reservation.OrderItemId)))
                Add("InvalidReservation", "Confirmed", "Reserva activa de un pedido o item eliminado, cancelado o completamente cobrado.",
                    reservation.ArticleId, reservation.WarehouseId, 0, reservation.BaseQuantity,
                    order is null ? null : $"Orden #{order.Number}", reservation.Id);
        }

        // Repeated ingredients across DIFFERENT partial payments are not duplicates.
        var repeatedSales = await movements.Where(x => x.Type == MovementType.SaleDeduction
            && x.OrderPaymentItemId != null && x.StockReservationId != null)
            .GroupBy(x => new { x.OrderPaymentItemId, x.StockReservationId, x.ArticleId, x.WarehouseId })
            .Where(g => g.Count() > 1)
            .Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        foreach (var sale in repeatedSales)
            Add("RepeatedConsumption", "Review", $"{sale.Count} salidas para la misma reserva y linea de cobro; verificar su origen.",
                sale.Key.ArticleId, sale.Key.WarehouseId, sourceId: sale.Key.OrderPaymentItemId);

        var invalidSales = await movements.Where(x => x.Type == MovementType.SaleDeduction
            && x.OrderPaymentItemId != null && x.StockReservationId != null
            && (!db.OrderPaymentItems.Any(p => p.BranchId == req.BranchId && p.Id == x.OrderPaymentItemId
                    && p.OrderItemId == x.OrderItemId && p.Payment != null && !p.Payment.IsDeleted
                    && p.Payment.BranchId == req.BranchId)
                || !db.StockReservations.Any(r => r.BranchId == req.BranchId && r.Id == x.StockReservationId
                    && r.OrderItemId == x.OrderItemId && r.ArticleId == x.ArticleId && r.WarehouseId == x.WarehouseId)
                || x.BaseQuantity > 0))
            .Select(x => new { x.Id, x.ArticleId, x.WarehouseId, x.Reference, x.OrderPaymentItemId }).ToListAsync(ct);
        foreach (var sale in invalidSales)
            Add("SaleOriginMismatch", "Confirmed", "La salida no corresponde a su reserva o linea de cobro, o tiene signo positivo.",
                sale.ArticleId, sale.WarehouseId, reference: sale.Reference,
                sourceId: sale.OrderPaymentItemId, movementId: sale.Id);

        var unlinked = await movements.Where(x =>
                (x.Type == MovementType.SaleDeduction && (x.OrderPaymentItemId == null || x.StockReservationId == null))
                || (x.Type == MovementType.PurchaseEntry && x.PurchaseItemId == null)
                || ((x.Type == MovementType.ProductionInput || x.Type == MovementType.ProductionOutput)
                    && !db.ProductionOrderMovements.Any(p => p.BranchId == req.BranchId && p.StockMovementId == x.Id)))
            .GroupBy(x => new { x.ArticleId, x.WarehouseId })
            .Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        foreach (var group in unlinked)
            Add("IncompleteTrace", "Incomplete", $"{group.Count} movimientos sin vinculo completo al origen. No prueba un consumo duplicado.",
                group.Key.ArticleId, group.Key.WarehouseId);

        // Production snapshots are historical quantities; never expand today's recipe.
        var productionOrders = await db.ProductionOrders.AsNoTracking().Where(x => x.BranchId == req.BranchId)
            .Include(x => x.Ingredients).ToListAsync(ct);
        var productionActual = await (from link in db.ProductionOrderMovements.AsNoTracking()
            join movement in movements on link.StockMovementId equals movement.Id
            where link.BranchId == req.BranchId
            group movement by new { link.ProductionOrderId, movement.ArticleId, movement.WarehouseId, movement.Type } into g
            select new { g.Key, Quantity = g.Sum(x => x.BaseQuantity) }).ToListAsync(ct);
        var actualByProduction = productionActual.ToDictionary(x =>
            (x.Key.ProductionOrderId, x.Key.ArticleId, x.Key.WarehouseId, x.Key.Type), x => x.Quantity);
        var expectedByProduction = new Dictionary<(Guid, Guid, Guid, MovementType), decimal>();
        foreach (var order in productionOrders.Where(x => x.Status == ProductionOrderStatus.Completed))
        {
            expectedByProduction[(order.Id, order.OutputArticleId, order.DestinationWarehouseId, MovementType.ProductionOutput)] = order.OutputBaseQuantity;
            foreach (var ingredient in order.Ingredients)
            {
                var key = (order.Id, ingredient.ArticleId, order.SourceWarehouseId, MovementType.ProductionInput);
                expectedByProduction[key] = expectedByProduction.GetValueOrDefault(key) - ingredient.BaseQuantity;
            }
        }
        var productionNumbers = productionOrders.ToDictionary(x => x.Id, x => x.Number);
        foreach (var key in expectedByProduction.Keys.Union(actualByProduction.Keys))
        {
            var expected = expectedByProduction.GetValueOrDefault(key);
            var actual = actualByProduction.GetValueOrDefault(key);
            if (InventoryReconciliationRules.Differs(expected, actual))
                Add("ProductionMismatch", "Confirmed", "Los movimientos no coinciden con las cantidades registradas en la produccion.",
                    key.Item2, key.Item3, expected, actual, productionNumbers.GetValueOrDefault(key.Item1), key.Item1);
        }

        // Compare purchase quantities in the ORIGINAL entry unit; conversions may have changed later.
        var purchaseRows = await db.PurchaseItems.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.BranchId == req.BranchId && (!req.ArticleId.HasValue || x.ArticleId == req.ArticleId))
            .Select(x => new { x.Id, x.PurchaseId, x.ArticleId, x.IsDeleted,
                Quantity = x.InventoryQuantity ?? x.Quantity, UnitId = x.InventoryUnitId ?? x.UnitId }).ToListAsync(ct);
        var purchases = await db.Purchases.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.BranchId == req.BranchId)
            .Select(x => new { x.Id, x.DocumentNumber, x.Status, x.IsDeleted, x.DestinationWarehouseId })
            .ToDictionaryAsync(x => x.Id, ct);
        var purchaseMovements = await movements.Where(x => x.PurchaseItemId != null)
            .Select(x => new { x.PurchaseItemId, x.ArticleId, x.WarehouseId, x.UnitId, x.Type, x.Quantity, x.BaseQuantity }).ToListAsync(ct);
        var byPurchaseItem = purchaseMovements.ToLookup(x => x.PurchaseItemId!.Value);
        foreach (var item in purchaseRows)
        {
            if (!purchases.TryGetValue(item.PurchaseId, out var purchase)) continue;
            var linked = byPurchaseItem[item.Id].ToList();
            var active = !item.IsDeleted && !purchase.IsDeleted && purchase.Status == PurchaseStatus.Registrada;
            if (linked.Count == 0)
            {
                if (active && purchase.DestinationWarehouseId.HasValue)
                    Add("PurchaseTraceUnavailable", "Incomplete", "Linea de compra sin entrada vinculada: verificar si es historica o falta el movimiento.",
                        item.ArticleId, purchase.DestinationWarehouseId.Value, reference: purchase.DocumentNumber, sourceId: purchase.Id);
                continue;
            }
            foreach (var group in linked.GroupBy(x => new { x.ArticleId, x.WarehouseId, x.UnitId }))
            {
                var expected = active && group.Key.ArticleId == item.ArticleId
                    && group.Key.WarehouseId == purchase.DestinationWarehouseId && group.Key.UnitId == item.UnitId ? item.Quantity : 0;
                var actual = group.Sum(x => x.BaseQuantity < 0 ? -Math.Abs(x.Quantity) : Math.Abs(x.Quantity));
                if (InventoryReconciliationRules.Differs(expected, actual))
                    Add("PurchaseMismatch", "Review", "El neto vinculado no coincide con la linea de compra; revisar correcciones historicas sin vinculo.",
                        group.Key.ArticleId, group.Key.WarehouseId, expected, actual, purchase.DocumentNumber, purchase.Id, unitId: group.Key.UnitId);
            }
        }
        await tx.CommitAsync(ct);
        result.ConfirmedCount = findings.Count(x => x.Severity == "Confirmed");
        result.ReviewCount = findings.Count(x => x.Severity == "Review");
        result.IncompleteCount = findings.Count(x => x.Severity == "Incomplete");
        var filtered = findings.Where(x => string.IsNullOrEmpty(req.Severity) || x.Severity == req.Severity)
            .Where(x => string.IsNullOrWhiteSpace(req.Search)
                || $"{x.ArticleName} {x.WarehouseName} {x.Reference} {x.Message}".Contains(req.Search.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Severity == "Confirmed" ? 0 : x.Severity == "Review" ? 1 : 2)
            .ThenBy(x => x.ArticleName).ThenBy(x => x.WarehouseName).ThenBy(x => x.Code).ThenBy(x => x.SourceId).ToList();
        result.TotalFindings = filtered.Count;
        var pageSize = Math.Clamp(req.PageSize, 1, 100);
        result.Findings = filtered.Skip((Math.Clamp(req.Page, 1, 1000000) - 1) * pageSize).Take(pageSize).ToList();
        return result;
    }
}
