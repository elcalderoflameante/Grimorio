using Grimorio.Application.Abstractions;
using Grimorio.Application.DTOs;
using Grimorio.Domain.Entities.Auth;
using Grimorio.Domain.Entities.Inventory;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Infrastructure.Features.Inventory;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Npgsql;

var checks = 0;
void Check(bool condition, string label) { if (!condition) throw new Exception(label); checks++; }
async Task Reject(Func<Task> action, string label) { try { await action(); } catch (InvalidOperationException) { checks++; return; } throw new Exception($"Expected rejection: {label}"); }
var monday = new DateOnly(2026, 10, 26);
Check(StaffInventoryCountService.IsDue("Daily", 1, monday, []), "Daily due");
Check(!StaffInventoryCountService.IsDue("Daily", 1, monday, [monday]), "Daily already counted");
Check(StaffInventoryCountService.IsDue("Daily", 1, monday, [monday.AddDays(-1)]), "Daily next date");
Check(!StaffInventoryCountService.IsDue("Weekly", 3, monday, []), "Before weekly day");
Check(StaffInventoryCountService.IsDue("Weekly", 3, monday.AddDays(4), []), "Missed weekly day remains pending");
Check(!StaffInventoryCountService.IsDue("Weekly", 3, monday.AddDays(4), [monday.AddDays(2)]), "Weekly counted");
Check(StaffInventoryCountService.IsDue("Weekly", 0, monday.AddDays(6), [monday.AddDays(-1)]), "Sunday belongs to the right week");
Check(StaffInventoryCountService.IsDue("Weekly", 1, monday.AddDays(7), [monday]), "Next week due");
if (!args.Contains("--database")) { Console.WriteLine($"{checks} scheduling checks passed. Use --database for the isolated persistence checks."); return; }

// Only disposable test databases are accepted. Never reads application configuration.
var connection = "Host=127.0.0.1;Port=15440;Database=inventory_counts_checks;Username=checks;Password=checks";
var connectionIndex = Array.IndexOf(args, "--checks-connection");
if (connectionIndex >= 0)
{
    using var connectionConfig = JsonDocument.Parse(await File.ReadAllTextAsync(args[connectionIndex + 1]));
    var c = connectionConfig.RootElement;
    var target = new NpgsqlConnectionStringBuilder {
        Host = c.GetProperty("host").GetString(), Port = c.GetProperty("port").GetInt32(),
        Database = c.GetProperty("database").GetString(), Username = c.GetProperty("username").GetString(),
        Password = c.GetProperty("password").GetString(),
    };
    if (target.Host != "127.0.0.1" || target.Port != 5432 || target.Database != "grimorio_checks" || target.Username != "grimorio_checks")
        throw new Exception("Only the isolated localhost grimorio_checks database and checks user are allowed.");
    connection = target.ConnectionString;
}
var options = new DbContextOptionsBuilder<GrimorioDbContext>().UseNpgsql(connection).Options;
var actor = new CheckUser { UserId = Guid.NewGuid() };
await using var db = new GrimorioDbContext(options, actor);
await db.Database.MigrateAsync();
var branchId = Guid.NewGuid(); var branch = new Branch { Id = branchId, BranchId = branchId, Name = "Inventory count checks", Code = Guid.NewGuid().ToString("N")[..8] };
var first = new User { Id = actor.UserId, BranchId = branchId, FirstName = "Ana", LastName = "Checks", Email = $"{Guid.NewGuid()}@example.invalid", PasswordHash = "checks" };
var second = new User { BranchId = branchId, FirstName = "Maria", LastName = "Checks", Email = $"{Guid.NewGuid()}@example.invalid", PasswordHash = "checks" };
var outsider = new User { BranchId = branchId, FirstName = "Other", LastName = "Checks", Email = $"{Guid.NewGuid()}@example.invalid", PasswordHash = "checks" };
var unit = new MeasurementUnit { BranchId = branchId, Name = "Unit", Symbol = "u" };
var category = new InventoryCategory { BranchId = branchId, Name = "Beverages" };
var warehouse = new Warehouse { BranchId = branchId, Name = "Bar" };
var article = new InventoryArticle { BranchId = branchId, Name = "Beer", BaseUnitId = unit.Id, CategoryId = category.Id, Type = ArticleType.FinishedProduct };
var extraArticle = new InventoryArticle { BranchId = branchId, Name = "Water", BaseUnitId = unit.Id, CategoryId = category.Id, Type = ArticleType.FinishedProduct };
var stock = new WarehouseStock { BranchId = branchId, ArticleId = article.Id, WarehouseId = warehouse.Id, Quantity = 12 };
db.AddRange(branch, first, second, outsider, unit, category, warehouse, article, extraArticle, stock); await db.SaveChangesAsync();
var service = new StaffInventoryCountService(db, actor);
var config = await service.SaveArea(branchId, new() { Name = "Bar", UserIds = [first.Id, second.Id], Rules = [new(article.Id, warehouse.Id, "Daily")] }, default);
db.ChangeTracker.Clear();
config.UserIds.Add(outsider.Id);
config.Rules = [new(article.Id, warehouse.Id, "Weekly", 3)];
await service.SaveArea(branchId, config, default);
db.ChangeTracker.Clear();
var edited = (await service.Catalog(branchId, default)).Areas.Single();
Check(edited.UserIds.Count == 3 && edited.Rules.Single().Frequency == "Weekly", "Existing area accepts a new member and frequency change");
edited.Rules.Add(new(extraArticle.Id, warehouse.Id, "Daily"));
await service.SaveArea(branchId, edited, default);
db.ChangeTracker.Clear();
edited = (await service.Catalog(branchId, default)).Areas.Single();
Check(edited.Rules.Count == 2, "Existing area accepts an additional article");
await service.SaveArea(branchId, edited, default);
db.ChangeTracker.Clear();
Check(await db.InventoryCountMembers.CountAsync(x => x.AreaId == config.Id) == 3
    && await db.InventoryCountRules.CountAsync(x => x.AreaId == config.Id) == 2, "Saving unchanged area does not duplicate assignments");
