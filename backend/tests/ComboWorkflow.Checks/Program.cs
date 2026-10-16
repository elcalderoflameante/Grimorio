using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Menu.Commands;
using Grimorio.Application.Features.Menu.Queries;
using Grimorio.Application.Features.POS.Commands;
using Grimorio.Application.Features.POS.Queries;
using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Application.Features.Billing.Queries;
using Grimorio.Application.Features.Inventory.Commands;
using Grimorio.Application.Features.TableService.Commands;
using Grimorio.Application.Features.TableService.Queries;
using Grimorio.Domain.Entities.Auth;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Domain.Entities.Menu;
using Grimorio.Domain.Entities.POS;
using Grimorio.Domain.Entities.Inventory;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.Purchases;
using Grimorio.Infrastructure.Features.POS.Commands;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Services.Sri;
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
dishes[0].GeneralName = "Porcion de alitas";
Check(MenuItemNames.Format(" Pluma mistica ", " Filete de pollo ") == "Pluma mistica - Filete de pollo", "Display thematic and general name");
Check(MenuItemNames.Format("Limonada", " limonada ") == "Limonada", "Do not duplicate equal names");
Check(MenuItemNames.Format("Limonada", null) == "Limonada", "Legacy name fallback");
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
Check(parent.ItemName == "Combo dragon - Combo alitas hamburguesa limonada", "Order line includes both names");
Check(parent.PreparationTasks.Single(t => t.MenuItemId == dishes[0].Id).ItemName == "Alitas fijas - Porcion de alitas (Combo: Combo dragon)", "Component description includes real dish and combo reference");
await Reject(new DeleteMenuItemCommand { BranchId = branch.Id, Id = combo.Id }, "ya tiene pedidos");
await Reject(new DeliverOrderCommand { BranchId = branch.Id, OrderId = draft.Id }, "componentes del combo");
await Read(async db => Check(await db.StockReservations.Where(x => x.OrderId == draft.Id && x.Status == StockReservationStatus.Active).SumAsync(x => x.BaseQuantity) == 10, "Reserve 2 combos, sum shared salt"));
for (var i = 0; i < 3; i++)
{
    var stationItems = await Send(new GetItemsByStationQuery { BranchId = branch.Id, StationId = stations[i].Id });
    Check(stationItems.Count == 1 && stationItems[0].Quantity == 2 && stationItems[0].ItemName.Contains("Combo dragon"), "Station gets only its component and combo reference");
    if (i == 0) Check(stationItems[0].ItemName == "Alitas fijas - Porcion de alitas (Combo: Combo dragon)", "Station query includes real component name");
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
var savingsTax = new TaxRate { BranchId = branch.Id, Name = "Savings IVA", Percentage = 15, SriCode = "4" };
var savingsPromotion = new Promotion { BranchId = branch.Id, Name = "Combo ahorro", Type = PromotionType.FixedPrice, FixedPrice = 6.99m,
    MenuItems = [new PromotionMenuItem { BranchId = branch.Id, MenuItemId = combo.Id }] };
await Read(async db => { db.AddRange(savingsTax, savingsPromotion); await db.SaveChangesAsync(); });
await Send(new UpdateMenuItemCommand { BranchId = branch.Id, Id = combo.Id, MenuCategoryId = category.Id,
    Name = "Combo nuevo", GeneralName = combo.GeneralName, Price = 9, TaxRateId = savingsTax.Id, IsActive = true, AvailableForSale = true,
    IsCombo = true, ComboComponents = Definition().ComboComponents });
var savingsOrder = await Send(new CreateDirectSaleCommand { BranchId = branch.Id,
    Items = [new() { MenuItemId = combo.Id, Quantity = 1, PromotionId = savingsPromotion.Id }] });
Check(savingsOrder.Total == 6.99m, "Promotion retains final charge");
var savingsPayment = new PayOrderCommand { BranchId = branch.Id, OrderId = savingsOrder.Id, UserId = user.Id, CashSessionId = session.Id,
    IdempotencyKey = Guid.NewGuid(), OrderAmount = 6.99m, Lines = [new() { MethodId = cash.Id, AmountTendered = 6.99m }],
    Items = [new() { OrderItemId = savingsOrder.Items.Single().Id, Quantity = 1 }] };
var savingsReceipt = await Send(savingsPayment);
await Read(async db =>
{
    var item = await db.OrderPaymentItems.SingleAsync(x => x.OrderPaymentId == savingsReceipt.Id);
    Check(item.OriginalTotal == 9m && item.Total == 6.99m && item.PromotionName == savingsPromotion.Name, "Original price and promotion persisted with payment");
    Check(item.OriginalSubtotal == 7.83m && item.Discount == 1.75m && item.Subtotal == 6.08m && item.Tax == .91m,
        "Explicit subtotal, discount and IVA persisted in PostgreSQL");
    (await db.MenuItems.SingleAsync(x => x.Id == combo.Id)).Price = 20;
    (await db.Promotions.SingleAsync(x => x.Id == savingsPromotion.Id)).FixedPrice = 5;
    await db.SaveChangesAsync();
});
await Read(async db =>
{
    var payment = await db.OrderPayments.Include(x => x.Items).ThenInclude(x => x.OrderItem).ThenInclude(x => x!.MenuItem)
        .Include(x => x.Order).SingleAsync(x => x.Id == savingsReceipt.Id);
    var lines = SriInvoiceLineFactory.Build(payment, payment.Order!);
    var totals = SriInvoiceLineFactory.CalculateTotals(lines, payment.OrderAmount);
    Check(totals.Total == 6.99m && totals.TotalTax == .91m && totals.TotalDiscount == 1.75m, "Invoice taxes use immutable payment prices after catalog edits");
    Check(SriInvoiceLineFactory.Amounts(lines.Single()).Savings == 2.01m, "Customer saving includes IVA");
    var document = new ElectronicDocument { BranchId = branch.Id, OrderPaymentId = payment.Id,
        ClaveAcceso = new string('1', 49), NumeroFactura = "001-001-000000001", Secuencial = 1,
        SubtotalBeforeDiscount = totals.SubtotalBeforeDiscount, TotalSinImpuestos = totals.TotalWithoutTaxes,
        TotalDescuento = totals.TotalDiscount, TotalIva = totals.TotalTax, ImporteTotal = totals.Total };
    db.Add(document);
    await db.SaveChangesAsync();
});
await Read(async db =>
{
    var document = await db.ElectronicDocuments.SingleAsync(x => x.OrderPaymentId == savingsReceipt.Id);
    Check(document.SubtotalBeforeDiscount == 7.83m && document.TotalDescuento == 1.75m
        && document.TotalSinImpuestos == 6.08m && document.TotalIva == .91m && document.ImporteTotal == 6.99m,
        "Explicit invoice header persisted in PostgreSQL");
});
Check((await Send(savingsPayment)).Id == savingsReceipt.Id, "Promotional payment retry returns original receipt after price edits");
await Read(async db => Check(await db.OrderPaymentItems.CountAsync(x => x.OrderPaymentId == savingsReceipt.Id) == 1, "Promotional retry does not duplicate invoice lines"));
// Fixed size options consume both their primary ingredient and additional sauce.
var ribsArticle = new InventoryArticle { BranchId = branch.Id, Name = "Costilla gramos", CategoryId = inventoryCategory.Id, BaseUnitId = unit.Id, Type = ArticleType.Ingredient };
var sauceArticle = new InventoryArticle { BranchId = branch.Id, Name = "Salsa gramos", CategoryId = inventoryCategory.Id, BaseUnitId = unit.Id, Type = ArticleType.Ingredient };
var drinkArticle = new InventoryArticle { BranchId = branch.Id, Name = "Bebida personal", CategoryId = inventoryCategory.Id, BaseUnitId = unit.Id, Type = ArticleType.Ingredient };
var ribs = new MenuItem { BranchId = branch.Id, Name = "Costillas", GeneralName = "Costillas", InternalCode = "M-RIB001",
    MenuCategoryId = category.Id, StationId = stations[1].Id, Price = 9 };
var smallDrink = new MenuItem { BranchId = branch.Id, Name = "Limonada personal", GeneralName = "Limonada personal", InternalCode = "M-DRK001",
    MenuCategoryId = category.Id, StationId = stations[2].Id, Price = 1, SaleVisibility = MenuItemSaleVisibility.ComponentsOnly };
await Read(async db =>
{
    db.AddRange(ribsArticle, sauceArticle, drinkArticle, ribs, smallDrink);
    foreach (var article in new[] { ribsArticle, sauceArticle, drinkArticle })
    {
        db.WarehouseStock.Add(new WarehouseStock { BranchId = branch.Id, ArticleId = article.Id, WarehouseId = warehouse.Id, Quantity = 10000 });
        db.StockMovements.Add(new StockMovement { BranchId = branch.Id, ArticleId = article.Id, WarehouseId = warehouse.Id,
            Type = MovementType.InitialInventory, UnitId = unit.Id, Quantity = 10000, BaseQuantity = 10000, UnitCost = 1, TotalCost = 10000 });
    }
    db.RecipeIngredients.AddRange(
        new RecipeIngredient { BranchId = branch.Id, MenuItemId = ribs.Id, ArticleId = ribsArticle.Id, UnitId = unit.Id, Quantity = 300 },
        new RecipeIngredient { BranchId = branch.Id, MenuItemId = ribs.Id, ArticleId = sauceArticle.Id, UnitId = unit.Id, Quantity = 30 },
        new RecipeIngredient { BranchId = branch.Id, MenuItemId = smallDrink.Id, ArticleId = drinkArticle.Id, UnitId = unit.Id, Quantity = 1 });
    await db.SaveChangesAsync();
});
var modifierRequest = new UpsertMenuItemModifiersCommand { BranchId = branch.Id, MenuItemId = ribs.Id,
    Groups = [new() { Name = "Porcion", IsRequired = true, MinSelections = 1, MaxSelections = 1,
        Options = [new() { Name = "300 g" }, new() { Name = "600 g", ArticleId = ribsArticle.Id, UnitId = unit.Id, Quantity = 300, PriceDelta = 5,
            AdditionalIngredients = [new(sauceArticle.Id, unit.Id, 20)] }] }] };
var savedGroups = await Send(modifierRequest);
var sizeGroup = savedGroups.Single();
var size600 = sizeGroup.Options.Single(o => o.Name == "600 g");
Check(size600.AdditionalIngredients.Single().Quantity == 20, "Option returns additional sauce consumption");
List<UpsertMenuComboComponentDto> FixedComponents(Guid optionId, int selectionQuantity = 1) =>
    [new() { ComponentItemId = ribs.Id, ModifierSelections = [new(optionId, selectionQuantity)] }, new() { ComponentItemId = smallDrink.Id }];
await Reject(Definition([new() { ComponentItemId = ribs.Id }]), "seleccionar al menos");
await Reject(Definition(FixedComponents(Guid.NewGuid())), "modificador");
await Reject(Definition(FixedComponents(size600.Id, 2)), "repetir");
var fixedCombo = await Send(Definition(FixedComponents(size600.Id)));
Check(fixedCombo.ComboComponents.First(c => c.ComponentItemId == ribs.Id).ModifierSelections.Single().ModifierOptionId == size600.Id,
    "Combo returns fixed size option");
var fixedCost = (await Send(new GetMenuProfitabilityQuery { BranchId = branch.Id })).Single(x => x.MenuItemId == fixedCombo.Id);
Check(fixedCost.Ingredients.Where(i => i.ArticleId == ribsArticle.Id).Sum(i => i.BaseQuantity) == 600
    && fixedCost.Ingredients.Where(i => i.ArticleId == sauceArticle.Id).Sum(i => i.BaseQuantity) == 50, "Combo cost includes size meat and additional sauce once");
var fixedDraft = await Send(Order(2, fixedCombo.Id));
Check(fixedDraft.Total == 30 && fixedDraft.Items.Single().ModifierSelections.Count == 0, "Combo ignores component option price and does not ask cashier to select it");
var fixedConfirmed = await Send(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = fixedDraft.Id });
var grillItems = await Send(new GetItemsByStationQuery { BranchId = branch.Id, StationId = stations[1].Id });
Check(grillItems.Single(x => x.OrderId == fixedDraft.Id).Notes!.Contains("600 g"), "Grill receives fixed size in kitchen notes");
await Read(async db =>
{
    var reserved = await db.StockReservations.Where(x => x.OrderId == fixedDraft.Id && x.Status == StockReservationStatus.Active).ToListAsync();
    Check(reserved.Single(x => x.ArticleId == ribsArticle.Id).BaseQuantity == 1200 && reserved.Single(x => x.ArticleId == sauceArticle.Id).BaseQuantity == 100,
        "Two combos reserve correct meat and sauce");
});
// Saving modifiers keeps IDs used by combos, while existing orders keep the old recipe snapshot.
modifierRequest.Groups[0].Id = sizeGroup.Id;
foreach (var option in modifierRequest.Groups[0].Options) option.Id = sizeGroup.Options.Single(o => o.Name == option.Name).Id;
modifierRequest.Groups[0].Options[1].AdditionalIngredients = [new(sauceArticle.Id, unit.Id, 40)];
var editedGroups = await Send(modifierRequest);
Check(editedGroups.Single().Id == sizeGroup.Id && editedGroups.Single().Options.Single(o => o.Name == "600 g").Id == size600.Id,
    "Editing modifiers preserves group and option IDs");
