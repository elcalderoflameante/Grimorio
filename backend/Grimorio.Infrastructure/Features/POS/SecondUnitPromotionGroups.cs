using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Features.POS.Commands;
using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.POS;

internal static class SecondUnitPromotionGroups
{
    public static async Task RecalculateAsync(GrimorioDbContext db, Order order, CancellationToken ct)
    {
        var items = order.Items.Where(i => !i.IsDeleted).ToList();
        var taxIds = items.Where(i => i.TaxRateId.HasValue).Select(i => i.TaxRateId!.Value).Distinct().ToArray();
        var taxes = await db.TaxRates.Where(t => taxIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, ct);
        foreach (var item in items)
            if (item.TaxRateId.HasValue && taxes.TryGetValue(item.TaxRateId.Value, out var tax)) item.TaxRate = tax;
        var ids = items.Select(i => i.PromotionId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToArray();
        if (ids.Length == 0) return;
        var promotions = await db.Promotions.Where(p => ids.Contains(p.Id) && p.BranchId == order.BranchId)
            .ToDictionaryAsync(p => p.Id, ct);
        var payments = await db.OrderPayments.Where(p => p.OrderId == order.Id && p.BranchId == order.BranchId && !p.IsDeleted)
            .Include(p => p.Items.Where(i => !i.IsDeleted)).ToListAsync(ct);
        // Legacy payments without line details cannot be repriced safely.
        if (payments.Any(p => p.Items.Count == 0)) return;
        var paidIds = payments.SelectMany(p => p.Items).Select(i => i.OrderItemId).ToHashSet();
        var frozenGroups = items.Where(i => paidIds.Contains(i.Id) && i.PromotionGroupId.HasValue)
            .Select(i => i.PromotionGroupId!.Value).ToHashSet();
        if (items.Any(i => i.PromotionGroupId.HasValue && frozenGroups.Contains(i.PromotionGroupId.Value)
            && i.Status == OrderItemStatus.Cancelled))
            throw new InvalidOperationException("No se puede cambiar una pareja promocional con cobros registrados. Revisa sus pagos primero.");
        var legacyPaid = items.Where(i => paidIds.Contains(i.Id) && !i.PromotionGroupId.HasValue)
            .Select(i => (i.MenuItemId, i.PromotionId)).ToHashSet();
        var candidates = items.Where(i => i.Status != OrderItemStatus.Cancelled && i.PromotionId.HasValue
            && promotions.TryGetValue(i.PromotionId.Value, out var p) && p.Type == PromotionType.SecondUnitPercentage
            && (!i.PromotionGroupId.HasValue || !frozenGroups.Contains(i.PromotionGroupId.Value))
            && !legacyPaid.Contains((i.MenuItemId, i.PromotionId)));
        foreach (var group in candidates.GroupBy(i => (i.MenuItemId, i.PromotionId)))
        {
            var lines = group.OrderByDescending(i => i.UnitPrice).ThenBy(i => i.CreatedAt).ThenBy(i => i.Id).ToList();
            var promotion = promotions[group.Key.PromotionId!.Value];
            var groupId = lines.Select(i => i.PromotionGroupId).FirstOrDefault(id => id.HasValue) ?? Guid.NewGuid();
            decimal discountedBase = 0;
            long position = 0;
            foreach (var line in lines)
            {
                // Pair in descending price order; the cheaper unit in each pair is discounted.
                discountedBase += ((position + line.Quantity) / 2 - position / 2) * line.UnitPrice;
                position += line.Quantity;
            }
            var gross = lines.Sum(i => i.UnitPrice * i.Quantity);
            var cashDiscount = Math.Round(discountedBase * (promotion.DiscountPercent ?? 0m) / 100m, 2);
            var cardDiscount = promotion.PaymentPolicy switch {
                PromotionPaymentPolicy.CashTransferOnly => 0m,
                PromotionPaymentPolicy.CardAlternativePercentage => Math.Round(discountedBase * (promotion.CardDiscountPercent ?? 0m) / 100m, 2),
                _ => cashDiscount,
            };
            decimal cumulative = 0, previousCash = 0, previousCard = 0;
            foreach (var line in lines)
            {
                cumulative += line.UnitPrice * line.Quantity;
                var nextCash = gross == 0 ? 0 : Math.Round(cashDiscount * cumulative / gross, 2);
                var nextCard = gross == 0 ? 0 : Math.Round(cardDiscount * cumulative / gross, 2);
                line.PromotionGroupId = groupId;
                line.PromotionCashDiscountAmount = nextCash - previousCash;
                line.PromotionCardDiscountAmount = promotion.PaymentPolicy == PromotionPaymentPolicy.CardAlternativePrice
                    ? PromotionEngine.Calculate(promotion, line.UnitPrice, line.Quantity, true).DiscountAmount
                    : nextCard - previousCard;
                previousCash = nextCash; previousCard = nextCard;
                var pricing = PosMapper.CalcItemFromDiscountAmount(line.UnitPrice, line.Quantity,
                    line.PromotionCashDiscountAmount.Value, line.TaxRate?.Percentage);
                line.DiscountAmount = pricing.discountAmount;
                line.TotalPrice = pricing.totalPrice;
                line.TaxAmount = pricing.taxAmount;
                line.DiscountPct = line.UnitPrice * line.Quantity == 0 ? 0
                    : Math.Round(line.DiscountAmount / (line.UnitPrice * line.Quantity) * 100m, 2);
            }
        }
        PosMapper.RecalculateOrderTotals(order);
    }
}
