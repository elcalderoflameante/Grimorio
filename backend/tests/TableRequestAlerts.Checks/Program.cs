using System.Text.Json;
using Grimorio.Application.Features.TableService.Commands;
using Grimorio.Application.Features.TableService.Queries;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Features.TableService.Commands;
using Grimorio.Infrastructure.Features.TableService.Queries;
using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

if (args is ["--serve"]) { await TableRequestChecksServer.Run(); return; }
if (args.Length != 1) throw new Exception("Pass an ignored isolated checks connection JSON file, or --serve for the DB-free browser fixture.");
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
var checks = 0;
void Check(bool value, string label) { if (!value) throw new Exception(label); checks++; }
async Task Reject(Func<Task> action, string text) {
    try { await action(); } catch (InvalidOperationException e) when (e.Message.Contains(text)) { checks++; return; }
    throw new Exception($"Expected rejection: {text}");
}
var branch = new Branch { Name = "Desktop requests checks", Code = Guid.NewGuid().ToString("N")[..10] }; branch.BranchId = branch.Id;
var other = new Branch { Name = "Other", Code = Guid.NewGuid().ToString("N")[..10] }; other.BranchId = other.Id;
var table = new RestaurantTable { BranchId = branch.Id, Code = "5", Area = "Principal" };
var otherTable = new RestaurantTable { BranchId = other.Id, Code = "20" };
var request = new TableServiceRequest { BranchId = branch.Id, RestaurantTableId = table.Id, Type = TableServiceRequestType.Napkins, RequestedAt = DateTime.UtcNow.AddDays(-3) };
var completed = new TableServiceRequest { BranchId = branch.Id, RestaurantTableId = table.Id, Status = TableServiceRequestStatus.Completed, CompletedAt = DateTime.UtcNow };
var cancelled = new TableServiceRequest { BranchId = branch.Id, RestaurantTableId = table.Id, Status = TableServiceRequestStatus.Cancelled };
var taken = new TableServiceRequest { BranchId = branch.Id, RestaurantTableId = table.Id, Status = TableServiceRequestStatus.Taken };
var processing = new TableServiceRequest { BranchId = branch.Id, RestaurantTableId = table.Id, Status = TableServiceRequestStatus.InProgress };
var foreign = new TableServiceRequest { BranchId = other.Id, RestaurantTableId = otherTable.Id };
await using (var db = Db()) {
    await db.Database.MigrateAsync();
    db.AddRange(branch, other, table, otherTable, request, completed, cancelled, taken, processing, foreign);
    await db.SaveChangesAsync();
}
await using (var db = Db()) {
    var rows = await new GetTableServiceRequestsQueryHandler(db).Handle(new() { BranchId = branch.Id, ActiveOnly = true }, default);
    Check(rows.Count == 3 && rows.Any(r => r.Id == request.Id), "Active inbox includes old unresolved requests and excludes terminal/foreign requests");
    var legacy = await new GetTableServiceRequestsQueryHandler(db).Handle(new() { BranchId = branch.Id, FromUtc = DateTime.UtcNow.AddDays(-1) }, default);
    Check(legacy.All(r => r.Id != request.Id), "Existing 24-hour history semantics unchanged");
}
async Task<Grimorio.Application.DTOs.TableServiceRequestDto> Take(Guid branchId, Guid id, Guid actor) {
    await using var db = Db(); return await new TakeTableServiceRequestCommandHandler(db).Handle(new() {
        BranchId = branchId, RequestId = id, UserId = actor, UserName = "Desk operator" }, default);
}
async Task<Grimorio.Application.DTOs.TableServiceRequestDto> Status(Guid branchId, Guid id, TableServiceRequestStatus status) {
    await using var db = Db(); return await new SetTableServiceRequestStatusCommandHandler(db).Handle(new() {
        BranchId = branchId, RequestId = id, Status = status }, default);
}
await Reject(async () => { await Take(other.Id, request.Id, Guid.NewGuid()); }, "no encontrada");
await Reject(async () => { await Status(other.Id, request.Id, TableServiceRequestStatus.Cancelled); }, "no encontrada");
await Reject(async () => { await Status(branch.Id, request.Id, TableServiceRequestStatus.Completed); }, "cambio");
await Reject(async () => { await Status(branch.Id, completed.Id, TableServiceRequestStatus.Pending); }, "cambio");
await Reject(async () => { await Status(branch.Id, request.Id, (TableServiceRequestStatus)123); }, "cambio");
var actorA = Guid.NewGuid(); var actorB = Guid.NewGuid();
async Task<bool> TryTake(Guid actor) {
    try { await Take(branch.Id, request.Id, actor); return true; }
    catch (InvalidOperationException e) when (e.Message.Contains("pendiente")) { return false; }
}
var results = await Task.WhenAll(TryTake(actorA), TryTake(actorB));
Check(results.Count(v => v) == 1, "Two devices racing to take a request have exactly one winner");
await using (var db = Db()) {
    var saved = await db.TableServiceRequests.SingleAsync(r => r.Id == request.Id);
    Check(saved.Status == TableServiceRequestStatus.Taken && saved.TakenAt.HasValue
        && saved.TakenByUserId == (results[0] ? actorA : actorB), "Winning assignment persisted atomically");
}
Check((await Status(branch.Id, request.Id, TableServiceRequestStatus.InProgress)).Status == TableServiceRequestStatus.InProgress, "Start request");
await Reject(async () => { await Status(branch.Id, request.Id, TableServiceRequestStatus.Taken); }, "cambio");
var done = await Status(branch.Id, request.Id, TableServiceRequestStatus.Completed);
Check(done.Status == TableServiceRequestStatus.Completed && done.CompletedAt.HasValue, "Complete request");
DateTime? persistedCompletion;
await using (var db = Db()) persistedCompletion = (await db.TableServiceRequests.SingleAsync(r => r.Id == request.Id)).CompletedAt;
Check((await Status(branch.Id, request.Id, TableServiceRequestStatus.Completed)).CompletedAt == persistedCompletion, "Completion retry preserves persisted completion timestamp");
await Reject(async () => { await Take(branch.Id, request.Id, actorA); }, "pendiente");
Check((await Status(branch.Id, taken.Id, TableServiceRequestStatus.Cancelled)).Status == TableServiceRequestStatus.Cancelled, "Cancel active request");
await using (var db = Db()) {
    Check((await db.TableServiceRequests.SingleAsync(r => r.Id == foreign.Id)).Status == TableServiceRequestStatus.Pending, "Foreign branch untouched");
    Check(!await db.OrderPayments.AnyAsync(p => p.BranchId == branch.Id) && !await db.StockMovements.AnyAsync(m => m.BranchId == branch.Id), "Request actions create no payments or stock movements");
}
Console.WriteLine($"{checks} table request checks passed on isolated Docker database.");
