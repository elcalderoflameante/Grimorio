using System.Text.Json;
using Grimorio.Application.Features.Inventory.Queries;
using Grimorio.Domain.Entities.Inventory;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Infrastructure.Features.Inventory.Queries;
using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var connection = Environment.GetEnvironmentVariable("STOCK_BALANCE_CHECKS_CONNECTION");
if (args.Length == 1)
{
    using var config = JsonDocument.Parse(await File.ReadAllTextAsync(args[0]));
    var c = config.RootElement;
    connection = new NpgsqlConnectionStringBuilder {
        Host = c.GetProperty("host").GetString(), Port = c.GetProperty("port").GetInt32(),
        Database = c.GetProperty("database").GetString(), Username = c.GetProperty("username").GetString(),
        Password = c.GetProperty("password").GetString(),
    }.ConnectionString;
}
if (string.IsNullOrEmpty(connection)) throw new Exception("Configure STOCK_BALANCE_CHECKS_CONNECTION or pass a local JSON file.");
var target = new NpgsqlConnectionStringBuilder(connection);
if (target.Host is not ("localhost" or "127.0.0.1" or "::1") || target.Database != "grimorio_checks")
    throw new Exception("Only the local isolated grimorio_checks database is permitted.");

await using var db = new GrimorioDbContext(new DbContextOptionsBuilder<GrimorioDbContext>().UseNpgsql(connection).Options);
await using var transaction = await db.Database.BeginTransactionAsync();
var count = 0;
void Check(bool value, string label) { if (!value) throw new Exception(label); count++; }
try
{
    var branchId = Guid.NewGuid(); var otherBranchId = Guid.NewGuid();
    var branch = new Branch { Id = branchId, BranchId = branchId, Name = "Balance checks", Code = Guid.NewGuid().ToString("N")[..8] };
    var otherBranch = new Branch { Id = otherBranchId, BranchId = otherBranchId, Name = "Other checks", Code = Guid.NewGuid().ToString("N")[..8] };
    var gram = new MeasurementUnit { BranchId = branchId, Name = "Gram", Symbol = "g" };
    var kilo = new MeasurementUnit { BranchId = branchId, Name = "Kilogram", Symbol = "kg" };
    var category = new InventoryCategory { BranchId = branchId, Name = "Checks" };
    var warehouse = new Warehouse { BranchId = branchId, Name = "Main" };
    var otherWarehouse = new Warehouse { BranchId = branchId, Name = "Other" };
    var article = new InventoryArticle { BranchId = branchId, Name = "Lettuce", BaseUnitId = gram.Id, CategoryId = category.Id };
    var otherArticle = new InventoryArticle { BranchId = branchId, Name = "Other article", BaseUnitId = gram.Id, CategoryId = category.Id };
    db.AddRange(branch, otherBranch, gram, kilo, category, warehouse, otherWarehouse, article, otherArticle);
    await db.SaveChangesAsync();
    var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    StockMovement Movement(decimal delta, MovementType type, int day, Guid? warehouseId = null, Guid? articleId = null, Guid? scope = null, bool deleted = false, Guid? id = null)
    {
        var row = new StockMovement {
            Id = id ?? Guid.NewGuid(), BranchId = scope ?? branchId, ArticleId = articleId ?? article.Id,
            WarehouseId = warehouseId ?? warehouse.Id, UnitId = gram.Id, Type = type,
            Quantity = Math.Abs(delta), BaseQuantity = delta, IsDeleted = deleted,
            Notes = day.ToString(),
        };
        db.Add(row); return row;
    }
    var first = Movement(500, MovementType.InitialInventory, 0);
    var exit = Movement(-200, MovementType.ManualExit, 1);
    var purchase = Movement(1000, MovementType.PurchaseEntry, 2);
    purchase.UnitId = kilo.Id; purchase.Quantity = 1;
    Movement(-25.125m, MovementType.Waste, 3);
    Movement(5.125m, MovementType.SaleRestoration, 4);
    Movement(-300, MovementType.TransferOut, 5);
    var tie1 = Movement(1, MovementType.ManualEntry, 6, id: Guid.Parse("10000000-0000-0000-0000-000000000001"));
    var tie2 = Movement(-2, MovementType.ManualExit, 6, id: Guid.Parse("10000000-0000-0000-0000-000000000002"));
    Movement(10000, MovementType.TransferIn, 1, warehouseId: otherWarehouse.Id);
    Movement(-3.25m, MovementType.ManualExit, 1, articleId: otherArticle.Id);
    Movement(9000, MovementType.ManualEntry, 1, scope: otherBranchId);
    Movement(9999, MovementType.ManualEntry, 1, deleted: true);
    var inserted = db.ChangeTracker.Entries<StockMovement>().Select(e => e.Entity).ToArray();
    await db.SaveChangesAsync();
    foreach (var row in inserted)
    {
        var instant = start.AddDays(int.Parse(row.Notes!));
        await db.StockMovements.IgnoreQueryFilters().Where(x => x.Id == row.Id)
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.CreatedAt, instant));
    }
    db.ChangeTracker.Clear();
    var handler = new GetStockMovementsHandler(db);
    GetStockMovementsQuery Query() => new() { BranchId = branchId, ArticleId = article.Id, WarehouseId = warehouse.Id, PageSize = 100 };
    var all = await handler.Handle(Query(), default);
    Check(all.Count == 8, "Only requested article, warehouse, branch and non-deleted movements");
    Check(all.Single(m => m.Id == first.Id).StockBefore == 0 && all.Single(m => m.Id == first.Id).StockAfter == 500, "Initial stock starts at zero");
    Check(all.Single(m => m.Id == exit.Id).StockBefore == 500 && all.Single(m => m.Id == exit.Id).StockAfter == 300, "Exit reduces balance");
    var entry = all.Single(m => m.Id == purchase.Id);
    Check(entry.StockBefore == 300 && entry.StockAfter == 1300 && entry.Quantity == 1 && entry.UnitSymbol == "kg" && entry.BaseUnitSymbol == "g", "Balances use base units, not purchase quantities");
    Check(all.Single(m => m.Type == "Waste").StockAfter == 1274.875m, "Fractional quantities preserve precision");
    Check(all.Single(m => m.Type == "SaleRestoration").StockAfter == 1280, "Restoration increases balance");
    Check(all.Single(m => m.Type == "TransferOut").StockAfter == 980, "Transfer affects source warehouse only");
    Check(all.All(m => m.StockBefore + m.BaseQuantity == m.StockAfter), "Before plus movement equals after");
    Check(all[0].Id == tie2.Id && all[0].StockBefore == 981 && all[0].StockAfter == 979 && all[1].Id == tie1.Id, "Timestamp ties use stable UUID order");
    var q = Query(); q.PageSize = 1;
    var limited = await handler.Handle(q, default);
    Check(limited.Count == 1 && limited[0].StockAfter == 979 && limited[0].StockBefore == 981, "Display limit does not truncate ledger");
    q = Query(); q.Type = "PurchaseEntry"; q.FromUtc = start.AddDays(2); q.ToUtc = start.AddDays(2);
    var filtered = await handler.Handle(q, default);
    Check(filtered.Count == 1 && filtered[0].StockBefore == 300 && filtered[0].StockAfter == 1300, "Date and type filters apply after ledger");
    q = Query(); q.WarehouseId = otherWarehouse.Id;
    var other = await handler.Handle(q, default);
    Check(other.Count == 1 && other[0].StockBefore == 0 && other[0].StockAfter == 10000, "Destination warehouse independent");
    q = Query(); q.ArticleId = otherArticle.Id;
    var negative = await handler.Handle(q, default);
    Check(negative.Count == 1 && negative[0].StockBefore == 0 && negative[0].StockAfter == -3.25m, "Negative balances remain visible");
    q = Query(); q.ArticleId = null; q.WarehouseId = null;
    var mixed = await handler.Handle(q, default);
    Check(mixed.Count == 10 && mixed.Single(m => m.Id == purchase.Id).StockAfter == 1300, "Mixed listing partitions balances correctly");
    Check(await db.StockMovements.IgnoreQueryFilters().CountAsync(x => x.BranchId == branchId || x.BranchId == otherBranchId) == inserted.Length, "Read-only handler leaves ledger unchanged");
}
finally { await transaction.RollbackAsync(); }
Console.WriteLine($"{count} stock movement balance checks passed on PostgreSQL. All fixtures rolled back.");
