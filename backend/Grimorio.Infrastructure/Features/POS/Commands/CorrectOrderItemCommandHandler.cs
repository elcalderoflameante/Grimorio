using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Inventory.Commands;
using Grimorio.Application.Features.POS.Commands;
using Grimorio.Domain.Entities.Inventory;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Features.Inventory;
using Grimorio.Infrastructure.Features.Inventory.Commands;
using Grimorio.Infrastructure.Features.Menu;
using Grimorio.Infrastructure.Persistence;
using Grimorio.SharedKernel.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.POS.Commands;

public class CorrectOrderItemCommandHandler(GrimorioDbContext db) : IRequestHandler<CorrectOrderItemCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CorrectOrderItemCommand req, CancellationToken ct)
    {
        var data = req.Data;
        var reason = data.Reason?.Trim() ?? "";
        if (data.RequestId == Guid.Empty || reason.Length is < 5 or > 500
            || data.Disposition is not ("RegistrationError" or "PreparedWaste") || data.ModifierSelections is null || data.ModifierSelections.Count > 100)
            throw new InvalidOperationException("Selecciona el tratamiento e indica un motivo de 5 a 500 caracteres.");
        if (!data.Replace && data.ModifierSelections.Count > 0)
            throw new InvalidOperationException("La cancelacion no admite opciones nuevas.");
        var actor = await db.UserRoles.Where(r => r.UserId == req.ActorUserId && r.BranchId == req.BranchId && !r.IsDeleted
            && r.User != null && r.User.BranchId == req.BranchId && r.User.IsActive && !r.User.IsDeleted
            && r.Role != null && r.Role.BranchId == req.BranchId && r.Role.IsActive && !r.Role.IsDeleted
            && (r.Role.Name == AppConstants.Roles.Admin || r.Role.RolePermissions.Any(p => p.BranchId == req.BranchId && !p.IsDeleted
                && p.Permission != null && p.Permission.BranchId == req.BranchId && p.Permission.IsActive && !p.Permission.IsDeleted
                && p.Permission.Code == AppConstants.Permissions.PosOrdersCorrectItems)))
            .Select(r => new { r.User!.FirstName, r.User.LastName }).FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("No tienes un rol activo con permiso para corregir platos sin cobrar.");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {
            req.OrderItemId, data.Replace, data.Disposition, Reason = reason,
            Options = data.ModifierSelections.OrderBy(s => s.ModifierOptionId).ThenBy(s => s.Quantity),
        }))));
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var orderId = await db.OrderItems.Where(i => i.Id == req.OrderItemId && i.BranchId == req.BranchId && !i.IsDeleted)
            .Select(i => (Guid?)i.OrderId).SingleOrDefaultAsync(ct) ?? throw new InvalidOperationException("Plato no encontrado.");
        await PosOrderLock.AcquireAsync(db, req.BranchId, orderId, ct);
        var applied = await db.OrderItemCorrections.SingleOrDefaultAsync(c => c.BranchId == req.BranchId && c.RequestId == data.RequestId, ct);
        if (applied != null)
        {
            if (applied.RequestHash != hash) throw new InvalidOperationException("Esta correccion ya se uso con otros datos. Actualiza el pedido.");
            await tx.CommitAsync(ct);
            return await Load(orderId, [], ct);
        }
        var order = await db.Orders.Include(o => o.Payments.Where(p => !p.IsDeleted)).ThenInclude(p => p.Items.Where(i => !i.IsDeleted))
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.ModifierSelections.Where(s => !s.IsDeleted))
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.TaxRate)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.MenuItem)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Station)
            .SingleAsync(o => o.Id == orderId && o.BranchId == req.BranchId && !o.IsDeleted, ct);
        var item = order.Items.Single(i => i.Id == req.OrderItemId);
        var payments = order.Payments.Where(p => !p.IsDeleted).ToList();
        if (order.Status == OrderStatus.Cancelled || item.Status == OrderItemStatus.Cancelled)
            throw new InvalidOperationException("El pedido o plato ya esta cancelado. Actualiza la cuenta.");
        if (order.PaidAt.HasValue || payments.Any(p => p.Items.Count == 0)
            || payments.SelectMany(p => p.Items).Any(i => !i.IsDeleted && i.OrderItemId == item.Id))
            throw new InvalidOperationException("Solo se pueden corregir platos sin cobrar y con pagos detallados.");
        if (data.Disposition == "PreparedWaste" && order.Status == OrderStatus.Draft)
            throw new InvalidOperationException("Un borrador no tiene consumos reservados para registrar merma.");

        OrderItem? replacement = null;
        if (data.Replace)
        {
            var menu = await db.MenuItems.Include(m => m.ModifierGroups.Where(g => !g.IsDeleted && g.IsActive))
                .ThenInclude(g => g.Options.Where(o => !o.IsDeleted && o.IsActive))
                .SingleOrDefaultAsync(m => m.Id == item.MenuItemId && m.BranchId == req.BranchId && !m.IsDeleted && m.IsActive && m.AvailableForSale, ct)
                ?? throw new InvalidOperationException("El plato ya no esta disponible en el menu.");
            await ComboModifierChoices.LoadAsync(db, req.BranchId, [menu], ct);
            var selections = PosMapper.BuildModifierSelections(req.BranchId,
                new CreateOrderItemDto { MenuItemId = item.MenuItemId, Quantity = item.Quantity, ModifierSelections = data.ModifierSelections }, menu);
            var oldOptions = item.ModifierSelections.OrderBy(s => s.ModifierOptionId).Select(s => (s.ModifierOptionId, s.Quantity)).ToList();
            var newOptions = selections.OrderBy(s => s.ModifierOptionId).Select(s => (s.ModifierOptionId, s.Quantity)).ToList();
            if (oldOptions.SequenceEqual(newOptions)) throw new InvalidOperationException("Selecciona una opcion diferente a la original.");
            var newPrice = item.UnitPrice - item.ModifierSelections.Sum(s => s.UnitPriceDelta * s.Quantity)
                + selections.Sum(s => s.UnitPriceDelta * s.Quantity);
            if (newPrice < 0) throw new InvalidOperationException("El precio corregido no es valido.");
            var discount = item.DiscountAmount;
            if (newPrice != item.UnitPrice)
            {
                var promotion = item.PromotionId.HasValue ? await db.Promotions.SingleOrDefaultAsync(p => p.Id == item.PromotionId && p.BranchId == req.BranchId && !p.IsDeleted, ct) : null;
                if (item.PromotionId.HasValue && promotion == null) throw new InvalidOperationException("No se puede recalcular una promocion eliminada.");
                discount = PosMapper.CalcItemWithPromotion(newPrice, item.Quantity, item.DiscountPct, item.TaxRate?.Percentage, promotion).DiscountAmount;
            }
            var pricing = PosMapper.CalcItemFromDiscountAmount(newPrice, item.Quantity, discount, item.TaxRate?.Percentage);
            replacement = new OrderItem { BranchId = req.BranchId, OrderId = order.Id, MenuItemId = item.MenuItemId,
                StationId = item.StationId, Quantity = item.Quantity, UnitPrice = newPrice, DiscountAmount = pricing.discountAmount,
                DiscountPct = newPrice == item.UnitPrice ? item.DiscountPct : newPrice * item.Quantity == 0 ? 0 : pricing.discountAmount / (newPrice * item.Quantity) * 100m,
                TaxRateId = item.TaxRateId, TaxRate = item.TaxRate, TaxAmount = pricing.taxAmount, TotalPrice = pricing.totalPrice,
                PromotionId = item.PromotionId, PromotionName = item.PromotionName, Notes = item.Notes, IsTakeout = item.IsTakeout,
                ModifierSelections = selections, Status = OrderItemStatus.Pending };
            db.OrderItems.Add(replacement);
            await ComboPreparation.InitializeAsync(db, req.BranchId, [replacement], ct);
        }
        var reservations = await db.StockReservations.Where(r => r.BranchId == req.BranchId && r.OrderItemId == item.Id
            && r.Status == StockReservationStatus.Active).ToListAsync(ct);
        var articleIds = reservations.Select(r => r.ArticleId).ToList();
        if (replacement != null && order.Status != OrderStatus.Draft)
        {
            var menus = await MenuRecipeExpansion.LoadMenuItemsWithRecipeAsync(db, req.BranchId, [item.MenuItemId], ct);
            articleIds.AddRange(menus.SelectMany(MenuRecipeExpansion.ReferencedArticleIds));
        }
        var ids = articleIds.Distinct().Order().ToArray();
        if (ids.Length > 0)
        {
            // Lock the complete old/new ingredient set in one consistent order before reallocation.
            _ = await db.InventoryArticles.FromSqlInterpolated($"""
                SELECT * FROM inv."InventoryArticles" WHERE "BranchId" = {req.BranchId}
                AND "Id" = ANY({ids}) AND NOT "IsDeleted" ORDER BY "Id" FOR NO KEY UPDATE
                """).AsNoTracking().ToListAsync(ct);
            _ = await db.WarehouseStock.FromSqlInterpolated($"""
                SELECT * FROM inv."WarehouseStock" WHERE "BranchId" = {req.BranchId}
                AND "ArticleId" = ANY({ids}) AND NOT "IsDeleted" ORDER BY "ArticleId", "WarehouseId" FOR UPDATE
                """).AsNoTracking().ToListAsync(ct);
        }
        var correction = new OrderItemCorrection { BranchId = req.BranchId, RequestId = data.RequestId, RequestHash = hash,
            OriginalItemId = item.Id, ReplacementItemId = replacement?.Id, OriginalStatus = item.Status,
            Disposition = data.Disposition, Reason = reason, ActorUserId = req.ActorUserId,
            ActorName = $"{actor.FirstName} {actor.LastName}".Trim() };
        db.OrderItemCorrections.Add(correction);
        item.Correction = correction;
        item.Status = OrderItemStatus.Cancelled;
        foreach (var task in item.PreparationTasks) task.Status = OrderItemStatus.Cancelled;
        foreach (var reservation in reservations)
        {
            reservation.Status = data.Disposition == "PreparedWaste" ? StockReservationStatus.Consumed : StockReservationStatus.Released;
            if (data.Disposition == "PreparedWaste") reservation.ConsumedAt = DateTime.UtcNow;
            else reservation.ReleasedAt = DateTime.UtcNow;
        }
        PosMapper.RecalculateOrderTotals(order);
        var paidAmount = payments.Sum(p => p.OrderAmount);
        if (paidAmount > order.Total + 0.01m)
            throw new InvalidOperationException("La correccion dejaria pagos superiores al total del pedido.");
        if (payments.Count > 0 && paidAmount >= order.Total - 0.01m) order.PaidAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        if (data.Disposition == "PreparedWaste")
        {
            var movements = new RegisterMovementHandler(db);
            var movementNote = $"Correccion {correction.Id}: {reason}";
            foreach (var reservation in reservations.OrderBy(r => r.ArticleId).ThenBy(r => r.WarehouseId))
            {
                var balance = await db.StockMovements.Where(m => m.BranchId == req.BranchId && m.ArticleId == reservation.ArticleId
                    && m.WarehouseId == reservation.WarehouseId && !m.IsDeleted).SumAsync(m => m.BaseQuantity, ct);
                var otherReserved = await db.StockReservations.Where(r => r.BranchId == req.BranchId && r.ArticleId == reservation.ArticleId
                    && r.WarehouseId == reservation.WarehouseId && r.Status == StockReservationStatus.Active).SumAsync(r => r.BaseQuantity, ct);
                if (balance - reservation.BaseQuantity < otherReserved - 0.0001m)
                    throw new InvalidOperationException("Stock insuficiente para registrar la merma sin afectar otras reservas.");
                await movements.Handle(new RegisterMovementCommand { BranchId = req.BranchId, ArticleId = reservation.ArticleId,
                    WarehouseId = reservation.WarehouseId, UnitId = reservation.UnitId, Quantity = -reservation.BaseQuantity,
                    Type = MovementType.Waste, OrderItemId = item.Id, StockReservationId = reservation.Id,
                    Reference = $"Orden #{order.Number}", Notes = movementNote.Length <= 500 ? movementNote : movementNote[..500] }, ct);
            }
        }
        if (replacement != null && order.Status != OrderStatus.Draft)
            await StockReservationService.ReserveOrderItemsAsync(db, req.BranchId, order.Id, [replacement], ct);
        var active = order.Items.Where(i => !i.IsDeleted && i.Status != OrderItemStatus.Cancelled).ToList();
        if (active.Count == 0) { order.Status = OrderStatus.Cancelled; order.DeliveredAt = null; }
        else if (order.Status != OrderStatus.Draft && (replacement != null || order.Status != OrderStatus.Delivered))
        {
            if (order.Status == OrderStatus.Delivered)
                foreach (var served in active.Where(i => i != replacement)) served.Status = OrderItemStatus.Ready;
            KitchenOrderState.Recalculate(order);
            order.DeliveredAt = null;
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await Load(order.Id, replacement == null ? [] : [replacement.Id], ct);
    }

    private async Task<OrderDto> Load(Guid orderId, List<Guid> added, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().Include(o => o.Table).Include(o => o.Payments.Where(p => !p.IsDeleted))
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.MenuItem)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Station)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.ModifierSelections.Where(s => !s.IsDeleted))
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Correction)
            .AsSplitQuery().SingleAsync(o => o.Id == orderId, ct);
        var dto = PosMapper.MapOrder(order); dto.AddedItemIds = added;
        return dto;
    }
}