PayOrderCommand FixedPayment() => new() { BranchId = branch.Id, OrderId = fixedDraft.Id, UserId = user.Id, CashSessionId = session.Id,
    IdempotencyKey = Guid.NewGuid(), OrderAmount = 15, Lines = [new() { MethodId = cash.Id, AmountTendered = 15 }],
    Items = [new() { OrderItemId = fixedConfirmed.Items.Single().Id, Quantity = 1 }] };
var fixedPay = FixedPayment();
var fixedReceipt = await Send(fixedPay);
Check((await Send(fixedPay)).Id == fixedReceipt.Id, "Fixed-option combo payment idempotent");
await Send(FixedPayment());
await Read(async db =>
{
    Check((await db.WarehouseStock.SingleAsync(x => x.ArticleId == ribsArticle.Id)).Quantity == 8800
        && (await db.WarehouseStock.SingleAsync(x => x.ArticleId == sauceArticle.Id)).Quantity == 9900,
        "Partial combo payments consume original snapshot, not edited sauce quantity");
    Check(await db.OrderPaymentItems.CountAsync(x => x.OrderPaymentId == fixedReceipt.Id) == 1, "Only parent combo billed");
});
var normal = await Send(new CreateDirectSaleCommand { BranchId = branch.Id,
    Items = [new() { MenuItemId = ribs.Id, Quantity = 1, ModifierSelections = [new() { ModifierOptionId = size600.Id, Quantity = 1 }] }] });
