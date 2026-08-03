using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Menu.Commands;
using Grimorio.Domain.Entities.Menu;
using Grimorio.Infrastructure.Features.Menu;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Menu.Commands;

public class CreateMenuCategoryHandler : IRequestHandler<CreateMenuCategoryCommand, MenuCategoryDto>
{
    private readonly GrimorioDbContext _db;
    public CreateMenuCategoryHandler(GrimorioDbContext db) => _db = db;

    public async Task<MenuCategoryDto> Handle(CreateMenuCategoryCommand req, CancellationToken ct)
    {
        if (req.CostCenterId.HasValue)
        {
            var exists = await _db.CostCenters.AnyAsync(x => x.Id == req.CostCenterId.Value && x.BranchId == req.BranchId, ct);
            if (!exists) throw new InvalidOperationException("Centro de costo no válido.");
        }

        var cat = new MenuCategory
        {
            BranchId = req.BranchId, Name = req.Name, Description = req.Description,
            Color = req.Color, Order = req.Order, CostCenterId = req.CostCenterId,
        };
        _db.MenuCategories.Add(cat);
        await _db.SaveChangesAsync(ct);
        return new MenuCategoryDto { Id = cat.Id, Name = cat.Name, Description = cat.Description, Color = cat.Color, Order = cat.Order, IsActive = cat.IsActive, CostCenterId = cat.CostCenterId };
    }
}

public class UpdateMenuCategoryHandler : IRequestHandler<UpdateMenuCategoryCommand, MenuCategoryDto>
{
    private readonly GrimorioDbContext _db;
    public UpdateMenuCategoryHandler(GrimorioDbContext db) => _db = db;

    public async Task<MenuCategoryDto> Handle(UpdateMenuCategoryCommand req, CancellationToken ct)
    {
        var cat = await _db.MenuCategories.FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId, ct)
            ?? throw new KeyNotFoundException("Categoría no encontrada");
        if (req.CostCenterId.HasValue)
        {
            var exists = await _db.CostCenters.AnyAsync(x => x.Id == req.CostCenterId.Value && x.BranchId == req.BranchId, ct);
            if (!exists) throw new InvalidOperationException("Centro de costo no válido.");
        }

        cat.Name = req.Name; cat.Description = req.Description;
        cat.Color = req.Color; cat.Order = req.Order; cat.IsActive = req.IsActive;
        cat.CostCenterId = req.CostCenterId;
        await _db.SaveChangesAsync(ct);
        var costCenterName = cat.CostCenterId.HasValue
            ? (await _db.CostCenters.FindAsync([cat.CostCenterId.Value], ct))?.Name
            : null;
        return new MenuCategoryDto { Id = cat.Id, Name = cat.Name, Description = cat.Description, Color = cat.Color, Order = cat.Order, IsActive = cat.IsActive, CostCenterId = cat.CostCenterId, CostCenterName = costCenterName };
    }
}

public class DeleteMenuCategoryHandler : IRequestHandler<DeleteMenuCategoryCommand, bool>
{
    private readonly GrimorioDbContext _db;
    public DeleteMenuCategoryHandler(GrimorioDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteMenuCategoryCommand req, CancellationToken ct)
    {
        var cat = await _db.MenuCategories.FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId, ct)
            ?? throw new KeyNotFoundException("Categoría no encontrada");
        cat.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public class CreateItemMenuHandler : IRequestHandler<CreateMenuItemCommand, MenuItemDto>
{
    private readonly GrimorioDbContext _db;
    public CreateItemMenuHandler(GrimorioDbContext db) => _db = db;

    public async Task<MenuItemDto> Handle(CreateMenuItemCommand req, CancellationToken ct)
    {
        var item = new MenuItem
        {
            BranchId = req.BranchId, MenuCategoryId = req.MenuCategoryId,
            Name = req.Name, Description = req.Description,
            InternalCode = req.InternalCode, ImageUrl = req.ImageUrl, Price = req.Price,
            StationId = req.StationId, TaxRateId = req.TaxRateId,
        };
        _db.MenuItems.Add(item);
        await _db.SaveChangesAsync(ct);

        var cat = await _db.MenuCategories.FindAsync([req.MenuCategoryId], ct);
        var est = req.StationId.HasValue ? await _db.WorkStations.FindAsync([req.StationId.Value], ct) : null;
        var tax = req.TaxRateId.HasValue ? await _db.TaxRates.FindAsync([req.TaxRateId.Value], ct) : null;
        return MenuMapper.MapItem(item, cat?.Name ?? string.Empty, cat?.Color, 0, est?.Name, tax);
    }
}

public class UpdateItemMenuHandler : IRequestHandler<UpdateMenuItemCommand, MenuItemDto>
{
    private readonly GrimorioDbContext _db;
    public UpdateItemMenuHandler(GrimorioDbContext db) => _db = db;

