using System.Text.Json;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Application.Features.Menu.Commands;
using Grimorio.Application.Features.Menu.Queries;
using Grimorio.Application.Features.POS.Commands;
using Grimorio.Application.Features.POS.Queries;
using Grimorio.Application.Features.TableService.Commands;
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

if (args.Length != 1) throw new Exception("Pass the ignored isolated checks connection JSON path.");
using var json = JsonDocument.Parse(await File.ReadAllTextAsync(args[0]));
var c = json.RootElement;
var target = new NpgsqlConnectionStringBuilder { Host = c.GetProperty("host").GetString(), Port = c.GetProperty("port").GetInt32(),
    Database = c.GetProperty("database").GetString(), Username = c.GetProperty("username").GetString(), Password = c.GetProperty("password").GetString() };
if (target.Host != "127.0.0.1" || target.Port != 5432 || target.Database != "grimorio_checks" || target.Username != "grimorio_checks")
    throw new Exception("Only the isolated localhost grimorio_checks database/user is allowed.");
var services = new ServiceCollection(); services.AddLogging();
services.AddDbContext<GrimorioDbContext>(o => o.UseNpgsql(target.ConnectionString));
services.AddMediatR(o => o.RegisterServicesFromAssembly(typeof(CreateOrderCommandHandler).Assembly));
await using var provider = services.BuildServiceProvider();
async Task<T> Send<T>(IRequest<T> command) { await using var scope = provider.CreateAsyncScope(); return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(command); }
async Task Read(Func<GrimorioDbContext, Task> action) { await using var scope = provider.CreateAsyncScope(); await action(scope.ServiceProvider.GetRequiredService<GrimorioDbContext>()); }
var checks = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
var suffix = Guid.NewGuid().ToString("N")[..8];
var branch = new Branch { Name = "Combo extra checks", Code = suffix, PublicMenuEnabled = true, PublicOrderingEnabled = true }; branch.BranchId = branch.Id;
var user = new User { BranchId = branch.Id, FirstName = "Checks", LastName = "Cashier", Email = $"{suffix}@checks.invalid", PasswordHash = "checks" };
var category = new MenuCategory { BranchId = branch.Id, Name = "Combos" };
var station = new WorkStation { BranchId = branch.Id, Name = "Kitchen" };
var unit = new MeasurementUnit { BranchId = branch.Id, Name = "Unidad", Symbol = "un" };
var invCategory = new InventoryCategory { BranchId = branch.Id, Name = "Ingredients" };
var meat = new InventoryArticle { BranchId = branch.Id, Name = "Meat", CategoryId = invCategory.Id, BaseUnitId = unit.Id };
var warehouse = new Warehouse { BranchId = branch.Id, Name = "Checks" };
var register = new CashRegister { BranchId = branch.Id, Name = "Checks", Code = suffix };
var session = new CashSession { BranchId = branch.Id, CashRegisterId = register.Id, OpenedBy = user.Id };
var cash = new PaymentMethodConfig { Name = $"Checks cash {suffix}", IsCash = true };
var card = new PaymentMethodConfig { Name = $"Checks card {suffix}", IsCard = true };
var bank = new CardBank { BranchId = branch.Id, Name = "Checks bank" };
var tax = new TaxRate { BranchId = branch.Id, Name = "IVA 15%", Percentage = 15, SriCode = "4" };
var table = new RestaurantTable { BranchId = branch.Id, Code = "Extra checks" };
var dishes = new[] { "Burger", "Four wings", "Fries", "Lemonade" }.Select(name => new MenuItem {
    BranchId = branch.Id, Name = name, MenuCategoryId = category.Id, Price = 4, StationId = station.Id,
    TaxRateId = tax.Id, SaleVisibility = MenuItemSaleVisibility.ComponentsOnly }).ToArray();
var extraGroup = new MenuItemModifierGroup { BranchId = branch.Id, MenuItemId = dishes[0].Id, Name = "Meat upgrade", IsRequired = false, MaxSelections = 1 };
var extra = new MenuItemModifierOption { BranchId = branch.Id, ModifierGroupId = extraGroup.Id, Name = "Extra meat", PriceDelta = 1.5m,
    ArticleId = meat.Id, UnitId = unit.Id, Quantity = 1 };
