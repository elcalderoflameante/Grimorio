using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Inventory.Queries;
using Grimorio.Domain.Entities.Inventory;
using Grimorio.Infrastructure.Features.Inventory;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Inventory.Queries;

public class GetMeasurementUnitsHandler : IRequestHandler<GetMeasurementUnitsQuery, List<MeasurementUnitDto>>
{
    private readonly GrimorioDbContext _db;
    public GetMeasurementUnitsHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<MeasurementUnitDto>> Handle(GetMeasurementUnitsQuery req, CancellationToken ct) =>
        await _db.MeasurementUnits
            .Where(x => x.BranchId == req.BranchId)
            .OrderBy(x => x.Name)
            .Select(x => new MeasurementUnitDto { Id = x.Id, Name = x.Name, Symbol = x.Symbol })
            .ToListAsync(ct);
}

public class GetUnitConversionsHandler : IRequestHandler<GetUnitConversionsQuery, List<UnitConversionDto>>
{
    private readonly GrimorioDbContext _db;
    public GetUnitConversionsHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<UnitConversionDto>> Handle(GetUnitConversionsQuery req, CancellationToken ct) =>
        await _db.UnitConversions
            .Include(x => x.OriginUnit)
            .Include(x => x.DestinationUnit)
            .Where(x => x.BranchId == req.BranchId)
            .OrderBy(x => x.OriginUnit!.Name)
            .Select(x => new UnitConversionDto
            {
                Id = x.Id,
                OriginUnitId = x.OriginUnitId,
                OriginUnitName = x.OriginUnit!.Name,
                OriginUnitSymbol = x.OriginUnit.Symbol,
                DestinationUnitId = x.DestinationUnitId,
                DestinationUnitName = x.DestinationUnit!.Name,
                DestinationUnitSymbol = x.DestinationUnit.Symbol,
                Factor = x.Factor,
            })
            .ToListAsync(ct);
}

public class GetInventoryCategoriesHandler : IRequestHandler<GetInventoryCategoriesQuery, List<InventoryCategoryDto>>
{
    private readonly GrimorioDbContext _db;
    public GetInventoryCategoriesHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<InventoryCategoryDto>> Handle(GetInventoryCategoriesQuery req, CancellationToken ct) =>
        await _db.InventoryCategories
            .Where(x => x.BranchId == req.BranchId)
            .OrderBy(x => x.Name)
            .Select(x => new InventoryCategoryDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Color = x.Color,
                TotalArticles = x.Articles.Count(a => !a.IsDeleted),
            })
            .ToListAsync(ct);
}

public class GetInventoryArticlesHandler : IRequestHandler<GetInventoryArticlesQuery, List<InventoryArticleDto>>
{
    private readonly GrimorioDbContext _db;
    public GetInventoryArticlesHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<InventoryArticleDto>> Handle(GetInventoryArticlesQuery req, CancellationToken ct)
    {
        var query = _db.InventoryArticles
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.BaseUnit)
            .Where(x => x.BranchId == req.BranchId);

        if (req.ActiveOnly == true) query = query.Where(x => x.IsActive);
        if (!string.IsNullOrEmpty(req.Type) && Enum.TryParse<ArticleType>(req.Type, out var articleType))
            query = query.Where(x => x.Type == articleType);
        if (req.CategoryId.HasValue) query = query.Where(x => x.CategoryId == req.CategoryId.Value);

        var articles = await query.OrderBy(x => x.Name).ToListAsync(ct);
        var articleIds = articles.Select(x => x.Id).ToList();
        var stockByArticle = articleIds.Count == 0
            ? new Dictionary<Guid, decimal>()
            : await _db.StockMovements
                .AsNoTracking()
                .Where(x => x.BranchId == req.BranchId && articleIds.Contains(x.ArticleId))
                .GroupBy(x => x.ArticleId)
                .Select(g => new { ArticleId = g.Key, Quantity = g.Sum(x => x.BaseQuantity) })
                .ToDictionaryAsync(x => x.ArticleId, x => x.Quantity, ct);

