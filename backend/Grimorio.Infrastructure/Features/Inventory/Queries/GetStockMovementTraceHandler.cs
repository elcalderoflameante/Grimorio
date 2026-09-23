using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Inventory.Queries;
using Grimorio.Domain.Entities.Inventory;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Inventory.Queries;

public class GetStockMovementTraceHandler(GrimorioDbContext db)
    : IRequestHandler<GetStockMovementTraceQuery, StockMovementTraceDto?>
{
    public async Task<StockMovementTraceDto?> Handle(GetStockMovementTraceQuery req, CancellationToken ct)
    {
        var movement = await db.StockMovements.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BranchId == req.BranchId && x.Id == req.MovementId, ct);
        if (movement is null) return null;
        var result = new StockMovementTraceDto
        {
            MovementId = movement.Id, Reference = movement.Reference, Notes = movement.Notes,
            CreatedAt = movement.CreatedAt, OrderItemId = movement.OrderItemId,
            OrderPaymentItemId = movement.OrderPaymentItemId, PurchaseItemId = movement.PurchaseItemId,
            StockReservationId = movement.StockReservationId,
            Origin = movement.Type is MovementType.SaleDeduction or MovementType.SaleRestoration
                or MovementType.PurchaseEntry or MovementType.ProductionInput or MovementType.ProductionOutput
                ? "Incomplete" : "Manual",
        };
        // Historical source rows may be soft-deleted after a purchase correction.
        // Every query bypassing filters remains explicitly scoped to the current branch.
        var paymentItem = await db.OrderPaymentItems.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(x => x.BranchId == req.BranchId && x.Id == movement.OrderPaymentItemId, ct);
        if (paymentItem != null)
        {
            result.OrderPaymentId = paymentItem.OrderPaymentId;
            result.OrderItemId = paymentItem.OrderItemId;
            result.PaidItemQuantity = paymentItem.Quantity;
            var payment = await db.OrderPayments.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(x => x.BranchId == req.BranchId && x.Id == paymentItem.OrderPaymentId, ct);
            if (payment != null)
            {
                result.OrderId = payment.OrderId;
                result.PaidAt = payment.PaidAt;
                result.Origin = "Sale";
            }
        }
        if (result.OrderItemId.HasValue && !result.OrderId.HasValue)
            result.OrderId = await db.OrderItems.IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.BranchId == req.BranchId && x.Id == result.OrderItemId)
                .Select(x => (Guid?)x.OrderId).FirstOrDefaultAsync(ct);
        if (result.OrderId.HasValue)
            result.OrderNumber = await db.Orders.IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.BranchId == req.BranchId && x.Id == result.OrderId)
                .Select(x => (int?)x.Number).FirstOrDefaultAsync(ct);
        var purchaseItem = await db.PurchaseItems.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(x => x.BranchId == req.BranchId && x.Id == movement.PurchaseItemId, ct);
        if (purchaseItem != null)
        {
            result.PurchaseId = purchaseItem.PurchaseId;
            result.DocumentNumber = await db.Purchases.IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.BranchId == req.BranchId && x.Id == purchaseItem.PurchaseId)
                .Select(x => x.DocumentNumber).FirstOrDefaultAsync(ct);
            result.Origin = "Purchase";
        }
        var productionId = await db.ProductionOrderMovements.AsNoTracking()
            .Where(x => x.BranchId == req.BranchId && x.StockMovementId == movement.Id)
            .Select(x => (Guid?)x.ProductionOrderId).FirstOrDefaultAsync(ct);
        if (productionId.HasValue)
        {
            result.ProductionOrderId = productionId;
            result.ProductionNumber = await db.ProductionOrders.IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.BranchId == req.BranchId && x.Id == productionId)
                .Select(x => x.Number).FirstOrDefaultAsync(ct);
            result.Origin = "Production";
        }
        return result;
    }
}
