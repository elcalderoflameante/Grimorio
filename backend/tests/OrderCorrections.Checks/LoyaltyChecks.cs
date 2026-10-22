using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Application.Features.Marketing;
using Grimorio.Application.Features.POS.Commands;
using Grimorio.Application.Features.Menu.Queries;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.Marketing;
using Grimorio.Domain.Entities.Menu;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

internal static class LoyaltyChecks
{
    internal static async Task Run(IServiceProvider services, HttpClient client, string adminToken, string deniedToken,
        Guid branch, Guid foreign, Guid actor, Guid dish, Guid size, Guid flavor, Guid cash, Guid session, Action<bool, string> check)
    {
        async Task Read(Func<GrimorioDbContext, Task> action) {
            await using var scope = services.CreateAsyncScope(); await action(scope.ServiceProvider.GetRequiredService<GrimorioDbContext>());
        }
        async Task<T> Send<T>(IRequest<T> command) {
            await using var scope = services.CreateAsyncScope(); return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(command);
        }
        async Task Reject(Func<Task> action, string name) {
            try { await action(); } catch (InvalidOperationException) { check(true, name); return; }
            throw new Exception(name);
        }
        var defaults = await Send(new GetLoyaltyProgramQuery(branch));
        check(!defaults.IsActive && defaults.MinimumSpend == 10 && defaults.RewardValidityDays == 30
            && defaults.Tiers.All(t => !t.IsEnabled), "Loyalty starts as safe inactive draft");
        var customer = new Customer { BranchId = branch, Name = "Loyalty Customer Private", Phone = "0991234501", TaxId = "1234567890", TaxIdType = TaxIdType.Cedula };
        var reward = new MenuItem { BranchId = branch, Name = "Reward sundae", InternalCode = "REWARD", Price = 2,
            SaleVisibility = MenuItemSaleVisibility.RewardsOnly };
        await Read(async db => {
            reward.MenuCategoryId = await db.MenuItems.Where(i => i.Id == dish).Select(i => i.MenuCategoryId).SingleAsync();
            var till = await db.CashSessions.SingleAsync(s => s.Id == session); till.Status = CashSessionStatus.Open; till.ClosedAt = null;
            db.AddRange(customer, reward); await db.SaveChangesAsync();
        });
        await Reject(async () => await Send(new EnrollLoyaltyMemberCommand(branch, new() { CustomerId = customer.Id })), "Enrollment requires consent");
        var member = await Send(new EnrollLoyaltyMemberCommand(branch, new() { CustomerId = customer.Id, AcceptsProgram = true }));
        var duplicate = await Send(new EnrollLoyaltyMemberCommand(branch, new() { CustomerId = customer.Id, AcceptsProgram = true }));
        check(member.Member.Id == duplicate.Member.Id && member.PublicToken.Length == 64, "Enrollment is idempotent with private random token");
        check(await Send(new GetLoyaltyMemberQuery(foreign, member.Member.Id)) is null, "Other branch cannot read member");
        var card = await Send(new GetPublicLoyaltyCardQuery(member.PublicToken));
        check(card?.CustomerName == "Loyalty", "Public card returns first name only");
        check(await Send(new GetPublicLoyaltyCardQuery("wrong")) is null, "Invalid card token not found");
        using (var denied = new HttpRequestMessage(HttpMethod.Get, "/api/marketing/loyalty/members")) {
            denied.Headers.Authorization = new AuthenticationHeaderValue("Bearer", deniedToken);
            using var response = await client.SendAsync(denied); check(response.StatusCode == HttpStatusCode.Forbidden, "Members protected by permission");
        }
        using (var response = await client.GetAsync($"/api/marketing/loyalty/card/{member.PublicToken}")) {
            check(response.IsSuccessStatusCode && response.Headers.CacheControl?.NoStore == true, "Public card works anonymously without caching");
            var json = await response.Content.ReadAsStringAsync();
            check(!json.Contains(customer.TaxId) && !json.Contains("Private"), "Public card does not expose identification or full name");
        }
        var program = new LoyaltyProgramDto { IsActive = true, MinimumSpend = 20, Tiers = [new() {
            VisitNumber = 5, RepeatEvery = 5, Name = "Sundae", IsEnabled = true, Products = [new() { MenuItemId = reward.Id }] }] };
        await Send(new SaveLoyaltyProgramCommand(branch, program));
        var visible = await Send(new GetMenuItemsQuery { BranchId = branch, AvailableOnly = true });
        check(visible.All(i => i.Id != reward.Id), "Rewards absent from ordinary sales catalog");
        await Reject(async () => await Send(new CreateOrderCommand { BranchId = branch, Type = "Takeout",
            Items = [new() { MenuItemId = reward.Id, Quantity = 1 }] }), "Private reward cannot be sold normally");
        async Task<OrderDto> Order(int quantity = 1) {
            var order = await Send(new CreateOrderCommand { BranchId = branch, Type = "Takeout", Items = [new() { MenuItemId = dish, Quantity = quantity,
                ModifierSelections = [new() { ModifierOptionId = size, Quantity = 1 }, new() { ModifierOptionId = flavor, Quantity = 1 }] }] });
            return await Send(new ConfirmOrderCommand { BranchId = branch, OrderId = order.Id });
        }
        PayOrderCommand Payment(OrderDto order, Guid? memberId, bool partial = false) => new() {
            BranchId = branch, UserId = actor, OrderId = order.Id, CashSessionId = session, IdempotencyKey = Guid.NewGuid(),
            DocumentType = "NotaDeVenta", OrderAmount = partial ? 10 : order.Total, LoyaltyMemberId = memberId,
            Items = partial ? [new() { OrderItemId = order.Items.Single().Id, Quantity = 1 }] : [],
            Lines = [new() { MethodId = cash, AmountTendered = partial ? 10 : order.Total }] };
        var today = BranchTimeZone.DateFromUtc(DateTime.UtcNow, "America/Guayaquil");
        await Read(async db => {
            for (var day = 1; day <= 4; day++) db.Add(new LoyaltyVisit { BranchId = branch, MemberId = member.Member.Id,
                OrderId = (await db.Orders.Where(o => o.BranchId == branch).FirstAsync()).Id,
                BusinessDate = today.AddDays(-day), MinimumSpend = 10, QualifiedSpend = 10 });
            await db.SaveChangesAsync();
        });
        var split = await Order(3);
        var first = Payment(split, member.Member.Id, true);
        var paid = await Send(first);
        check((await Send(new GetLoyaltyMemberQuery(branch, member.Member.Id)))!.Member.Visits == 4, "Below threshold does not stamp visit");
        var retried = await Send(first);
        check(retried.Id == paid.Id, "Payment retry retains same identity");
        await Send(Payment(split, member.Member.Id, true));
        var fifth = (await Send(new GetLoyaltyMemberQuery(branch, member.Member.Id)))!;
        check(fifth.Member.Visits == 5 && fifth.Rewards.Count == 1, "Partial payments accumulate and fifth visit issues one reward");
        var prize = fifth.Rewards.Single();
        check(prize.VisitNumber == 5 && prize.Status == "Upcoming" && prize.Rule.Products.Single().Name == reward.Name
            && prize.ExpiresAt == prize.IssuedAt.AddDays(30), "Reward captures product, next-day availability and 30-day expiry");
        await Send(Payment(split, member.Member.Id, true));
        check((await Send(new GetLoyaltyMemberQuery(branch, member.Member.Id)))!.Member.Visits == 5, "Only one visit per member per day");
        await Read(async db => check(await db.LoyaltyContributions.CountAsync(c => c.MemberId == member.Member.Id) == 3,
            "Retries do not duplicate contributions"));
        program.RewardValidityDays = 60; program.Tiers[0].Name = "Updated prize";
        await Send(new SaveLoyaltyProgramCommand(branch, program));
        var unchanged = (await Send(new GetLoyaltyMemberQuery(branch, member.Member.Id)))!.Rewards.Single();
        check(unchanged.Name == prize.Name && unchanged.ExpiresAt == prize.ExpiresAt, "Program edit preserves issued reward snapshot and expiry");
        var invalidOrder = await Order();
        var invalid = Payment(invalidOrder, Guid.NewGuid());
        try { await Send(invalid); throw new Exception("Invalid membership accepted"); }
        catch (PaymentRejectedException) { check(true, "Invalid member rejected"); }
        await Read(async db => check(!await db.OrderPayments.AnyAsync(p => p.OrderId == invalidOrder.Id)
            && !await db.StockMovements.AnyAsync(m => m.OrderItemId == invalidOrder.Items.Single().Id), "Invalid member rolls back payment and inventory consumption"));
        await Send(Payment(invalidOrder, null));
        check((await Send(new GetLoyaltyMemberQuery(branch, member.Member.Id)))!.Member.Visits == 5, "Ordinary payment does not assign loyalty automatically");
        var secondCustomer = new Customer { BranchId = branch, Name = "Recurring Member", Phone = "0991234502", TaxId = "LOYALTY-SECOND", TaxIdType = TaxIdType.Passport };
        await Read(async db => { db.Add(secondCustomer); await db.SaveChangesAsync(); });
        var second = await Send(new EnrollLoyaltyMemberCommand(branch, new() { CustomerId = secondCustomer.Id, AcceptsProgram = true }));
        await Read(async db => {
            for (var day = 1; day <= 9; day++) db.Add(new LoyaltyVisit { BranchId = branch, MemberId = second.Member.Id,
                OrderId = invalidOrder.Id, BusinessDate = today.AddDays(-day), MinimumSpend = 10, QualifiedSpend = 10 });
            await db.SaveChangesAsync();
        });
        program.MinimumSpend = 10;
        await Send(new SaveLoyaltyProgramCommand(branch, program));
        var forcedOrder = await Order(); var forcedPayment = Payment(forcedOrder, second.Member.Id);
        await Read(db => db.Database.ExecuteSqlRawAsync("ALTER TABLE marketing.\"Rewards\" ADD CONSTRAINT \"CK_LoyaltyChecks_Failure\" CHECK (\"Name\" <> 'Updated prize') NOT VALID"));
        try {
            try { await Send(forcedPayment); throw new Exception("Reward failure was not forced"); }
            catch (DbUpdateException) { check(true, "Forced reward failure surfaced"); }
        } finally { await Read(db => db.Database.ExecuteSqlRawAsync("ALTER TABLE marketing.\"Rewards\" DROP CONSTRAINT \"CK_LoyaltyChecks_Failure\"")); }
        await Read(async db => check(!await db.OrderPayments.AnyAsync(p => p.OrderId == forcedOrder.Id)
            && !await db.LoyaltyContributions.AnyAsync(c => c.OrderId == forcedOrder.Id)
            && await db.LoyaltyVisits.CountAsync(v => v.MemberId == second.Member.Id) == 9
            && !await db.StockMovements.AnyAsync(m => m.OrderItemId == forcedOrder.Items.Single().Id),
            "Reward insert failure rolls back payment, consumption, contribution and visit together"));
        await Send(forcedPayment);
        var tenth = (await Send(new GetLoyaltyMemberQuery(branch, second.Member.Id)))!;
        check(tenth.Member.Visits == 10 && tenth.Rewards.Single().VisitNumber == 10,
            "Same failed request can retry safely; recurring milestone issues again at tenth visit");
        var concurrentA = await Order(); var concurrentB = await Order();
        await Task.WhenAll(Send(Payment(concurrentA, second.Member.Id)), Send(Payment(concurrentB, second.Member.Id)));
        check((await Send(new GetLoyaltyMemberQuery(branch, second.Member.Id)))!.Member.Visits == 10,
            "Concurrent payments cannot add a second visit on same day");
        program.ExcludedItemIds = [dish];
        await Send(new SaveLoyaltyProgramCommand(branch, program));
        var excludedOrder = await Order(); await Send(Payment(excludedOrder, second.Member.Id));
        await Read(async db => check((await db.LoyaltyContributions.SingleAsync(c => c.OrderId == excludedOrder.Id)).EligibleAmount == 0,
            "Excluded product contributes no spend"));
    }
}
