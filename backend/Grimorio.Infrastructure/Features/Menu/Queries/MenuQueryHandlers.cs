using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Menu.Queries;
using Grimorio.Domain.Entities.Menu;
using Grimorio.Domain.Entities.Purchases;
using Grimorio.Infrastructure.Features.Menu.Commands;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Menu.Queries;

public class GetMenuCategoriesHandler : IRequestHandler<GetMenuCategoriesQuery, List<MenuCategoryDto>>
{
    private readonly GrimorioDbContext _db;
    public GetMenuCategoriesHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<MenuCategoryDto>> Handle(GetMenuCategoriesQuery req, CancellationToken ct) =>
        await _db.MenuCategories
            .Where(x => x.BranchId == req.BranchId)
            .OrderBy(x => x.Order).ThenBy(x => x.Name)
            .Select(x => new MenuCategoryDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Color = x.Color,
                Order = x.Order,
                IsActive = x.IsActive,
                TotalItems = x.Items.Count(i => !i.IsDeleted),
            })
            .ToListAsync(ct);
}

public class GetItemsMenuHandler : IRequestHandler<GetMenuItemsQuery, List<MenuItemDto>>
{
    private readonly GrimorioDbContext _db;
    public GetItemsMenuHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<MenuItemDto>> Handle(GetMenuItemsQuery req, CancellationToken ct)
    {
        var query = _db.MenuItems
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Station)
            .Include(x => x.TaxRate)
            .Include(x => x.Recipe.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.Article)
            .Include(x => x.Recipe.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.Unit)
            .Include(x => x.ModifierGroups.Where(g => !g.IsDeleted && g.IsActive))
                .ThenInclude(g => g.Options.Where(o => !o.IsDeleted && o.IsActive))
                    .ThenInclude(o => o.Article)
            .Include(x => x.ModifierGroups.Where(g => !g.IsDeleted && g.IsActive))
                .ThenInclude(g => g.Options.Where(o => !o.IsDeleted && o.IsActive))
                    .ThenInclude(o => o.Unit)
            .Where(x => x.BranchId == req.BranchId);

        if (req.CategoryId.HasValue) query = query.Where(x => x.MenuCategoryId == req.CategoryId.Value);
        if (req.ActiveOnly == true) query = query.Where(x => x.IsActive);
        if (req.AvailableOnly == true) query = query.Where(x => x.AvailableForSale);

        if (req.Lightweight)
        {
            return await query
                .OrderBy(x => x.Category!.Order)
                .ThenBy(x => x.Name)
                .Select(x => new MenuItemDto
                {
                    Id = x.Id,
                    MenuCategoryId = x.MenuCategoryId,
                    CategoryName = x.Category != null ? x.Category.Name : string.Empty,
                    CategoryColor = x.Category != null ? x.Category.Color : null,
                    Name = x.Name,
                    Description = x.Description,
                    InternalCode = x.InternalCode,
                    Price = x.Price,
                    IsActive = x.IsActive,
                    AvailableForSale = x.AvailableForSale,
                    TotalIngredients = x.Recipe.Count(r => !r.IsDeleted),
                    StationId = x.StationId,
                    StationName = x.Station != null ? x.Station.Name : null,
                    TaxRateId = x.TaxRateId,
                    TaxRateName = x.TaxRate != null ? x.TaxRate.Name : null,
                    TaxRatePercentage = x.TaxRate != null ? x.TaxRate.Percentage : null,
                    TaxRateSriCode = x.TaxRate != null ? x.TaxRate.SriCode : null,
                    HasModifiers = x.ModifierGroups.Any(g => !g.IsDeleted && g.IsActive),
                })
                .ToListAsync(ct);
        }

        var items = await query
            .AsSplitQuery()
            .OrderBy(x => x.Category!.Order).ThenBy(x => x.Name)
            .ToListAsync(ct);

