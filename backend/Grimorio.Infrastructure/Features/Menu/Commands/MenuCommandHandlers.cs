using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Inventory.Commands;
using Grimorio.Application.Features.Menu.Commands;
using Grimorio.Domain.Entities.Menu;
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
        var cat = new MenuCategory
        {
            BranchId = req.BranchId, Name = req.Name, Description = req.Description,
            Color = req.Color, Order = req.Order,
        };
        _db.MenuCategories.Add(cat);
        await _db.SaveChangesAsync(ct);
        return new MenuCategoryDto { Id = cat.Id, Name = cat.Name, Description = cat.Description, Color = cat.Color, Order = cat.Order, IsActive = cat.IsActive };
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
        cat.Name = req.Name; cat.Description = req.Description;
        cat.Color = req.Color; cat.Order = req.Order; cat.IsActive = req.IsActive;
        await _db.SaveChangesAsync(ct);
        return new MenuCategoryDto { Id = cat.Id, Name = cat.Name, Description = cat.Description, Color = cat.Color, Order = cat.Order, IsActive = cat.IsActive };
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
            InternalCode = req.InternalCode, Price = req.Price,
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
        item.Price = req.Price; item.IsActive = req.IsActive;
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
        var existentes = await _db.RecipeIngredients
            .Where(x => x.MenuItemId == req.MenuItemId && x.BranchId == req.BranchId)
            .ToListAsync(ct);
        foreach (var e in existentes) e.IsDeleted = true;

        foreach (var ing in req.Ingredients)
        {
            var recipeIng = new RecipeIngredient
            {
                BranchId = req.BranchId, MenuItemId = req.MenuItemId,
                ArticleId = ing.ArticleId, UnitId = ing.UnitId,
                Quantity = ing.Quantity, Notes = ing.Notes,
            };
            _db.RecipeIngredients.Add(recipeIng);
        }
        await _db.SaveChangesAsync(ct);

        var result = await _db.RecipeIngredients
            .Include(r => r.Article)
            .Include(r => r.Unit)
            .Where(r => r.MenuItemId == req.MenuItemId && r.BranchId == req.BranchId && !r.IsDeleted)
            .ToListAsync(ct);

        return result.Select(r => new RecipeIngredientDto
        {
            Id = r.Id, ArticleId = r.ArticleId,
            ArticleName = r.Article?.Name ?? string.Empty, InternalCode = r.Article?.InternalCode,
            UnitId = r.UnitId, UnitName = r.Unit?.Name ?? string.Empty,
            UnitSymbol = r.Unit?.Symbol ?? string.Empty, Quantity = r.Quantity, Notes = r.Notes,
        }).ToList();
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

public class DescontarStockVentaHandler : IRequestHandler<DeductStockFromSaleCommand, bool>
{
    private readonly GrimorioDbContext _db;
    private readonly IMediator _mediator;
    public DescontarStockVentaHandler(GrimorioDbContext db, IMediator mediator)
    { _db = db; _mediator = mediator; }

    public async Task<bool> Handle(DeductStockFromSaleCommand req, CancellationToken ct)
    {
        foreach (var saleItem in req.Items)
        {
            var recipe = await _db.RecipeIngredients
                .Where(r => r.MenuItemId == saleItem.MenuItemId && r.BranchId == req.BranchId && !r.IsDeleted)
                .ToListAsync(ct);

            foreach (var ingredient in recipe)
            {
                await _mediator.Send(new RegisterMovementCommand
                {
                    BranchId = req.BranchId,
                    ArticleId = ingredient.ArticleId,
                    WarehouseId = req.WarehouseId,
                    Type = Grimorio.Domain.Entities.Inventory.MovementType.SaleDeduction,
                    Quantity = ingredient.Quantity * saleItem.Quantity,
                    UnitId = ingredient.UnitId,
                    Reference = $"Venta item {saleItem.MenuItemId}",
                }, ct);
            }
        }
        return true;
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
            InternalCode = item.InternalCode, Price = item.Price,
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
}
