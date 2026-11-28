using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Application.Features.Menu.Commands;
using Grimorio.Application.Features.POS.Commands;
using Grimorio.Domain.Entities.Inventory;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Services.Sri;
using MediatR;
using Microsoft.EntityFrameworkCore;

internal static class SecondUnitPromotionChecks
{
    public static async Task Run(IServiceProvider services, Guid branch, Guid foreign, Guid actor, Guid dish,
        Guid size, Guid flavor, Guid cash, Guid session, Action<bool, string> check)
    {
        async Task<T> Send<T>(IRequest<T> request) {
            await using var scope = services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request);
        }
        UpsertPromotionDto Data(decimal? percentage = 50) => new() {
            Name = "Second ribeye half price", Type = "SecondUnitPercentage", DiscountPercent = percentage, MenuItemIds = [dish],
        };
        async Task Reject(UpsertPromotionDto data, Guid? targetBranch = null) {
            try { await Send(new CreatePromotionCommand { BranchId = targetBranch ?? branch, Data = data }); }
            catch (InvalidOperationException) { check(true, "Invalid promotion rejected"); return; }
            throw new Exception("Invalid promotion was accepted");
        }
        foreach (var percentage in new decimal?[] { null, 0, -1, 100.01m }) await Reject(Data(percentage));
        var invalidType = Data(); invalidType.Type = "999"; await Reject(invalidType);
        var invalidDates = Data(); invalidDates.StartsOn = new DateOnly(2026, 10, 31); invalidDates.EndsOn = new DateOnly(2026, 10, 30);
        await Reject(invalidDates);
        await Reject(Data(), foreign);
        foreach (var percentage in new decimal?[] { null, -1, 100.01m }) {
            var invalid = Data(); invalid.PaymentPolicy = "CardAlternativePercentage"; invalid.CardDiscountPercent = percentage;
            await Reject(invalid);
        }
        foreach (var type in new[] { "FixedPrice", "FixedAmount", "BuyXPayY" }) {
            var invalid = Data(); invalid.Type = type; invalid.PaymentPolicy = "CardAlternativePercentage"; invalid.CardDiscountPercent = 30;
            await Reject(invalid);
        }
        var promotion = await Send(new CreatePromotionCommand { BranchId = branch, Data = Data() });
        check(promotion.Type == "SecondUnitPercentage" && promotion.DiscountPercent == 50 && promotion.IsCurrentlyActive,
            "Second-unit promotion is persisted and mapped as active");
        async Task<OrderDto> Order(int quantity) {
            var order = await Send(new CreateOrderCommand { BranchId = branch, Type = "Takeout", Items = [new() {
                MenuItemId = dish, Quantity = quantity, PromotionId = promotion.Id,
                ModifierSelections = [new() { ModifierOptionId = size, Quantity = 1 }, new() { ModifierOptionId = flavor, Quantity = 1 }],
            }] });
            check(order.Total == quantity * 10m - (quantity / 2) * 5m && order.Items.Single().PromotionId == promotion.Id,
                $"Quantity {quantity}: only complete pairs receive the second-unit discount");
            return order;
        }
        foreach (var quantity in new[] { 1, 2, 3, 4, 5, 6 }) await Order(quantity);
        var split = await Order(2);
        await Send(new ConfirmOrderCommand { BranchId = branch, OrderId = split.Id });
        async Task<decimal> Consumed() {
            await using var scope = services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<GrimorioDbContext>().StockMovements
                .Where(m => m.BranchId == branch && m.Type == MovementType.SaleDeduction).SumAsync(m => m.BaseQuantity);
        }
        var before = await Consumed();
        var requests = new[] { Guid.NewGuid(), Guid.NewGuid() };
        async Task<OrderPaymentDto> Pay(int index) => await Send(new PayOrderCommand {
            BranchId = branch, UserId = actor, OrderId = split.Id, CashSessionId = session, IdempotencyKey = requests[index],
            OrderAmount = 7.50m, Items = [new() { OrderItemId = split.Items.Single().Id, Quantity = 1 }],
            Lines = [new() { MethodId = cash, AmountTendered = 7.50m }],
        });
        var first = await Pay(0); var second = await Pay(1); var after = await Consumed();
        check(first.OrderAmount + second.OrderAmount == 15, "Partial payments preserve the pair's total savings");
        await using (var scope = services.CreateAsyncScope()) {
            var db = scope.ServiceProvider.GetRequiredService<GrimorioDbContext>();
            var saved = await db.OrderPayments.Include(p => p.Items).ThenInclude(i => i.OrderItem).ThenInclude(i => i!.MenuItem)
                .SingleAsync(p => p.Id == first.Id);
            var savedSecond = await db.OrderPayments.Include(p => p.Items).SingleAsync(p => p.Id == second.Id);
            check(saved.Items.Single().OriginalTotal == 10 && savedSecond.Items.Single().OriginalTotal == 10,
                "Partial payments preserve full normal prices in their snapshots");
            var order = await db.Orders.Include(o => o.Items).SingleAsync(o => o.Id == split.Id);
            var totals = SriInvoiceLineFactory.CalculateTotals(SriInvoiceLineFactory.Build(saved, order), saved.OrderAmount);
            check(totals.Total == 7.50m && totals.TotalDiscount > 0, "Invoice snapshots retain the second-unit promotion discount");
        }
        check(after < before, "Promotion still consumes inventory for the actual portions sold");
        check((await Pay(0)).Id == first.Id && await Consumed() == after, "Checkout retry never repeats payment or inventory consumption");
        var alternative = Data(); alternative.PaymentPolicy = "CardAlternativePercentage"; alternative.CardDiscountPercent = 30;
        var withCard = await Send(new UpdatePromotionCommand { Id = promotion.Id, BranchId = branch, Data = alternative });
        check(withCard.CardDiscountPercent == 30 && withCard.DiscountPercent == 50 && withCard.PaymentPolicy == "CardAlternativePercentage",
            "Both percentages survive persistence and mapping");
        var card = new PaymentMethodConfig { Name = "Test card " + Guid.NewGuid(), IsCard = true };
        var transfer = new PaymentMethodConfig { Name = "Test transfer " + Guid.NewGuid() };
        var bank = new CardBank { BranchId = branch, Name = "Test bank " + Guid.NewGuid() };
        await using (var scope = services.CreateAsyncScope()) {
            var db = scope.ServiceProvider.GetRequiredService<GrimorioDbContext>();
            db.AddRange(card, transfer, bank); await db.SaveChangesAsync();
        }
        foreach (var methods in new[] { new[] { card.Id, card.Id }, new[] { cash, card.Id }, new[] { card.Id, cash }, new[] { transfer.Id, transfer.Id } }) {
            var mixed = await Order(2);
            await Send(new ConfirmOrderCommand { BranchId = branch, OrderId = mixed.Id });
            var paid = new List<OrderPaymentDto>();
            foreach (var method in methods) {
                var isCard = method == card.Id;
                var amount = isCard ? 8.50m : 7.50m;
                paid.Add(await Send(new PayOrderCommand {
                    BranchId = branch, UserId = actor, OrderId = mixed.Id, CashSessionId = session, IdempotencyKey = Guid.NewGuid(),
                    OrderAmount = amount, Items = [new() { OrderItemId = mixed.Items.Single().Id, Quantity = 1 }],
                    Lines = [new() { MethodId = method, AmountTendered = amount, CardPaymentType = isCard ? "Credit" : null,
                        CardBankId = isCard ? bank.Id : null, CardBrand = isCard ? "Visa" : null, AuthorizationNumber = isCard ? "TEST123" : null }],
                }));
            }
            var expected = methods.Sum(method => method == card.Id ? 8.50m : 7.50m);
            check(paid.Sum(p => p.OrderAmount) == expected, "Card, cash and transfer payments keep their own discount");
            await using var readScope = services.CreateAsyncScope();
            var stored = await readScope.ServiceProvider.GetRequiredService<GrimorioDbContext>().Orders
                .Include(o => o.Items).SingleAsync(o => o.Id == mixed.Id);
            check(stored.Total == expected && stored.Items.Single().DiscountAmount == 20m - expected,
                "Paid order total and savings match its actual payment methods");
        }
        var zero = Data(); zero.PaymentPolicy = "CardAlternativePercentage"; zero.CardDiscountPercent = 0;
        check((await Send(new UpdatePromotionCommand { Id = promotion.Id, BranchId = branch, Data = zero })).CardDiscountPercent == 0,
            "Zero card discount is accepted without removing the cash discount");
        var updated = Data(25);
        var edited = await Send(new UpdatePromotionCommand { Id = promotion.Id, BranchId = branch, Data = updated });
        check(edited.DiscountPercent == 25 && edited.Type == "SecondUnitPercentage", "Second-unit promotion percentage can be edited");
        await Send(new UpdatePromotionCommand { Id = promotion.Id, BranchId = branch, Data = alternative });
        async Task<OrderDto> Separate(int count) => await Send(new CreateOrderCommand {
            BranchId = branch, Type = "Takeout", Items = Enumerable.Range(0,count).Select(index => new CreateOrderItemDto {
                MenuItemId = dish, Quantity = 1, PromotionId = promotion.Id, Notes = "Different preparation " + index,
                ModifierSelections = [new() { ModifierOptionId = size, Quantity = 1 }, new() { ModifierOptionId = flavor, Quantity = 1 }],
            }).ToList(),
        });
        var separated = await Separate(2);
        check(separated.Total == 15 && separated.Items.Count == 2 && separated.Items.Sum(i => i.DiscountAmount) == 5,
            "Two separate ribeye lines form a promotional pair without merging their options");
        check(separated.Items.Select(i => i.Notes).Distinct().Count() == 2
            && separated.Items.All(i => i.PromotionGroupId.HasValue && i.ModifierSelections.Count == 2), "Preparation details survive grouping");
        check((await Separate(3)).Total == 25 && (await Separate(4)).Total == 30, "Separate odd and even lines preserve the number of pairs");
        await using (var scope = services.CreateAsyncScope()) {
            var db = scope.ServiceProvider.GetRequiredService<GrimorioDbContext>();
            var old = await db.Orders.Include(o => o.Items).SingleAsync(o => o.Id == separated.Id);
            foreach (var line in old.Items) {
                line.PromotionGroupId = null; line.PromotionCashDiscountAmount = null; line.PromotionCardDiscountAmount = null;
                line.DiscountAmount = 0; line.DiscountPct = 0; line.TotalPrice = 10;
            }
            old.Total = 20; old.DiscountTotal = 0;
            await db.SaveChangesAsync();
        }
        var stockBeforeReprice = await Consumed();
        separated = await Send(new RecalculateOrderPromotionsCommand(branch, separated.Id));
        check(separated.Total == 15 && await Consumed() == stockBeforeReprice, "Existing unpaid orders can be corrected without consuming inventory");
        check((await Send(new RecalculateOrderPromotionsCommand(branch, separated.Id))).Total == 15, "Repeated recalculation does not repeat the discount");
        await Send(new ConfirmOrderCommand { BranchId = branch, OrderId = separated.Id });
        var firstLine = separated.Items[0]; var secondLine = separated.Items[1];
        await Send(new PayOrderCommand { BranchId = branch, UserId = actor, OrderId = separated.Id, CashSessionId = session,
            IdempotencyKey = Guid.NewGuid(), OrderAmount = firstLine.TotalPrice,
            Items = [new() { OrderItemId = firstLine.Id, Quantity = 1 }], Lines = [new() { MethodId = cash, AmountTendered = firstLine.TotalPrice }] });
        try { await Send(new RecalculateOrderPromotionsCommand(branch, separated.Id)); throw new Exception("Paid promotion was repriced"); }
        catch (InvalidOperationException) { check(true, "Recalculation refuses orders with existing payments"); }
        try { await Send(new CancelOrderItemCommand { BranchId = branch, OrderItemId = secondLine.Id }); throw new Exception("Paid pair was changed"); }
        catch (InvalidOperationException ex) { check(ex.Message.Contains("pareja promocional"), "The unpaid partner of a partially paid group cannot be cancelled"); }
        var appended = await Send(new UpdateOrderItemsCommand { BranchId = branch, OrderId = separated.Id, ExpectedIsDraft = false,
            IdempotencyKey = Guid.NewGuid(), Items = [new() { MenuItemId = dish, Quantity = 2, PromotionId = promotion.Id,
                ModifierSelections = [new() { ModifierOptionId = size, Quantity = 1 }, new() { ModifierOptionId = flavor, Quantity = 1 }] }] });
        var preserved = appended.Items.Single(i => i.Id == secondLine.Id);
        check(preserved.PromotionGroupId == secondLine.PromotionGroupId && preserved.TotalPrice == secondLine.TotalPrice
            && appended.Total == 30, "Appending items never changes the already-started promotional group");
        var cardAmount = secondLine.UnitPrice - secondLine.PromotionCardDiscountAmount!.Value;
        var cardPayment = await Send(new PayOrderCommand { BranchId = branch, UserId = actor, OrderId = separated.Id, CashSessionId = session,
            IdempotencyKey = Guid.NewGuid(), OrderAmount = cardAmount, Items = [new() { OrderItemId = secondLine.Id, Quantity = 1 }],
            Lines = [new() { MethodId = card.Id, AmountTendered = cardAmount, CardBankId = bank.Id, CardPaymentType = "Credit", CardBrand = "Visa", AuthorizationNumber = "GROUP123" }] });
        check(firstLine.TotalPrice + cardPayment.OrderAmount == 16, "Separate lines keep their group discount when paid in cash then card");
        await using (var scope = services.CreateAsyncScope()) {
            var db = scope.ServiceProvider.GetRequiredService<GrimorioDbContext>();
            (await db.MenuItems.SingleAsync(m => m.Id == dish)).Price = 11.50m; await db.SaveChangesAsync();
        }
        var screenshotCase = await Separate(4);
        check(screenshotCase.Total == 34.50m && screenshotCase.DiscountTotal == 11.50m
            && screenshotCase.Items.Sum(i => i.PromotionCardDiscountAmount) == 6.90m,
            "Four separate 11.50 ribeyes conserve cash and card cents exactly");
        await MixedProducts(services, branch, actor, dish, cash, session, card.Id, bank.Id, check);
        var expired = Data(); expired.StartsOn = new DateOnly(2000, 1, 1); expired.EndsOn = new DateOnly(2000, 1, 1);
        var inactive = await Send(new UpdatePromotionCommand { Id = promotion.Id, BranchId = branch, Data = expired });
        check(!inactive.IsCurrentlyActive, "Promotion outside its date range is not active");
    }

    private static async Task MixedProducts(IServiceProvider services, Guid branch, Guid actor, Guid templateId,
        Guid cash, Guid session, Guid card, Guid bank, Action<bool, string> check)
    {
        async Task<T> Send<T>(IRequest<T> request) {
            await using var scope = services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request);
        }
        await using var read = services.CreateAsyncScope();
        var db = read.ServiceProvider.GetRequiredService<GrimorioDbContext>();
        var template = await db.MenuItems.AsNoTracking().SingleAsync(i => i.Id == templateId);
        async Task<MenuItemDto> Burger(string name, decimal price) => await Send(new CreateMenuItemCommand {
            BranchId = branch, Name = name, GeneralName = name, MenuCategoryId = template.MenuCategoryId,
            StationId = template.StationId, Price = price,
        });
        var cheap = await Burger("Classic burger", 4.25m);
        var expensive = await Burger("Special burger", 5.25m);
        var promotion = await Send(new CreatePromotionCommand { BranchId = branch, Data = new UpsertPromotionDto {
            Name = "Mixed burgers", Type = "SecondUnitPercentage", DiscountPercent = 50,
            PaymentPolicy = "CardAlternativePercentage", CardDiscountPercent = 30, MenuItemIds = [cheap.Id, expensive.Id],
        } });
        CreateOrderItemDto Line(Guid id, int quantity = 1, Guid? promo = null) => new() {
            MenuItemId = id, Quantity = quantity, PromotionId = promo,
        };
        async Task<OrderDto> Order(params CreateOrderItemDto[] lines) => await Send(new CreateOrderCommand {
            BranchId = branch, Type = "Takeout", Items = lines.ToList(),
        });
        foreach (var reverse in new[] { false, true }) {
            var lines = new[] { Line(cheap.Id, promo: promotion.Id), Line(expensive.Id, promo: promotion.Id) };
            if (reverse) Array.Reverse(lines);
            var order = await Order(lines);
            check(order.Total == 7.38m && order.DiscountTotal == 2.12m
                && order.Items.Sum(i => i.PromotionCardDiscountAmount) == 1.28m,
                "Different burgers pair by promotion, discount the cheaper one and preserve existing midpoint rounding");
            check(order.Items.Select(i => i.PromotionGroupId).Distinct().Count() == 1,
                "Mixed products share one promotional group regardless of input order");
            var reopened = (await Send(new Grimorio.Application.Features.POS.Queries.GetOrderDetailQuery {
                BranchId = branch, OrderId = order.Id }))!;
            check(reopened.Total == 7.38m, "Mixed promotion total survives save and reopen");
            check((await Send(new RecalculateOrderPromotionsCommand(branch, order.Id))).Total == 7.38m,
                "Recalculating mixed products is idempotent");
        }
        check((await Order(Line(cheap.Id, promo: promotion.Id), Line(expensive.Id, 2, promotion.Id))).Total == 12.13m,
            "Odd mixed quantities discount only the cheaper unit in each complete descending-price pair");
        check((await Order(Line(cheap.Id, 2, promotion.Id), Line(expensive.Id, 2, promotion.Id))).Total == 14.25m,
            "Multiple mixed pairs conserve group rounding exactly");
        check((await Order(Line(cheap.Id, promo: promotion.Id), Line(expensive.Id))).Total == 9.50m,
            "An ordinary menu item cannot complete the promotional pair");
        var otherPromotion = await Send(new CreatePromotionCommand { BranchId = branch, Data = new UpsertPromotionDto {
            Name = "Other burger offer", Type = "SecondUnitPercentage", DiscountPercent = 50, MenuItemIds = [cheap.Id, expensive.Id],
        } });
        check((await Order(Line(cheap.Id, promo: promotion.Id), Line(expensive.Id, promo: otherPromotion.Id))).Total == 9.50m,
            "Different promotions never form a mixed pair");
        var legacy = await Order(Line(cheap.Id, promo: promotion.Id), Line(cheap.Id));
        await Send(new ConfirmOrderCommand { BranchId = branch, OrderId = legacy.Id });
        var legacyLine = legacy.Items.Single(i => i.PromotionId.HasValue);
        await Send(new PayOrderCommand { BranchId = branch, UserId = actor, OrderId = legacy.Id, CashSessionId = session,
            IdempotencyKey = Guid.NewGuid(), OrderAmount = 4.25m,
            Items = [new() { OrderItemId = legacyLine.Id, Quantity = 1 }],
            Lines = [new() { MethodId = cash, AmountTendered = 4.25m }] });
        await using (var scope = services.CreateAsyncScope()) {
            var legacyDb = scope.ServiceProvider.GetRequiredService<GrimorioDbContext>();
            var saved = await legacyDb.Orders.Include(o => o.Items).SingleAsync(o => o.Id == legacy.Id);
            saved.Items.Single(i => i.Id == legacyLine.Id).PromotionGroupId = null;
            await legacyDb.SaveChangesAsync();
        }
        var legacyUpdated = await Send(new UpdateOrderItemsCommand { BranchId = branch, OrderId = legacy.Id,
            ExpectedIsDraft = false, IdempotencyKey = Guid.NewGuid(), Items = [Line(expensive.Id, promo: promotion.Id)] });
        check(legacyUpdated.Total == 13.75m && legacyUpdated.DiscountTotal == 0,
            "Legacy paid products without group metadata cannot be paired retroactively with a different product");
        var split = await Order(Line(cheap.Id, promo: promotion.Id), Line(expensive.Id, promo: promotion.Id));
        await Send(new ConfirmOrderCommand { BranchId = branch, OrderId = split.Id });
        var first = split.Items.Single(i => i.MenuItemId == cheap.Id);
        await Send(new PayOrderCommand { BranchId = branch, UserId = actor, OrderId = split.Id, CashSessionId = session,
            IdempotencyKey = Guid.NewGuid(), OrderAmount = first.TotalPrice,
            Items = [new() { OrderItemId = first.Id, Quantity = 1 }],
            Lines = [new() { MethodId = cash, AmountTendered = first.TotalPrice }] });
        var appended = await Send(new UpdateOrderItemsCommand { BranchId = branch, OrderId = split.Id, ExpectedIsDraft = false,
            IdempotencyKey = Guid.NewGuid(), Items = [Line(cheap.Id, promo: promotion.Id), Line(expensive.Id, promo: promotion.Id)] });
        check(appended.Total == 14.76m && split.Items.All(original => appended.Items.Any(i => i.Id == original.Id
            && i.PromotionGroupId == original.PromotionGroupId && i.PromotionCashDiscountAmount == original.PromotionCashDiscountAmount)),
            "Appending mixed products preserves groups with existing payments and pairs only the new units");
        var remaining = split.Items.Single(i => i.MenuItemId == expensive.Id);
        var cardAmount = remaining.UnitPrice - remaining.PromotionCardDiscountAmount!.Value;
        var paid = await Send(new PayOrderCommand { BranchId = branch, UserId = actor, OrderId = split.Id, CashSessionId = session,
            IdempotencyKey = Guid.NewGuid(), OrderAmount = cardAmount,
            Items = [new() { OrderItemId = remaining.Id, Quantity = 1 }],
            Lines = [new() { MethodId = card, AmountTendered = cardAmount, CardBankId = bank,
                CardPaymentType = "Credit", CardBrand = "Visa", AuthorizationNumber = "MIXED123" }] });
        check(first.TotalPrice + paid.OrderAmount == 7.84m,
            "Cash and card partial payments preserve their mixed-product discount snapshots");
    }
}
