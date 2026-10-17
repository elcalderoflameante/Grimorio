using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Application.Features.POS.Commands;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

internal static class SaleNoteInvoiceChecks
{
    public static async Task Run(IServiceProvider services, HttpClient client, string adminToken, string waiterToken,
        Guid branch, Guid foreign, Guid actor, Guid dish, Guid size, Guid flavor, Guid cash, Guid session, Action<bool, string> check)
    {
        async Task Read(Func<GrimorioDbContext, Task> action) {
            await using var scope = services.CreateAsyncScope();
            await action(scope.ServiceProvider.GetRequiredService<GrimorioDbContext>());
        }
        async Task<T> Send<T>(IRequest<T> command) {
            await using var scope = services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(command);
        }
        var customer = new Customer { BranchId = branch, Name = "Invoice customer", TaxId = "1712345678", TaxIdType = TaxIdType.Cedula,
            Email = "invoice@checks.invalid" };
        var outsider = new Customer { BranchId = foreign, Name = "Other branch", TaxId = "1712345678" };
        await Read(async db => {
            db.AddRange(customer, outsider);
            var tax = new TaxRate { BranchId = branch, Name = "Checks IVA", Percentage = 15, SriCode = "4" };
            db.TaxRates.Add(tax);
            (await db.MenuItems.SingleAsync(m => m.Id == dish)).TaxRateId = tax.Id;
            foreach (var ingredient in await db.RecipeIngredients.Where(i => i.MenuItemId == dish).ToListAsync()) ingredient.Quantity = 5;
            await db.SaveChangesAsync();
        });
        async Task<(OrderDto Order, OrderPaymentDto Payment, PayOrderCommand Request)> Note(decimal discountPct = 0) {
            var order = await Send(new CreateOrderCommand { BranchId = branch, Type = "Takeout", Items = [new() {
                MenuItemId = dish, Quantity = 2, DiscountPct = discountPct, ModifierSelections = [new() { ModifierOptionId = size, Quantity = 1 },
                    new() { ModifierOptionId = flavor, Quantity = 1 }] }] });
            await Send(new ConfirmOrderCommand { BranchId = branch, OrderId = order.Id });
            var request = new PayOrderCommand { BranchId = branch, OrderId = order.Id, UserId = actor, CashSessionId = session,
                IdempotencyKey = Guid.NewGuid(), OrderAmount = 10 * (1 - discountPct / 100), DocumentType = "NotaDeVenta",
                Items = [new() { OrderItemId = order.Items.Single().Id, Quantity = 1 }],
                Lines = [new() { MethodId = cash, AmountTendered = 10 * (1 - discountPct / 100) }] };
            return (order, await Send(request), request);
        }
        async Task<ElectronicDocumentDto?> Convert(Guid payment, HttpStatusCode expected = HttpStatusCode.OK,
            Guid? buyer = null, string? token = null, string reason = "Cashier selected note by mistake") {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/sri/documentos/facturar-nota/{payment}") {
                Content = JsonContent.Create(new ConvertSaleNoteDto { CustomerId = buyer ?? customer.Id, Reason = reason }) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token ?? adminToken);
            using var response = await client.SendAsync(request);
            check(response.StatusCode == expected, $"Note invoice expected {expected}, got {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<ElectronicDocumentDto>() : null;
        }
        var first = await Note();
        await Convert(first.Payment.Id, HttpStatusCode.Forbidden, token: waiterToken);
        await Convert(first.Payment.Id, HttpStatusCode.BadRequest, buyer: outsider.Id);
        await Convert(first.Payment.Id, HttpStatusCode.BadRequest, reason: "");
        await Convert(first.Payment.Id, HttpStatusCode.BadRequest);
        await Read(async db => {
            check((await db.OrderPayments.SingleAsync(p => p.Id == first.Payment.Id)).DocumentType == DocumentType.NotaDeVenta,
                "Missing issuer config rolls back note conversion");
            db.BranchTaxConfigs.Add(new() { BranchId = branch, Ruc = "1790012345001", RazonSocial = "Checks", Direccion = "Checks" });
            db.SriCertificates.Add(new() { BranchId = branch, FileName = "checks-only.p12", ExpiresAt = DateTime.UtcNow.AddDays(1) });
            await db.SaveChangesAsync();
        });
        var failed = await Note();
        var discounted = await Note(20);
        var otherPayment = await Send(new PayOrderCommand { BranchId = branch, OrderId = first.Order.Id, UserId = actor,
            CashSessionId = session, IdempotencyKey = Guid.NewGuid(), OrderAmount = 10,
            Items = [new() { OrderItemId = first.Order.Items.Single().Id, Quantity = 1 }],
            Lines = [new() { MethodId = cash, AmountTendered = 10 }] });
        await Read(db => db.Database.ExecuteSqlRawAsync("ALTER TABLE billing.\"ElectronicDocuments\" ADD CONSTRAINT \"CK_NoteInvoiceChecks_Failure\" CHECK (\"Secuencial\" < 0) NOT VALID"));
        try { await Convert(failed.Payment.Id, HttpStatusCode.InternalServerError); }
        finally { await Read(db => db.Database.ExecuteSqlRawAsync("ALTER TABLE billing.\"ElectronicDocuments\" DROP CONSTRAINT \"CK_NoteInvoiceChecks_Failure\"")); }
        await Read(async db => {
            var payment = await db.OrderPayments.SingleAsync(p => p.Id == failed.Payment.Id);
            check(payment.DocumentType == DocumentType.NotaDeVenta && payment.CustomerId == null && payment.NoteConvertedAt == null,
                "Document persistence failure rolls back customer, type and audit together");
            check((await db.BranchTaxConfigs.SingleAsync()).Secuencial == 0 && !await db.ElectronicDocuments.AnyAsync(),
                "Document persistence failure rolls back sequence and document");
        });
        string before = "";
        async Task<string> FinancialSnapshot(GrimorioDbContext db) => JsonSerializer.Serialize(new {
            Payments = await db.OrderPayments.OrderBy(p => p.Id).Select(p => new { p.Id, p.OrderId, p.OrderAmount, p.PaidAt, p.CashSessionId, p.IdempotencyKey }).ToListAsync(),
            Lines = await db.PaymentLines.OrderBy(p => p.Id).Select(p => new { p.Id, p.AmountTendered, p.Change, p.PaymentMethodConfigId }).ToListAsync(),
            Items = await db.OrderPaymentItems.AsNoTracking().OrderBy(p => p.Id).ToListAsync(),
            Movements = await db.StockMovements.AsNoTracking().OrderBy(p => p.Id).ToListAsync(),
            Stock = await db.WarehouseStock.AsNoTracking().OrderBy(p => p.Id).ToListAsync(),
            Orders = await db.Orders.OrderBy(o => o.Id).Select(o => new { o.Id, o.Total, o.PaidAt, o.Status }).ToListAsync(),
        });
        await Read(async db => {
            var closed = await db.CashSessions.SingleAsync(s => s.Id == session); closed.Status = CashSessionStatus.Closed; closed.ClosedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(); before = await FinancialSnapshot(db);
        });
        var documents = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => Convert(first.Payment.Id)));
        var invoice = documents[0]!;
        check(documents.All(d => d!.Id == invoice.Id) && invoice.Status == "Pending", "Concurrent conversions reuse one queued document without contacting SRI");
        await Read(async db => {
            check(await FinancialSnapshot(db) == before, "Conversion with closed cash session changes no amounts, payment lines, item snapshots, orders, stock or movements");
            var payment = await db.OrderPayments.SingleAsync(p => p.Id == first.Payment.Id);
            check(payment.DocumentType == DocumentType.Factura && payment.CustomerId == customer.Id && payment.NoteConvertedBy == actor
                && payment.NoteConvertedAt != null && payment.NoteConversionReason != null, "Conversion records customer, actor, reason and original note history");
            check(await db.ElectronicDocuments.CountAsync() == 1 && (await db.BranchTaxConfigs.SingleAsync()).Secuencial == 1,
                "Concurrent retry allocates exactly one invoice number");
            var document = await db.ElectronicDocuments.SingleAsync();
            check(document.ImporteTotal == 10 && document.TotalIva == 1.30m && document.EmailRecipient == customer.Email && document.EmissionDate == payment.PaidAt,
                "Invoice uses only partial payment amount, selected customer and original payment date");
            check((await db.OrderPayments.SingleAsync(p => p.Id == otherPayment.Id)).DocumentType == DocumentType.NotaDeVenta,
                "Other receipt of same order remains a sale note");
        });
        await Convert(first.Payment.Id, HttpStatusCode.BadRequest, reason: "A different conversion request");
        var replay = await Send(first.Request);
        check(replay.Id == first.Payment.Id && replay.DocumentType == "Factura", "Original payment retry after conversion returns same receipt without charging again");
        await Convert(failed.Payment.Id);
        var savings = (await Convert(discounted.Payment.Id))!;
        check(savings.ImporteTotal == 8 && savings.TotalDescuento == 1.74m && savings.TotalIva == 1.04m
            && savings.TotalSinImpuestos == 6.96m, "Converted invoice preserves original discount and VAT snapshots");
    }
}
