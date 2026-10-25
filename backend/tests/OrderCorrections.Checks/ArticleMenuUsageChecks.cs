using Grimorio.Application.Features.Inventory.Queries;
using Grimorio.Domain.Entities.Inventory;
using Grimorio.Domain.Entities.Menu;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Infrastructure.Features.Inventory.Queries;
using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

internal static class ArticleMenuUsageChecks
{
    public static async Task Run()
    {
        var connection = Environment.GetEnvironmentVariable("CORRECTION_CHECKS_CONNECTION")
            ?? throw new InvalidOperationException("Set CORRECTION_CHECKS_CONNECTION.");
        ChecksDatabaseTarget.Validate(new NpgsqlConnectionStringBuilder(connection));
        await using var db = new GrimorioDbContext(new DbContextOptionsBuilder<GrimorioDbContext>().UseNpgsql(connection).Options);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var branch = new Branch { Name = "Article usage checks", Code = "USAGE" };
        var other = new Branch { Name = "Other branch", Code = "OTHER" };
        var unit = new MeasurementUnit { BranchId = branch.Id, Name = "Unit", Symbol = "un" };
        var category = new InventoryCategory { BranchId = branch.Id, Name = "Ingredients" };
        var article = new InventoryArticle { BranchId = branch.Id, Name = "Cheddar", CategoryId = category.Id, BaseUnitId = unit.Id };
        var unused = new InventoryArticle { BranchId = branch.Id, Name = "Unused", CategoryId = category.Id, BaseUnitId = unit.Id };
        var menuCategory = new MenuCategory { BranchId = branch.Id, Name = "Menu" };
        MenuItem Item(string name) => new() { BranchId = branch.Id, MenuCategoryId = menuCategory.Id, Name = name };
        var direct = Item("Burger");
        var indirect = Item("Potatoes");
        var modified = Item("Sandwich");
        var combo = Item("Combo"); combo.IsCombo = true;
        var nested = Item("Nested combo"); nested.IsCombo = true;
        var deleted = Item("Deleted"); deleted.IsDeleted = true;
        var inactive = Item("Inactive"); inactive.IsActive = false;
        var sub = new SubRecipe { BranchId = branch.Id, Name = "Cheese sauce", OutputUnitId = unit.Id };
        RecipeIngredient Recipe(MenuItem item) => new() { BranchId = branch.Id, MenuItemId = item.Id, ArticleId = article.Id, UnitId = unit.Id, Quantity = 1 };
        var removed = Recipe(modified); removed.IsDeleted = true;
        var group = new MenuItemModifierGroup { BranchId = branch.Id, MenuItemId = modified.Id, Name = "Extras" };
        var option = new MenuItemModifierOption { BranchId = branch.Id, ModifierGroupId = group.Id, Name = "Double cheese",
            AdditionalIngredients = [new(article.Id, unit.Id, 1)] };
        db.AddRange(branch, other, unit, category, article, unused, menuCategory, direct, indirect, modified, combo, nested, deleted, inactive, sub, group, option);
        db.AddRange(Recipe(direct), Recipe(deleted), Recipe(inactive), removed,
            new SubRecipeIngredient { BranchId = branch.Id, SubRecipeId = sub.Id, ArticleId = article.Id, UnitId = unit.Id, Quantity = 1 },
            new RecipeIngredient { BranchId = branch.Id, MenuItemId = indirect.Id, Type = RecipeIngredientType.SubRecipe, SubRecipeId = sub.Id, UnitId = unit.Id, Quantity = 1 },
            new MenuComboComponent { BranchId = branch.Id, ComboId = combo.Id, ComponentItemId = direct.Id },
            new MenuComboComponent { BranchId = branch.Id, ComboId = nested.Id, ComponentItemId = combo.Id });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var handler = new GetArticleMenuUsageHandler(db);
        var result = await handler.Handle(new() { BranchId = branch.Id, ArticleId = article.Id }, default)
            ?? throw new Exception("Article missing");
        var count = 0;
        void Check(bool pass, string label) { if (!pass) throw new Exception(label); count++; }
        Check(result.Count == 6, "Unique plates, including inactive and nested combos");
        Check(result.Single(i => i.MenuItemId == direct.Id).Usages.SequenceEqual(["Receta"]), "Direct recipe");
        Check(result.Single(i => i.MenuItemId == indirect.Id).Usages.Contains("Subreceta: Cheese sauce"), "Subrecipe");
        Check(result.Single(i => i.MenuItemId == modified.Id).Usages.SequenceEqual(["Opcion: Extras / Double cheese"]), "Additional modifier consumption; deleted recipe ignored");
        Check(result.Single(i => i.MenuItemId == combo.Id).Usages.Contains("Componente: Burger"), "Combo");
        Check(result.Single(i => i.MenuItemId == nested.Id).Usages.Contains("Componente: Combo"), "Nested combo");
        Check(!result.Single(i => i.MenuItemId == inactive.Id).IsActive, "Inactive status");
        Check(result.All(i => i.MenuItemId != deleted.Id), "Deleted item excluded");
        Check((await handler.Handle(new() { BranchId = branch.Id, ArticleId = unused.Id }, default))?.Count == 0, "Unused article");
        Check(await handler.Handle(new() { BranchId = other.Id, ArticleId = article.Id }, default) is null, "Cross-branch article denied");
        Check(await handler.Handle(new() { BranchId = branch.Id, ArticleId = Guid.NewGuid() }, default) is null, "Missing article");
        await transaction.RollbackAsync();
        Console.WriteLine($"{count} article menu usage checks passed; existing PostgreSQL container, rolled back all test data.");
    }
}
