using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Grimorio.API.Controllers;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Application.Features.Billing.Queries;
using Grimorio.Application.Features.Menu.Commands;
using Grimorio.Application.Features.POS.Commands;
using Grimorio.Application.Features.POS.Queries;
using Grimorio.Domain.Entities.Auth;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.Inventory;
using Grimorio.Domain.Entities.Menu;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Features.POS.Commands;
using Grimorio.Infrastructure.Persistence;
using Grimorio.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

var connection = Environment.GetEnvironmentVariable("CORRECTION_CHECKS_CONNECTION") ?? throw new Exception("Set CORRECTION_CHECKS_CONNECTION.");
var destination = new NpgsqlConnectionStringBuilder(connection);
if (destination.Host != "127.0.0.1" || destination.Port != 15432 || destination.Database != "checks" || destination.Username != "checks")
    throw new Exception("Only disposable localhost:15432 checks database is allowed.");
var count = 0;
void Check(bool value, string name) { if (!value) throw new Exception(name); count++; }
var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Services.AddDbContext<GrimorioDbContext>(options => options.UseNpgsql(connection));
builder.Services.AddMediatR(options => options.RegisterServicesFromAssembly(typeof(CorrectOrderItemCommandHandler).Assembly));
builder.Services.AddSignalR(); builder.Services.AddControllers().AddApplicationPart(typeof(PosController).Assembly);
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("order-correction-checks-only-not-for-production-1234567890"));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => options.TokenValidationParameters = new() {
    ValidateIssuerSigningKey = true, IssuerSigningKey = key, ValidateIssuer = false, ValidateAudience = false, ValidateLifetime = true,
});
builder.Services.AddAuthorization();
await using var app = builder.Build(); app.UseAuthentication(); app.UseAuthorization(); app.MapControllers();
async Task Read(Func<GrimorioDbContext, Task> action) { await using var scope = app.Services.CreateAsyncScope(); await action(scope.ServiceProvider.GetRequiredService<GrimorioDbContext>()); }
async Task<T> Send<T>(IRequest<T> request) { await using var scope = app.Services.CreateAsyncScope(); return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request); }
var branch = new Branch { Name = "Correction checks", Code = "COR" };
var foreign = new Branch { Name = "Other", Code = "OTHER" };
var admin = new User { BranchId = branch.Id, FirstName = new string('A', 100), LastName = new string('B', 100), Email = "admin@checks.invalid", PasswordHash = "checks" };
var waiter = new User { BranchId = branch.Id, FirstName = "Waiter", LastName = "Checks", Email = "waiter@checks.invalid", PasswordHash = "checks" };
var role = new Role { BranchId = branch.Id, Name = AppConstants.Roles.Admin };
var station = new WorkStation { BranchId = branch.Id, Name = "Grill" };
var category = new MenuCategory { BranchId = branch.Id, Name = "Checks" };
var inventoryCategory = new InventoryCategory { BranchId = branch.Id, Name = "Ingredients" };
var unit = new MeasurementUnit { BranchId = branch.Id, Name = "Unit", Symbol = "un" };
var warehouse = new Warehouse { BranchId = branch.Id, Name = "Checks" };
var register = new CashRegister { BranchId = branch.Id, Name = "Checks", Code = "COR" };
var session = new CashSession { BranchId = branch.Id, CashRegisterId = register.Id, OpenedBy = admin.Id };
var cash = new PaymentMethodConfig { Name = "Cash", IsCash = true };
var articles = new[] { "Base", "BBQ", "Teriyaki", "Unavailable" }.Select(name => new InventoryArticle {
    BranchId = branch.Id, Name = name, CategoryId = inventoryCategory.Id, BaseUnitId = unit.Id }).ToArray();