    public async Task<MenuItemDto> Handle(UpdateMenuItemCommand req, CancellationToken ct)
    {
        var item = await _db.MenuItems
            .Include(x => x.Category)
            .Include(x => x.Station)
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId, ct)
            ?? throw new KeyNotFoundException("Item no encontrado");

        item.MenuCategoryId = req.MenuCategoryId; item.Name = req.Name;
        item.Description = req.Description; item.InternalCode = req.InternalCode;
        item.ImageUrl = req.ImageUrl; item.Price = req.Price; item.IsActive = req.IsActive;
        item.AvailableForSale = req.AvailableForSale;
        item.StationId = req.StationId;
        item.TaxRateId = req.TaxRateId;
        await _db.SaveChangesAsync(ct);

        string? stationName = item.StationId.HasValue
            ? (await _db.WorkStations.FindAsync([item.StationId.Value], ct))?.Name
            : null;
        var taxRate = item.TaxRateId.HasValue ? await _db.TaxRates.FindAsync([item.TaxRateId.Value], ct) : null;
        return MenuMapper.MapItem(item, item.Category?.Name ?? string.Empty, item.Category?.Color, 0, stationName, taxRate);
    }
}

public class UpdateMenuItemImageHandler : IRequestHandler<UpdateMenuItemImageCommand, MenuItemDto>
{
    private readonly GrimorioDbContext _db;
    public UpdateMenuItemImageHandler(GrimorioDbContext db) => _db = db;

    public async Task<MenuItemDto> Handle(UpdateMenuItemImageCommand req, CancellationToken ct)
    {
        var item = await _db.MenuItems
            .Include(x => x.Category)
            .Include(x => x.Station)
            .Include(x => x.TaxRate)
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId, ct)
            ?? throw new KeyNotFoundException("Item no encontrado");

        item.ImageUrl = req.ImageUrl;
        await _db.SaveChangesAsync(ct);

        return MenuMapper.MapItem(
            item,
            item.Category?.Name ?? string.Empty,
            item.Category?.Color,
            item.Recipe.Count(r => !r.IsDeleted),
            item.Station?.Name,
            item.TaxRate);
    }
}

public class DeleteItemMenuHandler : IRequestHandler<DeleteMenuItemCommand, bool>
{
    private readonly GrimorioDbContext _db;
    public DeleteItemMenuHandler(GrimorioDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteMenuItemCommand req, CancellationToken ct)
    {
        var item = await _db.MenuItems.FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId, ct)
            ?? throw new KeyNotFoundException("Item no encontrado");
        item.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public class UpsertRecipeHandler : IRequestHandler<UpsertRecipeCommand, List<RecipeIngredientDto>>
{
    private readonly GrimorioDbContext _db;
    public UpsertRecipeHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<RecipeIngredientDto>> Handle(UpsertRecipeCommand req, CancellationToken ct)
    {
        var itemExists = await _db.MenuItems.AnyAsync(x => x.Id == req.MenuItemId && x.BranchId == req.BranchId && !x.IsDeleted, ct);
        if (!itemExists) throw new KeyNotFoundException("Item no encontrado");

        var existentes = await _db.RecipeIngredients
            .Where(x => x.MenuItemId == req.MenuItemId && x.BranchId == req.BranchId)
            .ToListAsync(ct);
        foreach (var e in existentes) e.IsDeleted = true;

        foreach (var ing in req.Ingredients)
        {
            if (ing.Quantity <= 0) throw new InvalidOperationException("La cantidad de la receta debe ser mayor a cero.");
            var type = Enum.TryParse<RecipeIngredientType>(ing.Type, true, out var parsedType)
                ? parsedType
                : RecipeIngredientType.Article;

            if (type == RecipeIngredientType.Article && !ing.ArticleId.HasValue)
                throw new InvalidOperationException("Seleccione un artículo para el ingrediente.");
            if (type == RecipeIngredientType.SubRecipe && !ing.SubRecipeId.HasValue)
                throw new InvalidOperationException("Seleccione una subreceta.");

            var recipeIng = new RecipeIngredient
            {
                BranchId = req.BranchId, MenuItemId = req.MenuItemId,
                Type = type,
                ArticleId = type == RecipeIngredientType.Article ? ing.ArticleId : null,
                SubRecipeId = type == RecipeIngredientType.SubRecipe ? ing.SubRecipeId : null,
                UnitId = ing.UnitId,
                Quantity = ing.Quantity, Notes = ing.Notes,
            };
            _db.RecipeIngredients.Add(recipeIng);
        }
        await _db.SaveChangesAsync(ct);

        var result = await _db.RecipeIngredients
            .Include(r => r.Article)
            .Include(r => r.SubRecipe)
            .Include(r => r.Unit)
            .Where(r => r.MenuItemId == req.MenuItemId && r.BranchId == req.BranchId && !r.IsDeleted)
            .ToListAsync(ct);

        return result.Select(MenuMapper.MapRecipeIngredient).ToList();
    }
}

public class UpsertSubRecipeHandler : IRequestHandler<UpsertSubRecipeCommand, SubRecipeDto>
{
    private readonly GrimorioDbContext _db;
    public UpsertSubRecipeHandler(GrimorioDbContext db) => _db = db;

