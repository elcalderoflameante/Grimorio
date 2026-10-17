using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Application.Features.Billing.Queries;
using Grimorio.Application.Features.POS.Commands;
using Grimorio.Domain.Entities.Auth;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.Inventory;
using Grimorio.Infrastructure.Persistence;
using Grimorio.SharedKernel.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

internal static class PaymentMethodCorrectionChecks
{
    internal static async Task Run(IServiceProvider services, HttpClient client, SecurityKey key, string adminToken, string deniedToken,
        Guid branch, Guid foreign, Guid actor, Guid dish, Guid size, Guid flavor, Guid cashId, Guid sessionId, Action<bool, string> check)
    {
        async Task Read(Func<GrimorioDbContext, Task> action) {
            await using var scope = services.CreateAsyncScope(); await action(scope.ServiceProvider.GetRequiredService<GrimorioDbContext>());
        }
        async Task<T> Send<T>(IRequest<T> command) {
            await using var scope = services.CreateAsyncScope(); return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(command);
        }
        var card = new PaymentMethodConfig { Name = "Correction card", IsCard = true };
        var transfer = new PaymentMethodConfig { Name = "Correction transfer" };
        var employee = new PaymentMethodConfig { Name = "Correction payroll", Purpose = PaymentMethodPurpose.EmployeePayrollDeduction };
        var bank = new CardBank { BranchId = branch, Name = "Checks bank" };
        var foreignBank = new CardBank { BranchId = foreign, Name = "Other bank" };
        var user = new User { BranchId = branch, Email = "payment-correction@checks.invalid", FirstName = "Correction", LastName = "Delegate", PasswordHash = "checks" };
        var role = new Role { BranchId = branch, Name = "Payment correction delegate" };
        var permission = new Permission { BranchId = branch, Code = AppConstants.Permissions.BillingPaymentsCorrectMethod, Description = "Correction" };
        var grant = new RolePermission { BranchId = branch, RoleId = role.Id, PermissionId = permission.Id };
        await Read(async db => {
            db.AddRange(card, transfer, employee, bank, foreignBank, user, role, permission, grant);
            db.UserRoles.Add(new UserRole { BranchId = branch, RoleId = role.Id, UserId = user.Id });
            var till = await db.CashSessions.SingleAsync(s => s.Id == sessionId); till.Status = CashSessionStatus.Open; till.ClosedAt = null;
            foreach (var stock in await db.WarehouseStock.Where(s => s.BranchId == branch).Include(s => s.Article).ToListAsync()) {
                stock.Quantity += 10000;
                db.StockMovements.Add(new StockMovement { BranchId = branch, ArticleId = stock.ArticleId, WarehouseId = stock.WarehouseId,
                    UnitId = stock.Article!.BaseUnitId, Quantity = 10000, BaseQuantity = 10000, Type = MovementType.InitialInventory });
            }
            await db.SaveChangesAsync();
        });
        async Task<(OrderPaymentDto Payment, PayOrderCommand Request)> Sale(bool mixed = false) {
            var order = await Send(new CreateOrderCommand { BranchId = branch, Type = "Takeout", Items = [new() { MenuItemId = dish, Quantity = 1,
                ModifierSelections = [new() { ModifierOptionId = size, Quantity = 1 }, new() { ModifierOptionId = flavor, Quantity = 1 }] }] });
            await Send(new ConfirmOrderCommand { BranchId = branch, OrderId = order.Id });
            var request = new PayOrderCommand { BranchId = branch, UserId = actor, OrderId = order.Id,
                CashSessionId = sessionId, IdempotencyKey = Guid.NewGuid(), DocumentType = "NotaDeVenta", OrderAmount = order.Total,
                Lines = mixed ? [new() { MethodId = cashId, AmountTendered = order.Total / 2 + 5 }, new() { MethodId = transfer.Id, AmountTendered = order.Total / 2 }]
                    : [new() { MethodId = cashId, AmountTendered = order.Total + 5 }] };
            return (await Send(request), request);
        }
        CorrectPaymentMethodDto Data(OrderPaymentDto payment, Guid? method = null) {
            var line = payment.Lines.First(l => l.MethodId == cashId);
            return new() { RequestId = Guid.NewGuid(), PaymentLineId = line.Id, ExpectedMethodId = line.MethodId,
                ExpectedUpdatedAt = line.UpdatedAt, MethodId = method ?? card.Id, CardPaymentType = "Debit", CardBankId = bank.Id,
                CardBrand = "Visa", AuthorizationNumber = "CHECK-123", Reason = "Registro equivocado de efectivo en caja" };
        }
        async Task Correct(Guid paymentId, CorrectPaymentMethodDto data, HttpStatusCode expected = HttpStatusCode.OK, string? token = null) {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/cash/pagos/{paymentId}/corregir-medio") { Content = JsonContent.Create(data) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token ?? adminToken);
            using var response = await client.SendAsync(request);
            check(response.StatusCode == expected, $"Payment correction expected {expected}, received {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }
        async Task<string> InventorySnapshot() {
            var value = "";
            await Read(async db => value = JsonSerializer.Serialize(new {
                Stock = await db.WarehouseStock.OrderBy(s => s.Id).Select(s => new { s.Id, s.Quantity }).ToListAsync(),
                Movements = await db.StockMovements.OrderBy(s => s.Id).Select(s => new { s.Id, s.BaseQuantity }).ToListAsync(),
                Reservations = await db.StockReservations.OrderBy(s => s.Id).Select(s => new { s.Id, s.Status, s.ConsumedAt, s.ReleasedAt }).ToListAsync(),
                Items = await db.OrderPaymentItems.OrderBy(s => s.Id).Select(s => new { s.Id, s.Quantity, s.Total }).ToListAsync(),
            }));
            return value;
        }
        var (sale, original) = await Sale(true);
        var beforeInventory = await InventorySnapshot();
        var beforeSession = (await Send(new GetCashSessionDetailQuery { BranchId = branch, Id = sessionId }))!;
        var payload = Data(sale);
        await Correct(sale.Id, payload, HttpStatusCode.Forbidden, deniedToken);
        await Correct(sale.Id, payload);
        var afterSession = (await Send(new GetCashSessionDetailQuery { BranchId = branch, Id = sessionId }))!;
        var net = sale.Lines.Single(l => l.Id == payload.PaymentLineId).NetAmount;
        check(afterSession.ExpectedCash == beforeSession.ExpectedCash - net && afterSession.TotalSales == beforeSession.TotalSales,
            "Cash-to-card correction shifts only the cash expectation, never total sales");
        check(await InventorySnapshot() == beforeInventory, "Payment correction leaves stock, movements, reservations and sold items unchanged");
        var sales = await Send(new GetSalesQuery { BranchId = branch, PageSize = 1000 });
        var corrected = sales.Single(s => s.Id == sale.Id);
        var correctedLine = corrected.Lines.Single(l => l.Id == payload.PaymentLineId);
        check(corrected.OrderAmount == sale.OrderAmount && corrected.PaidAt.Ticks / 10 == sale.PaidAt.Ticks / 10 && corrected.Lines.Count == 2
            && correctedLine.MethodId == card.Id && correctedLine.AmountTendered == net && correctedLine.Change == 0
            && correctedLine.CardBankName == bank.Name && correctedLine.UpdatedAt.HasValue,
            "Mixed payment preserves payment/line identities and net amounts while replacing cash tender/change with card details");
        var history = await Send(new GetPaymentMethodCorrectionsQuery { BranchId = branch, PaymentId = sale.Id });
        check(history.Count == 1 && history[0].Before.Change == 5 && history[0].After.NetAmount == net && history[0].ActorName.Contains("A"),
            "Audit retains original cash/change, card details, actor and reason");
        await Correct(sale.Id, payload);
        check((await Send(new GetPaymentMethodCorrectionsQuery { BranchId = branch, PaymentId = sale.Id })).Count == 1,
            "Identical correction replay does not create a second audit or alter balances");
        var replay = await Send(original);
        check(replay.Id == sale.Id && replay.Lines.Any(l => l.MethodId == card.Id) && await InventorySnapshot() == beforeInventory,
            "Original payment request still replays after correction without another sale or inventory deduction");
        var conflicting = Data(sale); conflicting.RequestId = payload.RequestId; conflicting.Reason = "Different reason with same key";
        await Correct(sale.Id, conflicting, HttpStatusCode.BadRequest);
        await Correct(sale.Id, Data(sale), HttpStatusCode.BadRequest);
        var reverse = new CorrectPaymentMethodDto { RequestId = Guid.NewGuid(), PaymentLineId = correctedLine.Id,
            ExpectedMethodId = card.Id, ExpectedUpdatedAt = correctedLine.UpdatedAt, MethodId = cashId,
            Reason = "Correccion de tarjeta a efectivo confirmado" };
        await Correct(sale.Id, reverse);
        check((await Send(new GetCashSessionDetailQuery { BranchId = branch, Id = sessionId }))!.ExpectedCash == beforeSession.ExpectedCash,
            "Card-to-cash correction restores the expected cash by exactly the net amount");
        check((await Send(original)).Id == sale.Id && await InventorySnapshot() == beforeInventory,
            "Original payment replay remains stable after multiple method corrections");
        var (concurrent, _) = await Sale(); var concurrentData = Data(concurrent);
        await Task.WhenAll(Correct(concurrent.Id, concurrentData), Correct(concurrent.Id, concurrentData));
        check((await Send(new GetPaymentMethodCorrectionsQuery { BranchId = branch, PaymentId = concurrent.Id })).Count == 1,
            "Concurrent duplicate corrections are serialized and idempotent");
        var (invalid, _) = await Sale();
        var bad = Data(invalid); bad.CardBankId = foreignBank.Id;
        await Correct(invalid.Id, bad, HttpStatusCode.BadRequest);
        bad = Data(invalid, employee.Id); bad.CardPaymentType = null; bad.CardBankId = null; bad.CardBrand = null; bad.AuthorizationNumber = null;
        await Correct(invalid.Id, bad, HttpStatusCode.BadRequest);
        bad = Data(invalid); bad.Reason = "x";
        await Correct(invalid.Id, bad, HttpStatusCode.BadRequest);
        await Read(async db => { (await db.CashSessions.SingleAsync(s => s.Id == sessionId)).Status = CashSessionStatus.Closed; await db.SaveChangesAsync(); });
        await Correct(invalid.Id, Data(invalid), HttpStatusCode.BadRequest);
        await Correct(sale.Id, payload);
        await Read(async db => { (await db.CashSessions.SingleAsync(s => s.Id == sessionId)).Status = CashSessionStatus.Open;
            (await db.OrderPayments.SingleAsync(p => p.Id == invalid.Id)).DocumentType = DocumentType.Factura; await db.SaveChangesAsync(); });
        await Correct(invalid.Id, Data(invalid), HttpStatusCode.BadRequest);
        await Read(async db => { (await db.OrderPayments.SingleAsync(p => p.Id == invalid.Id)).DocumentType = DocumentType.NotaDeVenta; await db.SaveChangesAsync(); });
        var delegatedToken = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(claims: [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim("BranchId", branch.ToString()),
            new Claim(AppConstants.Claims.Permissions, AppConstants.Permissions.BillingPaymentsCorrectMethod)],
            expires: DateTime.UtcNow.AddHours(1), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
        await Correct(invalid.Id, Data(invalid), token: delegatedToken);
        await Read(async db => { db.RolePermissions.Remove(await db.RolePermissions.SingleAsync(g => g.Id == grant.Id)); await db.SaveChangesAsync(); });
        await Correct(invalid.Id, Data(invalid), HttpStatusCode.BadRequest, delegatedToken);
        check((await Send(new GetPaymentMethodCorrectionsQuery { BranchId = foreign, PaymentId = sale.Id })).Count == 0,
            "Audit history is isolated by branch");
        var (failure, _) = await Sale(); var failureData = Data(failure);
        await Read(db => db.Database.ExecuteSqlRawAsync("ALTER TABLE billing.\"PaymentMethodCorrections\" ADD CONSTRAINT \"CK_Correction_ForcedFailure\" CHECK (\"Reason\" <> 'FORCE FAILURE') NOT VALID"));
        failureData.Reason = "FORCE FAILURE";
        var beforeFailure = await Send(new GetCashSessionDetailQuery { BranchId = branch, Id = sessionId });
        try { await Correct(failure.Id, failureData, HttpStatusCode.InternalServerError); }
        finally { await Read(db => db.Database.ExecuteSqlRawAsync("ALTER TABLE billing.\"PaymentMethodCorrections\" DROP CONSTRAINT \"CK_Correction_ForcedFailure\"")); }
        await Read(async db => check((await db.PaymentLines.SingleAsync(l => l.Id == failureData.PaymentLineId)).PaymentMethodConfigId == cashId
            && (await db.OrderPayments.SingleAsync(p => p.Id == failure.Id)).OriginalPaymentLinesJson == null
            && !await db.PaymentMethodCorrections.AnyAsync(c => c.OrderPaymentId == failure.Id),
            "Forced persistence failure rolls back line, original replay snapshot and audit together"));
        check((await Send(new GetCashSessionDetailQuery { BranchId = branch, Id = sessionId }))!.ExpectedCash == beforeFailure!.ExpectedCash,
            "Failed correction leaves cash expectation unchanged");
        var (racing, _) = await Sale(); var raceData = Data(racing);
        var beforeClose = (await Send(new GetCashSessionDetailQuery { BranchId = branch, Id = sessionId }))!.ExpectedCash;
        using var raceRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/cash/pagos/{racing.Id}/corregir-medio") { Content = JsonContent.Create(raceData) };
        raceRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var correctionTask = client.SendAsync(raceRequest);
        var closeTask = Send(new CloseCashSessionCommand { BranchId = branch, Id = sessionId, UserId = actor, UserName = "Checks", ActualCash = 0 });
        using var raceResponse = await correctionTask;
        var closed = await closeTask;
        check(raceResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest,
            "Correction racing cash close finishes without deadlock or server failure");
        var succeeded = raceResponse.IsSuccessStatusCode;
        check(closed.Status == "Closed" && closed.ExpectedCash == beforeClose - (succeeded ? racing.OrderAmount : 0),
            "Closed session records the corrected cash total only if correction committed before closing");
        await Read(async db => { var till = await db.CashSessions.SingleAsync(s => s.Id == sessionId);
            till.Status = CashSessionStatus.Open; till.ClosedAt = null; till.ActualCash = null; await db.SaveChangesAsync(); });
    }
}