await Read(async db => {
    if (Environment.GetEnvironmentVariable("CORRECTION_CHECKS_ALLOW_RESET") == "true") await db.Database.EnsureDeletedAsync();
    await db.Database.MigrateAsync(); Check(!db.Database.HasPendingModelChanges(), "Migration matches model");
    db.AddRange(branch, foreign, admin, waiter, role, station, category, inventoryCategory, unit, warehouse, register, session, cash);
    db.UserRoles.Add(new UserRole { BranchId = branch.Id, UserId = admin.Id, RoleId = role.Id }); db.AddRange(articles);
    foreach (var article in articles.Take(3)) {
        db.WarehouseStock.Add(new WarehouseStock { BranchId = branch.Id, ArticleId = article.Id, WarehouseId = warehouse.Id, Quantity = 500 });
        db.StockMovements.Add(new StockMovement { BranchId = branch.Id, ArticleId = article.Id, WarehouseId = warehouse.Id,
            Type = MovementType.InitialInventory, Quantity = 500, BaseQuantity = 500, UnitId = unit.Id, UnitCost = 1, TotalCost = 500 });
    }
    await db.SaveChangesAsync();
});
var dish = await Send(new CreateMenuItemCommand { BranchId = branch.Id, Name = "Ribs", GeneralName = "Ribs", MenuCategoryId = category.Id, Price = 10, StationId = station.Id });
await Send(new UpsertRecipeCommand { BranchId = branch.Id, MenuItemId = dish.Id,
    Ingredients = [new() { ArticleId = articles[0].Id, UnitId = unit.Id, Quantity = 5 }] });
var size = (await Send(new UpsertMenuItemModifiersCommand { BranchId = branch.Id, MenuItemId = dish.Id, Groups = [new() {
    Name = "Size", MinSelections = 1, MaxSelections = 1, IsRequired = true, Options = [
        new() { Name = "Small", ArticleId = articles[0].Id, UnitId = unit.Id, Quantity = 1 },
        new() { Name = "Large", ArticleId = articles[0].Id, UnitId = unit.Id, Quantity = 2, PriceDelta = 3 }] }] })).Single();
var small = size.Options.Single(o => o.Name == "Small").Id; var large = size.Options.Single(o => o.Name == "Large").Id;
var modifiers = await Send(new UpsertMenuItemModifiersCommand { BranchId = branch.Id, MenuItemId = dish.Id, Groups = [new() {
    Id = size.Id, Name = size.Name, MinSelections = 1, MaxSelections = 1, IsRequired = true,
    Options = size.Options.Select(o => new UpsertMenuItemModifierOptionDto { Id = o.Id, Name = o.Name, ArticleId = o.ArticleId, UnitId = o.UnitId, Quantity = o.Quantity, PriceDelta = o.PriceDelta }).ToList()
}, new() { Name = "Flavor", MinSelections = 1, MaxSelections = 1, IsRequired = true, QuantitySourceGroupId = size.Id,
    QuantityRules = [new(small, 2), new(large, 4)], Options = articles.Skip(1).Select(a => new UpsertMenuItemModifierOptionDto {
        Name = a.Name, ArticleId = a.Id, UnitId = unit.Id, Quantity = 2 }).ToList() }] });
var flavor = modifiers.Single(g => g.Name == "Flavor");
var bbq = flavor.Options.Single(o => o.Name == "BBQ").Id; var teriyaki = flavor.Options.Single(o => o.Name == "Teriyaki").Id;
var unavailable = flavor.Options.Single(o => o.Name == "Unavailable").Id;
async Task<OrderDto> Order(string state, int quantity = 1, bool side = false) {
    var items = new List<CreateOrderItemDto> { new() { MenuItemId = dish.Id, Quantity = quantity,
        ModifierSelections = [new() { ModifierOptionId = small, Quantity = 1 }, new() { ModifierOptionId = bbq, Quantity = 1 }] } };
    if (side) items.Add(new() { MenuItemId = dish.Id, Quantity = 1,
        ModifierSelections = [new() { ModifierOptionId = small, Quantity = 1 }, new() { ModifierOptionId = teriyaki, Quantity = 1 }] });
    var order = await Send(new CreateOrderCommand { BranchId = branch.Id, Type = "Takeout", Items = items });
    if (state == "Draft") return order;
    order = await Send(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = order.Id });
    if (state == "Confirmed") return order;
    foreach (var item in order.Items) await Send(new SetOrderItemStatusCommand { BranchId = branch.Id, OrderItemId = item.Id, Status = state == "InPreparation" ? "InPreparation" : "Ready" });
    if (state == "Delivered") return await Send(new DeliverOrderCommand { BranchId = branch.Id, OrderId = order.Id });
    return (await Send(new GetOrderDetailQuery { BranchId = branch.Id, OrderId = order.Id }))!;
}
CorrectOrderItemDto Data(bool replace = false, bool waste = false, Guid? option = null, Guid? sizeOption = null) => new() {
    RequestId = Guid.NewGuid(), Replace = replace, Disposition = waste ? "PreparedWaste" : "RegistrationError", Reason = "Administrative correction checks",
    ModifierSelections = replace ? [new() { ModifierOptionId = sizeOption ?? small, Quantity = 1 }, new() { ModifierOptionId = option ?? teriyaki, Quantity = 1 }] : [],
};
string Token(Guid actor, bool adminRole = true, Guid? branchId = null) => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
    claims: [new Claim(ClaimTypes.NameIdentifier, actor.ToString()), new Claim(AppConstants.Claims.BranchId, (branchId ?? branch.Id).ToString()),
        new Claim(ClaimTypes.Role, adminRole ? AppConstants.Roles.Admin : AppConstants.Roles.Waiter)], expires: DateTime.UtcNow.AddMinutes(30),
    signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