    public async Task<SubRecipeDto> Handle(UpsertSubRecipeCommand req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.SubRecipe.Name))
            throw new InvalidOperationException("La subreceta requiere nombre.");
        if (req.SubRecipe.OutputQuantity <= 0)
            throw new InvalidOperationException("El rendimiento debe ser mayor a cero.");
        if (req.SubRecipe.Ingredients.Count == 0)
            throw new InvalidOperationException("La subreceta requiere al menos un insumo.");

        var subRecipe = req.Id.HasValue
            ? await _db.SubRecipes
                .Include(x => x.Ingredients)
                .FirstOrDefaultAsync(x => x.Id == req.Id.Value && x.BranchId == req.BranchId, ct)
            : null;

        if (req.Id.HasValue && subRecipe is null)
            throw new KeyNotFoundException("Subreceta no encontrada");

        if (subRecipe is null)
        {
            subRecipe = new SubRecipe { BranchId = req.BranchId };
            _db.SubRecipes.Add(subRecipe);
        }

        subRecipe.IsDeleted = false;
        subRecipe.Name = req.SubRecipe.Name.Trim();
        subRecipe.Description = req.SubRecipe.Description?.Trim();
        subRecipe.OutputQuantity = req.SubRecipe.OutputQuantity;
        subRecipe.OutputUnitId = req.SubRecipe.OutputUnitId;
        subRecipe.IsActive = req.SubRecipe.IsActive;

        foreach (var existing in subRecipe.Ingredients.Where(x => !x.IsDeleted))
            existing.IsDeleted = true;

        foreach (var ingredient in req.SubRecipe.Ingredients)
        {
            if (ingredient.Quantity <= 0)
                throw new InvalidOperationException("La cantidad de los insumos debe ser mayor a cero.");

            _db.SubRecipeIngredients.Add(new SubRecipeIngredient
            {
                BranchId = req.BranchId,
                SubRecipeId = subRecipe.Id,
                ArticleId = ingredient.ArticleId,
                UnitId = ingredient.UnitId,
                Quantity = ingredient.Quantity,
                Notes = ingredient.Notes?.Trim(),
            });
        }

        await _db.SaveChangesAsync(ct);

        var result = await _db.SubRecipes
            .AsNoTracking()
            .Include(x => x.OutputUnit)
            .Include(x => x.Ingredients.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Article)
            .Include(x => x.Ingredients.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Unit)
            .FirstAsync(x => x.Id == subRecipe.Id, ct);

        return MenuMapper.MapSubRecipe(result);
    }
}