        return articles.Select(x => MapArticulo(x, stockByArticle.GetValueOrDefault(x.Id))).ToList();
    }

    internal static InventoryArticleDto MapArticulo(InventoryArticle x, decimal stockTotal = 0)
    {
        return new InventoryArticleDto
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            InternalCode = x.InternalCode,
            Type = x.Type.ToString(),
            CategoryId = x.CategoryId,
            CategoryName = x.Category?.Name ?? string.Empty,
            CategoryColor = x.Category?.Color,
            BaseUnitId = x.BaseUnitId,
            BaseUnitName = x.BaseUnit?.Name ?? string.Empty,
            BaseUnitSymbol = x.BaseUnit?.Symbol ?? string.Empty,
            MinStock = x.MinStock,
            TotalStock = stockTotal,
            StockAlertActive = x.StockAlertActive,
            LowStock = x.StockAlertActive && stockTotal <= x.MinStock,
            IsActive = x.IsActive,
        };
    }
}

public class GetInventoryArticleHandler : IRequestHandler<GetInventoryArticleQuery, InventoryArticleDto?>
{
    private readonly GrimorioDbContext _db;
    public GetInventoryArticleHandler(GrimorioDbContext db) => _db = db;

    public async Task<InventoryArticleDto?> Handle(GetInventoryArticleQuery req, CancellationToken ct)
    {
        var x = await _db.InventoryArticles
            .Include(a => a.Category)
            .Include(a => a.BaseUnit)
            .FirstOrDefaultAsync(a => a.Id == req.Id && a.BranchId == req.BranchId, ct);
        if (x is null) return null;

        var stockTotal = await _db.StockMovements
            .AsNoTracking()
            .Where(m => m.BranchId == req.BranchId && m.ArticleId == req.Id)
            .SumAsync(m => m.BaseQuantity, ct);

        return GetInventoryArticlesHandler.MapArticulo(x, stockTotal);
    }
}

public class GetWarehousesHandler : IRequestHandler<GetWarehousesQuery, List<WarehouseDto>>
{
    private readonly GrimorioDbContext _db;
    public GetWarehousesHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<WarehouseDto>> Handle(GetWarehousesQuery req, CancellationToken ct)
    {
        var query = _db.Warehouses.Where(x => x.BranchId == req.BranchId);
        if (req.ActiveOnly == true) query = query.Where(x => x.IsActive);
        return await query.OrderBy(x => x.Name)
            .Select(x => new WarehouseDto
            {
                Id = x.Id, Name = x.Name,
                Description = x.Description, Location = x.Location, IsActive = x.IsActive,
            })
            .ToListAsync(ct);
    }
}

public class GetCurrentStockHandler : IRequestHandler<GetCurrentStockQuery, List<WarehouseStockDto>>
{
    private readonly GrimorioDbContext _db;
    public GetCurrentStockHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<WarehouseStockDto>> Handle(GetCurrentStockQuery req, CancellationToken ct)
    {
        var articlesQuery = _db.InventoryArticles
            .AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.BaseUnit)
            .Where(a => a.BranchId == req.BranchId && a.IsActive);

        if (req.CategoryId.HasValue)
            articlesQuery = articlesQuery.Where(a => a.CategoryId == req.CategoryId.Value);

        var articles = await articlesQuery.ToListAsync(ct);
        var articleIds = articles.Select(a => a.Id).ToList();
        var stockQuery = _db.StockMovements
            .AsNoTracking()
            .Include(m => m.Warehouse)
            .Where(m => m.BranchId == req.BranchId && articleIds.Contains(m.ArticleId));

        if (req.WarehouseId.HasValue)
            stockQuery = stockQuery.Where(m => m.WarehouseId == req.WarehouseId.Value);

        var stockByArticleWarehouse = articleIds.Count == 0
            ? []
            : await stockQuery
                .GroupBy(m => new { m.ArticleId, m.WarehouseId, WarehouseName = m.Warehouse!.Name })
                .Select(g => new
                {
                    g.Key.ArticleId,
                    g.Key.WarehouseId,
                    g.Key.WarehouseName,
                    Quantity = g.Sum(m => m.BaseQuantity),
                    LastUpdatedAt = g.Max(m => m.CreatedAt),
                })
                .ToListAsync(ct);

        var reservationsQuery = _db.StockReservations
            .AsNoTracking()
            .Where(r => r.BranchId == req.BranchId
                && articleIds.Contains(r.ArticleId)
                && r.Status == StockReservationStatus.Active
                && !r.IsDeleted);