Check(normal.Total == 14, "Individual dish still adds option price");
await Read(async db => Check((await db.StockReservations.Where(x => x.OrderId == normal.Id && x.ArticleId == sauceArticle.Id
    && x.Status == StockReservationStatus.Active).SingleAsync()).BaseQuantity == 70, "Individual dish reserves base sauce plus edited additional sauce"));
await Send(new GetPublicTableMenuQuery { TableToken = table.PublicToken });
var qrFixedRequest = new PublicCreateDraftOrderCommand { TableToken = table.PublicToken, IdempotencyKey = Guid.NewGuid(),
    Items = [new() { MenuItemId = fixedCombo.Id, Quantity = 1, ExpectedUnitPrice = 15 }] };
// The earlier QR order occupies the test table; release it before another customer order.
await Send(new CancelOrderCommand { BranchId = branch.Id, OrderId = qr.Order.Id });
var qrFixed = await Send(qrFixedRequest);
Check(qrFixed.Order.Items.Single().PreparationTasks.Count == 2 && qrFixed.Order.Total == 15, "QR accepts fixed-options combo and keeps combo price");
await Send(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = qrFixed.Order.Id });
await Read(async db => Check((await db.StockReservations.SingleAsync(x => x.OrderId == qrFixed.Order.Id
    && x.ArticleId == sauceArticle.Id && x.Status == StockReservationStatus.Active)).BaseQuantity == 70, "QR uses latest size recipe"));
