using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Menu.Commands;
using Grimorio.Application.Features.Menu.Queries;
using Grimorio.Application.Features.POS.Commands;
using Grimorio.Application.Features.POS.Queries;
using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Application.Features.Billing.Queries;
using Grimorio.Application.Features.TableService.Commands;
using Grimorio.Domain.Entities.Auth;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Domain.Entities.Menu;
using Grimorio.Domain.Entities.POS;
using Grimorio.Domain.Entities.Inventory;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.Purchases;
using Grimorio.Infrastructure.Features.POS.Commands;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

var connection = Environment.GetEnvironmentVariable("COMBO_CHECKS_CONNECTION") ?? throw new Exception("Set COMBO_CHECKS_CONNECTION.");
var target = new NpgsqlConnectionStringBuilder(connection);
if (target.Host != "127.0.0.1" || target.Port != 15432 || target.Database != "checks" || target.Username != "checks")
    throw new Exception("Only the dedicated localhost:15432 checks database is allowed.");
var services = new ServiceCollection();
services.AddLogging();
services.AddDbContext<GrimorioDbContext>(options => options.UseNpgsql(connection));
services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(CreateOrderCommandHandler).Assembly));
await using var provider = services.BuildServiceProvider();
var count = 0;
void Check(bool value, string message) { if (!value) throw new Exception(message); count++; }
async Task<T> Send<T>(IRequest<T> request) { await using var scope = provider.CreateAsyncScope(); return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request); }
async Task Reject<T>(IRequest<T> request, string expected)
{
    try { await Send(request); }
    catch (InvalidOperationException ex) when (ex.Message.Contains(expected)) { count++; return; }
    throw new Exception($"Expected rejection: {expected}");
}
async Task Read(Func<GrimorioDbContext, Task> action) { await using var scope = provider.CreateAsyncScope(); await action(scope.ServiceProvider.GetRequiredService<GrimorioDbContext>()); }
var branch = new Branch { Name = "Combo checks", Code = "COMBO", PublicMenuEnabled = true, PublicOrderingEnabled = true }; branch.BranchId = branch.Id;
var foreignBranch = new Branch { Name = "Other checks", Code = "OTHER" }; foreignBranch.BranchId = foreignBranch.Id;
var user = new User { BranchId = branch.Id, FirstName = "Checks", LastName = "Cashier", Email = "combo@example.test", PasswordHash = "checks" };
var category = new MenuCategory { BranchId = branch.Id, Name = "Combos" };
var table = new RestaurantTable { BranchId = branch.Id, Code = "Checks 7" };
var foreignDish = new MenuItem { BranchId = foreignBranch.Id, Name = "Foreign dish", GeneralName = "Foreign dish", MenuCategoryId = category.Id };
var inventoryCategory = new InventoryCategory { BranchId = branch.Id, Name = "Ingredients" };
var unit = new MeasurementUnit { BranchId = branch.Id, Name = "Unidad", Symbol = "un" };
var warehouse = new Warehouse { BranchId = branch.Id, Name = "Checks" };
var register = new CashRegister { BranchId = branch.Id, Name = "Checks", Code = "CHECKS" };
var session = new CashSession { BranchId = branch.Id, CashRegisterId = register.Id, OpenedBy = user.Id, Status = CashSessionStatus.Open };
var cash = new PaymentMethodConfig { Name = "Checks cash", IsCash = true };
var purchase = new Purchase { BranchId = branch.Id, DocumentType = PurchaseDocumentType.NotaDeVenta, DocumentNumber = "CHECKS",
    DocumentDate = DateTime.UtcNow, Status = PurchaseStatus.Registrada, DestinationWarehouseId = warehouse.Id, Total = 80, Subtotal = 80 };
var stations = new[] { "Fritos", "Parrilla", "Bar" }.Select(name => new WorkStation { BranchId = branch.Id, Name = name }).ToArray();
var articles = new[] { "Alitas", "Carne", "Limon", "Sal compartida" }.Select(name => new InventoryArticle
{ BranchId = branch.Id, Name = name, CategoryId = inventoryCategory.Id, BaseUnitId = unit.Id, Type = ArticleType.Ingredient }).ToArray();
var dishes = new[] { "Alitas fijas", "Hamburguesa fija", "Limonada" }.Select((name, index) => new MenuItem
{ BranchId = branch.Id, Name = name, GeneralName = name, MenuCategoryId = category.Id, StationId = stations[index].Id, Price = 4,
    InternalCode = $"M-00000{index}", SaleVisibility = MenuItemSaleVisibility.ComponentsOnly }).ToArray();
