using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Application.Features.POS.Commands;
using Grimorio.Application.Features.POS.Queries;
using Grimorio.Application.Features.TableService.Queries;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

internal static class TableTransferChecks
{
    public static async Task Run(IServiceProvider services, HttpClient client, string token, string forbiddenToken,
        Guid branch, Guid foreign, Guid actor, Guid dish, Guid size, Guid flavor, Guid cash, Guid session, Action<bool, string> check)
    {
        async Task Read(Func<GrimorioDbContext, Task> action) {
            await using var scope = services.CreateAsyncScope(); await action(scope.ServiceProvider.GetRequiredService<GrimorioDbContext>());
        }
        async Task<T> Send<T>(IRequest<T> command) {
            await using var scope = services.CreateAsyncScope(); return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(command);
        }
        var tables = Enumerable.Range(1, 18).Select(i => new RestaurantTable { BranchId = branch, Code = $"T{i}", Capacity = 4 }).ToList();
        var inactive = new RestaurantTable { BranchId = branch, Code = "Inactive", IsActive = false };
        var outsider = new RestaurantTable { BranchId = foreign, Code = "Foreign" };
        await Read(async db => {
            db.AddRange(tables); db.AddRange(inactive, outsider);
            var till = await db.CashSessions.SingleAsync(s => s.Id == session); till.Status = CashSessionStatus.Open; till.ClosedAt = null;
            await db.SaveChangesAsync();
        });
        async Task<OrderDto> Order(RestaurantTable table, string state = "Draft") {
            var order = await Send(new CreateOrderCommand { BranchId = branch, TableId = table.Id, Type = "DineIn", WaiterId = actor,
                Items = [new() { MenuItemId = dish, Quantity = 2, ModifierSelections = [new() { ModifierOptionId = size, Quantity = 1 }, new() { ModifierOptionId = flavor, Quantity = 1 }] }] });
            if (state != "Draft") order = await Send(new ConfirmOrderCommand { BranchId = branch, OrderId = order.Id });
            if (state is "InPreparation" or "Ready" or "Delivered") {
                await Send(new SetOrderItemStatusCommand { BranchId = branch, OrderItemId = order.Items.Single().Id, Status = state == "InPreparation" ? state : "Ready" });
                if (state == "Delivered") await Send(new DeliverOrderCommand { BranchId = branch, OrderId = order.Id });
            }
            return (await Send(new GetOrderDetailQuery { BranchId = branch, OrderId = order.Id }))!;
        }
        async Task<OrderDto?> Move(OrderDto order, RestaurantTable target, HttpStatusCode expected = HttpStatusCode.OK,
            TransferOrderTableDto? data = null, string? jwt = null) {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/pos/ordenes/{order.Id}/trasladar-mesa") {
                Content = JsonContent.Create(data ?? new TransferOrderTableDto { RequestId = Guid.NewGuid(), FromTableId = order.TableId!.Value, ToTableId = target.Id }) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt ?? token);
            using var response = await client.SendAsync(request);
            check(response.StatusCode == expected, $"Table transfer expected {expected}, got {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<OrderDto>() : null;
        }
        async Task<string> Snapshot() {
            var snapshot = "";
            await Read(async db => snapshot = JsonSerializer.Serialize(new {
                Items = await db.OrderItems.AsNoTracking().IgnoreAutoIncludes().OrderBy(i => i.Id).ToListAsync(),
                Payments = await db.OrderPayments.AsNoTracking().OrderBy(i => i.Id).ToListAsync(),
                Lines = await db.PaymentLines.AsNoTracking().OrderBy(i => i.Id).ToListAsync(),
                Reservations = await db.StockReservations.AsNoTracking().OrderBy(i => i.Id).ToListAsync(),
                Movements = await db.StockMovements.AsNoTracking().OrderBy(i => i.Id).ToListAsync(),
                Stock = await db.WarehouseStock.AsNoTracking().OrderBy(i => i.Id).ToListAsync(),
            }));
            return snapshot;
        }
        var index = 0;
        OrderDto? confirmed = null;
        foreach (var state in new[] { "Draft", "Confirmed", "InPreparation", "Ready", "Delivered" }) {
            var order = await Order(tables[index++], state); var target = tables[index++];
            if (state == "Draft") await Read(async db => {
                (await db.Orders.SingleAsync(o => o.Id == order.Id)).PublicSubmissionId = Guid.NewGuid();
                db.TableServiceRequests.Add(new() { BranchId = branch, RestaurantTableId = order.TableId!.Value,
                    Type = TableServiceRequestType.Custom, CustomMessage = $"Pedido #{order.Number} pendiente de confirmar" });
                db.TableServiceRequests.Add(new() { BranchId = branch, RestaurantTableId = order.TableId.Value,
                    Type = TableServiceRequestType.Napkins });
                await db.SaveChangesAsync();
            });
            var before = await Snapshot(); var moved = (await Move(order, target))!;
            if (state == "Confirmed") confirmed = moved;
            check(moved.Id == order.Id && moved.Number == order.Number && moved.Status == order.Status && moved.Total == order.Total
                && moved.TableId == target.Id && moved.TableCode == target.Code, $"Transfer {state} retains order identity, totals and preparation state");
            check(await Snapshot() == before, $"Transfer {state} leaves all items, money and inventory unchanged");
            if (state == "Draft") await Read(async db => check(
                await db.TableServiceRequests.AnyAsync(r => r.Type == TableServiceRequestType.Custom && r.RestaurantTableId == target.Id)
                && await db.TableServiceRequests.AnyAsync(r => r.Type == TableServiceRequestType.Napkins && r.RestaurantTableId == order.TableId),
                "QR draft notice follows the order; unrelated physical-table requests stay at their original table"));
            var map = await Send(new GetRestaurantTablesQuery { BranchId = branch });
            check(map.Single(t => t.Id == order.TableId).CurrentStatus == "Free" && map.Single(t => t.Id == target.Id).CurrentOrderId == order.Id,
                "Original table freed and destination occupied by the same order");
        }
        var source = await Order(tables[10], "Ready");
        var retry = new TransferOrderTableDto { RequestId = Guid.NewGuid(), FromTableId = source.TableId!.Value, ToTableId = tables[11].Id };
        await Move(source, inactive, HttpStatusCode.BadRequest);
        await Move(source, tables[10], HttpStatusCode.BadRequest);
        await Move(source, outsider, HttpStatusCode.BadRequest);
        await Move(source, tables[1], HttpStatusCode.BadRequest);
        await Move(source, tables[11], HttpStatusCode.Forbidden, jwt: forbiddenToken);
        await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => Move(source, tables[11], data: retry)));
        await Read(async db => check(await db.OrderTableTransfers.CountAsync(t => t.OrderId == source.Id) == 1
            && (await db.OrderTableTransfers.SingleAsync(t => t.OrderId == source.Id)).ActorUserId == actor, "Concurrent retry creates one audited transfer"));
        await Move(source, tables[12], HttpStatusCode.BadRequest);
        retry.ToTableId = tables[12].Id; await Move(source, tables[12], HttpStatusCode.BadRequest, retry);
        var contenderA = await Order(tables[12]); var contenderB = await Order(tables[13]);
        var commandA = new TransferOrderTableCommand(branch, contenderA.Id, actor, new() { RequestId = Guid.NewGuid(), FromTableId = tables[12].Id, ToTableId = tables[14].Id });
        var commandB = new TransferOrderTableCommand(branch, contenderB.Id, actor, new() { RequestId = Guid.NewGuid(), FromTableId = tables[13].Id, ToTableId = tables[14].Id });
        async Task<bool> TryMove(TransferOrderTableCommand command) { try { await Send(command); return true; } catch (InvalidOperationException) { return false; } }
        var winners = await Task.WhenAll(TryMove(commandA), TryMove(commandB));
        check(winners.Count(success => success) == 1, "Two concurrent transfers cannot occupy the same free table");
        var failure = await Order(tables[15]);
        await Read(db => db.Database.ExecuteSqlRawAsync("ALTER TABLE pos.\"OrderTableTransfers\" ADD CONSTRAINT \"CK_TableTransferChecks_Failure\" CHECK (false) NOT VALID"));
        try { await Move(failure, tables[16], HttpStatusCode.InternalServerError); }
        finally { await Read(db => db.Database.ExecuteSqlRawAsync("ALTER TABLE pos.\"OrderTableTransfers\" DROP CONSTRAINT \"CK_TableTransferChecks_Failure\"")); }
        check((await Send(new GetOrderDetailQuery { BranchId = branch, OrderId = failure.Id }))!.TableId == tables[15].Id,
            "Persistence failure rolls back table assignment and audit atomically");
        await Read(async db => check(!await db.OrderTableTransfers.AnyAsync(t => t.OrderId == failure.Id), "Failed transfer leaves no audit entry"));
        async Task<bool> TryCreate() {
            try { await Send(new CreateOrderCommand { BranchId = branch, Type = "DineIn", TableId = tables[16].Id }); return true; }
            catch (InvalidOperationException) { return false; }
        }
        var creationRace = await Task.WhenAll(TryCreate(), TryMove(new TransferOrderTableCommand(branch, failure.Id, actor,
            new() { RequestId = Guid.NewGuid(), FromTableId = tables[15].Id, ToTableId = tables[16].Id })));
        check(creationRace.Count(success => success) == 1, "Creating a new order races safely with transfer into the same free table");
        var unpaid = (await Send(new GetOrderDetailQuery { BranchId = branch, OrderId = source.Id }))!;
        await Send(new PayOrderCommand { BranchId = branch, OrderId = source.Id, UserId = actor, CashSessionId = session,
            IdempotencyKey = Guid.NewGuid(), OrderAmount = unpaid.Total, Lines = [new() { MethodId = cash, AmountTendered = unpaid.Total }] });
        await Move(unpaid, tables[17], HttpStatusCode.BadRequest);
        var queue = await Send(new GetItemsByStationQuery { BranchId = branch,
            StationId = (await Send(new GetOrderDetailQuery { BranchId = branch, OrderId = source.Id }))!.Items.Single().StationId!.Value });
        check(queue.Any(i => i.OrderId == confirmed!.Id) && queue.Where(i => i.OrderId == confirmed!.Id).All(i => i.TableCode == tables[3].Code),
            "Kitchen station query reflects the new table on the actual pending dishes");
    }
}