modifierRequest.Groups[0].Options = modifierRequest.Groups[0].Options.Where(o => o.Id != size600.Id).ToList();
await Send(modifierRequest);
var unavailable = (await Send(new GetMenuAvailabilityQuery { BranchId = branch.Id })).Single(x => x.MenuItemId == fixedCombo.Id);
Check(!unavailable.IsAvailable, "Removed fixed option disables affected combo without breaking catalog");
await Reject(Order(1, fixedCombo.Id), "modificador");
await Send(new GetMenuProfitabilityQuery { BranchId = branch.Id });
// A sauce is a separate choice whose consumed quantity depends on the selected size.
var grams = new MeasurementUnit { BranchId = branch.Id, Name = "Gramo", Symbol = "g" };
var bbq = new InventoryArticle { BranchId = branch.Id, Name = "BBQ", CategoryId = inventoryCategory.Id, BaseUnitId = grams.Id, Type = ArticleType.Ingredient };
var teriyaki = new InventoryArticle { BranchId = branch.Id, Name = "Teriyaki", CategoryId = inventoryCategory.Id, BaseUnitId = grams.Id, Type = ArticleType.Ingredient };
var sizedRibs = new MenuItem { BranchId = branch.Id, Name = "Costillas por tamano", GeneralName = "Costillas por tamano", InternalCode = "M-SIZE01",
    MenuCategoryId = category.Id, StationId = stations[1].Id, Price = 9 };
await Read(async db =>
{
    db.AddRange(grams, bbq, teriyaki, sizedRibs);
    foreach (var article in new[] { bbq, teriyaki })
    {
        db.WarehouseStock.Add(new WarehouseStock { BranchId = branch.Id, ArticleId = article.Id, WarehouseId = warehouse.Id, Quantity = 10000 });
        db.StockMovements.Add(new StockMovement { BranchId = branch.Id, ArticleId = article.Id, WarehouseId = warehouse.Id,
            Type = MovementType.InitialInventory, UnitId = grams.Id, Quantity = 10000, BaseQuantity = 10000, UnitCost = 1, TotalCost = 10000 });
    }
    await db.SaveChangesAsync();
});
var sizeId = Guid.NewGuid(); var smallId = Guid.NewGuid(); var mediumId = Guid.NewGuid(); var largeId = Guid.NewGuid();
var sauceId = Guid.NewGuid(); var bbqId = Guid.NewGuid(); var teriyakiId = Guid.NewGuid();
var dependentRequest = new UpsertMenuItemModifiersCommand { BranchId = branch.Id, MenuItemId = sizedRibs.Id,
    Groups = [new() { Id = sizeId, Name = "Tamano", DisplayOrder = 1, IsRequired = true, MinSelections = 1, MaxSelections = 1,
        Options = [new() { Id = smallId, Name = "300 g", ArticleId = ribsArticle.Id, UnitId = unit.Id, Quantity = 1 },
            new() { Id = mediumId, Name = "600 g", ArticleId = ribsArticle.Id, UnitId = unit.Id, Quantity = 2, PriceDelta = 5 },
            new() { Id = largeId, Name = "1000 g", ArticleId = ribsArticle.Id, UnitId = unit.Id, Quantity = 3, PriceDelta = 9 }] },
        new() { Id = sauceId, Name = "Salsa", DisplayOrder = 2, IsRequired = true, MinSelections = 1, MaxSelections = 1,
            QuantitySourceGroupId = sizeId, QuantityRules = [new(smallId, 30), new(mediumId, 50), new(largeId, 80)],
            Options = [new() { Id = bbqId, Name = "BBQ", ArticleId = bbq.Id, UnitId = grams.Id, Quantity = 30 },
                new() { Id = teriyakiId, Name = "Teriyaki", ArticleId = teriyaki.Id, UnitId = grams.Id, Quantity = 30 }] }] };