public class DeleteSubRecipeHandler : IRequestHandler<DeleteSubRecipeCommand, bool>
{
    private readonly GrimorioDbContext _db;
    public DeleteSubRecipeHandler(GrimorioDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteSubRecipeCommand req, CancellationToken ct)
    {
        var inUse = await _db.RecipeIngredients.AnyAsync(x =>
            x.BranchId == req.BranchId && !x.IsDeleted && x.SubRecipeId == req.Id, ct);
        if (inUse) throw new InvalidOperationException("No se puede eliminar una subreceta usada en platos.");

        var subRecipe = await _db.SubRecipes.FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId, ct)
            ?? throw new KeyNotFoundException("Subreceta no encontrada");
        subRecipe.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public class DeleteRecipeIngredientHandler : IRequestHandler<DeleteRecipeIngredientCommand, bool>
{
    private readonly GrimorioDbContext _db;
    public DeleteRecipeIngredientHandler(GrimorioDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteRecipeIngredientCommand req, CancellationToken ct)
    {
        var ing = await _db.RecipeIngredients.FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId, ct)
            ?? throw new KeyNotFoundException("Ingrediente no encontrado");
        ing.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public class UpsertMenuItemModifiersHandler : IRequestHandler<UpsertMenuItemModifiersCommand, List<MenuItemModifierGroupDto>>
{
    private readonly GrimorioDbContext _db;
    public UpsertMenuItemModifiersHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<MenuItemModifierGroupDto>> Handle(UpsertMenuItemModifiersCommand req, CancellationToken ct)
    {
        var itemExists = await _db.MenuItems
            .AnyAsync(x => x.Id == req.MenuItemId && x.BranchId == req.BranchId && !x.IsDeleted, ct);
        if (!itemExists)
            throw new KeyNotFoundException("Item no encontrado");

        foreach (var groupDto in req.Groups)
        {
            if (string.IsNullOrWhiteSpace(groupDto.Name))
                throw new InvalidOperationException("El grupo de opciones requiere nombre.");
            if (groupDto.MaxSelections < 1)
                throw new InvalidOperationException("El máximo de selecciones debe ser mayor a cero.");
            if (groupDto.MinSelections < 0 || groupDto.MinSelections > groupDto.MaxSelections)
                throw new InvalidOperationException("El mínimo de selecciones no puede superar el máximo.");
            if (groupDto.IsRequired && groupDto.MinSelections == 0)
                groupDto.MinSelections = 1;
            if (groupDto.Options.Count == 0)
                throw new InvalidOperationException($"El grupo {groupDto.Name} requiere opciones.");
        }

        var existingGroups = await _db.MenuItemModifierGroups
            .Include(g => g.Options)
            .Where(g => g.MenuItemId == req.MenuItemId && g.BranchId == req.BranchId && !g.IsDeleted)
            .ToListAsync(ct);

        foreach (var existing in existingGroups)
        {
            existing.IsDeleted = true;
            foreach (var option in existing.Options.Where(o => !o.IsDeleted))
                option.IsDeleted = true;
        }

        foreach (var groupDto in req.Groups)
        {
            var group = new MenuItemModifierGroup
            {
                BranchId = req.BranchId,
                MenuItemId = req.MenuItemId,
                Name = groupDto.Name.Trim(),
                MinSelections = groupDto.MinSelections,
                MaxSelections = groupDto.MaxSelections,
                IsRequired = groupDto.IsRequired,
                AllowDuplicates = groupDto.AllowDuplicates,
                DisplayOrder = groupDto.DisplayOrder,
                IsActive = groupDto.IsActive,
            };
            _db.MenuItemModifierGroups.Add(group);

            foreach (var optionDto in groupDto.Options)
            {
                if (string.IsNullOrWhiteSpace(optionDto.Name))
                    throw new InvalidOperationException($"Una opción del grupo {groupDto.Name} no tiene nombre.");
                if (optionDto.ArticleId.HasValue && (!optionDto.UnitId.HasValue || optionDto.Quantity <= 0))
                    throw new InvalidOperationException($"La opción {optionDto.Name} requiere unidad y cantidad para inventario.");

                _db.MenuItemModifierOptions.Add(new MenuItemModifierOption
                {
                    BranchId = req.BranchId,
                    ModifierGroupId = group.Id,
                    Name = optionDto.Name.Trim(),
                    ArticleId = optionDto.ArticleId,
                    UnitId = optionDto.UnitId,
                    Quantity = optionDto.Quantity,
                    PriceDelta = optionDto.PriceDelta,
                    DisplayOrder = optionDto.DisplayOrder,
                    IsActive = optionDto.IsActive,
                });
            }
        }

        await _db.SaveChangesAsync(ct);

        var groups = await _db.MenuItemModifierGroups
            .AsNoTracking()
            .Include(g => g.Options.Where(o => !o.IsDeleted))
                .ThenInclude(o => o.Article)
            .Include(g => g.Options.Where(o => !o.IsDeleted))
                .ThenInclude(o => o.Unit)
            .Where(g => g.MenuItemId == req.MenuItemId && g.BranchId == req.BranchId && !g.IsDeleted)
            .OrderBy(g => g.DisplayOrder).ThenBy(g => g.Name)
            .ToListAsync(ct);

        return MenuMapper.MapModifierGroups(groups);
    }
}

public class UpsertMenuItemPreparationHandler : IRequestHandler<UpsertMenuItemPreparationCommand, MenuItemPreparationDto>
{
    private readonly GrimorioDbContext _db;
    public UpsertMenuItemPreparationHandler(GrimorioDbContext db) => _db = db;

    public async Task<MenuItemPreparationDto> Handle(UpsertMenuItemPreparationCommand req, CancellationToken ct)
    {
        var itemExists = await _db.MenuItems
            .AnyAsync(x => x.Id == req.MenuItemId && x.BranchId == req.BranchId && !x.IsDeleted, ct);
        if (!itemExists)
            throw new KeyNotFoundException("Item no encontrado");

        var validSteps = req.Preparation.Steps
            .Where(x => !string.IsNullOrWhiteSpace(x.Instructions))
            .OrderBy(x => x.StepNumber)
            .ToList();

        for (var i = 0; i < validSteps.Count; i++)
            validSteps[i].StepNumber = i + 1;

        var preparation = await _db.MenuItemPreparations
            .Include(x => x.Steps)
            .FirstOrDefaultAsync(x => x.MenuItemId == req.MenuItemId && x.BranchId == req.BranchId, ct);

        if (preparation is null)
        {
            preparation = new MenuItemPreparation
            {
                BranchId = req.BranchId,
                MenuItemId = req.MenuItemId,
            };
            _db.MenuItemPreparations.Add(preparation);
        }

        preparation.IsDeleted = false;
        preparation.EstimatedMinutes = req.Preparation.EstimatedMinutes;
        preparation.Yield = req.Preparation.Yield?.Trim();
        preparation.Temperature = req.Preparation.Temperature?.Trim();
        preparation.Presentation = req.Preparation.Presentation?.Trim();
        preparation.Notes = req.Preparation.Notes?.Trim();

        foreach (var existingStep in preparation.Steps.Where(x => !x.IsDeleted))
            existingStep.IsDeleted = true;

        foreach (var stepDto in validSteps)
        {
            _db.MenuItemPreparationSteps.Add(new MenuItemPreparationStep
            {
                BranchId = req.BranchId,
                MenuItemPreparationId = preparation.Id,
                StepNumber = stepDto.StepNumber,
                Title = stepDto.Title?.Trim(),
                Instructions = stepDto.Instructions.Trim(),
                EstimatedMinutes = stepDto.EstimatedMinutes,
                Temperature = stepDto.Temperature?.Trim(),
                IsCritical = stepDto.IsCritical,
            });
        }

        await _db.SaveChangesAsync(ct);

        var result = await _db.MenuItemPreparations
            .AsNoTracking()
            .Include(x => x.Steps.Where(s => !s.IsDeleted))
            .FirstAsync(x => x.Id == preparation.Id, ct);

        return MenuMapper.MapPreparation(result);
    }
}

internal static class MenuMapper
{
    internal static MenuItemDto MapItem(MenuItem item, string categoryName, string? categoriaColor,
        int totalIngredients = 0, string? stationName = null,
        Grimorio.Domain.Entities.Billing.TaxRate? taxRate = null) =>
        new()
        {
            Id = item.Id, MenuCategoryId = item.MenuCategoryId,
            CategoryName = categoryName, CategoryColor = categoriaColor,
            Name = item.Name, Description = item.Description,
            InternalCode = item.InternalCode, ImageUrl = item.ImageUrl, Price = item.Price,
            IsActive = item.IsActive, AvailableForSale = item.AvailableForSale,
            TotalIngredients = totalIngredients,
            StationId = item.StationId, StationName = stationName,
            TaxRateId = item.TaxRateId,
            TaxRateName = taxRate?.Name ?? item.TaxRate?.Name,
            TaxRatePercentage = taxRate?.Percentage ?? item.TaxRate?.Percentage,
            TaxRateSriCode = taxRate?.SriCode ?? item.TaxRate?.SriCode,
            HasModifiers = item.ModifierGroups.Any(g => !g.IsDeleted && g.IsActive),
            ModifierGroups = MapModifierGroups(item.ModifierGroups
                .Where(g => !g.IsDeleted && g.IsActive)
                .OrderBy(g => g.DisplayOrder)
                .ThenBy(g => g.Name)
                .ToList()),
        };

    internal static List<MenuItemModifierGroupDto> MapModifierGroups(IEnumerable<MenuItemModifierGroup> groups) =>
        groups.Select(g => new MenuItemModifierGroupDto
        {
            Id = g.Id,
            MenuItemId = g.MenuItemId,
            Name = g.Name,
            MinSelections = g.MinSelections,
            MaxSelections = g.MaxSelections,
            IsRequired = g.IsRequired,
            AllowDuplicates = g.AllowDuplicates,
            DisplayOrder = g.DisplayOrder,
            IsActive = g.IsActive,
            Options = g.Options
                .Where(o => !o.IsDeleted && o.IsActive)
                .OrderBy(o => o.DisplayOrder)
                .ThenBy(o => o.Name)
                .Select(o => new MenuItemModifierOptionDto
                {
                    Id = o.Id,
                    ModifierGroupId = o.ModifierGroupId,
                    Name = o.Name,
                    ArticleId = o.ArticleId,
                    ArticleName = o.Article?.Name,
                    UnitId = o.UnitId,
                    UnitName = o.Unit?.Name,
                    UnitSymbol = o.Unit?.Symbol,
                    Quantity = o.Quantity,
                    PriceDelta = o.PriceDelta,
                    DisplayOrder = o.DisplayOrder,
                    IsActive = o.IsActive,
                }).ToList(),
        }).ToList();

    internal static MenuItemPreparationDto MapPreparation(MenuItemPreparation preparation) =>
        new()
        {
            Id = preparation.Id,
            MenuItemId = preparation.MenuItemId,
            EstimatedMinutes = preparation.EstimatedMinutes,
            Yield = preparation.Yield,
            Temperature = preparation.Temperature,
            Presentation = preparation.Presentation,
            Notes = preparation.Notes,
            Steps = preparation.Steps
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.StepNumber)
                .Select(s => new MenuItemPreparationStepDto
                {
                    Id = s.Id,
                    StepNumber = s.StepNumber,
                    Title = s.Title,
                    Instructions = s.Instructions,
                    EstimatedMinutes = s.EstimatedMinutes,
                    Temperature = s.Temperature,
                    IsCritical = s.IsCritical,
                })
                .ToList(),
        };

    internal static RecipeIngredientDto MapRecipeIngredient(RecipeIngredient r) =>
        new()
        {
            Id = r.Id,
            Type = r.Type.ToString(),
            ArticleId = r.ArticleId,
            ArticleName = r.Article?.Name ?? string.Empty,
            InternalCode = r.Article?.InternalCode,
            SubRecipeId = r.SubRecipeId,
            SubRecipeName = r.SubRecipe?.Name,
            UnitId = r.UnitId,
            UnitName = r.Unit?.Name ?? string.Empty,
            UnitSymbol = r.Unit?.Symbol ?? string.Empty,
            Quantity = r.Quantity,
            Notes = r.Notes,
        };

    internal static SubRecipeDto MapSubRecipe(SubRecipe subRecipe) =>
        new()
        {
            Id = subRecipe.Id,
            Name = subRecipe.Name,
            Description = subRecipe.Description,
            OutputQuantity = subRecipe.OutputQuantity,
            OutputUnitId = subRecipe.OutputUnitId,
            OutputUnitName = subRecipe.OutputUnit?.Name ?? string.Empty,
            OutputUnitSymbol = subRecipe.OutputUnit?.Symbol ?? string.Empty,
            IsActive = subRecipe.IsActive,
            Ingredients = subRecipe.Ingredients
                .Where(i => !i.IsDeleted)
                .Select(i => new SubRecipeIngredientDto
                {
                    Id = i.Id,
                    ArticleId = i.ArticleId,
                    ArticleName = i.Article?.Name ?? string.Empty,
                    InternalCode = i.Article?.InternalCode,
                    UnitId = i.UnitId,
                    UnitName = i.Unit?.Name ?? string.Empty,
                    UnitSymbol = i.Unit?.Symbol ?? string.Empty,
                    Quantity = i.Quantity,
                    Notes = i.Notes,
                })
                .ToList(),
        };
}