await Read(async db =>
{
    if (Environment.GetEnvironmentVariable("COMBO_CHECKS_ALLOW_RESET") == "true")
        await db.Database.EnsureDeletedAsync();
    await db.Database.MigrateAsync();
    Check(!db.Database.HasPendingModelChanges(), "Migration matches model");
    db.AddRange(branch, foreignBranch, foreignDish, table, user, category, inventoryCategory, unit, warehouse, register, session, cash, purchase);
    db.AddRange(stations); db.AddRange(articles); db.AddRange(dishes);
    for (var i = 0; i < 3; i++)
        db.RecipeIngredients.Add(new RecipeIngredient { BranchId = branch.Id, MenuItemId = dishes[i].Id, ArticleId = articles[i].Id, UnitId = unit.Id, Quantity = 1 });
    for (var i = 0; i < 2; i++)
        db.RecipeIngredients.Add(new RecipeIngredient { BranchId = branch.Id, MenuItemId = dishes[i].Id, ArticleId = articles[3].Id, UnitId = unit.Id, Quantity = 1 });
    foreach (var article in articles)
    {
        var purchaseItem = new PurchaseItem { BranchId = branch.Id, PurchaseId = purchase.Id, ArticleId = article.Id, UnitId = unit.Id,
            InventoryUnitId = unit.Id, InventoryQuantity = 20, Quantity = 20, UnitPrice = 1, TotalPrice = 20 };
        db.PurchaseItems.Add(purchaseItem);
        db.WarehouseStock.Add(new WarehouseStock { BranchId = branch.Id, ArticleId = article.Id, WarehouseId = warehouse.Id, Quantity = 20 });
        db.StockMovements.Add(new StockMovement { BranchId = branch.Id, ArticleId = article.Id, WarehouseId = warehouse.Id, Type = MovementType.PurchaseEntry, PurchaseItemId = purchaseItem.Id,
            UnitId = unit.Id, Quantity = 20, BaseQuantity = 20, UnitCost = 1, TotalCost = 20 });
    }
    await db.SaveChangesAsync();
});
CreateMenuItemCommand Definition(List<UpsertMenuComboComponentDto>? components = null) => new()
{ BranchId = branch.Id, MenuCategoryId = category.Id, Name = "Combo dragon", GeneralName = "Combo alitas hamburguesa limonada", Price = 15,
    IsCombo = true, ComboComponents = components ?? dishes.Select(d => new UpsertMenuComboComponentDto { ComponentItemId = d.Id, Quantity = 1 }).ToList() };
