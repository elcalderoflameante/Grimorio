using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Features.POS.Commands;

namespace Grimorio.Infrastructure.Features.Billing;

public static class OrderPaymentPricing
{
    public static decimal GetLineTotal(OrderItem item, bool useCardPrice)
    {
        if (item.Promotion == null) return item.TotalPrice;
        return PosMapper.CalcItemWithPromotion(item.UnitPrice, item.Quantity,
            item.DiscountPct, item.TaxRate?.Percentage, item.Promotion, useCardPrice).TotalPrice;
    }

    public static decimal AllocateTotal(decimal total, decimal quantity, decimal paidQuantity, decimal selectedQuantity)
    {
        if (quantity <= 0 || paidQuantity < 0 || selectedQuantity < 0 || paidQuantity + selectedQuantity > quantity)
            throw new ArgumentOutOfRangeException(nameof(selectedQuantity));

        // Cumulative rounding distributes cents without changing the full promotion total.
        return Math.Round(total * (paidQuantity + selectedQuantity) / quantity, 2, MidpointRounding.AwayFromZero)
            - Math.Round(total * paidQuantity / quantity, 2, MidpointRounding.AwayFromZero);
    }

    public static void UpdateItemTotal(OrderItem item, decimal paidTotal, decimal newTotal, decimal pendingTotal)
    {
        var total = paidTotal + newTotal + pendingTotal;
        var gross = item.UnitPrice * item.Quantity;
        item.TotalPrice = total;
        item.DiscountAmount = gross - total;
        item.DiscountPct = gross > 0 ? Math.Round(item.DiscountAmount / gross * 100m, 2) : 0;
        var taxPct = item.TaxRate?.Percentage ?? 0m;
        item.TaxAmount = taxPct > 0 ? total - Math.Round(total / (1m + taxPct / 100m), 2) : 0;
    }
}