        return items.Select(x => new MenuItemDto
        {
            Id = x.Id,
            MenuCategoryId = x.MenuCategoryId,
            CategoryName = x.Category?.Name ?? string.Empty,
            CategoryColor = x.Category?.Color,
            Name = x.Name,
            Description = x.Description,
            InternalCode = x.InternalCode,
            Price = x.Price,
            IsActive = x.IsActive,
            AvailableForSale = x.AvailableForSale,
            TotalIngredients = x.Recipe.Count(r => !r.IsDeleted),
            StationId = x.StationId,
            StationName = x.Station?.Name,
            TaxRateId = x.TaxRateId,
            TaxRateName = x.TaxRate?.Name,
            TaxRatePercentage = x.TaxRate?.Percentage,
            TaxRateSriCode = x.TaxRate?.SriCode,
            HasModifiers = x.ModifierGroups.Any(g => !g.IsDeleted && g.IsActive),
            ModifierGroups = MenuMapper.MapModifierGroups(x.ModifierGroups
                .Where(g => !g.IsDeleted && g.IsActive)
                .OrderBy(g => g.DisplayOrder)
                .ThenBy(g => g.Name)
                .ToList()),
        }).ToList();
    }
}

public class GetItemMenuDetalleHandler : IRequestHandler<GetMenuItemDetailQuery, MenuItemDetailDto?>
{
    private readonly GrimorioDbContext _db;
    public GetItemMenuDetalleHandler(GrimorioDbContext db) => _db = db;

    public async Task<MenuItemDetailDto?> Handle(GetMenuItemDetailQuery req, CancellationToken ct)
    {
        var item = await _db.MenuItems
            .Include(x => x.Category)
            .Include(x => x.Station)
            .Include(x => x.Recipe.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.Article)
            .Include(x => x.Recipe.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.Unit)
            .Include(x => x.ModifierGroups.Where(g => !g.IsDeleted && g.IsActive))
                .ThenInclude(g => g.Options.Where(o => !o.IsDeleted && o.IsActive))
                    .ThenInclude(o => o.Article)
                        .ThenInclude(a => a!.BaseUnit)
            .Include(x => x.ModifierGroups.Where(g => !g.IsDeleted && g.IsActive))
                .ThenInclude(g => g.Options.Where(o => !o.IsDeleted && o.IsActive))
                    .ThenInclude(o => o.Unit)
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId, ct);

        if (item is null) return null;
        var modifierAvailability = await BuildModifierAvailabilityAsync(item, req.BranchId, ct);

        var dto = new MenuItemDetailDto
        {
            Id = item.Id,
            MenuCategoryId = item.MenuCategoryId,
            CategoryName = item.Category?.Name ?? string.Empty,
            CategoryColor = item.Category?.Color,
            Name = item.Name,
            Description = item.Description,
            InternalCode = item.InternalCode,
            Price = item.Price,
            IsActive = item.IsActive,
            AvailableForSale = item.AvailableForSale,
            TotalIngredients = item.Recipe.Count,
            StationId = item.StationId,
            StationName = item.Station?.Name,
            TaxRateId = item.TaxRateId,
            TaxRateName = item.TaxRate?.Name,
            TaxRatePercentage = item.TaxRate?.Percentage,
            TaxRateSriCode = item.TaxRate?.SriCode,
            HasModifiers = item.ModifierGroups.Any(g => !g.IsDeleted && g.IsActive),
            ModifierGroups = MenuMapper.MapModifierGroups(item.ModifierGroups
                .Where(g => !g.IsDeleted && g.IsActive)
                .OrderBy(g => g.DisplayOrder)
                .ThenBy(g => g.Name)
                .ToList()),
            Recipe = item.Recipe.Select(r => new RecipeIngredientDto
            {
                Id = r.Id,
                ArticleId = r.ArticleId,
                ArticleName = r.Article?.Name ?? string.Empty,
                InternalCode = r.Article?.InternalCode,
                UnitId = r.UnitId,
                UnitName = r.Unit?.Name ?? string.Empty,
                UnitSymbol = r.Unit?.Symbol ?? string.Empty,
                Quantity = r.Quantity,
                Notes = r.Notes,
            }).ToList(),
        };

