using System.Text.Json;
using Grimorio.Application.Features.Billing.Queries;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.Inventory;
using Grimorio.Domain.Entities.Menu;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Domain.Entities.POS;
using Grimorio.Domain.Entities.Purchases;
using Grimorio.Infrastructure.Features.Billing.Queries;
using Grimorio.Infrastructure.Persistence;
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
await using var db = new GrimorioDbContext(new DbContextOptionsBuilder<GrimorioDbContext>().UseNpgsql(target.ConnectionString).Options);
await db.Database.MigrateAsync();
await using var transaction = await db.Database.BeginTransactionAsync();
var checks = 0;
void Check(bool value, string label) { if (!value) throw new Exception(label); checks++; }
var branch = new Branch { Name = "Promotion report checks", Code = Guid.NewGuid().ToString("N")[..10] }; branch.BranchId = branch.Id;
var category = new MenuCategory { BranchId = branch.Id, Name = "Food" };
var invCategory = new InventoryCategory { BranchId = branch.Id, Name = "Ingredients" };
var unit = new MeasurementUnit { BranchId = branch.Id, Name = "Unit", Symbol = "un" };
var warehouse = new Warehouse { BranchId = branch.Id, Name = "Checks" };
var article = new InventoryArticle { BranchId = branch.Id, Name = "Ingredient", CategoryId = invCategory.Id, BaseUnitId = unit.Id };
var tax = new TaxRate { BranchId = branch.Id, Name = "IVA 15", Percentage = 15 };
var wings = new MenuItem { BranchId = branch.Id, Name = "12 alitas", MenuCategoryId = category.Id, Price = 99, TaxRateId = tax.Id };
var ribeye = new MenuItem { BranchId = branch.Id, Name = "Ribeye", MenuCategoryId = category.Id, Price = 99, TaxRateId = tax.Id };
var combo = new MenuItem { BranchId = branch.Id, Name = "Combo jueves", MenuCategoryId = category.Id, Price = 99, IsCombo = true };
var noRecipe = new MenuItem { BranchId = branch.Id, Name = "Sin receta", MenuCategoryId = category.Id };
var station = new WorkStation { BranchId = branch.Id, Name = "Kitchen" };
var tuesday = new Promotion { BranchId = branch.Id, Name = "Renamed catalog promo", Type = PromotionType.FixedPrice, IsActive = false };
var second = new Promotion { BranchId = branch.Id, Name = "Second ribeye", Type = PromotionType.SecondUnitPercentage };
var thursday = new Promotion { BranchId = branch.Id, Name = "Thursday combo", Type = PromotionType.FixedPrice };
var purchase = new Purchase { BranchId = branch.Id, DocumentType = PurchaseDocumentType.NotaDeVenta,
    DocumentDate = DateTime.UtcNow, Status = PurchaseStatus.Registrada, DestinationWarehouseId = warehouse.Id, Total = 20, Subtotal = 20 };
db.AddRange(branch, category, invCategory, unit, warehouse, article, tax, wings, ribeye, combo, noRecipe, station, tuesday, second, thursday, purchase);
foreach (var dish in new[] { wings, ribeye }) db.RecipeIngredients.Add(new RecipeIngredient {
    BranchId = branch.Id, MenuItemId = dish.Id, ArticleId = article.Id, UnitId = unit.Id, Quantity = dish == wings ? 2 : 3,
});
db.PurchaseItems.Add(new PurchaseItem { BranchId = branch.Id, PurchaseId = purchase.Id, ArticleId = article.Id,
    UnitId = unit.Id, InventoryUnitId = unit.Id, InventoryQuantity = 20, Quantity = 20, UnitPrice = 1, TotalPrice = 20 });