var flavor = new MenuItemModifierGroup { BranchId = branch.Id, MenuItemId = dishes[1].Id, Name = "Flavor", MinSelections = 1, MaxSelections = 1 };
var included = new MenuItemModifierOption { BranchId = branch.Id, ModifierGroupId = flavor.Id, Name = "Included flavor", PriceDelta = 3 };
await Read(async db => {
    await db.Database.MigrateAsync();
    db.AddRange(branch, user, category, station, unit, invCategory, meat, warehouse, register, session, cash, card, bank, tax, table);
    db.AddRange(dishes); db.AddRange(extraGroup, extra, flavor, included);
    db.RecipeIngredients.Add(new RecipeIngredient { BranchId = branch.Id, MenuItemId = dishes[0].Id, ArticleId = meat.Id, UnitId = unit.Id, Quantity = 1 });
    db.WarehouseStock.Add(new WarehouseStock { BranchId = branch.Id, ArticleId = meat.Id, WarehouseId = warehouse.Id, Quantity = 100 });
    db.StockMovements.Add(new StockMovement { BranchId = branch.Id, ArticleId = meat.Id, WarehouseId = warehouse.Id,
        Type = MovementType.InitialInventory, UnitId = unit.Id, Quantity = 100, BaseQuantity = 100, UnitCost = 1, TotalCost = 100 });
    await db.SaveChangesAsync();
});
async Task<MenuItemDto> Combo(decimal price, int burgers = 1) => await Send(new CreateMenuItemCommand {
    BranchId = branch.Id, Name = "Lunch combo", MenuCategoryId = category.Id, IsCombo = true, Price = price, TaxRateId = tax.Id,
    ComboComponents = dishes.Select((dish, index) => new UpsertMenuComboComponentDto {
        ComponentItemId = dish.Id, Quantity = index == 0 ? burgers : 1,
        SelectableModifierGroupIds = index == 0 ? [extraGroup.Id] : [],
        ModifierSelections = index == 1 ? [new(included.Id)] : [],
    }).ToList() });
var combo = await Combo(6.99m);
var originalPriceCombo = await Combo(10);
var twoBurgers = await Combo(6.99m, 2);
CreateOrderItemDto Line(Guid menuId, bool upgrade = true, int quantity = 1, Guid? promotionId = null) => new() {
    MenuItemId = menuId, Quantity = quantity, PromotionId = promotionId,
    ModifierSelections = upgrade ? [new() { ModifierOptionId = extra.Id, Quantity = 1 }] : [] };
async Task<OrderDto> Order(Guid menuId, bool upgrade = true, int quantity = 1, Guid? promotionId = null) => await Send(new CreateOrderCommand {
    BranchId = branch.Id, Type = "Takeout", Items = [Line(menuId, upgrade, quantity, promotionId)] });
var menuDetail = await Send(new GetMenuItemDetailQuery { BranchId = branch.Id, Id = combo.Id });
Check(menuDetail!.ModifierGroups.Single().Options.Single().PriceDelta == 1.5m, "Catalog exposes the extra instead of zeroing it");
var noExtra = await Order(combo.Id, false); Check(noExtra.Total == 6.99m, "Included fixed choices remain included in the combo price");
var upgraded = await Order(combo.Id); Check(upgraded.Total == 8.49m && upgraded.Items.Single().UnitPrice == 8.49m, "6.99 combo plus 1.50 meat is 8.49");
Check(upgraded.Items.Single().ModifierSelections.Single().UnitPriceDelta == 1.5m, "Parent retains the price snapshot");
var scaled = await Order(twoBurgers.Id); Check(scaled.Total == 9.99m, "Two component burgers charge two meat upgrades per combo");
Check((await Order(combo.Id, quantity: 2)).Total == 16.98m, "Two upgraded combos multiply the surcharge once per combo");
var edited = await Send(new UpdateOrderItemsCommand { BranchId = branch.Id, OrderId = upgraded.Id, ExpectedIsDraft = true,
    Items = [Line(combo.Id)] });
Check(edited.Total == 8.49m, "Saving an existing draft retains the surcharge without doubling it");
var direct = await Send(new CreateDirectSaleCommand { BranchId = branch.Id, Items = [Line(combo.Id)] });
Check(direct.Total == 8.49m, "Direct sale uses the same extra price");
var promo = new Promotion { BranchId = branch.Id, Name = "Lunch offer", Type = PromotionType.FixedPrice, FixedPrice = 6.99m,
    PaymentPolicy = PromotionPaymentPolicy.CardAlternativePrice, CardPrice = 7.99m,
    MenuItems = [new() { BranchId = branch.Id, MenuItemId = originalPriceCombo.Id }] };