var dependentGroups = await Send(dependentRequest);
Check(dependentGroups.Single(g => g.Id == sauceId).QuantityRules.Count == 3, "Size-dependent sauce rules persisted");
CreateOrderCommand SizedOrder(Guid size, Guid sauce, int quantity = 1) => new() { BranchId = branch.Id, Type = "Takeout",
    Items = [new() { MenuItemId = sizedRibs.Id, Quantity = quantity, ModifierSelections =
        [new() { ModifierOptionId = size, Quantity = 1 }, new() { ModifierOptionId = sauce, Quantity = 1 }] }] };
foreach (var (size, meat, sauceQuantity, price) in new[] { (smallId, 1m, 30m, 9m), (mediumId, 2m, 50m, 14m), (largeId, 3m, 80m, 18m) })
{
    var sample = await Send(SizedOrder(size, bbqId));
    Check(sample.Total == price, "Individual price follows size");
    await Send(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = sample.Id });
    await Read(async db =>
    {
        var reservations = await db.StockReservations.Where(r => r.OrderId == sample.Id && r.Status == StockReservationStatus.Active).ToListAsync();
        Check(reservations.Single(r => r.ArticleId == ribsArticle.Id).BaseQuantity == meat
            && reservations.Single(r => r.ArticleId == bbq.Id).BaseQuantity == sauceQuantity
            && reservations.All(r => r.ArticleId != teriyaki.Id), "Size consumes correct meat and only chosen sauce");
    });
    await Send(new CancelOrderCommand { BranchId = branch.Id, OrderId = sample.Id });
}
var dependentOrder = await Send(SizedOrder(mediumId, teriyakiId, 2));
await Send(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = dependentOrder.Id });
dependentRequest.Groups[1].QuantityRules = [new(smallId, 30), new(mediumId, 70), new(largeId, 80)];
await Send(dependentRequest);
var dependentPay = new PayOrderCommand { BranchId = branch.Id, OrderId = dependentOrder.Id, UserId = user.Id, CashSessionId = session.Id,
    IdempotencyKey = Guid.NewGuid(), OrderAmount = 14, Lines = [new() { MethodId = cash.Id, AmountTendered = 14 }],
    Items = [new() { OrderItemId = dependentOrder.Items.Single().Id, Quantity = 1 }] };
var dependentReceipt = await Send(dependentPay);
Check((await Send(dependentPay)).Id == dependentReceipt.Id, "Dependent consumption payment idempotent");
await Read(async db => Check((await db.WarehouseStock.SingleAsync(s => s.ArticleId == teriyaki.Id)).Quantity == 9950,
    "Paid consumption retains original 50g rule after changing it to 70g"));
var choiceCombo = await Send(Definition([new() { ComponentItemId = sizedRibs.Id, Quantity = 2,
    ModifierSelections = [new(smallId)], SelectableModifierGroupIds = [sauceId] }, new() { ComponentItemId = smallDrink.Id }]));
var choiceDetail = (await Send(new GetMenuItemDetailQuery { BranchId = branch.Id, Id = choiceCombo.Id }))!;
Check(choiceDetail.HasModifiers && choiceDetail.ModifierGroups.Count == 1
    && choiceDetail.ModifierGroups.Single().Options.All(o => o.PriceDelta == 0 && o.Quantity == 60), "Combo exposes only sauce with fixed-size quantity and no price surcharge");
Check((await Send(new GetMenuItemsQuery { BranchId = branch.Id, Lightweight = true })).Single(i => i.Id == choiceCombo.Id).HasModifiers,
    "Lightweight catalog advertises combo customer choices");
