using System.Text.Json;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Application.Features.Billing.Queries;
using Grimorio.Domain.Entities.Auth;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.Finance;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Features.Billing.Commands;
using Grimorio.Infrastructure.Features.Finance.Commands;
using Grimorio.Infrastructure.Persistence;
using Grimorio.SharedKernel.Constants;
using Microsoft.EntityFrameworkCore;
using Npgsql;

if (args.Length != 1) throw new Exception("Pass an ignored local checks connection JSON file.");
using var config = JsonDocument.Parse(await File.ReadAllTextAsync(args[0]));
var c = config.RootElement;
var target = new NpgsqlConnectionStringBuilder {
    Host = c.GetProperty("host").GetString(), Port = c.GetProperty("port").GetInt32(),
    Database = c.GetProperty("database").GetString(), Username = c.GetProperty("username").GetString(),
    Password = c.GetProperty("password").GetString(),
};
if (target.Host != "127.0.0.1" || target.Port != 5432 || target.Database != "grimorio_checks" || target.Username != "grimorio_checks")
    throw new Exception("Only the isolated localhost grimorio_checks database and checks user are allowed.");
GrimorioDbContext Db() => new(new DbContextOptionsBuilder<GrimorioDbContext>().UseNpgsql(target.ConnectionString).Options);
await using (var db = Db()) { await db.Database.MigrateAsync(); if (db.Database.HasPendingModelChanges()) throw new Exception("Model does not match migration"); }
var checks = 1;
void Check(bool value, string label) { if (!value) throw new Exception(label); checks++; }
async Task Reject(Func<Task> action, string fragment) {
    try { await action(); } catch (InvalidOperationException e) when (e.Message.Contains(fragment)) { checks++; return; }
    throw new Exception($"Expected rejection: {fragment}");
}
var branch = new Branch { Name = "Cash separation checks", Code = Guid.NewGuid().ToString("N")[..10] }; branch.BranchId = branch.Id;
var actor = new User { BranchId = branch.Id, FirstName = "Admin", LastName = "Checks", Email = $"{Guid.NewGuid()}@example.invalid", PasswordHash = "checks" };
var cashier = new User { BranchId = branch.Id, FirstName = "Cashier", LastName = "Original", Email = $"{Guid.NewGuid()}@example.invalid", PasswordHash = "checks" };
var admin = new Role { BranchId = branch.Id, Name = AppConstants.Roles.Admin };
var register = new CashRegister { BranchId = branch.Id, Name = "Main", Code = "CHECKS" };
var cutoff = DateTime.UtcNow.Date.AddHours(-12);
var cash = new PaymentMethodConfig { Name = Guid.NewGuid().ToString(), IsCash = true };
var card = new PaymentMethodConfig { Name = Guid.NewGuid().ToString(), IsCard = true };
var transfer = new PaymentMethodConfig { Name = Guid.NewGuid().ToString() };
var center = new CostCenter { BranchId = branch.Id, Name = "Checks" };
var category = new ExpenseCategory { BranchId = branch.Id, Name = "Checks" };
var session = new CashSession { BranchId = branch.Id, CashRegisterId = register.Id, OpenedBy = cashier.Id,
    OpenedByName = "Cashier Original", OpenedAt = cutoff.AddHours(-14), OpeningBalance = 50 };