var combo = await Send(Definition());
Check(combo.IsCombo && combo.ComboComponents.Count == 3 && combo.StationId is null, "Combo definition");
await Reject(Definition([]), "Selecciona entre");
await Reject(Definition([new() { ComponentItemId = combo.Id, Quantity = 1 }]), "Cada componente");
await Reject(Definition([new() { ComponentItemId = dishes[0].Id, Quantity = 0 }]), "Selecciona entre");
await Reject(Definition([new() { ComponentItemId = dishes[0].Id }, new() { ComponentItemId = dishes[0].Id }]), "Selecciona entre");
await Reject(Definition([new() { ComponentItemId = Guid.NewGuid() }]), "Cada componente");
await Reject(Definition([new() { ComponentItemId = foreignDish.Id }]), "Cada componente");
var lightweight = await Send(new GetMenuItemsQuery { BranchId = branch.Id, Lightweight = true });
Check(lightweight.Single(x => x.Id == combo.Id).ComboComponents.Count == 3, "Lightweight catalog preserves combo metadata");
var menu = await Send(new GetMenuItemsQuery { BranchId = branch.Id, ActiveOnly = true, AvailableOnly = true });
Check(menu.All(x => x.Id != dishes[0].Id) && menu.Any(x => x.Id == combo.Id), "Components hidden from ordinary catalog");
var availability = (await Send(new GetMenuAvailabilityQuery { BranchId = branch.Id })).Single(x => x.MenuItemId == combo.Id);
Check(availability.AvailableQuantity == 10, "Shared ingredient sums both components");
var costs = (await Send(new GetMenuProfitabilityQuery { BranchId = branch.Id })).Single(x => x.MenuItemId == combo.Id);
Check(costs.HasRecipe && costs.RecipeCost == 5, "Combo cost sums ingredients once");
await Reject(new UpsertRecipeCommand { BranchId = branch.Id, MenuItemId = combo.Id }, "receta propia");
await Reject(new DeleteMenuItemCommand { BranchId = branch.Id, Id = dishes[0].Id }, "forma parte");
CreateOrderCommand Order(int quantity, Guid? itemId = null) => new()
{ BranchId = branch.Id, Type = "Takeout", Items = [new() { MenuItemId = itemId ?? combo.Id, Quantity = quantity, IsTakeout = true, Notes = "Para llevar" }] };
await Reject(Order(1, dishes[0].Id), "dentro de un combo");
var draft = await Send(Order(2));
Check(draft.Items.Count == 1 && draft.Total == 30 && draft.Items[0].PreparationTasks.Count == 3, "One priced order line, three tasks");
Check((await Send(new GetItemsByStationQuery { BranchId = branch.Id, StationId = stations[0].Id })).Count == 0, "Draft not visible in kitchen");
var confirmed = await Send(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = draft.Id });
var parent = confirmed.Items.Single();
await Reject(new DeleteMenuItemCommand { BranchId = branch.Id, Id = combo.Id }, "ya tiene pedidos");
await Reject(new DeliverOrderCommand { BranchId = branch.Id, OrderId = draft.Id }, "componentes del combo");
await Read(async db => Check(await db.StockReservations.Where(x => x.OrderId == draft.Id && x.Status == StockReservationStatus.Active).SumAsync(x => x.BaseQuantity) == 10, "Reserve 2 combos, sum shared salt"));
for (var i = 0; i < 3; i++)
{
    var stationItems = await Send(new GetItemsByStationQuery { BranchId = branch.Id, StationId = stations[i].Id });
    Check(stationItems.Count == 1 && stationItems[0].Quantity == 2 && stationItems[0].ItemName.Contains("Combo dragon"), "Station gets only its component and combo reference");
}
await Reject(new SetOrderItemStatusCommand { BranchId = branch.Id, OrderItemId = parent.Id, Status = "Ready" }, "cada componente");
var firstTask = parent.PreparationTasks[0];
await Send(new SetOrderItemStatusCommand { BranchId = branch.Id, OrderItemId = firstTask.Id, Status = "Ready" });
await Send(new SetOrderItemStatusCommand { BranchId = branch.Id, OrderItemId = firstTask.Id, Status = "Ready" });
var detail = (await Send(new GetOrderDetailQuery { BranchId = branch.Id, OrderId = draft.Id }))!;
Check(detail.Status == "InPreparation" && detail.Items.Single().Status == "InPreparation", "Partial ready does not mark combo ready");
await Reject(new SetOrderItemStatusCommand { BranchId = branch.Id, OrderItemId = firstTask.Id, Status = "InPreparation" }, "retroceder");
await Reject(new CancelOrderItemCommand { BranchId = branch.Id, OrderItemId = parent.Id }, "empezo");
await Task.WhenAll(parent.PreparationTasks.Skip(1).Select(task => Send(new SetOrderItemStatusCommand { BranchId = branch.Id, OrderItemId = task.Id, Status = "Ready" })));
detail = (await Send(new GetOrderDetailQuery { BranchId = branch.Id, OrderId = draft.Id }))!;
Check(detail.Status == "Ready" && detail.Items.Single().Status == "Ready", "All tasks ready");
var completed = await Send(new GetCompletedStationItemsQuery { BranchId = branch.Id, StationId = firstTask.StationId!.Value,
    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5)) });
Check(completed.Any(x => x.OrderItemId == firstTask.Id), "Completed component visible");
Check((await Send(new DeliverOrderCommand { BranchId = branch.Id, OrderId = draft.Id })).Status == "Delivered", "Completed combo can be delivered");
// Editing the catalog does not rewrite preparation or ingredient snapshots on existing orders.
await Send(new UpdateMenuItemCommand { BranchId = branch.Id, Id = combo.Id, MenuCategoryId = category.Id, Name = "Combo nuevo", GeneralName = combo.GeneralName,
    Price = 15, IsActive = true, AvailableForSale = true, IsCombo = true,
    ComboComponents = [new() { ComponentItemId = dishes[2].Id, Quantity = 2 }] });
