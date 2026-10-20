using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.POS.Commands;
using Grimorio.Application.Features.POS.Queries;
using Grimorio.Application.Features.TableService.Queries;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

internal static class TableDraftCancellationChecks
{
    internal static async Task Run(IServiceProvider services, HttpClient client, string token, string deniedToken,
        Guid branchId, Guid actor, Guid dish, Guid size, Guid flavor, Action<bool, string> check)
    {
        async Task<T> Send<T>(IRequest<T> command)
        {
            await using var scope = services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(command);
        }
        async Task Read(Func<GrimorioDbContext, Task> action)
        {
            await using var scope = services.CreateAsyncScope();
            await action(scope.ServiceProvider.GetRequiredService<GrimorioDbContext>());
        }
        var table = new RestaurantTable { BranchId = branchId, Code = "Draft cancel checks", Capacity = 4 };
        await Read(async db => { db.Add(table); await db.SaveChangesAsync(); });
        var draft = await Send(new CreateOrderCommand { BranchId = branchId, TableId = table.Id, Type = "DineIn", WaiterId = actor,
            Items = [new() { MenuItemId = dish, Quantity = 1, ModifierSelections = [new() { ModifierOptionId = size, Quantity = 1 }, new() { ModifierOptionId = flavor, Quantity = 1 }] }] });
        var map = await Send(new GetRestaurantTablesQuery { BranchId = branchId });
        check(map.Single(t => t.Id == table.Id).CurrentStatus == "Draft", "Saved table draft is visible on the map before cancellation");
        var movements = 0;
        decimal stock = 0;
        await Read(async db => {
            movements = await db.StockMovements.CountAsync();
            stock = await db.WarehouseStock.SumAsync(s => s.Quantity);
            check(!await db.StockReservations.AnyAsync(r => r.OrderId == draft.Id), "Unconfirmed table draft has no ingredient reservations");
        });
        async Task<HttpResponseMessage> Cancel(string jwt)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/pos/ordenes/{draft.Id}/cancelar");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            return await client.SendAsync(request);
        }
        using var denied = await Cancel(deniedToken);
        check(denied.StatusCode == HttpStatusCode.Forbidden, "Draft cancellation requires order cancellation permission");
        using var response = await Cancel(token);
        check(response.StatusCode == HttpStatusCode.OK, "Authorized user can cancel a saved table draft");
        var cancelled = await response.Content.ReadFromJsonAsync<OrderDto>();
        check(cancelled?.Id == draft.Id && cancelled.Status == "Cancelled", "Draft cancellation retains order identity and history");
        map = await Send(new GetRestaurantTablesQuery { BranchId = branchId });
        check(map.Single(t => t.Id == table.Id).CurrentStatus == "Free" && map.Single(t => t.Id == table.Id).CurrentOrderId == null,
            "Cancelling the draft frees the table");
        await Read(async db => check(await db.StockMovements.CountAsync() == movements
            && await db.WarehouseStock.SumAsync(s => s.Quantity) == stock
            && !await db.StockReservations.AnyAsync(r => r.OrderId == draft.Id)
            && !await db.OrderPayments.AnyAsync(p => p.OrderId == draft.Id),
            "Draft cancellation leaves inventory movements, stock and payments unchanged"));
        var replacement = await Send(new CreateOrderCommand { BranchId = branchId, TableId = table.Id, Type = "DineIn", WaiterId = actor,
            Items = [new() { MenuItemId = dish, Quantity = 1, ModifierSelections = [new() { ModifierOptionId = size, Quantity = 1 }, new() { ModifierOptionId = flavor, Quantity = 1 }] }] });
        check(replacement.Id != draft.Id, "Freed table accepts a new order without deleting the cancelled draft history");

        await Read(async db => {
            var user = await db.Users.SingleAsync(u => u.Id == actor);
            var originalFirstName = user.FirstName;
            var originalLastName = user.LastName;
            user.FirstName = "  Jessica Elizabeth  ";
            user.LastName = "  Chicaiza Vargas  ";
            await db.SaveChangesAsync();
            try {
                map = await Send(new GetRestaurantTablesQuery { BranchId = branchId });
                check(map.Single(t => t.Id == table.Id).CurrentOrderWaiterName == "Jessica Chicaiza",
                    "Table draft shows only the waiter's first name and first surname");
                var order = await db.Orders.SingleAsync(o => o.Id == replacement.Id);
                order.Status = OrderStatus.Confirmed;
                await db.SaveChangesAsync();
                map = await Send(new GetRestaurantTablesQuery { BranchId = branchId });
                check(map.Single(t => t.Id == table.Id).CurrentOrderWaiterName == "Jessica Chicaiza",
                    "Occupied table retains the assigned waiter name");
                order.WaiterId = null;
                await db.SaveChangesAsync();
                map = await Send(new GetRestaurantTablesQuery { BranchId = branchId });
                check(map.Single(t => t.Id == table.Id).CurrentOrderWaiterName == null,
                    "Unassigned order does not invent a waiter name");
                order.Status = OrderStatus.Cancelled;
                await db.SaveChangesAsync();
                map = await Send(new GetRestaurantTablesQuery { BranchId = branchId });
                check(map.Single(t => t.Id == table.Id).CurrentOrderWaiterName == null,
                    "Free table does not retain a previous waiter name");
            }
            finally {
                user.FirstName = originalFirstName;
                user.LastName = originalLastName;
                await db.SaveChangesAsync();
            }
        });
    }
}