await Reject(Order(1, choiceCombo.Id), "seleccionar al menos");
await Reject(new CreateOrderCommand { BranchId = branch.Id, Type = "Takeout", Items = [new() { MenuItemId = choiceCombo.Id, Quantity = 1,
    ModifierSelections = [new() { ModifierOptionId = mediumId, Quantity = 1 }] }] }, "eleccion del cliente");
var choiceOrder = await Send(new CreateOrderCommand { BranchId = branch.Id, Type = "Takeout", Items = [new() { MenuItemId = choiceCombo.Id, Quantity = 2,
    ModifierSelections = [new() { ModifierOptionId = bbqId, Quantity = 1 }] }] });
Check(choiceOrder.Total == 30 && choiceOrder.Items.Single().ModifierSelections.Single().UnitPriceDelta == 0,
    "Combo customer choice preserves parent price");
await Send(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = choiceOrder.Id });
await Read(async db =>
{
    var reservations = await db.StockReservations.Where(r => r.OrderId == choiceOrder.Id && r.Status == StockReservationStatus.Active).ToListAsync();
    Check(reservations.Single(r => r.ArticleId == bbq.Id).BaseQuantity == 120
        && reservations.Single(r => r.ArticleId == ribsArticle.Id).BaseQuantity == 4, "Combo and component quantities multiply once without double consumption");
});
Check((await Send(new GetItemsByStationQuery { BranchId = branch.Id, StationId = stations[1].Id })).Single(i => i.OrderId == choiceOrder.Id).Notes!.Contains("BBQ"),
    "Kitchen receives size and chosen sauce");
dependentRequest.Groups[1].QuantityRules = [new(smallId, 45), new(mediumId, 70), new(largeId, 80)];
await Send(dependentRequest);
await Send(new PayOrderCommand { BranchId = branch.Id, OrderId = choiceOrder.Id, UserId = user.Id, CashSessionId = session.Id,
    IdempotencyKey = Guid.NewGuid(), OrderAmount = 15, Lines = [new() { MethodId = cash.Id, AmountTendered = 15 }],
    Items = [new() { OrderItemId = choiceOrder.Items.Single().Id, Quantity = 1 }] });
await Read(async db => Check((await db.WarehouseStock.SingleAsync(s => s.ArticleId == bbq.Id)).Quantity == 9940,
    "Combo payment retains original chosen sauce quantity after rule changes"));
await Send(new CancelOrderCommand { BranchId = branch.Id, OrderId = qrFixed.Order.Id });
var choicePublicMenu = await Send(new GetPublicTableMenuQuery { TableToken = table.PublicToken });
Check(choicePublicMenu.Items.Single(i => i.Id == choiceCombo.Id).ModifierGroups.Count == 1, "QR catalog exposes combo sauce choices");
var choiceQr = await Send(new PublicCreateDraftOrderCommand { TableToken = table.PublicToken, IdempotencyKey = Guid.NewGuid(),
    Items = [new() { MenuItemId = choiceCombo.Id, Quantity = 1, ExpectedUnitPrice = 15,
        ModifierSelections = [new() { ModifierOptionId = teriyakiId, Quantity = 1 }] }] });
await Send(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = choiceQr.Order.Id });
await Read(async db => Check((await db.StockReservations.SingleAsync(r => r.OrderId == choiceQr.Order.Id && r.ArticleId == teriyaki.Id
    && r.Status == StockReservationStatus.Active)).BaseQuantity == 90, "QR combo resolves fixed size and selected sauce"));
await Send(new CancelOrderCommand { BranchId = branch.Id, OrderId = choiceQr.Order.Id });
decimal stockToRemove = 0;
await Read(async db =>
{
    var reserved = await db.StockReservations.Where(r => r.ArticleId == teriyaki.Id && r.Status == StockReservationStatus.Active).SumAsync(r => r.BaseQuantity);
    var stock = (await db.WarehouseStock.SingleAsync(s => s.ArticleId == teriyaki.Id)).Quantity;
    stockToRemove = stock - reserved - 20;
});
await Send(new RegisterMovementCommand { BranchId = branch.Id, ArticleId = teriyaki.Id, WarehouseId = warehouse.Id,
    Type = MovementType.NegativeAdjustment, UnitId = grams.Id, Quantity = stockToRemove, Reference = "Size sauce shortage checks" });
var shortage = await Send(SizedOrder(largeId, teriyakiId));
await Reject(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = shortage.Id }, "Stock insuficiente");
await Read(async db => Check((await db.Orders.SingleAsync(o => o.Id == shortage.Id)).Status == OrderStatus.Draft
    && !await db.StockReservations.AnyAsync(r => r.OrderId == shortage.Id), "Insufficient selected sauce rolls back confirmation and all ingredient reservations"));
