using System.Text.Json;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Application.Features.Inventory.Commands;
using Grimorio.Application.Features.Inventory.Queries;
using Grimorio.Application.Features.Menu.Queries;
using Grimorio.Application.Features.Menu.Commands;
using Grimorio.Application.Features.POS.Commands;
using Grimorio.Application.Features.TableService.Commands;
using Grimorio.Application.Features.TableService.Queries;
using Grimorio.Domain.Entities.Auth;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.Inventory;
using Grimorio.Domain.Entities.Menu;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Features.POS.Commands;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Grimorio.SharedKernel.Constants;

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
var services = new ServiceCollection();
services.AddLogging();
services.AddDbContext<GrimorioDbContext>(o => o.UseNpgsql(target.ConnectionString));
services.AddMediatR(o => o.RegisterServicesFromAssembly(typeof(CreateOrderCommandHandler).Assembly));
await using var provider = services.BuildServiceProvider();
async Task<T> Send<T>(IRequest<T> request) { await using var scope = provider.CreateAsyncScope(); return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request); }
async Task Read(Func<GrimorioDbContext, Task> action) { await using var scope = provider.CreateAsyncScope(); await action(scope.ServiceProvider.GetRequiredService<GrimorioDbContext>()); }
var checks = 0;
void Check(bool value, string label) { if (!value) throw new Exception(label); checks++; }
async Task Reject<T>(IRequest<T> request, string fragment)
{
    try { await Send(request); }
    catch (InvalidOperationException error) when (error.Message.Contains(fragment)) { checks++; return; }
    throw new Exception($"Expected rejection: {fragment}");
}
var branch = new Branch { Name = "Negative stock checks", Code = Guid.NewGuid().ToString("N")[..10], PublicMenuEnabled = true, PublicOrderingEnabled = true }; branch.BranchId = branch.Id;
var actor = new User { BranchId = branch.Id, FirstName = "Stock", LastName = "Checks", Email = $"{Guid.NewGuid()}@example.invalid", PasswordHash = "checks" };
var category = new InventoryCategory { BranchId = branch.Id, Name = "Ingredients" };
var grams = new MeasurementUnit { BranchId = branch.Id, Name = "Gram", Symbol = "g" };
var kilo = new MeasurementUnit { BranchId = branch.Id, Name = "Kilogram", Symbol = "kg" };
var unit = new MeasurementUnit { BranchId = branch.Id, Name = "Unit", Symbol = "un" };
var warehouse = new Warehouse { BranchId = branch.Id, Name = "Main" };
var station = new WorkStation { BranchId = branch.Id, Name = "Kitchen" };
var menuCategory = new MenuCategory { BranchId = branch.Id, Name = "Food" };
var lettuce = new InventoryArticle { BranchId = branch.Id, Name = "Lettuce", CategoryId = category.Id, BaseUnitId = grams.Id, AllowNegativeStock = true };
var tomato = new InventoryArticle { BranchId = branch.Id, Name = "Tomato", CategoryId = category.Id, BaseUnitId = grams.Id, AllowNegativeStock = true };
var meat = new InventoryArticle { BranchId = branch.Id, Name = "Meat", CategoryId = category.Id, BaseUnitId = unit.Id };
var soda = new InventoryArticle { BranchId = branch.Id, Name = "Soda", CategoryId = category.Id, BaseUnitId = unit.Id, Type = ArticleType.FinishedProduct };
var salad = new MenuItem { BranchId = branch.Id, Name = "Salad", GeneralName = "Salad", InternalCode = "NS-SALAD", MenuCategoryId = menuCategory.Id, StationId = station.Id, Price = 5 };
var dish = new MenuItem { BranchId = branch.Id, Name = "Meat with salad", GeneralName = "Meat with salad", InternalCode = "NS-MEAT", MenuCategoryId = menuCategory.Id, StationId = station.Id, Price = 10 };
var drink = new MenuItem { BranchId = branch.Id, Name = "Soda", GeneralName = "Soda", InternalCode = "NS-SODA", MenuCategoryId = menuCategory.Id, StationId = station.Id, Price = 2 };
var modifierGroup = new MenuItemModifierGroup { BranchId = branch.Id, MenuItemId = dish.Id, Name = "Extra", MinSelections = 0, MaxSelections = 2, IsRequired = false };
var tomatoOption = new MenuItemModifierOption { BranchId = branch.Id, ModifierGroupId = modifierGroup.Id, Name = "Tomato", ArticleId = tomato.Id, UnitId = grams.Id, Quantity = 10 };
var sodaOption = new MenuItemModifierOption { BranchId = branch.Id, ModifierGroupId = modifierGroup.Id, Name = "Soda", ArticleId = soda.Id, UnitId = unit.Id, Quantity = 1 };
var register = new CashRegister { BranchId = branch.Id, Name = "Checks", Code = "CHECKS" };
var session = new CashSession { BranchId = branch.Id, CashRegisterId = register.Id, OpenedBy = actor.Id, Status = CashSessionStatus.Open };
var cash = new PaymentMethodConfig { Name = $"Cash {Guid.NewGuid():N}", IsCash = true };
var table = new RestaurantTable { BranchId = branch.Id, Code = "Checks" };
await Read(async db => {
    await db.Database.MigrateAsync();
    Check(!db.Database.HasPendingModelChanges(), "Migration matches model");
    db.AddRange(branch, actor, category, grams, kilo, unit, warehouse, station, menuCategory, lettuce, tomato, meat, soda, salad, dish, drink, modifierGroup, tomatoOption, sodaOption, register, session, cash, table);
    db.UnitConversions.Add(new UnitConversion { BranchId = branch.Id, OriginUnitId = kilo.Id, DestinationUnitId = grams.Id, Factor = 1000 });
    db.RecipeIngredients.AddRange(
        new() { BranchId = branch.Id, MenuItemId = salad.Id, ArticleId = lettuce.Id, UnitId = kilo.Id, Quantity = .1m },
        new() { BranchId = branch.Id, MenuItemId = salad.Id, ArticleId = tomato.Id, UnitId = grams.Id, Quantity = 20 },
        new() { BranchId = branch.Id, MenuItemId = dish.Id, ArticleId = lettuce.Id, UnitId = grams.Id, Quantity = 50 },
        new() { BranchId = branch.Id, MenuItemId = dish.Id, ArticleId = meat.Id, UnitId = unit.Id, Quantity = 1 },
        new() { BranchId = branch.Id, MenuItemId = drink.Id, ArticleId = soda.Id, UnitId = unit.Id, Quantity = 1 });
    await db.SaveChangesAsync();
});
async Task Seed(InventoryArticle article, decimal amount) => await Send(new RegisterMovementCommand { BranchId = branch.Id, ArticleId = article.Id, WarehouseId = warehouse.Id, UnitId = article.BaseUnitId, Type = MovementType.InitialInventory, Quantity = amount, UnitCost = 1 });
await Seed(lettuce, 30); await Seed(meat, 3);
var defaultArticle = await Send(new CreateInventoryArticleCommand { BranchId = branch.Id, Name = "Default strict", CategoryId = category.Id, BaseUnitId = unit.Id });
Check(!defaultArticle.AllowNegativeStock, "New articles default to strict");
var update = new UpdateInventoryArticleCommand { BranchId = branch.Id, Id = lettuce.Id, Name = lettuce.Name, CategoryId = category.Id, BaseUnitId = grams.Id, IsActive = true, StockAlertActive = true };
Check((await Send(update)).AllowNegativeStock, "Old clients omitting policy do not reset it");
async Task<List<MenuItemAvailabilityDto>> Availability() => await Send(new GetMenuAvailabilityQuery { BranchId = branch.Id, ActiveOnly = true, AvailableOnly = false });
var availability = await Availability();
Check(availability.Single(a => a.MenuItemId == salad.Id) is { IsAvailable: true, AvailableQuantity: null }, "All approximate ingredients have no artificial limit");
Check(availability.Single(a => a.MenuItemId == dish.Id).AvailableQuantity == 3, "Strict meat remains the limiting ingredient");
Check(!availability.Single(a => a.MenuItemId == drink.Id).IsAvailable, "Missing soda remains unavailable");
var detail = await Send(new GetMenuItemDetailQuery { BranchId = branch.Id, Id = dish.Id });
Check(detail!.ModifierGroups.Single().Options.Single(o => o.Id == tomatoOption.Id) is { IsAvailable: true, IsTracked: false, AvailableQuantity: null }, "Approximate modifier available even without warehouse row");
Check(!detail.ModifierGroups.Single().Options.Single(o => o.Id == sodaOption.Id).IsAvailable, "Strict modifier remains unavailable");
var publicMenu = await Send(new GetPublicTableMenuQuery { TableToken = table.PublicToken });
Check(publicMenu.Items.Single(i => i.Id == salad.Id).IsAvailable && !publicMenu.Items.Single(i => i.Id == drink.Id).IsAvailable, "QR follows same stock policy");
CreateOrderCommand Order(Guid menuId, int quantity = 1) => new() { BranchId = branch.Id, Type = "Takeout", Items = [new() { MenuItemId = menuId, Quantity = quantity }] };
var order = await Send(Order(salad.Id, 2));
await Send(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = order.Id });
var reconciliation = await Send(new GetInventoryReconciliationQuery { BranchId = branch.Id });
Check(!reconciliation.Findings.Any(f => f.Code == "OverReserved" && (f.ArticleId == lettuce.Id || f.ArticleId == tomato.Id)), "Authorized excess reservations do not raise false reconciliation errors");
await Read(async db => {
    Check(await db.StockReservations.Where(r => r.OrderId == order.Id && r.ArticleId == lettuce.Id).SumAsync(r => r.BaseQuantity) == 200, "Full recipe quantity reserved including deficit and conversion");
    Check(await db.StockReservations.Where(r => r.OrderId == order.Id && r.ArticleId == tomato.Id && r.WarehouseId == warehouse.Id).SumAsync(r => r.BaseQuantity) == 40, "Missing stock row uses a real active warehouse");
    Check(await db.StockMovements.Where(m => m.ArticleId == lettuce.Id).SumAsync(m => m.BaseQuantity) == 30, "Confirmation reserves but does not deduct early");
});
PayOrderCommand Pay(int quantity) => new() { BranchId = branch.Id, OrderId = order.Id, UserId = actor.Id, CashSessionId = session.Id, IdempotencyKey = Guid.NewGuid(), OrderAmount = quantity * 5,
    Items = [new() { OrderItemId = order.Items.Single().Id, Quantity = quantity }], Lines = [new() { MethodId = cash.Id, AmountTendered = quantity * 5 }] };
