using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Application.Features.POS.Commands;
using Grimorio.Domain.Entities.Inventory;
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
        var updated = Data(25);
        var edited = await Send(new UpdatePromotionCommand { Id = promotion.Id, BranchId = branch, Data = updated });
        check(edited.DiscountPercent == 25 && edited.Type == "SecondUnitPercentage", "Second-unit promotion percentage can be edited");
        var expired = Data(); expired.StartsOn = new DateOnly(2000, 1, 1); expired.EndsOn = new DateOnly(2000, 1, 1);
        var inactive = await Send(new UpdatePromotionCommand { Id = promotion.Id, BranchId = branch, Data = expired });
        check(!inactive.IsCurrentlyActive, "Promotion outside its date range is not active");
    }
}