await Send(new UpsertRecipeCommand { BranchId = branch.Id, MenuItemId = dishes[0].Id,
    Ingredients = [new() { ArticleId = articles[0].Id, UnitId = unit.Id, Quantity = 7 }] });
PayOrderCommand Payment(decimal quantity, Guid? key = null) => new()
{ BranchId = branch.Id, OrderId = draft.Id, UserId = user.Id, CashSessionId = session.Id, IdempotencyKey = key ?? Guid.NewGuid(), OrderAmount = quantity * 15,
    Lines = [new() { MethodId = cash.Id, AmountTendered = quantity * 15 }], Items = [new() { OrderItemId = parent.Id, Quantity = quantity }] };
var pay1 = Payment(1);
var receipt1 = await Send(pay1);
var replay = await Send(pay1);
Check(receipt1.Id == replay.Id, "Payment retry idempotent");
await Read(async db =>
{
    Check(await db.StockMovements.CountAsync(x => x.Type == MovementType.SaleDeduction) == 4, "Four grouped deductions, not duplicated on retry");
    Check(await db.OrderPaymentItems.CountAsync(x => x.OrderPaymentId == receipt1.Id) == 1, "Single combo invoice line");
    Check((await db.WarehouseStock.SingleAsync(x => x.ArticleId == articles[0].Id)).Quantity == 19, "Original wings recipe deducted, not edited recipe");
    Check((await db.WarehouseStock.SingleAsync(x => x.ArticleId == articles[3].Id)).Quantity == 18, "Shared salt deducted twice per combo");
});
await Send(Payment(1));
await Read(async db => Check(await db.StockReservations.AnyAsync(x => x.OrderId == draft.Id && x.Status == StockReservationStatus.Active) == false, "Second partial payment consumes remaining reservations"));
var report = await Send(new GetSalesProfitabilityQuery { BranchId = branch.Id });
Check(report.Items.Count == 1 && report.Items.Single().Quantity == 2 && report.FoodCost == 10, "Profitability retains snapshot and no child sales");
// Cancellation, idempotent append, Alexa and rollback on insufficient stock.
var cancelDraft = await Send(Order(1));
await Send(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = cancelDraft.Id });
await Send(new CancelOrderItemCommand { BranchId = branch.Id, OrderItemId = cancelDraft.Items.Single().Id });
await Read(async db => Check(!await db.StockReservations.AnyAsync(x => x.OrderId == cancelDraft.Id && x.Status == StockReservationStatus.Active), "Cancellation releases combo reservations"));
var voiceOrder = await Send(Order(1));
await Send(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = voiceOrder.Id });
var voice = await Send(new ProcessAlexaKitchenCommand { BranchId = branch.Id, OrderNumber = voiceOrder.Number, StationNames = ["Bar"], AllItems = true, Action = "listo" });
Check(voice.Success && voice.UpdatedCount == 1, "Alexa updates task scoped to station");
var voiceDetail = (await Send(new GetOrderDetailQuery { BranchId = branch.Id, OrderId = voiceOrder.Id }))!;
Check(voiceDetail.Status == "Ready", "Single-component combo ready through Alexa");
var failedPayment = new PayOrderCommand { BranchId = branch.Id, OrderId = voiceOrder.Id, UserId = user.Id, CashSessionId = session.Id,
    IdempotencyKey = Guid.NewGuid(), OrderAmount = 15, Lines = [new() { MethodId = cash.Id, AmountTendered = 15 }],
    Items = [new() { OrderItemId = voiceDetail.Items.Single().Id, Quantity = 1 }] };
