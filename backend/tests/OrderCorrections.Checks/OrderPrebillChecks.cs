using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Grimorio.Application.DTOs;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Persistence;
using Grimorio.SharedKernel.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

static class OrderPrebillChecks
{
    public static async Task Run(IServiceProvider services, HttpClient client, SymmetricSecurityKey key,
        Guid branch, Guid foreign, Guid waiterId, Guid dishId, Guid bbq, Action<bool, string> check)
    {
        async Task<T> Db<T>(Func<GrimorioDbContext, Task<T>> action) {
            await using var scope = services.CreateAsyncScope();
            return await action(scope.ServiceProvider.GetRequiredService<GrimorioDbContext>());
        }
        var order = await Db(async db => {
            var tax = new TaxRate { BranchId = branch, Name = "Prebill IVA 15", Percentage = 15, SriCode = "4" };
            var table = new RestaurantTable { BranchId = branch, Code = "PRE-01" };
            var customer = new Customer { BranchId = branch, Name = "Prebill customer", Email = "prebill@checks.invalid", TaxId = "PRE-CHECKS", Address = "Checks address" };
            var result = new Order { BranchId = branch, Number = 200001, Type = OrderType.DineIn, Status = OrderStatus.Confirmed,
                TableId = table.Id, WaiterId = waiterId, CustomerId = customer.Id, ConfirmedAt = DateTime.UtcNow,
                Subtotal = 11.5m, DiscountTotal = 1.15m, TaxAmount = 1.35m, Total = 10.35m };
            var item = new OrderItem { BranchId = branch, OrderId = result.Id, MenuItemId = dishId, Quantity = 2, UnitPrice = 5.75m,
                TotalPrice = 10.35m, DiscountAmount = 1.15m, TaxAmount = 1.35m, TaxRateId = tax.Id, Status = OrderItemStatus.Ready };
            var option = await db.MenuItemModifierOptions.SingleAsync(o => o.Id == bbq);
            item.ModifierSelections.Add(new OrderItemModifierSelection { BranchId = branch, OrderItemId = item.Id,
                ModifierGroupId = option.ModifierGroupId, ModifierOptionId = bbq, GroupName = "Sabor", OptionName = "BBQ historica", Quantity = 2 });
            result.Items.Add(item);
            result.Items.Add(new OrderItem { BranchId = branch, OrderId = result.Id, MenuItemId = dishId, Quantity = 1, TotalPrice = 50, Status = OrderItemStatus.Cancelled });
            result.Items.Add(new OrderItem { BranchId = branch, OrderId = result.Id, MenuItemId = dishId, Quantity = 1, TotalPrice = 50, IsDeleted = true });
            result.Payments.Add(new OrderPayment { BranchId = branch, OrderId = result.Id, OrderAmount = 3 });
            result.Payments.Add(new OrderPayment { BranchId = branch, OrderId = result.Id, OrderAmount = 1, IsDeleted = true });
            db.AddRange(tax, table, customer, result);
            foreach (var state in new[] { OrderStatus.Draft, OrderStatus.Cancelled })
                db.Orders.Add(new Order { BranchId = branch, Number = 200002 + (int)state, Status = state, Total = 9 });
            db.Orders.Add(new Order { BranchId = branch, Number = 200020, Total = 9, IsDeleted = true });
            await db.SaveChangesAsync();
            return result;
        });
        string Token(Guid currentBranch, bool allowed) => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            claims: new[] { new Claim(ClaimTypes.NameIdentifier, waiterId.ToString()), new Claim(ClaimTypes.Role, AppConstants.Roles.Waiter),
                new Claim(AppConstants.Claims.BranchId, currentBranch.ToString()) }.Concat(allowed
                    ? [new Claim(AppConstants.Claims.Permissions, AppConstants.Permissions.PosOrdersView)] : Array.Empty<Claim>()),
            expires: DateTime.UtcNow.AddMinutes(30), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
        async Task<HttpResponseMessage> Get(Guid id, Guid? currentBranch = null, bool allowed = true) {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/pos/ordenes/{id}/precuenta");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token(currentBranch ?? branch, allowed));
            return await client.SendAsync(request);
        }
        async Task<string> Snapshot() => await Db(async db => JsonSerializer.Serialize(new {
            Order = await db.Orders.AsNoTracking().Where(o => o.Id == order.Id).Select(o => new { o.Total, o.Subtotal, o.DiscountTotal, o.Status, o.PaidAt, o.UpdatedAt }).SingleAsync(),
            Payments = await db.OrderPayments.CountAsync(), PaymentItems = await db.OrderPaymentItems.CountAsync(),
            Movements = await db.StockMovements.CountAsync(), Reservations = await db.StockReservations.CountAsync(),
            Stock = await db.WarehouseStock.AsNoTracking().OrderBy(s => s.Id).Select(s => new { s.Id, s.Quantity }).ToListAsync(),
            Documents = await db.ElectronicDocuments.CountAsync(), Cash = await db.CashSessions.CountAsync(),
        }));
        var before = await Snapshot();
        using var response = await Get(order.Id);
        check(response.StatusCode == HttpStatusCode.OK, "Waiter can print prebill with order-view permission and no cash permissions or session");
        check(response.Headers.CacheControl?.NoStore == true, "Prebill responses are not cached between prints");
        var bill = (await response.Content.ReadFromJsonAsync<OrderPrebillDto>())!;
        check(bill.TableCode == "PRE-01" && bill.OrderNumber == order.Number && !string.IsNullOrEmpty(bill.WaiterName), "Prebill includes table, order and waiter");
        check(bill.Customer?.Email == "prebill@checks.invalid" && bill.Customer.Name == "Prebill customer", "Prebill includes existing billing customer");
        check(bill.Items.Count == 1 && bill.Items.Single().Quantity == 2 && bill.Items.Single().Total == 10.35m, "Prebill excludes cancelled and deleted items");
        check(bill.Items.Single().ModifierSelections.Single().OptionName == "BBQ historica" && bill.Items.Single().ModifierSelections.Single().Quantity == 2,
            "Prebill uses historical selected option names and quantities, not current menu labels");
        check(bill.Subtotal == 11.5m && bill.Discount == 1.15m && bill.Tax == 1.35m && bill.Total == 10.35m && bill.PaidAmount == 3 && bill.PendingAmount == 7.35m,
            "Prebill preserves stored gross totals and tax, subtracts active abonos and ignores deleted payments");
        check(bill.SubtotalWithoutTax == 10 && bill.DiscountWithoutTax == 1
            && bill.SubtotalWithoutTax - bill.DiscountWithoutTax + bill.Tax == bill.Total,
            "Prebill separates original net subtotal, net discount and discounted IVA without double taxation");
        using var repeat = await Get(order.Id);
        check(repeat.StatusCode == HttpStatusCode.OK && (await repeat.Content.ReadFromJsonAsync<OrderPrebillDto>())!.PendingAmount == 7.35m, "Prebill can be printed repeatedly");
        check(await Snapshot() == before, "Prebill GET leaves orders, cash, payments, stock, reservations and SRI documents untouched");
        using var denied = await Get(order.Id, allowed: false);
        check(denied.StatusCode == HttpStatusCode.Forbidden, "Prebill requires order-view permission");
        using var wrongBranch = await Get(order.Id, foreign);
        check(wrongBranch.StatusCode == HttpStatusCode.NotFound, "Prebill cannot disclose another branch's orders or customer data");
        using var missing = await Get(Guid.NewGuid());
        check(missing.StatusCode == HttpStatusCode.NotFound, "Missing prebill order returns 404");
        var invalid = await Db(async db => await db.Orders.IgnoreQueryFilters().Where(o => o.BranchId == branch && o.Number >= 200002 && o.Number <= 200020)
            .Select(o => new { o.Id, o.IsDeleted }).ToListAsync());
        foreach (var row in invalid) {
            using var result = await Get(row.Id);
            check(result.StatusCode == (row.IsDeleted ? HttpStatusCode.NotFound : HttpStatusCode.BadRequest), "Draft, cancelled and deleted orders do not produce precuentas");
        }
        await Db(async db => {
            var payment = await db.OrderPayments.SingleAsync(p => p.OrderId == order.Id);
            payment.OrderAmount = 4;
            await db.SaveChangesAsync(); return true;
        });
        using var refreshed = await Get(order.Id);
        check((await refreshed.Content.ReadFromJsonAsync<OrderPrebillDto>())!.PendingAmount == 6.35m, "Each print fetches current abonos instead of trusting browser totals");
        await Db(async db => {
            var payment = await db.OrderPayments.SingleAsync(p => p.OrderId == order.Id);
            payment.OrderAmount = 10.35m;
            await db.SaveChangesAsync(); return true;
        });
        using var settled = await Get(order.Id);
        check(settled.StatusCode == HttpStatusCode.BadRequest, "Fully paid orders cannot produce a pending precuenta even without PaidAt");

        var taxId = await Db(db => db.OrderItems.Where(i => i.OrderId == order.Id && i.TaxRateId != null).Select(i => i.TaxRateId).SingleAsync());
        var cases = new[] {
            (Name: "no discount", Gross: 11.5m, Discount: 0m, Tax: 1.5m, Total: 11.5m, Net: 10m, NetDiscount: 0m, Taxed: true),
            (Name: "zero IVA", Gross: 10m, Discount: 1m, Tax: 0m, Total: 9m, Net: 10m, NetDiscount: 1m, Taxed: false),
            (Name: "rounding", Gross: 3.5m, Discount: .51m, Tax: .39m, Total: 2.99m, Net: 3.04m, NetDiscount: .44m, Taxed: true),
        };
        foreach (var sample in cases) {
            var id = await Db(async db => {
                var extra = new Order { BranchId = branch, Number = 201000 + Array.IndexOf(cases, sample), Status = OrderStatus.Confirmed,
                    Subtotal = sample.Gross, DiscountTotal = sample.Discount, TaxAmount = sample.Tax, Total = sample.Total };
                extra.Items.Add(new OrderItem { BranchId = branch, MenuItemId = dishId, Quantity = 1, UnitPrice = sample.Gross,
                    DiscountAmount = sample.Discount, TotalPrice = sample.Total, TaxAmount = sample.Tax, TaxRateId = sample.Taxed ? taxId : null });
                db.Add(extra); await db.SaveChangesAsync(); return extra.Id;
            });
            using var result = await Get(id);
            var detail = (await result.Content.ReadFromJsonAsync<OrderPrebillDto>())!;
            check(result.StatusCode == HttpStatusCode.OK && detail.SubtotalWithoutTax == sample.Net && detail.DiscountWithoutTax == sample.NetDiscount
                && detail.SubtotalWithoutTax - detail.DiscountWithoutTax + detail.Tax == detail.Total,
                $"Prebill net breakdown reconciles with saved total: {sample.Name}");
        }
    }
}
