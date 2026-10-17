using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Application.Features.POS.Commands;
using Grimorio.Domain.Entities.Auth;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.Inventory;
using Grimorio.Infrastructure.Persistence;
using Grimorio.SharedKernel.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

internal static class OrderCorrectionPermissionChecks
{
    public static async Task Run(IServiceProvider services, HttpClient client, SecurityKey key, Guid branch, Guid foreign,
        Guid dish, Guid size, Guid bbq, Guid teriyaki, Guid cash, Guid session, Action<bool, string> check)
    {
        async Task Read(Func<GrimorioDbContext, Task> action) {
            await using var scope = services.CreateAsyncScope(); await action(scope.ServiceProvider.GetRequiredService<GrimorioDbContext>());
        }
        async Task<T> Send<T>(IRequest<T> request) {
            await using var scope = services.CreateAsyncScope(); return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request);
        }
        var cashier = new User { BranchId = branch, FirstName = "Delegated", LastName = "Cashier", Email = "delegated@checks.invalid", PasswordHash = "checks" };
        var role = new Role { BranchId = branch, Name = AppConstants.Roles.Cashier };
        var permission = new Permission { BranchId = branch, Code = AppConstants.Permissions.PosOrdersCorrectItems, Description = "Corregir platos sin cobrar", Category = "POS" };
        var grant = new RolePermission { BranchId = branch, RoleId = role.Id, PermissionId = permission.Id };
        Guid opener = Guid.Empty;
        await Read(async db => {
            db.AddRange(cashier, role, permission);
            db.UserRoles.Add(new() { BranchId = branch, UserId = cashier.Id, RoleId = role.Id });
            opener = (await db.CashSessions.SingleAsync(s => s.Id == session)).OpenedBy;
            foreach (var stock in await db.WarehouseStock.ToListAsync()) {
                var article = await db.InventoryArticles.SingleAsync(a => a.Id == stock.ArticleId);
                stock.Quantity += 500;
                db.StockMovements.Add(new() { BranchId = branch, WarehouseId = stock.WarehouseId, ArticleId = stock.ArticleId,
                    UnitId = article.BaseUnitId, Type = MovementType.InitialInventory, Quantity = 500, BaseQuantity = 500, UnitCost = 1, TotalCost = 500 });
            }
            await db.SaveChangesAsync();
        });
        string Token(bool claim = true, Guid? tokenBranch = null) {
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, cashier.Id.ToString()),
                new(AppConstants.Claims.BranchId, (tokenBranch ?? branch).ToString()), new(ClaimTypes.Role, AppConstants.Roles.Cashier) };
            if (claim) claims.Add(new(AppConstants.Claims.Permissions, AppConstants.Permissions.PosOrdersCorrectItems));
            return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(claims: claims, expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
        }
        async Task<OrderDto> Order(bool ready = false, int quantity = 1) {
            var order = await Send(new CreateOrderCommand { BranchId = branch, Type = "Takeout", Items = [new() {
                MenuItemId = dish, Quantity = quantity, ModifierSelections = [new() { ModifierOptionId = size, Quantity = 1 }, new() { ModifierOptionId = bbq, Quantity = 1 }] }] });
            order = await Send(new ConfirmOrderCommand { BranchId = branch, OrderId = order.Id });
            if (ready) await Send(new SetOrderItemStatusCommand { BranchId = branch, OrderItemId = order.Items.Single().Id, Status = "Ready" });
            return order;
        }
        CorrectOrderItemDto Data(bool replace = false) => new() { RequestId = Guid.NewGuid(), Disposition = replace ? "PreparedWaste" : "RegistrationError",
            Reason = "Cashier correction permission checks", Replace = replace,
            ModifierSelections = replace ? [new() { ModifierOptionId = size, Quantity = 1 }, new() { ModifierOptionId = teriyaki, Quantity = 1 }] : [] };
        async Task<OrderDto?> Correct(OrderDto order, HttpStatusCode expected, string? token = null, CorrectOrderItemDto? data = null) {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/pos/ordenes/items/{order.Items.Single().Id}/corregir") {
                Content = JsonContent.Create(data ?? Data()) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token ?? Token());
            using var response = await client.SendAsync(request);
            check(response.StatusCode == expected, $"Cashier correction expected {expected}, got {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<OrderDto>() : null;
        }
        var pending = await Order();
        await Correct(pending, HttpStatusCode.Forbidden, Token(false));
        await Correct(pending, HttpStatusCode.BadRequest); // A token claim alone cannot grant this sensitive operation.
        await Read(async db => { db.RolePermissions.Add(grant); await db.SaveChangesAsync(); });
        await Correct(pending, HttpStatusCode.Forbidden, Token(false));
        foreach (var path in new[] { $"/api/pos/ordenes/{pending.Id}/cancelar", $"/api/pos/ordenes/items/{pending.Items.Single().Id}/cancelar" }) {
            using var request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token());
            using var response = await client.SendAsync(request);
            check(response.StatusCode == HttpStatusCode.Forbidden, "Correct-items permission does not grant order cancellation or legacy cancellation");
        }
        var cancelled = (await Correct(pending, HttpStatusCode.OK))!;
        check(cancelled.Status == "Cancelled" && cancelled.Total == 0, "Non-admin cashier cancels an unpaid dish using audited correction");
        var prepared = await Order(ready: true);
        var replaced = (await Correct(prepared, HttpStatusCode.OK, data: Data(replace: true)))!;
        check(replaced.Items.Count(i => i.Status == "Pending") == 1 && replaced.Total == prepared.Total, "Non-admin cashier can correct prepared-dish options");
        await Read(async db => check(await db.OrderItemCorrections.CountAsync(c => c.ActorUserId == cashier.Id) == 2
            && await db.StockMovements.AnyAsync(m => m.OrderItemId == prepared.Items.Single().Id && m.Type == MovementType.Waste),
            "Delegated corrections retain actor audit and original prepared ingredients as waste"));
        var blocked = await Order();
        await Read(async db => { (await db.RolePermissions.SingleAsync(p => p.Id == grant.Id)).IsDeleted = true; await db.SaveChangesAsync(); });
        await Correct(blocked, HttpStatusCode.BadRequest);
        await Read(async db => { (await db.RolePermissions.IgnoreQueryFilters().SingleAsync(p => p.Id == grant.Id)).IsDeleted = false; await db.SaveChangesAsync(); });
        await Read(async db => { (await db.Roles.SingleAsync(r => r.Id == role.Id)).IsActive = false; await db.SaveChangesAsync(); });
        await Correct(blocked, HttpStatusCode.BadRequest);
        await Read(async db => { (await db.Roles.SingleAsync(r => r.Id == role.Id)).IsActive = true; (await db.Permissions.SingleAsync(p => p.Id == permission.Id)).IsActive = false; await db.SaveChangesAsync(); });
        await Correct(blocked, HttpStatusCode.BadRequest);
        await Read(async db => { (await db.Permissions.SingleAsync(p => p.Id == permission.Id)).IsActive = true; (await db.Users.SingleAsync(u => u.Id == cashier.Id)).IsActive = false; await db.SaveChangesAsync(); });
        await Correct(blocked, HttpStatusCode.BadRequest);
        await Read(async db => { (await db.Users.SingleAsync(u => u.Id == cashier.Id)).IsActive = true; await db.SaveChangesAsync(); });
        await Correct(blocked, HttpStatusCode.BadRequest, Token(tokenBranch: foreign));
        var partiallyPaid = await Order(quantity: 2);
        await Send(new PayOrderCommand { BranchId = branch, OrderId = partiallyPaid.Id, UserId = opener, CashSessionId = session,
            IdempotencyKey = Guid.NewGuid(), OrderAmount = 10, Lines = [new() { MethodId = cash, AmountTendered = 10 }],
            Items = [new() { OrderItemId = partiallyPaid.Items.Single().Id, Quantity = 1 }] });
        await Correct(partiallyPaid, HttpStatusCode.BadRequest);
        await Read(async db => check(!await db.OrderItemCorrections.AnyAsync(c => c.OriginalItemId == blocked.Items.Single().Id
            || c.OriginalItemId == partiallyPaid.Items.Single().Id), "Unauthorized and paid-line attempts leave no correction audit or mutation"));
    }
}