edited.UserIds = [first.Id, second.Id];
edited.Rules = [];
await service.SaveArea(branchId, edited, default);
db.ChangeTracker.Clear();
edited.Rules = [new(article.Id, warehouse.Id, "Daily")];
await service.SaveArea(branchId, edited, default);
db.ChangeTracker.Clear();
var restored = (await service.Catalog(branchId, default)).Areas.Single();
Check(restored.UserIds.Count == 2 && restored.Rules.Single().Frequency == "Daily", "Removed article rule can be added again to an existing area");
restored.UserIds.Add(outsider.Id);
restored.Name = "Bar edited"; restored.IsActive = false;
await service.SaveArea(branchId, restored, default);
db.ChangeTracker.Clear();
var inactive = (await service.Catalog(branchId, default)).Areas.Single();
Check(inactive.Name == "Bar edited" && !inactive.IsActive && inactive.UserIds.Count == 3, "Rename, deactivate and restore a removed member");
inactive.Name = "Bar"; inactive.IsActive = true; inactive.UserIds = [first.Id, second.Id];
await service.SaveArea(branchId, inactive, default);
db.ChangeTracker.Clear();
await Reject(() => service.SaveArea(Guid.NewGuid(), new() { Name = "Cross branch", UserIds = [first.Id], Rules = [] }, default), "Cross branch assignment");
await Reject(() => service.SaveArea(branchId, new() { Name = "Duplicate ownership", UserIds = [first.Id], Rules = config.Rules }, default), "Duplicate area ownership");
var count = await service.Start(branchId, config.Id, default);
var id = count.Id; var lineId = count.Lines.Single().Id;
Check((await service.Start(branchId, config.Id, default)).Id == id, "Start is idempotent");
await Reject(() => service.Submit(branchId, id, default), "Missing is not zero");
actor.UserId = outsider.Id;
await Reject(() => service.Detail(branchId, id, true, default), "Unassigned staff cannot read count");
await Reject(() => service.SaveLine(branchId, id, lineId, new(1, ""), default), "Unassigned staff cannot write count");
actor.UserId = second.Id;
Check((await service.Start(branchId, config.Id, default)).Id == id, "Shared bar has one draft");
await Reject(() => service.Detail(Guid.NewGuid(), id, false, default), "Cross branch count access");
await Reject(() => service.SaveLine(branchId, id, lineId, new(-1, ""), default), "Negative quantity");
count = await service.SaveLine(branchId, id, lineId, new(0, "No bottles"), default);
Check(count.Lines.Single().Quantity == 0 && count.Lines.Single().ExpectedQuantity == null && count.Lines.Single().Difference == null, "Zero saved, staff cannot see expected stock");
var capturedAt = count.Lines.Single().CountedAt;
// A real stock movement after counting must not change the historical comparison.
(await db.WarehouseStock.SingleAsync(x => x.Id == stock.Id)).Quantity = 10;
await db.SaveChangesAsync(); db.ChangeTracker.Clear();
await Reject(() => service.SaveLine(branchId, id, lineId, new(2, "Stale edit"), default), "Stale shared edits rejected");
count = await service.SaveLine(branchId, id, lineId, new(0, "Confirmed empty", capturedAt), default);
var supervisor = await service.Detail(branchId, id, false, default);
Check(supervisor.Lines.Single().ExpectedQuantity == 12 && supervisor.Lines.Single().Difference == -12, "Snapshot preserved after movement and note edit");
count = await service.Submit(branchId, id, default);
Check(count.Status == "Submitted" && count.CounterName.StartsWith("Maria"), "Submission attributed to sender");
Check((await service.Submit(branchId, id, default)).Status == "Submitted", "Submission retry is idempotent");
await Reject(() => service.SaveLine(branchId, id, lineId, new(1, "", capturedAt), default), "Submitted immutable");
Check((await service.Mine(branchId, default)).Single().PendingArticles == 0, "No duplicate daily count");
await service.Review(branchId, id, new("RecountRequested", "Verify bottles"), default);
var recount = await service.Start(branchId, config.Id, default);
Check(recount.Id != id && recount.RecountOfId == id, "Recount creates a separate historical record");
recount = await service.SaveLine(branchId, recount.Id, recount.Lines.Single().Id, new(9, "One broken"), default);
await service.Submit(branchId, recount.Id, default);
await service.Review(branchId, recount.Id, new("Reviewed", "Investigate breakage separately"), default);
await Reject(() => service.Review(branchId, recount.Id, new("Dismissed", "Overwrite"), default), "Review cannot be overwritten");
db.ChangeTracker.Clear();
Check(await db.WarehouseStock.Where(x => x.Id == stock.Id).Select(x => x.Quantity).SingleAsync() == 10, "Counts and reviews never adjust stock");
Check(!await db.StockMovements.AnyAsync(x => x.BranchId == branchId) && !await db.StockReservations.AnyAsync(x => x.BranchId == branchId), "Counts never create movements or reservations");
var original = await service.Detail(branchId, id, false, default);
Check(original.Status == "Recounted" && original.Lines.Single().Quantity == 0 && original.ReviewNotes == "Verify bottles", "Recount preserves original observation and reviewer decision");
await service.SaveArea(branchId, new() { Id = config.Id, Name = "Bar after counts", UserIds = [first.Id], Rules = [new(extraArticle.Id, warehouse.Id, "Weekly", 3)] }, default);
db.ChangeTracker.Clear();
var historical = await service.Detail(branchId, id, false, default);
Check(historical.AreaName == "Bar" && historical.Lines.Single().ArticleId == article.Id
    && historical.Lines.Single().Quantity == 0, "Area edits preserve historical count names, articles and quantities");
var today = BranchTimeZone.DateFromUtc(DateTime.UtcNow, branch.TimeZoneId);
Check((await service.List(branchId, today, today, null, default)).Count == 2, "History includes original and recount");
Console.WriteLine($"{checks} staff inventory count checks passed, including isolated PostgreSQL persistence.");

sealed class CheckUser : ICurrentUserContext
{
    public Guid UserId { get; set; }
    public string? UserEmail => "checks@example.invalid";
    public bool IsAuthenticated => true;
}