await Read(async db => { db.Add(promo); await db.SaveChangesAsync(); });
var promotional = await Order(originalPriceCombo.Id, quantity: 2, promotionId: promo.Id);
Check(promotional.Total == 16.98m && promotional.Items.Single().DiscountAmount == 6.02m, "Fixed-price offer discounts only the base, not the extra");
var confirmed = await Send(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = promotional.Id });
Check(confirmed.Total == 16.98m && confirmed.Items.Single().PreparationTasks.Count == 4, "Confirmation retains price and component tasks");
await Read(async db => {
    var parentId = confirmed.Items.Single().Id;
    Check(await db.StockReservations.Where(r => r.OrderItemId == parentId && r.ArticleId == meat.Id && r.Status == StockReservationStatus.Active).SumAsync(r => r.BaseQuantity) == 4,
        "Base meat and extra are reserved once, not on both parent and component");
    var parent = await db.OrderItems.Include(i => i.ModifierSelections).SingleAsync(i => i.Id == parentId);
    Check(parent.ModifierSelections.All(s => s.ArticleId == null && s.InventoryQuantity == 0), "Parent modifiers price only; component owns consumption");
});
var reloaded = (await Send(new GetOrderDetailQuery { BranchId = branch.Id, OrderId = promotional.Id }))!;
Check(reloaded.Total == 16.98m && reloaded.Items.Single().ModifierSelections.Single().UnitPriceDelta == 1.5m, "Reopening the order preserves price and modifier snapshot");
var itemId = promotional.Items.Single().Id;
PayOrderCommand Payment(bool withCard) => new() { BranchId = branch.Id, OrderId = promotional.Id, UserId = user.Id, CashSessionId = session.Id,
    IdempotencyKey = Guid.NewGuid(), OrderAmount = withCard ? 9.49m : 8.49m,
    Items = [new() { OrderItemId = itemId, Quantity = 1 }], Lines = [new() { MethodId = withCard ? card.Id : cash.Id,
        AmountTendered = withCard ? 9.49m : 8.49m, CardPaymentType = withCard ? "Debit" : null,
        CardBankId = withCard ? bank.Id : null, CardBrand = withCard ? "Visa" : null, AuthorizationNumber = withCard ? "checks123" : null }] };
var cashRequest = Payment(false); var cashPaid = await Send(cashRequest);
Check(cashPaid.OrderAmount == 8.49m && cashPaid.Items.Single().Total == 8.49m, "Cash checkout charges base offer plus extra");
Check((await Send(cashRequest)).Id == cashPaid.Id, "Retrying cash payment does not duplicate the extra charge");
var cardPaid = await Send(Payment(true)); Check(cardPaid.OrderAmount == 9.49m, "Card alternative price also keeps the extra");
Check((await Send(new GetOrderDetailQuery { BranchId = branch.Id, OrderId = promotional.Id }))!.Total == 17.98m, "Split cash/card payments retain both confirmed totals");
await Read(async db => {
    Check(await db.OrderPayments.CountAsync(p => p.OrderId == promotional.Id) == 2, "Exactly two payments persisted");
    Check(await db.StockMovements.Where(m => m.OrderItemId == itemId && m.ArticleId == meat.Id && m.Type == MovementType.SaleDeduction).SumAsync(m => m.BaseQuantity) == -4,
        "Payment stock deduction includes the extra exactly once");
});
var qr = await Send(new PublicCreateDraftOrderCommand { TableToken = table.PublicToken, IdempotencyKey = Guid.NewGuid(),
    Items = [new() { MenuItemId = originalPriceCombo.Id, Quantity = 1, PromotionId = promo.Id, ExpectedUnitPrice = 11.5m,
        ModifierSelections = [new() { ModifierOptionId = extra.Id, Quantity = 1 }] }] });
Check(qr.Order.Total == 8.49m, "Public QR uses the same fixed offer plus surcharge");
Console.WriteLine($"{checks} combo extra pricing checks passed on isolated Docker database.");