        if (req.WarehouseId.HasValue)
            reservationsQuery = reservationsQuery.Where(r => r.WarehouseId == req.WarehouseId.Value);

        var reservedByArticleWarehouse = articleIds.Count == 0
            ? new Dictionary<(Guid ArticleId, Guid WarehouseId), decimal>()
            : await reservationsQuery
                .GroupBy(r => new { r.ArticleId, r.WarehouseId })
                .Select(g => new { g.Key.ArticleId, g.Key.WarehouseId, Quantity = g.Sum(r => r.BaseQuantity) })
                .ToDictionaryAsync(x => (x.ArticleId, x.WarehouseId), x => x.Quantity, ct);

        var result = new List<WarehouseStockDto>();

        foreach (var a in articles)
        {
            var stocks = stockByArticleWarehouse.Where(s => s.ArticleId == a.Id).ToList();

            if (!stocks.Any())
            {
                if (!req.WarehouseId.HasValue)
                {
                    result.Add(new WarehouseStockDto
                    {
                        ArticleId = a.Id,
                        ArticleName = a.Name,
                        InternalCode = a.InternalCode,
                        CategoryName = a.Category?.Name ?? string.Empty,
                        CategoryColor = a.Category?.Color,
                        Type = a.Type.ToString(),
                        WarehouseId = Guid.Empty,
                        WarehouseName = "—",
                        Quantity = 0,
                        UnitSymbol = a.BaseUnit?.Symbol ?? string.Empty,
                        MinStock = a.MinStock,
                        LowStock = a.StockAlertActive && 0 <= a.MinStock,
                        LastUpdatedAt = a.CreatedAt,
                    });
                }
            }
            else
            {
                foreach (var s in stocks)
                {
                    if (req.WarehouseId.HasValue && s.WarehouseId != req.WarehouseId.Value) continue;
                    var reservedQuantity = reservedByArticleWarehouse.GetValueOrDefault((s.ArticleId, s.WarehouseId));
                    var availableQuantity = Math.Max(0, s.Quantity - reservedQuantity);
                    result.Add(new WarehouseStockDto
                    {
                        ArticleId = a.Id,
                        ArticleName = a.Name,
                        InternalCode = a.InternalCode,
                        CategoryName = a.Category?.Name ?? string.Empty,
                        CategoryColor = a.Category?.Color,
                        Type = a.Type.ToString(),
                        WarehouseId = s.WarehouseId,
                        WarehouseName = s.WarehouseName,
                        Quantity = s.Quantity,
                        ReservedQuantity = reservedQuantity,
                        AvailableQuantity = availableQuantity,
                        UnitSymbol = a.BaseUnit?.Symbol ?? string.Empty,
                        MinStock = a.MinStock,
                        LowStock = a.StockAlertActive && availableQuantity <= a.MinStock,
                        LastUpdatedAt = s.LastUpdatedAt,
                    });
                }
            }
        }

        if (req.LowStockOnly == true) result = result.Where(x => x.LowStock).ToList();
        return result.OrderBy(x => x.ArticleName).ThenBy(x => x.WarehouseName).ToList();
    }
}

public class GetStockMovementsHandler : IRequestHandler<GetStockMovementsQuery, List<StockMovementDto>>
{
    private readonly GrimorioDbContext _db;
    public GetStockMovementsHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<StockMovementDto>> Handle(GetStockMovementsQuery req, CancellationToken ct)
    {
        var query = _db.StockMovements
            .Include(x => x.Article)
            .Include(x => x.Warehouse)
            .Include(x => x.Unit)
            .Where(x => x.BranchId == req.BranchId);

        if (req.ArticleId.HasValue) query = query.Where(x => x.ArticleId == req.ArticleId.Value);
        if (req.WarehouseId.HasValue) query = query.Where(x => x.WarehouseId == req.WarehouseId.Value);
        if (!string.IsNullOrEmpty(req.Type) && Enum.TryParse<MovementType>(req.Type, out var movementType))
            query = query.Where(x => x.Type == movementType);
        if (req.FromUtc.HasValue) query = query.Where(x => x.CreatedAt >= req.FromUtc.Value);
        if (req.ToUtc.HasValue) query = query.Where(x => x.CreatedAt <= req.ToUtc.Value);

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(req.PageSize)
            .Select(x => new StockMovementDto
            {
                Id = x.Id,
                ArticleId = x.ArticleId,
                ArticleName = x.Article!.Name,
                WarehouseId = x.WarehouseId,
                WarehouseName = x.Warehouse!.Name,
                Type = x.Type.ToString(),
                Quantity = x.Quantity,
                UnitSymbol = x.Unit!.Symbol,
                BaseQuantity = x.BaseQuantity,
                BaseUnitSymbol = x.Article.BaseUnit!.Symbol,
                UnitCost = x.UnitCost,
                TotalCost = x.TotalCost,
                Reference = x.Reference,
                Notes = x.Notes,
                MovedAt = x.CreatedAt,
            })
            .ToListAsync(ct);
    }
}