        ApplyModifierAvailability(dto.ModifierGroups, modifierAvailability);
        return dto;
    }

    private async Task<Dictionary<Guid, ModifierOptionAvailabilityInfo>> BuildModifierAvailabilityAsync(
        MenuItem item,
        Guid branchId,
        CancellationToken ct)
    {
        var trackedOptions = item.ModifierGroups
            .Where(g => !g.IsDeleted && g.IsActive)
            .SelectMany(g => g.Options.Where(o => !o.IsDeleted && o.IsActive && o.ArticleId.HasValue && o.UnitId.HasValue && o.Quantity > 0))
            .ToList();
        if (trackedOptions.Count == 0) return [];

        var articleIds = trackedOptions.Select(o => o.ArticleId!.Value).Distinct().ToList();
        var stockByArticle = await _db.WarehouseStock
            .AsNoTracking()
            .Where(x => x.BranchId == branchId && !x.IsDeleted && articleIds.Contains(x.ArticleId))
            .GroupBy(x => x.ArticleId)
            .Select(g => new { ArticleId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ArticleId, x => x.Quantity, ct);

        var reservedByArticle = await _db.StockReservations
            .AsNoTracking()
            .Where(x => x.BranchId == branchId
                && !x.IsDeleted
                && articleIds.Contains(x.ArticleId)
                && x.Status == Grimorio.Domain.Entities.Inventory.StockReservationStatus.Active)
            .GroupBy(x => x.ArticleId)
            .Select(g => new { ArticleId = g.Key, Quantity = g.Sum(x => x.BaseQuantity) })
            .ToDictionaryAsync(x => x.ArticleId, x => x.Quantity, ct);

        foreach (var (articleId, reservedQuantity) in reservedByArticle)
        {
            stockByArticle[articleId] = Math.Max(0, stockByArticle.GetValueOrDefault(articleId) - reservedQuantity);
        }

        var conversions = await _db.UnitConversions
            .AsNoTracking()
            .Where(x => x.BranchId == branchId && !x.IsDeleted)
            .Select(x => new UnitConversionInfo(x.OriginUnitId, x.DestinationUnitId, x.Factor))
            .ToListAsync(ct);

        var result = new Dictionary<Guid, ModifierOptionAvailabilityInfo>();
        foreach (var option in trackedOptions)
        {
            var article = option.Article;
            var unitId = option.UnitId!.Value;
            var articleId = option.ArticleId!.Value;
            var requiredBase = article is null ? 0m : ConvertQuantity(option.Quantity, unitId, article.BaseUnitId, conversions);
            var stock = Math.Max(0, stockByArticle.GetValueOrDefault(articleId));
            var availableQuantity = requiredBase > 0 ? Math.Floor(stock / requiredBase) : 0m;
            result[option.Id] = new ModifierOptionAvailabilityInfo(true, availableQuantity > 0, availableQuantity);
        }

        return result;
    }

    private static void ApplyModifierAvailability(
        IEnumerable<MenuItemModifierGroupDto> groups,
        IReadOnlyDictionary<Guid, ModifierOptionAvailabilityInfo> availabilityByOptionId)
    {
        foreach (var option in groups.SelectMany(g => g.Options))
        {
            if (!availabilityByOptionId.TryGetValue(option.Id, out var availability))
            {
                option.IsTracked = false;
                option.IsAvailable = true;
                option.AvailableQuantity = null;
                continue;
            }

            option.IsTracked = availability.IsTracked;
            option.IsAvailable = availability.IsAvailable;
            option.AvailableQuantity = availability.AvailableQuantity;
        }
    }

    private sealed record ModifierOptionAvailabilityInfo(
        bool IsTracked,
        bool IsAvailable,
        decimal AvailableQuantity);

    private static decimal ConvertQuantity(
        decimal quantity,
        Guid originUnitId,
        Guid destinationUnitId,
        IEnumerable<UnitConversionInfo> conversions)
    {
        if (originUnitId == destinationUnitId) return quantity;

        var direct = conversions.FirstOrDefault(x => x.OriginUnitId == originUnitId && x.DestinationUnitId == destinationUnitId);
        if (direct is not null) return quantity * direct.Factor;

        var reverse = conversions.FirstOrDefault(x => x.OriginUnitId == destinationUnitId && x.DestinationUnitId == originUnitId);
        if (reverse is not null && reverse.Factor != 0) return quantity / reverse.Factor;

        return 0m;
    }

    private sealed record UnitConversionInfo(Guid OriginUnitId, Guid DestinationUnitId, decimal Factor);
}