var payment = Pay(1);
await Send(payment);
await Send(payment);
await Read(async db => {
    Check(await db.StockMovements.Where(m => m.ArticleId == lettuce.Id).SumAsync(m => m.BaseQuantity) == -70, "Partial payment deducts and records negative ledger");
    Check(await db.WarehouseStock.Where(s => s.ArticleId == lettuce.Id).SumAsync(s => s.Quantity) == -70, "Negative stock projection remains real");
    Check(await db.StockMovements.Where(m => m.ArticleId == tomato.Id).SumAsync(m => m.BaseQuantity) == -20, "Previously absent warehouse stock is created negative");
    Check(await db.StockReservations.Where(r => r.OrderId == order.Id && r.ArticleId == lettuce.Id && r.Status == StockReservationStatus.Active).SumAsync(r => r.BaseQuantity) == 100, "Partial payment leaves exact remaining reservation");
});
await Send(Pay(1));
await Read(async db => Check(await db.StockMovements.Where(m => m.ArticleId == lettuce.Id).SumAsync(m => m.BaseQuantity) == -170, "Full payment consumes complete recipe once"));
var cancel = await Send(Order(salad.Id));
await Send(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = cancel.Id });
await Send(new CancelOrderCommand { BranchId = branch.Id, OrderId = cancel.Id });
await Read(async db => Check(!await db.StockReservations.AnyAsync(r => r.OrderId == cancel.Id && r.Status == StockReservationStatus.Active), "Cancellation releases negative-capable reservations"));
var strict = await Send(Order(drink.Id));
await Reject(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = strict.Id }, "Stock insuficiente");
var tooMany = await Send(Order(dish.Id, 4));
await Reject(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = tooMany.Id }, "Stock insuficiente");
await Read(async db => Check(!await db.StockReservations.AnyAsync(r => r.OrderId == tooMany.Id), "Mixed-order failure rolls back all reservations"));
await Reject(new PublicCreateDraftOrderCommand { TableToken = table.PublicToken, IdempotencyKey = Guid.NewGuid(), Items = [new() { MenuItemId = drink.Id, Quantity = 1, ExpectedUnitPrice = 2 }] }, "disponible");
var qr = await Send(new PublicCreateDraftOrderCommand { TableToken = table.PublicToken, IdempotencyKey = Guid.NewGuid(), Items = [new() { MenuItemId = salad.Id, Quantity = 3, ExpectedUnitPrice = 5 }] });
Check(qr.Order.Items.Single().Quantity == 3, "QR accepts multiple servings with negative ingredients");
await Reject(new PublicCreateDraftOrderCommand { TableToken = new RestaurantTable().PublicToken, IdempotencyKey = Guid.NewGuid(), Items = [] }, "Mesa");
var combo = await Send(new CreateMenuItemCommand { BranchId = branch.Id, MenuCategoryId = menuCategory.Id, Name = "Salad combo", GeneralName = "Salad combo", Price = 12,
    IsCombo = true, ComboComponents = [new() { ComponentItemId = salad.Id, Quantity = 2 }] });
