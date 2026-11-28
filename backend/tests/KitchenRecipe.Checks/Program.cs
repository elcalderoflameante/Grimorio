using System.Reflection;
using System.Text.Json;
using Grimorio.API.Controllers;
using Grimorio.Application.Features.POS.Queries;
using Grimorio.Domain.Entities.Inventory;
using Grimorio.Domain.Entities.Menu;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Features.POS.Queries;
using Grimorio.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var connection = Environment.GetEnvironmentVariable("KITCHEN_RECIPE_CHECKS_CONNECTION")
    ?? "Host=127.0.0.1;Port=15441;Database=kitchen_recipe_checks;Username=checks;Password=checks";
var target = new NpgsqlConnectionStringBuilder(connection);
if (target.Host != "127.0.0.1" || target.Port != 15441 || target.Database != "kitchen_recipe_checks" || target.Username != "checks")
    throw new InvalidOperationException("Only the isolated localhost:15441 kitchen_recipe_checks database is allowed.");

await using var db = new GrimorioDbContext(new DbContextOptionsBuilder<GrimorioDbContext>().UseNpgsql(connection).Options);
await db.Database.MigrateAsync();
var checks = 0;
void Check(bool value, string scenario) { if (!value) throw new Exception(scenario); checks++; }
var branch = Guid.NewGuid();
var otherBranch = Guid.NewGuid();
var category = new MenuCategory { BranchId = branch, Name = "Kitchen recipe checks" };
var inventoryCategory = new InventoryCategory { BranchId = branch, Name = "Recipe ingredients" };
var gram = new MeasurementUnit { BranchId = branch, Name = "Gramo", Symbol = "g" };
var kg = new MeasurementUnit { BranchId = branch, Name = "Kilogramo", Symbol = "kg" };
var ml = new MeasurementUnit { BranchId = branch, Name = "Mililitro", Symbol = "ml" };
var chicken = new InventoryArticle { BranchId = branch, Name = "Alitas", CategoryId = inventoryCategory.Id, BaseUnitId = gram.Id };
var salt = new InventoryArticle { BranchId = branch, Name = "Sal", CategoryId = inventoryCategory.Id, BaseUnitId = gram.Id };
var station = new WorkStation { BranchId = branch, Name = "Fritos" };
var dish = new MenuItem { BranchId = branch, MenuCategoryId = category.Id, Name = "Alitas crispy", GeneralName = "Alitas crispy",
    ImageUrl = "/uploads/menu/recipe.webp", Price = 123m };
var combo = new MenuItem { BranchId = branch, MenuCategoryId = category.Id, Name = "Combo cocina", IsCombo = true };
var emptyDish = new MenuItem { BranchId = branch, MenuCategoryId = category.Id, Name = "Sin receta" };
var foreignDish = new MenuItem { BranchId = otherBranch, MenuCategoryId = category.Id, Name = "Receta privada" };
var sauce = new SubRecipe { BranchId = branch, Name = "Salsa base", OutputUnitId = kg.Id, OutputQuantity = 1 };
var order = new Order { BranchId = branch, Number = 1, Type = OrderType.DineIn, Status = OrderStatus.Confirmed };
var mainItem = new OrderItem { BranchId = branch, OrderId = order.Id, MenuItemId = dish.Id, Quantity = 3, Status = OrderItemStatus.Pending };
var comboItem = new OrderItem { BranchId = branch, OrderId = order.Id, MenuItemId = combo.Id, Quantity = 2, Status = OrderItemStatus.Pending };
var emptyItem = new OrderItem { BranchId = branch, OrderId = order.Id, MenuItemId = emptyDish.Id, Quantity = 1 };
var badLink = new OrderItem { BranchId = branch, OrderId = order.Id, MenuItemId = foreignDish.Id, Quantity = 1 };
var task = new OrderItemPreparationTask { BranchId = branch, OrderItemId = comboItem.Id, ComponentItemId = dish.Id,
    StationId = station.Id, ComponentName = dish.Name, ComboName = combo.Name, QuantityPerCombo = 1 };
var sauceIngredient = new RecipeIngredient { BranchId = branch, MenuItemId = dish.Id, Type = RecipeIngredientType.SubRecipe,
    SubRecipeId = sauce.Id, UnitId = gram.Id, Quantity = 20 };
var preparation = new MenuItemPreparation { BranchId = branch, MenuItemId = dish.Id, EstimatedMinutes = 12,
    Yield = "1 porcion", Presentation = "Servir con salsa aparte" };
db.AddRange(category, inventoryCategory, gram, kg, ml, chicken, salt, station, dish, combo, emptyDish,
    foreignDish, sauce, order, mainItem, comboItem, emptyItem, badLink, task, sauceIngredient, preparation);