await db.SaveChangesAsync();
var date = new DateTime(2026, 11, 12, 17, 0, 0, DateTimeKind.Utc);
var number = 0;
Order NewOrder() { var o = new Order { BranchId = branch.Id, Number = ++number, Type = OrderType.Takeout, Status = OrderStatus.Delivered }; db.Add(o); return o; }
OrderItem Item(Order order, MenuItem menu, int qty, decimal unitPrice, Promotion? promo = null, string? name = null, Guid? group = null) {
    var item = new OrderItem { BranchId = branch.Id, OrderId = order.Id, MenuItemId = menu.Id, Quantity = qty, UnitPrice = unitPrice,
        TaxRateId = menu.TaxRateId, Status = OrderItemStatus.Ready, PromotionId = promo?.Id, PromotionName = name ?? promo?.Name, PromotionGroupId = group };
    order.Items.Add(item); db.Add(item); return item;
}
OrderPayment Pay(Order order, OrderItem item, decimal qty, decimal gross, string? promoName = null, DateTime? paidAt = null) {
    var payment = new OrderPayment { BranchId = branch.Id, OrderId = order.Id, PaidAt = paidAt ?? date, OrderAmount = gross };
    payment.Items.Add(new OrderPaymentItem { BranchId = branch.Id, OrderItemId = item.Id, Quantity = qty, UnitPrice = item.UnitPrice,
        Total = gross, OriginalTotal = item.UnitPrice * qty, TaxRatePercentage = item.TaxRateId == tax.Id ? 15 : 0, PromotionName = promoName });
    db.Add(payment); return payment;
}
var normal = NewOrder(); Pay(normal, Item(normal, wings, 2, 11.5m), 2, 23);
var promoOrder = NewOrder(); var promoItem = Item(promoOrder, wings, 3, 11.5m, tuesday, "Wrong mutable name");
Pay(promoOrder, promoItem, 1, 9.2m, "Martes alitas"); Pay(promoOrder, promoItem, 2, 18.4m, "Martes alitas");
var pairOrder = NewOrder(); var group = Guid.NewGuid();
var pair1 = Item(pairOrder, ribeye, 1, 11.5m, second, group: group);
var pair2 = Item(pairOrder, ribeye, 1, 11.5m, second, group: group);
Pay(pairOrder, pair1, 1, 8.63m, second.Name); Pay(pairOrder, pair2, 1, 8.62m, second.Name);
var odd = NewOrder(); var oddItem = Item(odd, ribeye, 3, 11.5m, second, group: Guid.NewGuid());
Pay(odd, oddItem, 3, 28.75m, second.Name);
var split = NewOrder(); var splitItem = Item(split, ribeye, 2, 11.5m, second, group: Guid.NewGuid());
Pay(split, splitItem, 1, 8.63m, second.Name); Pay(split, splitItem, 1, 8.62m, second.Name, date.AddDays(-1));
var comboOrder = NewOrder(); var comboItem = Item(comboOrder, combo, 2, 25, thursday);
// Frozen recipe includes every component, even after the live catalog changes.
comboItem.PreparationTasks.Add(new OrderItemPreparationTask { BranchId = branch.Id, ComponentItemId = wings.Id,
    StationId = station.Id, QuantityPerCombo = 2, ComponentName = "Wings", RecipeSnapshot = JsonSerializer.Serialize(new[] {
        new { ArticleId = article.Id, ArticleName = article.Name, UnitId = unit.Id, BaseUnitId = unit.Id, Quantity = 2m, BaseQuantity = 2m }
    }) });
comboItem.PreparationTasks.Add(new OrderItemPreparationTask { BranchId = branch.Id, ComponentItemId = ribeye.Id,
    StationId = station.Id, QuantityPerCombo = 1, ComponentName = "Side", RecipeSnapshot = JsonSerializer.Serialize(new[] {
        new { ArticleId = article.Id, ArticleName = article.Name, UnitId = unit.Id, BaseUnitId = unit.Id, Quantity = 1m, BaseQuantity = 1m }
    }) });