await Reject(new PublicCreateDraftOrderCommand { TableToken = table.PublicToken, IdempotencyKey = Guid.NewGuid(),
    Items = [new() { MenuItemId = sizedRibs.Id, Quantity = 1, ExpectedUnitPrice = 18,
        ModifierSelections = [new() { ModifierOptionId = largeId, Quantity = 1 }, new() { ModifierOptionId = teriyakiId, Quantity = 1 }] }] }, "disponible");
dependentRequest.Groups[1].QuantityRules = [new(smallId, 30)];
await Reject(dependentRequest, "cada tamano");
var namedDish = await Send(new CreateMenuItemCommand { BranchId = branch.Id, MenuCategoryId = category.Id,
    Name = "Pluma mistica", GeneralName = "Filete de pollo", Price = 5.5m, StationId = stations[1].Id });
var additionalArticle = new InventoryArticle { BranchId = branch.Id, Name = "Additional checks", CategoryId = inventoryCategory.Id,
    BaseUnitId = unit.Id, Type = ArticleType.Ingredient };
await Read(async db =>
{
    db.Add(additionalArticle);
    db.WarehouseStock.Add(new WarehouseStock { BranchId = branch.Id, ArticleId = additionalArticle.Id, WarehouseId = warehouse.Id, Quantity = 10 });
    db.StockMovements.Add(new StockMovement { BranchId = branch.Id, ArticleId = additionalArticle.Id, WarehouseId = warehouse.Id,
        Type = MovementType.PurchaseEntry, UnitId = unit.Id, Quantity = 10, BaseQuantity = 10, UnitCost = 1, TotalCost = 10 });
    db.RecipeIngredients.Add(new RecipeIngredient { BranchId = branch.Id, MenuItemId = namedDish.Id, ArticleId = additionalArticle.Id, UnitId = unit.Id, Quantity = 1 });
    await db.SaveChangesAsync();
});
var namedOrder = await Send(Order(1, namedDish.Id));
Check(namedOrder.Items.Single().ItemName == "Pluma mistica - Filete de pollo", "New ordinary order includes both names");
await Send(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = namedOrder.Id });
var namedStationItem = (await Send(new GetItemsByStationQuery { BranchId = branch.Id, StationId = stations[1].Id }))
    .Single(i => i.OrderId == namedOrder.Id);
Check(namedStationItem.ItemName == "Pluma mistica - Filete de pollo", "Ordinary station item includes both names");
var readyNamed = await Send(new SetOrderItemStatusCommand { BranchId = branch.Id, OrderItemId = namedStationItem.OrderItemId, Status = "Ready" });
Check(readyNamed.ItemName == "Pluma mistica - Filete de pollo", "Station status update preserves both names");
UpdateOrderItemsCommand Additional(Guid orderId, Guid menuId, int quantity = 1) => new()
{
    BranchId = branch.Id, OrderId = orderId, IdempotencyKey = Guid.NewGuid(), ExpectedIsDraft = false,
    Items = [new() { MenuItemId = menuId, Quantity = quantity }],
};
var readyAddition = await Send(Additional(namedOrder.Id, namedDish.Id));
Check(readyAddition.Status == "Confirmed" && readyAddition.Total == 11 && readyAddition.AddedItemIds.Count == 1,
    "Ready order accepts an addition and recalculates total and preparation state");
Check(readyAddition.Items.Single(i => i.Id == namedStationItem.OrderItemId).Status == "Ready", "Previous dish remains ready");
await Send(new SetOrderItemStatusCommand { BranchId = branch.Id, OrderItemId = readyAddition.AddedItemIds.Single(), Status = "Ready" });
await Send(new DeliverOrderCommand { BranchId = branch.Id, OrderId = namedOrder.Id });
var servedTable = new RestaurantTable { BranchId = branch.Id, Code = "Served checks" };
await Read(async db =>
{
    db.Add(servedTable);
    (await db.Orders.SingleAsync(o => o.Id == namedOrder.Id)).TableId = servedTable.Id;
    await db.SaveChangesAsync();
});
var occupied = (await Send(new GetRestaurantTablesQuery { BranchId = branch.Id })).Single(t => t.Id == servedTable.Id);
Check(occupied.CurrentStatus == "Occupied" && occupied.CurrentOrderId == namedOrder.Id, "Unpaid delivered account keeps its table occupied");
await Reject(new CreateOrderCommand { BranchId = branch.Id, Type = "DineIn", TableId = servedTable.Id,
    Items = [new() { MenuItemId = namedDish.Id, Quantity = 1 }] }, "pedido activo");
