using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Inventory.Queries;
using Grimorio.Domain.Entities.Inventory;
using Grimorio.Infrastructure.Persistence;
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
            .Include(x => x.Category)
            .Include(x => x.BaseUnit)
            .Include(x => x.Stocks.Where(s => !s.IsDeleted))
            .Where(x => x.BranchId == req.BranchId);

        if (req.ActiveOnly == true) query = query.Where(x => x.IsActive);
        if (!string.IsNullOrEmpty(req.Type) && Enum.TryParse<ArticleType>(req.Type, out var articleType))
            query = query.Where(x => x.Type == articleType);
        if (req.CategoryId.HasValue) query = query.Where(x => x.CategoryId == req.CategoryId.Value);

        var articles = await query.OrderBy(x => x.Name).ToListAsync(ct);
        return articles.Select(MapArticulo).ToList();
    }

    internal static InventoryArticleDto MapArticulo(InventoryArticle x)
    {
        var stockTotal = x.Stocks.Where(s => !s.IsDeleted).Sum(s => s.Quantity);
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
            .Include(a => a.Stocks.Where(s => !s.IsDeleted))
            .FirstOrDefaultAsync(a => a.Id == req.Id && a.BranchId == req.BranchId, ct);
        return x is null ? null : GetInventoryArticlesHandler.MapArticulo(x);
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
        // Parte de artículos para mostrar también los que tienen stock 0 (sin movimientos aún)
        var articlesQuery = _db.InventoryArticles
            .Include(a => a.Category)
            .Include(a => a.BaseUnit)
            .Include(a => a.Stocks.Where(s => !s.IsDeleted))
                .ThenInclude(s => s.Warehouse)
            .Where(a => a.BranchId == req.BranchId && a.IsActive);

        if (req.CategoryId.HasValue)
            articlesQuery = articlesQuery.Where(a => a.CategoryId == req.CategoryId.Value);

        var articles = await articlesQuery.ToListAsync(ct);

        var result = new List<WarehouseStockDto>();

        foreach (var a in articles)
        {
            var stocks = a.Stocks.Where(s => !s.IsDeleted).ToList();

            if (!stocks.Any())
            {
                // Artículo sin movimientos: aparece con stock 0 y sin bodega asignada
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
                    result.Add(new WarehouseStockDto
                    {
                        ArticleId = a.Id,
                        ArticleName = a.Name,
                        InternalCode = a.InternalCode,
                        CategoryName = a.Category?.Name ?? string.Empty,
                        CategoryColor = a.Category?.Color,
                        Type = a.Type.ToString(),
                        WarehouseId = s.WarehouseId,
                        WarehouseName = s.Warehouse?.Name ?? string.Empty,
                        Quantity = s.Quantity,
                        UnitSymbol = a.BaseUnit?.Symbol ?? string.Empty,
                        MinStock = a.MinStock,
                        LowStock = a.StockAlertActive && s.Quantity <= a.MinStock,
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
        var stocks = await _db.WarehouseStock
            .Include(x => x.Article).ThenInclude(a => a!.BaseUnit)
            .Where(x => x.BranchId == req.BranchId && x.Article!.StockAlertActive && x.Article.IsActive)
            .GroupBy(x => new { x.ArticleId, x.Article!.Name, x.Article.InternalCode, x.Article.MinStock, Symbol = x.Article.BaseUnit!.Symbol })
            .Select(g => new StockAlertDto
            {
                ArticleId = g.Key.ArticleId,
                ArticleName = g.Key.Name,
                InternalCode = g.Key.InternalCode,
                UnitSymbol = g.Key.Symbol,
                CurrentStock = g.Sum(s => s.Quantity),
                MinStock = g.Key.MinStock,
            })
            .ToListAsync(ct);

        return stocks.Where(x => x.CurrentStock <= x.MinStock).OrderBy(x => x.ArticleName).ToList();
    }
}