var pending = new Order { BranchId = branch.Id, Number = 90, Type = OrderType.DineIn, Status = OrderStatus.InPreparation };
var payments = new List<OrderPayment>();
var expenses = new List<Expense>();
await using (var db = Db()) {
    db.AddRange(branch, actor, cashier, admin, register, cash, card, transfer, center, category, session, pending);
    db.UserRoles.Add(new UserRole { BranchId = branch.Id, RoleId = admin.Id, UserId = actor.Id });
    OrderPayment Payment(int number, DateTime at, decimal amount, PaymentMethodConfig method, decimal change = 0) {
        var order = new Order { BranchId = branch.Id, Number = number, Type = OrderType.Takeout, Status = OrderStatus.Delivered, Total = amount };
        var p = new OrderPayment { BranchId = branch.Id, CashSessionId = session.Id, OrderId = order.Id, PaidAt = at, OrderAmount = amount };
        p.Lines.Add(new PaymentLine { BranchId = branch.Id, PaymentMethodConfigId = method.Id, AmountTendered = amount + change, Change = change });
        db.Add(order); db.Add(p); payments.Add(p); return p;
    }
    Payment(1, cutoff.AddHours(-2), 100, cash, 10); Payment(2, cutoff.AddHours(-1), 50, card);
    Payment(3, cutoff, 5, cash); Payment(4, cutoff.AddHours(1), 30, cash); Payment(5, cutoff.AddHours(2), 70, transfer);
    foreach (var (at, amount, status) in new[] { (cutoff.AddHours(-1), 10m, ExpenseStatus.Registered),
        (cutoff.AddHours(1), 3m, ExpenseStatus.Registered), (cutoff.AddHours(2), 99m, ExpenseStatus.Cancelled) }) {
        var e = new Expense { BranchId = branch.Id, CashSessionId = session.Id, RegisteredAt = at,
            ExpenseDate = cutoff.AddDays(-2), Amount = amount, Status = status, RegisteredBy = cashier.Id,
            CostCenterId = center.Id, ExpenseCategoryId = category.Id, PaymentMethodConfigId = cash.Id };
        db.Add(e); expenses.Add(e);
    }
    await db.SaveChangesAsync();
}
SeparateCashSessionDto Data() => new() { RequestId = Guid.NewGuid(), Cutoff = cutoff, NewOpeningBalance = 20,
    PreviousActualCash = null, Reason = "Forgot previous closing" };