public class GetMenuAvailabilityHandler : IRequestHandler<GetMenuAvailabilityQuery, List<MenuItemAvailabilityDto>>
{
    private readonly GrimorioDbContext _db;
    public GetMenuAvailabilityHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<MenuItemAvailabilityDto>> Handle(GetMenuAvailabilityQuery req, CancellationToken ct)
    {
        var query = _db.MenuItems
            .AsNoTracking()
            .Include(x => x.Recipe.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.Unit)
            .Include(x => x.Recipe.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.Article)
                    .ThenInclude(a => a!.BaseUnit)
            .Where(x => x.BranchId == req.BranchId && !x.IsDeleted);

        if (req.CategoryId.HasValue) query = query.Where(x => x.MenuCategoryId == req.CategoryId.Value);
        if (req.ActiveOnly) query = query.Where(x => x.IsActive);
        if (req.AvailableOnly) query = query.Where(x => x.AvailableForSale);

        var items = await query.AsSplitQuery().ToListAsync(ct);

        var articleIds = items
            .SelectMany(item => item.Recipe.Where(r => !r.IsDeleted).SelectMany(r =>
                new[] { r.ArticleId }))
            .Distinct()
            .ToList();

        var stockByArticle = articleIds.Count == 0
            ? new Dictionary<Guid, decimal>()
            : await _db.WarehouseStock
                .AsNoTracking()
                .Where(x => x.BranchId == req.BranchId && !x.IsDeleted && articleIds.Contains(x.ArticleId))
                .GroupBy(x => x.ArticleId)
                .Select(g => new { ArticleId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.ArticleId, x => x.Quantity, ct);

        var reservedByArticle = articleIds.Count == 0
            ? new Dictionary<Guid, decimal>()
            : await _db.StockReservations
                .AsNoTracking()
                .Where(x => x.BranchId == req.BranchId
                    && !x.IsDeleted
                    && articleIds.Contains(x.ArticleId)
                    && x.Status == Grimorio.Domain.Entities.Inventory.StockReservationStatus.Active)
                .GroupBy(x => x.ArticleId)
                .Select(g => new { ArticleId = g.Key, Quantity = g.Sum(x => x.BaseQuantity) })
                .ToDictionaryAsync(x => x.ArticleId, x => x.Quantity, ct);

        foreach (var (articleId, reservedQuantity) in reservedByArticle)
        {
            stockByArticle[articleId] = Math.Max(0, stockByArticle.GetValueOrDefault(articleId) - reservedQuantity);
        }

        var conversions = await _db.UnitConversions
            .AsNoTracking()
            .Where(x => x.BranchId == req.BranchId && !x.IsDeleted)
            .Select(x => new UnitConversionInfo(x.OriginUnitId, x.DestinationUnitId, x.Factor))
            .ToListAsync(ct);

        return items.Select(item => BuildAvailability(item, stockByArticle, conversions)).ToList();
    }

    private static MenuItemAvailabilityDto BuildAvailability(
        MenuItem item,
        IReadOnlyDictionary<Guid, decimal> stockByArticle,
        IReadOnlyCollection<UnitConversionInfo> conversions)
    {
        var recipe = item.Recipe.Where(r => !r.IsDeleted).ToList();
        if (recipe.Count == 0)
        {
            return new MenuItemAvailabilityDto
            {
                MenuItemId = item.Id,
                IsTracked = false,
                IsAvailable = true,
                AvailableQuantity = null,
            };
        }

        var dto = new MenuItemAvailabilityDto
        {
            MenuItemId = item.Id,
            IsTracked = true,
        };

        var capacities = new List<(decimal Quantity, string? LimitingArticleName)>();

        foreach (var ingredient in recipe)
        {
            var article = ingredient.Article;
            var requiredBase = article is null ? 0m : ConvertQuantity(ingredient.Quantity, ingredient.UnitId, article.BaseUnitId, conversions);
            var stock = stockByArticle.TryGetValue(ingredient.ArticleId, out var quantity) ? quantity : 0m;
            var servings = requiredBase > 0 ? stock / requiredBase : 0m;

            dto.Components.Add(new MenuItemAvailabilityComponentDto
            {
                RecipeIngredientId = ingredient.Id,
                ArticleId = ingredient.ArticleId,
                ArticleName = article?.Name ?? string.Empty,
                RequiredQuantity = ingredient.Quantity,
                RequiredUnitSymbol = ingredient.Unit?.Symbol ?? string.Empty,
                StockQuantity = Math.Round(stock, 4),
                StockUnitSymbol = article?.BaseUnit?.Symbol ?? string.Empty,
                AvailableServings = Math.Floor(servings),
            });
            capacities.Add((servings, article?.Name));
        }

        var limiting = capacities.OrderBy(x => x.Quantity).FirstOrDefault();
        dto.AvailableQuantity = Math.Floor(limiting.Quantity);
        dto.IsAvailable = dto.AvailableQuantity > 0;
        dto.LimitingArticleName = dto.IsAvailable ? null : limiting.LimitingArticleName;
        return dto;
    }

    private static decimal ConvertQuantity(
        decimal quantity,
        Guid originUnitId,
        Guid destinationUnitId,
        IEnumerable<UnitConversionInfo> conversions)
    {
        if (originUnitId == destinationUnitId) return quantity;

        var direct = conversions.FirstOrDefault(x => x.OriginUnitId == originUnitId && x.DestinationUnitId == destinationUnitId);
        if (direct is not null) return quantity * direct.Factor;

        var reverse = conversions.FirstOrDefault(x => x.OriginUnitId == destinationUnitId && x.DestinationUnitId == originUnitId);
        if (reverse is not null && reverse.Factor != 0) return quantity / reverse.Factor;

        return 0m;
    }

    private sealed record UnitConversionInfo(Guid OriginUnitId, Guid DestinationUnitId, decimal Factor);
}

public class GetMenuProfitabilityHandler : IRequestHandler<GetMenuProfitabilityQuery, List<MenuItemProfitabilityDto>>
{
    private readonly GrimorioDbContext _db;
    public GetMenuProfitabilityHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<MenuItemProfitabilityDto>> Handle(GetMenuProfitabilityQuery req, CancellationToken ct)
    {
        var query = _db.MenuItems
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.TaxRate)
            .Include(x => x.Recipe.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.Article)
                    .ThenInclude(a => a!.BaseUnit)
            .Include(x => x.Recipe.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.Unit)
            .Where(x => x.BranchId == req.BranchId && !x.IsDeleted);