await app.StartAsync();
try {
    using var client = new HttpClient { BaseAddress = new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single()) };
    async Task<OrderDto?> Correct(Guid id, CorrectOrderItemDto data, HttpStatusCode expected = HttpStatusCode.OK, string? token = null) {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/pos/ordenes/items/{id}/corregir") { Content = JsonContent.Create(data) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token ?? Token(admin.Id));
        using var response = await client.SendAsync(request);
        Check(response.StatusCode == expected, $"Correction: expected {expected}, got {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<OrderDto>() : null;
    }
    foreach (var state in new[] { "Draft", "Confirmed", "InPreparation", "Ready", "Delivered" }) {
        var order = await Order(state); var id = order.Items.Single().Id;
        var corrected = (await Correct(id, Data()))!;
        Check(corrected.Status == "Cancelled" && corrected.Total == 0 && corrected.Items.Single().Status == "Cancelled", $"Cancel unpaid {state}, retain history and remove charge");
        await Read(async db => Check(!await db.StockReservations.AnyAsync(r => r.OrderId == order.Id && r.Status == StockReservationStatus.Active)
            && !await db.StockMovements.AnyAsync(m => m.OrderItemId == id), "Registration error releases without consuming ingredients"));
        Check(corrected.Items.Single().CorrectionDisposition == "RegistrationError" && corrected.Items.Single().CorrectionReason != null, "Correction reason returned");
    }
    var prepared = await Order("Ready"); var preparedId = prepared.Items.Single().Id; var preparedData = Data(waste: true);
    await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => Correct(preparedId, preparedData)));
    await Read(async db => {
        Check(await db.OrderItemCorrections.CountAsync(c => c.OriginalItemId == preparedId) == 1, "Concurrent retries have one audit entry");
        Check(await db.StockMovements.CountAsync(m => m.OrderItemId == preparedId && m.Type == MovementType.Waste) == 2, "Waste exactly once per original ingredient reservation");
        Check(await db.StockMovements.Where(m => m.OrderItemId == preparedId).SumAsync(m => m.BaseQuantity) == -8, "Waste uses exact reserved base and sauce quantities");
    });
    var changedDraft = await Order("Draft"); var changedDraftId = changedDraft.Items.Single().Id;
    var draftReplacement = (await Correct(changedDraftId, Data(replace: true)))!;
    Check(draftReplacement.Status == "Draft", "Corrected draft remains a draft");
    var savedDraft = await Send(new UpdateOrderItemsCommand { BranchId = branch.Id, OrderId = changedDraft.Id,
        ExpectedIsDraft = true, IdempotencyKey = Guid.NewGuid(),
        Items = draftReplacement.Items.Where(i => i.Status != "Cancelled").Select(i => new CreateOrderItemDto {
            MenuItemId = i.MenuItemId, Quantity = i.Quantity,
            ModifierSelections = i.ModifierSelections.Select(s => new CreateModifierSelectionDto {
                ModifierOptionId = s.ModifierOptionId, Quantity = s.Quantity }).ToList() }).ToList() });
    Check(savedDraft.Items.Count == 2 && savedDraft.Items.Single(i => i.Id == changedDraftId).CorrectionReason != null
        && savedDraft.Items.Count(i => i.Status != "Cancelled") == 1 && savedDraft.Total == 10,
        "Saving corrected draft retains cancelled audit history without resurrecting its charge");
    await Send(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = changedDraft.Id });
    await Read(async db => Check(!await db.StockReservations.AnyAsync(r => r.OrderItemId == changedDraftId)
        && await db.StockReservations.Where(r => r.OrderId == changedDraft.Id && r.Status == StockReservationStatus.Active).SumAsync(r => r.BaseQuantity) == 8,
        "Confirming corrected draft reserves only replacement, not cancelled historical dish"));
    var delivered = await Order("Delivered", side: true); var original = delivered.Items.First(); var replaceData = Data(replace: true, sizeOption: large);
    var replaced = (await Correct(original.Id, replaceData))!;
    var replacement = replaced.Items.Single(i => i.Id == replaced.Items.Single(i => i.Id == original.Id).ReplacementItemId);
    Check(replacement.Status == "Pending" && replaced.Status == "Confirmed" && replacement.TotalPrice == 13 && replaced.Total == 23, "Option correction recalculates surcharge and order total");
    Check(replaced.Items.Single(i => i.Id != original.Id && i.Id != replacement.Id).Status == "Ready", "Previously served other dish remains ready");
    var queued = (await Send(new GetItemsByStationQuery { BranchId = branch.Id, StationId = station.Id })).Where(i => i.OrderId == delivered.Id).ToList();
    Check(queued.Count == 1 && queued.Single().OrderItemId == replacement.Id, "Station queue contains only replacement");
    await Read(async db => {
        Check(await db.StockReservations.Where(r => r.OrderItemId == replacement.Id && r.ArticleId == articles[2].Id && r.Status == StockReservationStatus.Active).SumAsync(r => r.BaseQuantity) == 4,
            "Dependent flavor quantity recalculated for large size");
        Check(!await db.StockMovements.AnyAsync(m => m.OrderItemId == original.Id), "Registration replacement does not consume original ingredients");
    });
    var replay = (await Correct(original.Id, replaceData))!; Check(replay.Items.Count == 3, "Replacement retry does not duplicate order lines");
    replaceData.Reason = "Different reason with same request"; await Correct(original.Id, replaceData, HttpStatusCode.BadRequest);
    var wastedReplacement = await Order("Ready"); var wastedId = wastedReplacement.Items.Single().Id;
    var withWaste = (await Correct(wastedId, Data(replace: true, waste: true)))!;
    Check(withWaste.Items.Count == 2 && withWaste.Total == 10, "Replacing a prepared dish keeps one payable portion");
    await Read(async db => Check(await db.StockMovements.Where(m => m.OrderItemId == wastedId && m.Type == MovementType.Waste).SumAsync(m => m.BaseQuantity) == -8,
        "Prepared replacement consumes original ingredients as waste"));
    var shortage = await Order("Ready"); var shortageId = shortage.Items.Single().Id;
    await Correct(shortageId, Data(replace: true, waste: true, option: unavailable), HttpStatusCode.BadRequest);
    await Read(async db => {
        Check((await db.OrderItems.SingleAsync(i => i.Id == shortageId)).Status == OrderItemStatus.Ready
            && !await db.OrderItemCorrections.AnyAsync(c => c.OriginalItemId == shortageId), "Shortage rolls back old status and audit");
        Check(!await db.StockMovements.AnyAsync(m => m.OrderItemId == shortageId)
            && await db.StockReservations.AnyAsync(r => r.OrderItemId == shortageId && r.Status == StockReservationStatus.Active), "Shortage rolls back waste and restores original reservations");
        Check(await db.OrderItems.CountAsync(i => i.OrderId == shortage.Id) == 1, "Shortage rolls back replacement creation");
    });
    var paid = await Order("Ready", quantity: 2); var paidId = paid.Items.Single().Id;
    await Send(new PayOrderCommand { BranchId = branch.Id, OrderId = paid.Id, UserId = admin.Id, CashSessionId = session.Id, IdempotencyKey = Guid.NewGuid(), OrderAmount = 10,
        Lines = [new() { MethodId = cash.Id, AmountTendered = 10 }], Items = [new() { OrderItemId = paidId, Quantity = 1 }] });
    await Correct(paidId, Data(), HttpStatusCode.BadRequest);
    var partiallyPaid = await Order("Ready", side: true); var sideItem = partiallyPaid.Items.Last(); var unpaidItem = partiallyPaid.Items.First();
    await Send(new PayOrderCommand { BranchId = branch.Id, OrderId = partiallyPaid.Id, UserId = admin.Id, CashSessionId = session.Id, IdempotencyKey = Guid.NewGuid(), OrderAmount = 10,
        Lines = [new() { MethodId = cash.Id, AmountTendered = 10 }], Items = [new() { OrderItemId = sideItem.Id, Quantity = 1 }] });
    var cashBefore = JsonSerializer.Serialize(await Send(new GetActiveCashSessionQuery { BranchId = branch.Id, UserId = admin.Id }));
    var settled = (await Correct(unpaidItem.Id, Data()))!;
    Check(settled.PaidAt.HasValue && settled.Total == 10, "Cancel last unpaid item settles existing paid balance");
    Check(JsonSerializer.Serialize(await Send(new GetActiveCashSessionQuery { BranchId = branch.Id, UserId = admin.Id })) == cashBefore, "Correction does not change previous cash receipts");
    await Correct(sideItem.Id, Data(replace: true), HttpStatusCode.BadRequest);
    var authorization = await Order("Confirmed"); var authorizationId = authorization.Items.Single().Id;
    await Correct(authorizationId, Data(), HttpStatusCode.Forbidden, Token(waiter.Id, false));
    await Correct(authorizationId, Data(), HttpStatusCode.BadRequest, Token(waiter.Id));
    await Correct(authorizationId, Data(), HttpStatusCode.BadRequest, Token(admin.Id, branchId: foreign.Id));
    var blank = Data(); blank.Reason = ""; await Correct(authorizationId, blank, HttpStatusCode.BadRequest);
    var unchanged = Data(replace: true, option: bbq); await Correct(authorizationId, unchanged, HttpStatusCode.BadRequest);
    var invalid = Data(replace: true); invalid.ModifierSelections = []; await Correct(authorizationId, invalid, HttpStatusCode.BadRequest);
    var draftWaste = await Order("Draft"); await Correct(draftWaste.Items.Single().Id, Data(waste: true), HttpStatusCode.BadRequest);
    var combo = await Send(new CreateMenuItemCommand { BranchId = branch.Id, Name = "Ribs combo", GeneralName = "Ribs and drink", MenuCategoryId = category.Id,
        Price = 15, IsCombo = true, ComboComponents = [new() { ComponentItemId = dish.Id, Quantity = 1,
            ModifierSelections = [new(small)], SelectableModifierGroupIds = [flavor.Id] }] });
    var comboOrder = await Send(new CreateOrderCommand { BranchId = branch.Id, Type = "Takeout", Items = [new() { MenuItemId = combo.Id,
        Quantity = 1, ModifierSelections = [new() { ModifierOptionId = bbq, Quantity = 1 }] }] });
    await Send(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = comboOrder.Id });
    var oldComboItem = comboOrder.Items.Single();
    await Send(new SetOrderItemStatusCommand { BranchId = branch.Id, OrderItemId = oldComboItem.PreparationTasks.Single().Id, Status = "Ready" });
    var comboData = Data(replace: true, waste: true); comboData.ModifierSelections = [new() { ModifierOptionId = teriyaki, Quantity = 1 }];
    var correctedCombo = (await Correct(oldComboItem.Id, comboData))!;
    var newComboItem = correctedCombo.Items.Single(i => i.Status != "Cancelled");
    Check(correctedCombo.Total == 15 && newComboItem.PreparationTasks.Single().Status == "Pending"
        && correctedCombo.Items.Single(i => i.Id == oldComboItem.Id).PreparationTasks.Single().Status == "Cancelled", "Combo correction replaces component tasks without doubling combo price");
    await Read(async db => Check(await db.StockMovements.Where(m => m.OrderItemId == oldComboItem.Id && m.Type == MovementType.Waste).SumAsync(m => m.BaseQuantity) == -8
        && await db.StockReservations.Where(r => r.OrderItemId == newComboItem.Id && r.ArticleId == articles[2].Id && r.Status == StockReservationStatus.Active).SumAsync(r => r.BaseQuantity) == 2,
        "Combo original recipe snapshot wasted; new selected component ingredients reserved"));
    var forceFailure = await Order("Ready"); var forceId = forceFailure.Items.Single().Id; var forceData = Data(waste: true);
    await Read(db => db.Database.ExecuteSqlRawAsync("ALTER TABLE inv.\"StockMovements\" ADD CONSTRAINT \"CK_CorrectionChecks_Failure\" CHECK (\"Type\" <> 'Waste') NOT VALID"));
    try { await Correct(forceId, forceData, HttpStatusCode.InternalServerError); }
    finally { await Read(db => db.Database.ExecuteSqlRawAsync("ALTER TABLE inv.\"StockMovements\" DROP CONSTRAINT \"CK_CorrectionChecks_Failure\"")); }
    await Read(async db => Check((await db.OrderItems.SingleAsync(i => i.Id == forceId)).Status == OrderItemStatus.Ready
        && !await db.OrderItemCorrections.AnyAsync(c => c.OriginalItemId == forceId)
        && !await db.StockMovements.AnyAsync(m => m.OrderItemId == forceId)
        && await db.StockReservations.AnyAsync(r => r.OrderItemId == forceId && r.Status == StockReservationStatus.Active),
        "Database failure rolls back correction, waste and reservation changes together"));
    await Correct(forceId, forceData);
    var promotion = new Promotion { BranchId = branch.Id, Name = "Correction discount", Type = PromotionType.Percentage, DiscountPercent = 20,
        MenuItems = [new PromotionMenuItem { BranchId = branch.Id, MenuItemId = dish.Id }] };
    await Read(async db => { db.Add(promotion); await db.SaveChangesAsync(); });
    var promotional = await Send(new CreateOrderCommand { BranchId = branch.Id, Type = "Takeout", Items = [new() { MenuItemId = dish.Id, Quantity = 1,
        PromotionId = promotion.Id, ModifierSelections = [new() { ModifierOptionId = small, Quantity = 1 }, new() { ModifierOptionId = bbq, Quantity = 1 }] }] });
    await Send(new ConfirmOrderCommand { BranchId = branch.Id, OrderId = promotional.Id });
    var priceCorrection = (await Correct(promotional.Items.Single().Id, Data(replace: true, sizeOption: large)))!;
    var promotionalLine = priceCorrection.Items.Single(i => i.Status != "Cancelled");
    Check(priceCorrection.Total == 10.4m && promotionalLine.DiscountAmount == 2.6m && promotionalLine.PromotionId == promotion.Id,
        "Modifier price correction preserves promotion and recalculates discount in detail and header");
    var snapshot = await Order("Ready"); var snapshotId = snapshot.Items.Single().Id;
    await Send(new UpsertRecipeCommand { BranchId = branch.Id, MenuItemId = dish.Id,
        Ingredients = [new() { ArticleId = articles[0].Id, UnitId = unit.Id, Quantity = 50 }] });
    await Correct(snapshotId, Data(waste: true));
    await Read(async db => Check(await db.StockMovements.Where(m => m.OrderItemId == snapshotId && m.Type == MovementType.Waste).SumAsync(m => m.BaseQuantity) == -8,
        "Waste uses original reservations even after catalog recipe changes"));
    await Read(async db => {
        foreach (var stock in await db.WarehouseStock.ToListAsync())
            Check(stock.Quantity == await db.StockMovements.Where(m => m.ArticleId == stock.ArticleId && m.WarehouseId == stock.WarehouseId).SumAsync(m => m.BaseQuantity), "Ledger agrees with stored stock");
        Check(!await db.ElectronicDocuments.AnyAsync(), "No SRI calls or documents generated by corrections");
        var actorName = $"{admin.FirstName} {admin.LastName}";
        Check(await db.OrderItemCorrections.AllAsync(c => c.ActorUserId == admin.Id && c.ActorName == actorName), "Every correction records full administrator identity, including maximum-length names");
    });
    Console.WriteLine($"Order correction checks passed: {count}. Real isolated PostgreSQL and HTTP; no production services.");
} finally { await app.StopAsync(); }