Pay(comboOrder, comboItem, 2, 39.98m, thursday.Name);
var manual = NewOrder(); Pay(manual, Item(manual, wings, 1, 11.5m), 1, 10.35m);
var legacy = NewOrder(); legacy.Total = 23; Item(legacy, wings, 2, 11.5m, tuesday, "Martes alitas");
db.Add(new OrderPayment { BranchId = branch.Id, OrderId = legacy.Id, OrderAmount = 11.5m, PaidAt = date });
var cancelled = NewOrder(); var cancelledItem = Item(cancelled, wings, 100, 11.5m); cancelledItem.Status = OrderItemStatus.Cancelled; Pay(cancelled, cancelledItem, 100, 1150);
var missing = NewOrder(); Pay(missing, Item(missing, noRecipe, 1, 1), 1, 1);
var deleted = NewOrder(); Pay(deleted, Item(deleted, wings, 100, 11.5m), 100, 1150).IsDeleted = true;
await db.SaveChangesAsync();
db.ChangeTracker.Clear();
var handler = new GetSalesProfitabilityHandler(db);
var query = new GetSalesProfitabilityQuery { BranchId = branch.Id, FromUtc = date.Date, ToUtc = date.Date.AddDays(1).AddTicks(-1) };
var report = await handler.Handle(query, default);
var wingRows = report.SalesBreakdown.Where(r => r.MenuItemId == wings.Id).ToList();
Check(wingRows.Count == 3, "Same dish separates regular, promotion and unattributed discount");
Check(wingRows.Single(r => r.SaleKind == "Regular").Quantity == 2, "Regular quantity not mixed with promo");
var promo = wingRows.Single(r => r.SaleKind == "Promotion");
Check(promo.Quantity == 4 && promo.NetSales == 34 && promo.TotalFoodCost == 8, "Paid promo quantities and recipe costs, including legacy allocation");
Check(promo.PromotionName == "Martes alitas", "Payment snapshot takes precedence over renamed/inactive catalog");
Check(promo.DiscountAmount == 6.9m && promo.GrossProfit == 26, "Real discount and net profitability");
Check(promo.HasEstimatedQuantities, "Legacy payments explicitly marked estimated");
Check(wingRows.Single(r => r.SaleKind == "Unclassified").Quantity == 1, "Manual or unknown discount is not guessed to be a promotion");
var ribs = report.Promotions.Single(r => r.PromotionId == second.Id);
Check(ribs.Quantity == 6 && ribs.PairEquivalents == 2.5m, "Pairs span lines; odd quantities and partial dated payments prorated correctly");
Check(ribs.TotalOrders == 3 && ribs.TotalFoodCost == 18, "No double counting split payments or paired ingredient costs");
var comboRow = report.Promotions.Single(r => r.PromotionId == thursday.Id);
Check(comboRow.Quantity == 2 && comboRow.TotalFoodCost == 10 && comboRow.GrossProfit == 29.98m, "Two whole combos, with all frozen component recipe costs");
Check(report.SalesBreakdown.Single(r => r.MenuItemId == noRecipe.Id).HasMissingCosts, "Empty recipe does not claim complete cost");
Check(report.SalesBreakdown.Sum(r => r.Quantity) == report.TotalItems, "Detailed quantities reconcile to original summary");
Check(report.SalesBreakdown.Sum(r => r.NetSales) == report.NetSales, "Detailed net sales reconcile to summary");
Check(report.SalesBreakdown.Sum(r => r.TotalFoodCost) == report.FoodCost, "Detailed cost reconciles to summary");
Check(report.Items.Single(r => r.MenuItemId == wings.Id).Quantity == 7, "Original dish report retained without double counting");
Check((await handler.Handle(new GetSalesProfitabilityQuery { BranchId = Guid.NewGuid() }, default)).SalesBreakdown.Count == 0, "Branch isolation");
Check((await handler.Handle(new GetSalesProfitabilityQuery { BranchId = branch.Id, FromUtc = date.AddYears(1) }, default)).Promotions.Count == 0, "Empty date range");
Check(!db.ChangeTracker.HasChanges(), "Reporting makes no writes");
await transaction.RollbackAsync();
Console.WriteLine($"{checks} sales promotion report checks passed; isolated test data rolled back.");
