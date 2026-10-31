using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Features.Billing;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.Menu;
using Grimorio.Infrastructure.Services.Sri;

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

promotion.Type = PromotionType.SecondUnitPercentage;
promotion.DiscountPercent = 50;
promotion.PaymentPolicy = PromotionPaymentPolicy.AnyPayment;
item.UnitPrice = 20;
foreach (var (quantity, expected) in new[] { (1, 20m), (2, 30m), (3, 50m), (4, 60m), (5, 80m), (6, 90m) })
{
    item.Quantity = quantity;
    Equal(expected, OrderPaymentPricing.GetLineTotal(item, false));
    Equal(expected, OrderPaymentPricing.GetLineTotal(item, true));
    Equal(expected, Enumerable.Range(0, quantity).Sum(paid =>
        OrderPaymentPricing.AllocateTotal(expected, quantity, paid, 1)));
}
item.Quantity = 2;
Equal(15m, OrderPaymentPricing.AllocateTotal(OrderPaymentPricing.GetLineTotal(item, false), 2, 0, 1));
Equal(15m, OrderPaymentPricing.AllocateTotal(OrderPaymentPricing.GetLineTotal(item, false), 2, 1, 1));
promotion.PaymentPolicy = PromotionPaymentPolicy.CashTransferOnly;
Equal(40m, OrderPaymentPricing.GetLineTotal(item, true));
Equal(30m, OrderPaymentPricing.GetLineTotal(item, false));
promotion.PaymentPolicy = PromotionPaymentPolicy.CardAlternativePrice;
promotion.CardPrice = 18;
Equal(36m, OrderPaymentPricing.GetLineTotal(item, true));
Equal(30m, OrderPaymentPricing.GetLineTotal(item, false));
promotion.PaymentPolicy = PromotionPaymentPolicy.AnyPayment;
promotion.DiscountPercent = 100;
item.Quantity = 3;
Equal(40m, OrderPaymentPricing.GetLineTotal(item, false));
promotion.DiscountPercent = 50;
item.Quantity = 2;
item.UnitPrice = .05m;
Equal(.08m, OrderPaymentPricing.GetLineTotal(item, false));
Console.WriteLine("Second-unit promotion pricing and partial payment checks passed.");

promotion.PaymentPolicy = PromotionPaymentPolicy.CardAlternativePercentage;
promotion.CardDiscountPercent = 30;
item.UnitPrice = 20;
foreach (var (quantity, cashTotal, cardTotal) in new[] { (1,20m,20m), (2,30m,34m), (3,50m,54m), (4,60m,68m), (5,80m,88m), (6,90m,102m) })
{
    item.Quantity = quantity;
    Equal(cashTotal, OrderPaymentPricing.GetLineTotal(item, false));
    Equal(cardTotal, OrderPaymentPricing.GetLineTotal(item, true));
}
item.Quantity = 2;
foreach (var cardFirst in new[] { false, true })
{
    var first = OrderPaymentPricing.AllocateTotal(OrderPaymentPricing.GetLineTotal(item, cardFirst), 2, 0, 1);
    var second = OrderPaymentPricing.AllocateTotal(OrderPaymentPricing.GetLineTotal(item, !cardFirst), 2, 1, 1);
    Equal(32m, first + second);
}
promotion.Type = PromotionType.Percentage;
Equal(20m, OrderPaymentPricing.GetLineTotal(item, false));
Equal(28m, OrderPaymentPricing.GetLineTotal(item, true));
promotion.Type = PromotionType.SecondUnitPercentage;
promotion.CardDiscountPercent = 0;
Equal(40m, OrderPaymentPricing.GetLineTotal(item, true));
promotion.CardDiscountPercent = 100;
Equal(20m, OrderPaymentPricing.GetLineTotal(item, true));
promotion.CardDiscountPercent = 50;
item.UnitPrice = .05m;
Equal(.08m, OrderPaymentPricing.GetLineTotal(item, true));
Console.WriteLine("Alternative card percentage pricing checks passed.");

var menuItemId = Guid.Parse("12345678-1234-1234-1234-123456789abc");
var invoiceItem = new OrderItem
{
    MenuItemId = menuItemId,
    MenuItem = new MenuItem { InternalCode = "M-ABCDEF", Name = "Costillas ahumadas del reino" },
};
var invoicePayment = new OrderPayment
{
    OrderAmount = 3.50m,
    Items = [new OrderPaymentItem
    {
        OrderItem = invoiceItem, Quantity = 1m, UnitPrice = 3.50m, Total = 3.50m,
        ItemCode = "Costillas ahumadas del reino", ItemName = "Costillas",
        TaxRateSriCode = "4", TaxRatePercentage = 15m,
    }],
};
var invoiceOrder = new Order();
var recoveredLine = SriInvoiceLineFactory.Build(invoicePayment, invoiceOrder).Single();
if (recoveredLine.Code != "M-ABCDEF" || recoveredLine.Description != "Costillas")
    throw new Exception("Legacy invoice code was not recovered without changing its description.");
Equal(3.50m, SriInvoiceLineFactory.CalculateTotals([recoveredLine], 3.50m).Total);
invoicePayment.Items.Single().ItemCode = new string('A', 25);
if (SriInvoiceLineFactory.Build(invoicePayment, invoiceOrder).Single().Code != new string('A', 25))
    throw new Exception("A valid saved invoice code must remain unchanged.");
invoicePayment.Items.Single().ItemCode = new string('A', 26);
invoiceItem.MenuItem.InternalCode = new string('B', 26);
static void RequiresManualCode(Action action)
{
    try { action(); }
    catch (InvalidOperationException ex) when (ex.Message.Contains("Costillas") && ex.Message.Contains("25"))
    {
        return;
    }
    throw new Exception("Invalid or missing invoice codes must require manual correction.");
}
RequiresManualCode(() => SriInvoiceLineFactory.Build(invoicePayment, invoiceOrder));
if (invoiceItem.MenuItem.InternalCode != new string('B', 26))
    throw new Exception("Invalid menu codes must not be replaced automatically.");
RequiresManualCode(() => SriInvoiceLineFactory.ResolveCode("Costillas", null, " "));
if (SriInvoiceLineFactory.ResolveCode("Costillas", " ", " M-ABCDEF ") != "M-ABCDEF")
    throw new Exception("Blank codes must fall back to the trimmed current code.");
Console.WriteLine("Invoice legacy code recovery checks passed.");