Check((await Availability()).Single(a => a.MenuItemId == combo.Id).IsAvailable, "Combo expansion respects approximate ingredients");
var comboOrder = await Send(Order(combo.Id));
await Send(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = comboOrder.Id });
await Send(new PayOrderCommand { BranchId = branch.Id, OrderId = comboOrder.Id, UserId = actor.Id, CashSessionId = session.Id,
    IdempotencyKey = Guid.NewGuid(), OrderAmount = 12, Lines = [new() { MethodId = cash.Id, AmountTendered = 12 }] });
await Read(async db => Check(await db.StockMovements.Where(m => m.ArticleId == lettuce.Id).SumAsync(m => m.BaseQuantity) == -370, "Combo payment deducts all component consumption, including deficits"));
var modifierRequest = Order(dish.Id);
modifierRequest.Items[0].ModifierSelections = [new() { ModifierOptionId = tomatoOption.Id, Quantity = 1 }];
var modifierOrder = await Send(modifierRequest);
await Send(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = modifierOrder.Id });
await Send(new PayOrderCommand { BranchId = branch.Id, OrderId = modifierOrder.Id, UserId = actor.Id, CashSessionId = session.Id,
    IdempotencyKey = Guid.NewGuid(), OrderAmount = 10, Lines = [new() { MethodId = cash.Id, AmountTendered = 10 }] });