public class GetStockAlertsHandler : IRequestHandler<GetStockAlertsQuery, List<StockAlertDto>>
{
    private readonly GrimorioDbContext _db;
    public GetStockAlertsHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<StockAlertDto>> Handle(GetStockAlertsQuery req, CancellationToken ct)
    {
        var stocks = await _db.StockMovements
            .AsNoTracking()
            .Where(x => x.BranchId == req.BranchId && x.Article!.StockAlertActive && x.Article.IsActive)
            .GroupBy(x => new { x.ArticleId, x.Article!.Name, x.Article.InternalCode, x.Article.MinStock, Symbol = x.Article.BaseUnit!.Symbol })
            .Where(g => g.Sum(s => s.BaseQuantity) <= g.Key.MinStock)
            .Select(g => new StockAlertDto
            {
                ArticleId = g.Key.ArticleId,
                ArticleName = g.Key.Name,
                InternalCode = g.Key.InternalCode,
                UnitSymbol = g.Key.Symbol,
                CurrentStock = g.Sum(s => s.BaseQuantity),
                MinStock = g.Key.MinStock,
            })
            .ToListAsync(ct);

        return stocks.OrderBy(x => x.ArticleName).ToList();
    }
}

public class GetProductionRecipesHandler : IRequestHandler<GetProductionRecipesQuery, List<ProductionRecipeDto>>
{
    private readonly GrimorioDbContext _db;
    public GetProductionRecipesHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<ProductionRecipeDto>> Handle(GetProductionRecipesQuery req, CancellationToken ct)
    {
        var query = _db.ProductionRecipes
            .Include(x => x.OutputArticle).ThenInclude(x => x!.BaseUnit)
            .Include(x => x.OutputUnit)
            .Include(x => x.Ingredients.Where(i => !i.IsDeleted)).ThenInclude(x => x.Article).ThenInclude(x => x!.BaseUnit)
            .Include(x => x.Ingredients.Where(i => !i.IsDeleted)).ThenInclude(x => x.Unit)
            .Include(x => x.PreparationSteps.Where(s => !s.IsDeleted))
            .Where(x => x.BranchId == req.BranchId);

        if (!req.IncludeRestricted)
            query = query.Where(x => x.AccessLevel == ProductionRecipeAccessLevel.General);

        if (req.ActiveOnly == true) query = query.Where(x => x.IsActive);

        var recipes = await query.OrderBy(x => x.OutputArticle!.Name).ToListAsync(ct);
        return recipes.Select(InventoryProductionMapper.MapRecipe).ToList();
    }
}

public class GetProductionRecipeByArticleHandler : IRequestHandler<GetProductionRecipeByArticleQuery, ProductionRecipeDto?>
{
    private readonly GrimorioDbContext _db;
    public GetProductionRecipeByArticleHandler(GrimorioDbContext db) => _db = db;

    public async Task<ProductionRecipeDto?> Handle(GetProductionRecipeByArticleQuery req, CancellationToken ct)
    {
        var recipe = await _db.ProductionRecipes
            .Include(x => x.OutputArticle).ThenInclude(x => x!.BaseUnit)
            .Include(x => x.OutputUnit)
            .Include(x => x.Ingredients.Where(i => !i.IsDeleted)).ThenInclude(x => x.Article).ThenInclude(x => x!.BaseUnit)
            .Include(x => x.Ingredients.Where(i => !i.IsDeleted)).ThenInclude(x => x.Unit)
            .Include(x => x.PreparationSteps.Where(s => !s.IsDeleted))
            .FirstOrDefaultAsync(x => x.BranchId == req.BranchId
                && x.OutputArticleId == req.OutputArticleId
                && (req.IncludeRestricted || x.AccessLevel == ProductionRecipeAccessLevel.General), ct);

        return recipe is null ? null : InventoryProductionMapper.MapRecipe(recipe);
    }
}