        if (req.CategoryId.HasValue) query = query.Where(x => x.MenuCategoryId == req.CategoryId.Value);
        if (req.ActiveOnly) query = query.Where(x => x.IsActive);
        if (req.AvailableOnly) query = query.Where(x => x.AvailableForSale);

        var items = await query
            .AsSplitQuery()
            .OrderBy(x => x.Category!.Order)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);

        var articleIds = items
            .SelectMany(i => i.Recipe.Where(r => !r.IsDeleted).Select(r => r.ArticleId))
            .Distinct()
            .ToList();

        var conversions = await _db.UnitConversions
            .AsNoTracking()
            .Where(x => x.BranchId == req.BranchId && !x.IsDeleted)
            .Select(x => new UnitConversionInfo(x.OriginUnitId, x.DestinationUnitId, x.Factor))
            .ToListAsync(ct);

        var purchaseItems = articleIds.Count == 0
            ? []
            : await _db.PurchaseItems
                .AsNoTracking()
                .Include(x => x.Purchase)
                .Include(x => x.Article)
                .Where(x => x.BranchId == req.BranchId
                    && !x.IsDeleted
                    && articleIds.Contains(x.ArticleId)
                    && x.Purchase != null
                    && !x.Purchase.IsDeleted
                    && x.Purchase.Status == PurchaseStatus.Registrada)
                .Select(x => new PurchaseCostInput(
                    x.ArticleId,
                    x.UnitId,
                    x.Article != null ? x.Article.BaseUnitId : Guid.Empty,
                    x.Quantity,
                    x.UnitPrice,
                    x.DiscountAmount,
                    x.Purchase!.DocumentDate,
                    x.CreatedAt))
                .ToListAsync(ct);