await Reject(Additional(namedOrder.Id, namedDish.Id, 1000), "Stock insuficiente");
await Read(async db =>
{
    var unchanged = await db.Orders.Include(o => o.Items).SingleAsync(o => o.Id == namedOrder.Id);
    Check(unchanged.Status == OrderStatus.Delivered && unchanged.DeliveredAt.HasValue && unchanged.Total == 11 && unchanged.Items.Count == 2,
        "Failed addition rolls back items, total and delivery state");
    Check(await db.StockReservations.Where(r => r.OrderId == namedOrder.Id && r.Status == StockReservationStatus.Active).SumAsync(r => r.BaseQuantity) == 2,
        "Failed addition preserves original reservations");
});
var additionalCombo = await Send(new CreateMenuItemCommand { BranchId = branch.Id, MenuCategoryId = category.Id,
    Name = "Additional combo", GeneralName = "Additional combo", Price = 6, IsCombo = true,
    ComboComponents = [new() { ComponentItemId = namedDish.Id, Quantity = 1 }] });
await Send(new PayOrderCommand { BranchId = branch.Id, OrderId = namedOrder.Id, UserId = user.Id, CashSessionId = session.Id,
    IdempotencyKey = Guid.NewGuid(), OrderAmount = 5.5m, Lines = [new() { MethodId = cash.Id, AmountTendered = 5.5m }],
    Items = [new() { OrderItemId = namedStationItem.OrderItemId, Quantity = 1 }] });
await Read(async db =>
{
    // Simulate a legacy whole-order delivery with an unadvanced individual dish.
    (await db.OrderItems.SingleAsync(i => i.Id == readyAddition.AddedItemIds.Single())).Status = OrderItemStatus.Pending;
    await db.SaveChangesAsync();
});
var deliveredRequest = Additional(namedOrder.Id, additionalCombo.Id);
var deliveredAddition = await Send(deliveredRequest);
Check(deliveredAddition.Status == "Confirmed" && deliveredAddition.Total == 17 && deliveredAddition.Items.Count == 3,
    "Delivered order accepts a combo without replacing previous dishes");
Check(deliveredAddition.Items.Where(i => i.Id != deliveredAddition.AddedItemIds.Single()).All(i => i.Status == "Ready"),
    "Already served dishes do not return to pending");
var kitchenAddition = (await Send(new GetItemsByStationQuery { BranchId = branch.Id, StationId = stations[1].Id }))
    .Where(i => i.OrderId == namedOrder.Id).ToList();
Check(kitchenAddition.Count == 1 && kitchenAddition.Single().OrderItemId == deliveredAddition.Items.Single(i => i.Id == deliveredAddition.AddedItemIds.Single()).PreparationTasks.Single().Id,
    "Only new component appears in active station queue");
var additionReplay = await Send(deliveredRequest);
Check(additionReplay.Items.Count == 3 && additionReplay.AddedItemIds.Count == 0, "Lost-response retry does not repeat the addition or station notification");
await Read(async db =>
{
    Check((await db.Orders.SingleAsync(o => o.Id == namedOrder.Id)).DeliveredAt == null, "New consumption clears previous whole-order delivery timestamp");
    Check(await db.StockReservations.Where(r => r.OrderId == namedOrder.Id && r.Status == StockReservationStatus.Active).SumAsync(r => r.BaseQuantity) == 2,
        "Only additional consumption is reserved, once");
    Check(await db.StockMovements.Where(m => m.ArticleId == additionalArticle.Id && m.Type == MovementType.SaleDeduction).SumAsync(m => m.BaseQuantity) == -1,
        "Appending does not deduct stock again after a partial payment");
});
await Send(new SetOrderItemStatusCommand { BranchId = branch.Id, OrderItemId = kitchenAddition.Single().OrderItemId, Status = "Ready" });
await Send(new DeliverOrderCommand { BranchId = branch.Id, OrderId = namedOrder.Id });
await Send(new PayOrderCommand { BranchId = branch.Id, OrderId = namedOrder.Id, UserId = user.Id, CashSessionId = session.Id,
    IdempotencyKey = Guid.NewGuid(), OrderAmount = 11.5m, Lines = [new() { MethodId = cash.Id, AmountTendered = 11.5m }],
    Items = deliveredAddition.Items.Where(i => i.Id != namedStationItem.OrderItemId)
        .Select(i => new PaymentItemCommand { OrderItemId = i.Id, Quantity = 1 }).ToList() });
await Reject(Additional(namedOrder.Id, namedDish.Id), "pagada");
Check((await Send(new GetRestaurantTablesQuery { BranchId = branch.Id })).Single(t => t.Id == servedTable.Id).CurrentStatus == "Free",
    "Paid delivered account releases its table");
var cancelledOrder = await Send(Order(1, namedDish.Id));
await Send(new CancelOrderCommand { BranchId = branch.Id, OrderId = cancelledOrder.Id });
await Reject(Additional(cancelledOrder.Id, namedDish.Id), "estado actual");
Console.WriteLine($"Combo workflow checks passed: {count}. Real isolated PostgreSQL, no SRI or email.");