public class GetProductionOrdersHandler : IRequestHandler<GetProductionOrdersQuery, List<ProductionOrderDto>>
{
    private readonly GrimorioDbContext _db;
    public GetProductionOrdersHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<ProductionOrderDto>> Handle(GetProductionOrdersQuery req, CancellationToken ct)
    {
        var query = _db.ProductionOrders
            .Include(x => x.OutputArticle).ThenInclude(x => x!.BaseUnit)
            .Include(x => x.OutputUnit)
            .Include(x => x.SourceWarehouse)
            .Include(x => x.DestinationWarehouse)
            .Include(x => x.Ingredients.Where(i => !i.IsDeleted)).ThenInclude(x => x.Article).ThenInclude(x => x!.BaseUnit)
            .Include(x => x.Ingredients.Where(i => !i.IsDeleted)).ThenInclude(x => x.Unit)
            .Where(x => x.BranchId == req.BranchId);

        if (!req.IncludeRestricted)
            query = query.Where(x => x.ProductionRecipe != null
                && x.ProductionRecipe.AccessLevel == ProductionRecipeAccessLevel.General);

        if (req.OutputArticleId.HasValue) query = query.Where(x => x.OutputArticleId == req.OutputArticleId.Value);
        if (req.WarehouseId.HasValue)
            query = query.Where(x => x.SourceWarehouseId == req.WarehouseId.Value || x.DestinationWarehouseId == req.WarehouseId.Value);
        if (req.FromUtc.HasValue) query = query.Where(x => x.ProducedAt >= req.FromUtc.Value);
        if (req.ToUtc.HasValue) query = query.Where(x => x.ProducedAt <= req.ToUtc.Value);

        var orders = await query
            .OrderByDescending(x => x.ProducedAt)
            .Take(req.PageSize)
            .ToListAsync(ct);

        return orders.Select(InventoryProductionMapper.MapOrder).ToList();
    }
}

public class GenerateProductionRecipeOperationalSheetPdfHandler
    : IRequestHandler<GenerateProductionRecipeOperationalSheetPdfQuery, byte[]?>
{
    private readonly GrimorioDbContext _db;
    public GenerateProductionRecipeOperationalSheetPdfHandler(GrimorioDbContext db) => _db = db;

    public async Task<byte[]?> Handle(GenerateProductionRecipeOperationalSheetPdfQuery req, CancellationToken ct)
    {
        var recipe = await _db.ProductionRecipes
            .AsNoTracking()
            .Include(x => x.OutputArticle)
            .Include(x => x.OutputUnit)
            .Include(x => x.Ingredients.Where(i => !i.IsDeleted)).ThenInclude(x => x.Article)
            .Include(x => x.Ingredients.Where(i => !i.IsDeleted)).ThenInclude(x => x.Unit)
            .Include(x => x.PreparationSteps.Where(s => !s.IsDeleted))
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == req.Id
                && x.BranchId == req.BranchId
                && x.IsActive
                && (req.IncludeRestricted || x.AccessLevel == ProductionRecipeAccessLevel.General), ct);

        if (recipe is null) return null;

        var branch = await _db.Branches.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.BranchId && !x.IsDeleted, ct);
        var taxConfig = await _db.BranchTaxConfigs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BranchId == req.BranchId && !x.IsDeleted, ct);
        var invoiceTemplate = await _db.InvoiceTemplates.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BranchId == req.BranchId && !x.IsDeleted, ct);

        var restaurantName = taxConfig is null
            ? "Restaurante"
            : string.IsNullOrWhiteSpace(taxConfig.NombreComercial)
                ? taxConfig.RazonSocial
                : taxConfig.NombreComercial!;
        var logoBytes = TryReadImage(req.WebRootPath, branch?.LogoUrl)
            ?? TryParseLogo(invoiceTemplate?.LogoBase64);
        var generatedAtLocal = BranchTimeZone.FromUtc(DateTime.UtcNow, branch?.TimeZoneId);

        return ProductionRecipeOperationalSheetPdfGenerator.Generate(
            recipe, logoBytes, restaurantName, generatedAtLocal);
    }

    private static byte[]? TryReadImage(string? webRootPath, string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(webRootPath)
            || string.IsNullOrWhiteSpace(imageUrl)
            || !imageUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return null;

        var relativePath = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(webRootPath, relativePath));
        var uploadsRoot = Path.GetFullPath(Path.Combine(webRootPath, "uploads"));
        if (!fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase)) return null;
        return File.Exists(fullPath) ? File.ReadAllBytes(fullPath) : null;
    }

    private static byte[]? TryParseLogo(string? logoBase64)
    {
        if (string.IsNullOrWhiteSpace(logoBase64)) return null;
        try
        {
            var data = logoBase64.Contains(',', StringComparison.Ordinal)
                ? logoBase64.Split(',', 2)[1]
                : logoBase64;
            return Convert.FromBase64String(data);
        }
        catch
        {
            return null;
        }
    }
}

