using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Features.Billing;

static void Equal(decimal expected, decimal actual)
{
    if (expected != actual) throw new Exception($"Expected {expected}, got {actual}.");
}

var promotion = new Promotion
{
    Id = Guid.NewGuid(), Type = PromotionType.FixedPrice,
    FixedPrice = 7.99m, CardPrice = 8.99m,
    PaymentPolicy = PromotionPaymentPolicy.CardAlternativePrice,
};
var item = new OrderItem
{
    UnitPrice = 11m, Quantity = 2, TotalPrice = 15.98m,
    Promotion = promotion, PromotionId = promotion.Id,
};
var cash = OrderPaymentPricing.AllocateTotal(OrderPaymentPricing.GetLineTotal(item, false), 2, 0, 1);
var card = OrderPaymentPricing.AllocateTotal(OrderPaymentPricing.GetLineTotal(item, true), 2, 1, 1);
Equal(7.99m, cash);
Equal(8.99m, card);
OrderPaymentPricing.UpdateItemTotal(item, cash, card, 0);
Equal(16.98m, item.TotalPrice);
Equal(5.02m, item.DiscountAmount);
if (item.PromotionId != promotion.Id) throw new Exception("Promotion reference was lost.");

// Reversing payment methods must not use the blended line total as a new unit price.
OrderPaymentPricing.UpdateItemTotal(item, 0, card, cash);
Equal(7.99m, OrderPaymentPricing.AllocateTotal(OrderPaymentPricing.GetLineTotal(item, false), 2, 1, 1));
promotion.PaymentPolicy = PromotionPaymentPolicy.CashTransferOnly;
Equal(11m, OrderPaymentPricing.AllocateTotal(OrderPaymentPricing.GetLineTotal(item, true), 2, 0, 1));
Equal(7.99m, OrderPaymentPricing.AllocateTotal(OrderPaymentPricing.GetLineTotal(item, false), 2, 1, 1));

promotion.Type = PromotionType.BuyXPayY;
promotion.BuyQuantity = 3;
promotion.PayQuantity = 2;
item.Quantity = 3;
item.UnitPrice = 10;
var total = OrderPaymentPricing.GetLineTotal(item, false);
Equal(20m, total);
Equal(6.67m, OrderPaymentPricing.AllocateTotal(total, 3, 0, 1));
Equal(6.66m, OrderPaymentPricing.AllocateTotal(total, 3, 1, 1));
Equal(6.67m, OrderPaymentPricing.AllocateTotal(total, 3, 2, 1));
Equal(total, Enumerable.Range(0, 3).Sum(paid => OrderPaymentPricing.AllocateTotal(total, 3, paid, 1)));
Console.WriteLine("Payment promotion pricing checks passed.");