decimal beforeStock = 0;
await Read(async db =>
{
    beforeStock = (await db.WarehouseStock.SingleAsync(x => x.ArticleId == articles[2].Id)).Quantity;
    await db.Database.ExecuteSqlRawAsync("ALTER TABLE inv.\"StockMovements\" ADD CONSTRAINT \"CK_ComboChecks_ForcedFailure\" CHECK (\"Type\" <> 'SaleDeduction') NOT VALID");
});
try
{
    try { await Send(failedPayment); throw new Exception("Expected forced movement failure"); }
    catch (DbUpdateException) { count++; }
    await Read(async db =>
    {
        Check(!await db.OrderPayments.AnyAsync(x => x.OrderId == voiceOrder.Id), "Payment rolls back after movement failure");
        Check((await db.WarehouseStock.SingleAsync(x => x.ArticleId == articles[2].Id)).Quantity == beforeStock, "Stock unchanged after payment failure");
        Check(await db.StockReservations.AnyAsync(x => x.OrderId == voiceOrder.Id && x.Status == StockReservationStatus.Active), "Reservations remain active after failed payment");
    });
}
finally { await Read(async db => { await db.Database.ExecuteSqlRawAsync("ALTER TABLE inv.\"StockMovements\" DROP CONSTRAINT \"CK_ComboChecks_ForcedFailure\""); }); }
var large = await Send(Order(100));
await Reject(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = large.Id }, "Stock insuficiente");
await Read(async db => Check((await db.Orders.SingleAsync(x => x.Id == large.Id)).Status == OrderStatus.Draft
    && !await db.StockReservations.AnyAsync(x => x.OrderId == large.Id), "Insufficient stock rolls back confirmation and reservations"));
var append = await Send(Order(1));
await Send(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = append.Id });
var appendRequest = new UpdateOrderItemsCommand { BranchId = branch.Id, OrderId = append.Id, IdempotencyKey = Guid.NewGuid(), ExpectedIsDraft = false,
    Items = [new() { MenuItemId = combo.Id, Quantity = 1 }] };
await Send(appendRequest); await Send(appendRequest);
var appended = (await Send(new GetOrderDetailQuery { BranchId = branch.Id, OrderId = append.Id }))!;
Check(appended.Items.Count == 2 && appended.Items.Sum(x => x.PreparationTasks.Count) == 2, "Appending retry does not duplicate tasks");
var direct = await Send(new CreateDirectSaleCommand { BranchId = branch.Id, Items = [new() { MenuItemId = combo.Id, Quantity = 1 }] });
Check(direct.Status == "Confirmed" && direct.Items.Count == 1 && direct.Items.Single().PreparationTasks.Count == 1, "Direct sale creates tasks and keeps one priced line");
var qrRequest = new PublicCreateDraftOrderCommand { TableToken = table.PublicToken, IdempotencyKey = Guid.NewGuid(),
    Items = [new() { MenuItemId = combo.Id, Quantity = 1, ExpectedUnitPrice = 15 }] };
var qr = await Send(qrRequest);
var qrRetry = await Send(qrRequest);
Check(qr.Order.Id == qrRetry.Order.Id && qr.Order.Items.Single().PreparationTasks.Count == 1, "QR retries preserve one combo draft and tasks");
await Send(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = qr.Order.Id });
var repeat = await Send(new GetAlexaOrderRepeatQuery { BranchId = branch.Id, OrderNumber = qr.Order.Number, StationNames = ["Bar"] });
Check(repeat.Success && repeat.Items.Count == 1 && repeat.Items.Single().Quantity == 2, "Alexa repeats only station component");
await Reject(new UpdateMenuItemCommand { BranchId = branch.Id, Id = combo.Id, MenuCategoryId = category.Id,
    Name = "Combo nuevo", GeneralName = combo.GeneralName, Price = 15, IsActive = true, AvailableForSale = true,
    IsCombo = false, ComboComponents = [] }, "ya tiene pedidos");
await Send(new UpdateMenuItemCommand { BranchId = branch.Id, Id = combo.Id, MenuCategoryId = category.Id,
    Name = "Combo nuevo", GeneralName = combo.GeneralName, Price = 15, IsActive = true, AvailableForSale = true,
    IsCombo = true, ComboComponents = Definition().ComboComponents });
var readded = (await Send(new GetMenuItemDetailQuery { BranchId = branch.Id, Id = combo.Id }))!;
Check(readded.ComboComponents.Count == 3, "Removed components can be re-added without unique conflicts");
var imageUpdated = await Send(new UpdateMenuItemImageCommand { BranchId = branch.Id, Id = combo.Id, ImageUrl = "/uploads/checks.png" });
Check(imageUpdated.IsCombo && imageUpdated.ComboComponents.Count == 3, "Image updates preserve combo metadata");
var sheet = await Send(new GenerateMenuItemOperationalSheetPdfQuery { BranchId = branch.Id, Id = combo.Id });
Check(sheet is { Length: > 100 } && System.Text.Encoding.ASCII.GetString(sheet, 0, 4) == "%PDF", "Combo operational sheet generated");
Console.WriteLine($"Combo workflow checks passed: {count}. Real isolated PostgreSQL, no SRI or email.");
