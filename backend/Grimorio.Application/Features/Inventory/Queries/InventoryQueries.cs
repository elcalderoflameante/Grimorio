using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.Inventory.Queries;

public class GetMeasurementUnitsQuery : IRequest<List<MeasurementUnitDto>>
{
    public Guid BranchId { get; set; }
}

public class GetUnitConversionsQuery : IRequest<List<UnitConversionDto>>
{
    public Guid BranchId { get; set; }
}

public class GetInventoryCategoriesQuery : IRequest<List<InventoryCategoryDto>>
{
    public Guid BranchId { get; set; }
}

public class GetInventoryArticlesQuery : IRequest<List<InventoryArticleDto>>
{
    public Guid BranchId { get; set; }
    public bool? ActiveOnly { get; set; }
    public string? Type { get; set; }
    public Guid? CategoryId { get; set; }
}

public class GetInventoryArticleQuery : IRequest<InventoryArticleDto?>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
}

public class GetWarehousesQuery : IRequest<List<WarehouseDto>>
{
    public Guid BranchId { get; set; }
    public bool? ActiveOnly { get; set; }
}

public class GetCurrentStockQuery : IRequest<List<WarehouseStockDto>>
{
    public Guid BranchId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? CategoryId { get; set; }
    public bool? LowStockOnly { get; set; }
}

public class GetStockMovementsQuery : IRequest<List<StockMovementDto>>
{
    public Guid BranchId { get; set; }
    public Guid? ArticleId { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? Type { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int PageSize { get; set; } = 50;
}

public class GetStockAlertsQuery : IRequest<List<StockAlertDto>>
{
    public Guid BranchId { get; set; }
}