        var unitCosts = purchaseItems
            .Select(x =>
            {
                var baseQty = ConvertQuantity(x.Quantity, x.UnitId, x.ArticleBaseUnitId, conversions);
                var netCost = x.UnitPrice * x.Quantity - x.DiscountAmount;
                var unitCost = baseQty > 0 ? netCost / baseQty : 0m;
                return new ArticleCostSample(x.ArticleId, baseQty, netCost, unitCost, x.PurchaseDate, x.CreatedAt);
            })
            .Where(x => x.BaseQuantity > 0 && x.NetCost >= 0)
            .GroupBy(x => x.ArticleId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var totalQty = g.Sum(x => x.BaseQuantity);
                    var totalCost = g.Sum(x => x.NetCost);
                    var last = g.OrderByDescending(x => x.PurchaseDate)
                        .ThenByDescending(x => x.CreatedAt)
                        .First();
                    return new ArticleUnitCost(
                        totalQty > 0 ? totalCost / totalQty : 0m,
                        last.UnitCost);
                });

        return items.Select(item => BuildProfitability(item, conversions, unitCosts)).ToList();
    }

    private static MenuItemProfitabilityDto BuildProfitability(
        MenuItem item,
        IReadOnlyCollection<UnitConversionInfo> conversions,
        IReadOnlyDictionary<Guid, ArticleUnitCost> unitCosts)
    {
        var taxPct = item.TaxRate?.Percentage ?? 0m;
        var netSalePrice = taxPct > 0
            ? Math.Round(item.Price / (1m + taxPct / 100m), 4)
            : item.Price;
        var taxAmount = item.Price - netSalePrice;

        var dto = new MenuItemProfitabilityDto
        {
            MenuItemId = item.Id,
            MenuItemName = item.Name,
            InternalCode = item.InternalCode,
            CategoryName = item.Category?.Name ?? string.Empty,
            CategoryColor = item.Category?.Color,
            GrossSalePrice = Math.Round(item.Price, 2),
            TaxPercentage = taxPct,
            NetSalePrice = Math.Round(netSalePrice, 2),
            TaxAmount = Math.Round(taxAmount, 2),
            HasRecipe = item.Recipe.Any(r => !r.IsDeleted),
        };

        foreach (var ingredient in item.Recipe.Where(r => !r.IsDeleted))
        {
            var article = ingredient.Article;
            var warning = article is null ? "Articulo no encontrado." : null;
            var baseQty = article is null
                ? 0m
                : ConvertQuantity(ingredient.Quantity, ingredient.UnitId, article.BaseUnitId, conversions);

            if (article is not null && ingredient.UnitId != article.BaseUnitId && baseQty == 0)
                warning = "No existe conversion hacia la unidad base.";

            ArticleUnitCost? costInfo = null;
            var hasCost = article is not null
                && unitCosts.TryGetValue(article.Id, out costInfo)
                && costInfo.Average > 0;
            var averageCost = hasCost ? costInfo!.Average : 0m;
            decimal? lastCost = hasCost ? costInfo!.Last : null;
            var totalCost = baseQty * averageCost;

            dto.Ingredients.Add(new MenuItemProfitabilityIngredientDto
            {
                RecipeIngredientId = ingredient.Id,
                ArticleId = ingredient.ArticleId,
                ArticleName = article?.Name ?? string.Empty,
                InternalCode = article?.InternalCode,
                Quantity = ingredient.Quantity,
                UnitId = ingredient.UnitId,
                UnitSymbol = ingredient.Unit?.Symbol ?? string.Empty,
                BaseQuantity = Math.Round(baseQty, 4),
                BaseUnitSymbol = article?.BaseUnit?.Symbol ?? string.Empty,
                AverageUnitCost = Math.Round(averageCost, 4),
                LastUnitCost = lastCost.HasValue ? Math.Round(lastCost.Value, 4) : null,
                TotalCost = Math.Round(totalCost, 4),
                HasCost = hasCost,
                Warning = warning ?? (hasCost ? null : "Sin compras registradas para calcular costo."),
            });
        }

        dto.RecipeCost = Math.Round(dto.Ingredients.Sum(i => i.TotalCost), 2);
        dto.GrossProfit = Math.Round(dto.NetSalePrice - dto.RecipeCost, 2);
        dto.FoodCostPercentage = dto.NetSalePrice > 0
            ? Math.Round(dto.RecipeCost / dto.NetSalePrice * 100m, 2)
            : 0m;
        dto.GrossMarginPercentage = dto.NetSalePrice > 0
            ? Math.Round(dto.GrossProfit / dto.NetSalePrice * 100m, 2)
            : 0m;
        dto.HasMissingCosts = dto.Ingredients.Any(i => !i.HasCost);
        dto.HasConversionWarnings = dto.Ingredients.Any(i => i.Warning?.Contains("conversion", StringComparison.OrdinalIgnoreCase) == true);

        foreach (var ingredient in dto.Ingredients)
        {
            ingredient.CostSharePercentage = dto.RecipeCost > 0
                ? Math.Round(ingredient.TotalCost / dto.RecipeCost * 100m, 2)
                : 0m;
        }

        (dto.Status, dto.StatusLabel) = GetStatus(dto);
        return dto;
    }

    private static decimal ConvertQuantity(
        decimal quantity,
        Guid originUnitId,
        Guid destinationUnitId,
        IEnumerable<UnitConversionInfo> conversions)
    {
        if (originUnitId == destinationUnitId) return quantity;

        var direct = conversions.FirstOrDefault(x => x.OriginUnitId == originUnitId && x.DestinationUnitId == destinationUnitId);
        if (direct is not null) return quantity * direct.Factor;

        var reverse = conversions.FirstOrDefault(x => x.OriginUnitId == destinationUnitId && x.DestinationUnitId == originUnitId);
        if (reverse is not null && reverse.Factor != 0) return quantity / reverse.Factor;

        return 0m;
    }

    private static (string Status, string Label) GetStatus(MenuItemProfitabilityDto item)
    {
        if (!item.HasRecipe) return ("NoRecipe", "Sin receta");
        if (item.HasConversionWarnings) return ("Warning", "Revisar unidades");
        if (item.HasMissingCosts) return ("Warning", "Faltan costos");
        if (item.NetSalePrice <= 0) return ("Critical", "Sin precio neto");
        if (item.FoodCostPercentage <= 0) return ("Warning", "Sin costo");
        if (item.FoodCostPercentage < 25m) return ("Low", "Bajo objetivo");
        if (item.FoodCostPercentage <= 35m) return ("Healthy", "Saludable");
        if (item.FoodCostPercentage <= 45m) return ("High", "Alto");
        return ("Critical", "Critico");
    }

    private sealed record UnitConversionInfo(Guid OriginUnitId, Guid DestinationUnitId, decimal Factor);
    private sealed record PurchaseCostInput(
        Guid ArticleId,
        Guid UnitId,
        Guid ArticleBaseUnitId,
        decimal Quantity,
        decimal UnitPrice,
        decimal DiscountAmount,
        DateTime PurchaseDate,
        DateTime CreatedAt);
    private sealed record ArticleCostSample(
        Guid ArticleId,
        decimal BaseQuantity,
        decimal NetCost,
        decimal UnitCost,
        DateTime PurchaseDate,
        DateTime CreatedAt);
    private sealed record ArticleUnitCost(decimal Average, decimal Last);
}