db.AddRange(
    new UnitConversion { BranchId = branch, OriginUnitId = kg.Id, DestinationUnitId = gram.Id, Factor = 1000 },
    new RecipeIngredient { BranchId = branch, MenuItemId = dish.Id, ArticleId = chicken.Id, UnitId = gram.Id, Quantity = 250, Notes = "Porcion limpia" },
    new RecipeIngredient { BranchId = branch, MenuItemId = dish.Id, ArticleId = salt.Id, UnitId = gram.Id, Quantity = 99, IsDeleted = true },
    new SubRecipeIngredient { BranchId = branch, SubRecipeId = sauce.Id, ArticleId = salt.Id, UnitId = gram.Id, Quantity = 50 },
    new MenuItemPreparationStep { BranchId = branch, MenuItemPreparationId = preparation.Id, StepNumber = 2, Instructions = "Servir" },
    new MenuItemPreparationStep { BranchId = branch, MenuItemPreparationId = preparation.Id, StepNumber = 1, Instructions = "Freir", IsCritical = true },
    new MenuItemPreparationStep { BranchId = branch, MenuItemPreparationId = preparation.Id, StepNumber = 3, Instructions = "Obsoleto", IsDeleted = true });
await db.SaveChangesAsync();
db.ChangeTracker.Clear();
var handler = new GetKitchenRecipeQueryHandler(db);
async Task<Grimorio.Application.DTOs.KitchenRecipeDto?> Read(Guid id, Guid? scope = null) =>
    await handler.Handle(new GetKitchenRecipeQuery(scope ?? branch, id), CancellationToken.None);

var recipe = (await Read(mainItem.Id))!;
Check(recipe.Name == dish.Name && recipe.ImageUrl == dish.ImageUrl, "Recipe includes name and image");
Check(recipe.Ingredients.Count == 2 && recipe.Ingredients.Single(i => i.Name == "Alitas").Quantity == 250,
    "Ingredients are per portion, not multiplied by order quantity, excluding deleted rows");
Check(recipe.Ingredients.Single(i => i.Name == "Salsa base").Ingredients.Single().Quantity == 1,
    "Subrecipe ingredients are scaled using reverse unit conversion and batch yield");
Check(recipe.Preparation!.Steps.Select(s => s.StepNumber).SequenceEqual([1, 2]) && recipe.Preparation.Steps[0].IsCritical,
    "Preparation excludes deleted steps and preserves order and critical flags");
Check(!(JsonSerializer.Serialize(recipe).Contains("Price") || JsonSerializer.Serialize(recipe).Contains("Cost")), "Kitchen DTO has no prices or costs");
Check(!db.ChangeTracker.HasChanges(), "Reading a recipe does not change the order");
Check((await Read(task.Id))!.MenuItemId == dish.Id, "Combo task resolves the component recipe");
Check(await Read(mainItem.Id, otherBranch) is null, "Cannot read another branch's item");
Check(await Read(task.Id, otherBranch) is null, "Cannot read another branch's combo task");
Check(await Read(badLink.Id) is null, "Foreign menu reference is rejected even under a local order");
Check(await Read(Guid.NewGuid()) is null, "Unknown task returns not found");
Check((await Read(emptyItem.Id)) is { Ingredients.Count: 0, Preparation: null }, "Empty recipe returns a readable empty state");

await db.RecipeIngredients.Where(i => i.Id == sauceIngredient.Id)
    .ExecuteUpdateAsync(update => update.SetProperty(i => i.UnitId, ml.Id));
Check((await Read(mainItem.Id))!.Ingredients.Single(i => i.Name == "Salsa base").Ingredients.Single().Quantity is null,
    "Missing unit conversion does not invent subrecipe quantities");
await db.Orders.Where(o => o.Id == order.Id)
    .ExecuteUpdateAsync(update => update.SetProperty(o => o.Status, OrderStatus.Draft));
Check(await Read(mainItem.Id) is null && await Read(task.Id) is null, "Draft orders cannot expose recipes through kitchen tasks");
await db.Orders.Where(o => o.Id == order.Id)
    .ExecuteUpdateAsync(update => update.SetProperty(o => o.Status, OrderStatus.Cancelled));
Check(await Read(mainItem.Id) is null && await Read(task.Id) is null, "Cancelled orders are rejected");

var policies = typeof(PosController).GetMethod(nameof(PosController.GetKitchenRecipe))!
    .GetCustomAttributes<AuthorizeAttribute>().Select(a => a.Policy).ToList();
Check(policies.SequenceEqual(["POS.Kitchen.View"]), "Kitchen permission suffices without menu-management privileges");
Console.WriteLine($"Kitchen recipe checks passed: {checks}.");