async Task<CashSessionSeparationPreviewDto> Preview(SeparateCashSessionDto data, Guid? user = null, Guid? branchId = null) {
    await using var db = Db(); return await new PreviewCashSessionSeparationHandler(db).Handle(new() {
        BranchId = branchId ?? branch.Id, ActorUserId = user ?? actor.Id, SessionId = session.Id, Data = data }, default);
}
async Task<CashSessionSeparationResultDto> Apply(SeparateCashSessionDto data) {
    await using var db = Db(); return await new SeparateCashSessionHandler(db).Handle(new() {
        BranchId = branch.Id, ActorUserId = actor.Id, SessionId = session.Id, Data = data }, default);
}
await Reject(async () => { await Preview(Data(), cashier.Id); }, "permiso");
await Reject(async () => { await Preview(Data(), branchId: Guid.NewGuid()); }, "permiso");
var invalid = Data(); invalid.Cutoff = DateTimeOffset.UtcNow.AddDays(1); await Reject(async () => { await Preview(invalid); }, "futuro");
invalid = Data(); invalid.Cutoff = session.OpenedAt; await Reject(async () => { await Preview(invalid); }, "posterior");
invalid = Data(); invalid.NewOpeningBalance = -1; await Reject(async () => { await Preview(invalid); }, "importes");
invalid = Data(); invalid.Reason = "x"; await Reject(async () => { await Preview(invalid); }, "motivo");
var data = Data(); var preview = await Preview(data);
Check(preview.PreviousSession.TotalSales == 150 && preview.NewSession.TotalSales == 105, "Cut partitions paid totals, including exact boundary");
Check(preview.PreviousSession.ExpectedCash == 140 && preview.NewSession.ExpectedCash == 52, "Net cash after change, expenses and independent opening funds");
Check(preview.PreviousSession.ActualCash == null && preview.PreviousSession.CashDifference == null, "Unknown count remains unknown");
Check(preview.Movements.Count(m => m.MovesToNewSession) == 5, "Moves three payments, registered expense and cancelled expense history");
Check(preview.NewSession.TotalExpenses == 3, "Expenses partition by registration, not backdated business date; cancelled excluded");
var counted = Data(); counted.PreviousActualCash = 138;
Check((await Preview(counted)).PreviousSession.CashDifference == -2, "Known physical count retains actual difference");
data.PreviewToken = preview.Token;
await using (var db = Db()) {
    var line = await db.PaymentLines.SingleAsync(l => l.OrderPaymentId == payments[4].Id);
    line.PaymentMethodConfigId = card.Id; await db.SaveChangesAsync();
}
await Reject(async () => { await Apply(data); }, "cambiaron");
await using (var db = Db()) {
    Check((await db.CashSessions.SingleAsync(s => s.Id == session.Id)).Status == CashSessionStatus.Open, "Stale preview makes no partial changes");
    var line = await db.PaymentLines.SingleAsync(l => l.OrderPaymentId == payments[4].Id);
    line.PaymentMethodConfigId = transfer.Id; await db.SaveChangesAsync();
}
preview = await Preview(data); data.PreviewToken = preview.Token;
long stockCount; string beforeLines; string beforePayments;
await using (var db = Db()) {
    stockCount = await db.StockMovements.LongCountAsync();
    beforeLines = JsonSerializer.Serialize(await db.PaymentLines.AsNoTracking().Where(l => payments.Select(p => p.Id).Contains(l.OrderPaymentId)).OrderBy(l => l.Id)
        .Select(l => new { l.Id, l.OrderPaymentId, l.PaymentMethodConfigId, l.AmountTendered, l.Change }).ToListAsync());
    beforePayments = JsonSerializer.Serialize(await db.OrderPayments.AsNoTracking().Where(p => p.BranchId == branch.Id).OrderBy(p => p.Id)
        .Select(p => new { p.Id, p.PaidAt, p.OrderAmount, p.OrderId, p.DocumentType }).ToListAsync());
}
// Hold the real ledger lock: the separation must wait, then detect the new movement.
await using (var blocker = Db()) {
    await using var tx = await blocker.Database.BeginTransactionAsync();
    await blocker.CashSessions.FromSqlInterpolated($"SELECT * FROM billing.\"CashSessions\" WHERE \"Id\" = {session.Id} FOR UPDATE").SingleAsync();
    var inFlight = Apply(data);
    await Task.Delay(250);
    Check(!inFlight.IsCompleted, "Separation waits for a concurrent cash writer's session lock");
    var extra = new Expense { BranchId = branch.Id, CashSessionId = session.Id, RegisteredAt = cutoff.AddHours(3), ExpenseDate = cutoff,
        Amount = 1, CostCenterId = center.Id, ExpenseCategoryId = category.Id, PaymentMethodConfigId = cash.Id };
    blocker.Expenses.Add(extra); await blocker.SaveChangesAsync(); await tx.CommitAsync();
    await Reject(async () => { await inFlight; }, "cambiaron");
}
preview = await Preview(data); data.PreviewToken = preview.Token;
// Force a database failure after the ledger updates have been scheduled.
var forced = Data(); forced.Reason = "Force rollback"; forced.PreviewToken = (await Preview(forced)).Token;
await using (var db = Db()) {
    await db.Database.ExecuteSqlRawAsync("""
        ALTER TABLE billing."CashSessionSeparations" ADD CONSTRAINT "CK_SeparationChecks_Rollback"
        CHECK ("Reason" <> 'Force rollback') NOT VALID
        """);
}
try {
    try { await Apply(forced); throw new Exception("Expected database rollback"); }
    catch (DbUpdateException) { checks++; }
    await using var db = Db();
    Check(await db.CashSessions.CountAsync(s => s.BranchId == branch.Id) == 1
        && (await db.CashSessions.SingleAsync(s => s.Id == session.Id)).Status == CashSessionStatus.Open,
        "Database failure rolls back close and creation of new session");
    Check(await db.OrderPayments.CountAsync(p => p.BranchId == branch.Id && p.CashSessionId == session.Id) == payments.Count,
        "Database failure rolls back all payment reassignments");
} finally {
    await using var db = Db();
    await db.Database.ExecuteSqlRawAsync("ALTER TABLE billing.\"CashSessionSeparations\" DROP CONSTRAINT \"CK_SeparationChecks_Rollback\"");
}
await using var staleExpenseContext = Db();
await staleExpenseContext.Expenses.Include(e => e.CashSession).SingleAsync(e => e.Id == expenses[1].Id);
var results = await Task.WhenAll(Apply(data), Apply(data));
Check(results[0].NewSessionId == results[1].NewSessionId && results[0].Id == results[1].Id, "Concurrent identical confirmations are idempotent");
var replay = await Apply(data); Check(replay.NewSessionId == results[0].NewSessionId, "Retry after response loss returns same session");
await Reject(async () => { await new CancelExpenseHandler(staleExpenseContext).Handle(new() {
    BranchId = branch.Id, Id = expenses[1].Id, UserId = cashier.Id, UserName = "Cashier Original", Reason = "Stale operation",
}, default); }, "cerrada");
var different = Data(); different.PreviewToken = data.PreviewToken;
await Reject(async () => { await Apply(different); }, "cerrada");
var changed = JsonSerializer.Deserialize<SeparateCashSessionDto>(JsonSerializer.Serialize(data))!; changed.NewOpeningBalance = 99;
await Reject(async () => { await Apply(changed); }, "otros datos");
await using (var db = Db()) {
    var old = await db.CashSessions.SingleAsync(s => s.Id == session.Id);
    var next = await db.CashSessions.SingleAsync(s => s.Id == replay.NewSessionId);
    Check(old.Status == CashSessionStatus.Closed && old.ClosedAt == cutoff && old.ClosedBy == actor.Id, "Previous session closes effectively at cutoff with actual actor");
    Check(old.ActualCash == null && next.OpenedBy == cashier.Id && next.OpenedAt == cutoff && next.OpeningBalance == 20, "New session preserves cashier; pending count not fabricated");
    Check(await db.CashSessions.CountAsync(s => s.BranchId == branch.Id && s.Status == CashSessionStatus.Open) == 1, "Exactly one open session");
    foreach (var p in payments) Check((await db.OrderPayments.SingleAsync(x => x.Id == p.Id)).CashSessionId == (p.PaidAt < cutoff ? session.Id : next.Id), "Payment reassigned once by paid time");
    foreach (var e in expenses) Check((await db.Expenses.SingleAsync(x => x.Id == e.Id)).CashSessionId == (e.RegisteredAt < cutoff ? session.Id : next.Id), "Expense including cancellation history reassigned by registration");
    Check(stockCount == await db.StockMovements.LongCountAsync(), "No inventory movements added or removed");
    Check(beforeLines == JsonSerializer.Serialize(await db.PaymentLines.AsNoTracking().Where(l => payments.Select(p => p.Id).Contains(l.OrderPaymentId)).OrderBy(l => l.Id)
        .Select(l => new { l.Id, l.OrderPaymentId, l.PaymentMethodConfigId, l.AmountTendered, l.Change }).ToListAsync()), "Payment lines untouched");
    Check(beforePayments == JsonSerializer.Serialize(await db.OrderPayments.AsNoTracking().Where(p => p.BranchId == branch.Id).OrderBy(p => p.Id)
        .Select(p => new { p.Id, p.PaidAt, p.OrderAmount, p.OrderId, p.DocumentType }).ToListAsync()), "Payment identities, amounts, dates and document types untouched");
    Check((await db.Orders.SingleAsync(o => o.Id == pending.Id)).Status == OrderStatus.InPreparation, "Pending orders remain independent of cash session");
    var audits = await new GetCashSessionSeparationsHandler(db).Handle(new() { BranchId = branch.Id, SessionId = next.Id }, default);
    Check(audits.Count == 1 && audits[0].RecordedAt > cutoff && audits[0].CutoffUtc == cutoff && audits[0].ActorName == "Admin Checks", "Immutable audit distinguishes effective cutoff and actual operation time");
    Check(audits[0].Preview.NewSession.Id == next.Id && audits[0].Preview.NewSession.ExpectedCash == 51, "Audit retains exact preview and actual new session identity");
    Check((await new GetCashSessionSeparationsHandler(db).Handle(new() { BranchId = Guid.NewGuid(), SessionId = next.Id }, default)).Count == 0, "Audit branch isolation");
}
Console.WriteLine($"{checks} cash session separation checks passed on isolated Docker database.");