await Read(async db => {
    Check(await db.StockMovements.Where(m => m.ArticleId == tomato.Id).SumAsync(m => m.BaseQuantity) == -90, "Modifiers without finite UI limits still deduct full physical consumption");
    Check(await db.WarehouseStock.Where(s => s.ArticleId == meat.Id).SumAsync(s => s.Quantity) == 2, "Strict ingredients are still deducted alongside approximate ones");
});
var role = new Role { BranchId = branch.Id, Name = AppConstants.Roles.Admin };
await Read(async db => {
    db.Roles.Add(role);
    db.UserRoles.Add(new UserRole { BranchId = branch.Id, UserId = actor.Id, RoleId = role.Id });
    await db.SaveChangesAsync();
});
var wasteOrder = await Send(Order(salad.Id));
await Send(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = wasteOrder.Id });
var correctionRequest = new CorrectOrderItemCommand(branch.Id, wasteOrder.Items.Single().Id, actor.Id,
    new() { RequestId = Guid.NewGuid(), Disposition = "PreparedWaste", Reason = "Prepared salad discarded during isolated check" });
await Send(correctionRequest);
await Send(correctionRequest);
await Read(async db => {
    Check(await db.StockMovements.Where(m => m.OrderItemId == wasteOrder.Items.Single().Id && m.ArticleId == lettuce.Id && m.Type == MovementType.Waste).SumAsync(m => m.BaseQuantity) == -100,
        "Prepared waste keeps full negative-capable consumption and retry does not duplicate it");
    Check(!await db.StockReservations.AnyAsync(r => r.OrderId == wasteOrder.Id && r.Status == StockReservationStatus.Active), "Prepared waste consumes its reservations");
});
await Seed(soda, 1);
var oneDrink = await Send(Order(drink.Id));
var anotherDrink = await Send(Order(drink.Id));
async Task<bool> TryConfirm(Guid id) {
    try { await Send(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = id }); return true; }
    catch (InvalidOperationException e) when (e.Message.Contains("Stock insuficiente")) { return false; }
}
var races = await Task.WhenAll(TryConfirm(oneDrink.Id), TryConfirm(anotherDrink.Id));
Check(races.Count(success => success) == 1, "Concurrent strict reservations cannot oversell soda");
await Send(new CancelOrderCommand { BranchId = branch.Id, OrderId = races[0] ? oneDrink.Id : anotherDrink.Id });
var packet = new MeasurementUnit { BranchId = branch.Id, Name = "Packet", Symbol = "pkt" };
var invalid = new MenuItem { BranchId = branch.Id, MenuCategoryId = menuCategory.Id, StationId = station.Id, Name = "Invalid conversion", GeneralName = "Invalid conversion", Price = 1 };
await Read(async db => {
    db.AddRange(packet, invalid);
    db.RecipeIngredients.Add(new() { BranchId = branch.Id, MenuItemId = invalid.Id, ArticleId = lettuce.Id, UnitId = packet.Id, Quantity = 1 });
    await db.SaveChangesAsync();
});
Check(!(await Availability()).Single(a => a.MenuItemId == invalid.Id).IsAvailable, "Negative stock cannot bypass missing unit conversions");
var invalidOrder = await Send(Order(invalid.Id));
await Reject(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = invalidOrder.Id }, "cantidad y unidad");
await Read(async db => { (await db.Warehouses.SingleAsync(w => w.Id == warehouse.Id)).IsActive = false; await db.SaveChangesAsync(); });
var noWarehouse = await Send(Order(salad.Id));
await Reject(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = noWarehouse.Id }, "bodega activa");
await Read(async db => { (await db.Warehouses.SingleAsync(w => w.Id == warehouse.Id)).IsActive = true; await db.SaveChangesAsync(); });
update.AllowNegativeStock = false;
await Send(update);
Check(!(await Availability()).Single(a => a.MenuItemId == salad.Id).IsAvailable, "Turning policy off restores strict availability");
var off = await Send(Order(salad.Id));
await Reject(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = off.Id }, "Stock insuficiente");
update.AllowNegativeStock = true; update.IsActive = false; await Send(update);
Check(!(await Availability()).Single(a => a.MenuItemId == salad.Id).IsAvailable, "Allow-negative never overrides inactive article");
update.IsActive = true; await Send(update);
await Read(async db => { (await db.MenuItems.SingleAsync(m => m.Id == salad.Id)).AvailableForSale = false; await db.SaveChangesAsync(); });
Check(!(await Availability()).Single(a => a.MenuItemId == salad.Id).IsAvailable, "Manually unavailable dish stays unavailable");
await Reject(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = off.Id }, "platos ya no esta disponible");
var alert = await Send(new GetStockAlertsQuery { BranchId = branch.Id });
Check(alert.Any(a => a.ArticleId == lettuce.Id), "Negative ingredients remain in low-stock alerts");
Console.WriteLine($"{checks} negative-stock checks passed. Real isolated PostgreSQL; synthetic fixtures remain under branch {branch.Id}. No SRI, email or production calls.");