internal static class InventoryProductionMapper
{
    internal static ProductionRecipeDto MapRecipe(ProductionRecipe recipe) => new()
    {
        Id = recipe.Id,
        OutputArticleId = recipe.OutputArticleId,
        OutputArticleName = recipe.OutputArticle?.Name ?? string.Empty,
        OutputQuantity = recipe.OutputQuantity,
        OutputUnitId = recipe.OutputUnitId,
        OutputUnitSymbol = recipe.OutputUnit?.Symbol ?? string.Empty,
        Notes = recipe.Notes,
        IsActive = recipe.IsActive,
        AccessLevel = recipe.AccessLevel.ToString(),
        Ingredients = recipe.Ingredients
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Article!.Name)
            .Select(x => new ProductionRecipeIngredientDto
            {
                Id = x.Id,
                ArticleId = x.ArticleId,
                ArticleName = x.Article?.Name ?? string.Empty,
                InternalCode = x.Article?.InternalCode,
                Quantity = x.Quantity,
                UnitId = x.UnitId,
                UnitSymbol = x.Unit?.Symbol ?? string.Empty,
                BaseUnitSymbol = x.Article?.BaseUnit?.Symbol ?? string.Empty,
                Notes = x.Notes,
            })
            .ToList(),
        PreparationSteps = recipe.PreparationSteps
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.StepNumber)
            .Select(x => new ProductionRecipePreparationStepDto
            {
                Id = x.Id,
                StepNumber = x.StepNumber,
                Instructions = x.Instructions,
                EstimatedMinutes = x.EstimatedMinutes,
                Temperature = x.Temperature,
            })
            .ToList(),
    };

    internal static ProductionOrderDto MapOrder(ProductionOrder order) => new()
    {
        Id = order.Id,
        Number = order.Number,
        ProductionRecipeId = order.ProductionRecipeId,
        OutputArticleId = order.OutputArticleId,
        OutputArticleName = order.OutputArticle?.Name ?? string.Empty,
        SourceWarehouseId = order.SourceWarehouseId,
        SourceWarehouseName = order.SourceWarehouse?.Name ?? string.Empty,
        DestinationWarehouseId = order.DestinationWarehouseId,
        DestinationWarehouseName = order.DestinationWarehouse?.Name ?? string.Empty,
        OutputQuantity = order.OutputQuantity,
        OutputUnitId = order.OutputUnitId,
        OutputUnitSymbol = order.OutputUnit?.Symbol ?? string.Empty,
        OutputBaseQuantity = order.OutputBaseQuantity,
        OutputBaseUnitSymbol = order.OutputArticle?.BaseUnit?.Symbol ?? string.Empty,
        TotalCost = order.TotalCost,
        UnitCost = order.UnitCost,
        Status = order.Status.ToString(),
        Notes = order.Notes,
        ProducedAt = order.ProducedAt,
        Ingredients = order.Ingredients
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Article!.Name)
            .Select(x => new ProductionOrderIngredientDto
            {
                ArticleId = x.ArticleId,
                ArticleName = x.Article?.Name ?? string.Empty,
                Quantity = x.Quantity,
                UnitSymbol = x.Unit?.Symbol ?? string.Empty,
                BaseQuantity = x.BaseQuantity,
                BaseUnitSymbol = x.Article?.BaseUnit?.Symbol ?? string.Empty,
                UnitCost = x.UnitCost,
                TotalCost = x.TotalCost,
            })
            .ToList(),
    };
}
