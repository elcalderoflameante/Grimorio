using Grimorio.Domain.Entities.Inventory;

namespace Grimorio.Application.DTOs;

// ── Unidades de medida ────────────────────────────────────────────────────

public class MeasurementUnitDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
}

public class CreateMeasurementUnitDto
{
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
}

// ── Conversiones de unidad ────────────────────────────────────────────────

public class UnitConversionDto
{
    public Guid Id { get; set; }
    public Guid OriginUnitId { get; set; }
    public string OriginUnitName { get; set; } = string.Empty;
    public string OriginUnitSymbol { get; set; } = string.Empty;
    public Guid DestinationUnitId { get; set; }
    public string DestinationUnitName { get; set; } = string.Empty;
    public string DestinationUnitSymbol { get; set; } = string.Empty;
    public decimal Factor { get; set; }
}

public class CreateUnitConversionDto
{
    public Guid OriginUnitId { get; set; }
    public Guid DestinationUnitId { get; set; }
    public decimal Factor { get; set; }
}

// ── Categorías ────────────────────────────────────────────────────────────

public class InventoryCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public int TotalArticles { get; set; }
}

public class CreateInventoryCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
}

// ── Artículos ─────────────────────────────────────────────────────────────

public class InventoryArticleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? InternalCode { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? CategoryColor { get; set; }
    public Guid BaseUnitId { get; set; }
    public string BaseUnitName { get; set; } = string.Empty;
    public string BaseUnitSymbol { get; set; } = string.Empty;
    public decimal MinStock { get; set; }
    public decimal TotalStock { get; set; }
    public bool StockAlertActive { get; set; }
    public bool LowStock { get; set; }
    public bool IsActive { get; set; }
}

public class CreateInventoryArticleDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? InternalCode { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Guid BaseUnitId { get; set; }
    public decimal MinStock { get; set; }
    public bool StockAlertActive { get; set; } = true;
}

public class UpdateInventoryArticleDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? InternalCode { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Guid BaseUnitId { get; set; }
    public decimal MinStock { get; set; }
    public bool StockAlertActive { get; set; }
    public bool IsActive { get; set; }
}

// ── Warehouses ───────────────────────────────────────────────────────────────

public class WarehouseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public bool IsActive { get; set; }
}

public class CreateWarehouseDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
}

// ── Stock ─────────────────────────────────────────────────────────────────

public class WarehouseStockDto
{
    public Guid ArticleId { get; set; }
    public string ArticleName { get; set; } = string.Empty;
    public string? InternalCode { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? CategoryColor { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public string UnitSymbol { get; set; } = string.Empty;
    public decimal MinStock { get; set; }
    public bool LowStock { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}

// ── Movements ───────────────────────────────────────────────────────────

public class StockMovementDto
{
    public Guid Id { get; set; }
    public Guid ArticleId { get; set; }
    public string ArticleName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitSymbol { get; set; } = string.Empty;
    public decimal BaseQuantity { get; set; }
    public string BaseUnitSymbol { get; set; } = string.Empty;
    public decimal? UnitCost { get; set; }
    public decimal? TotalCost { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime MovedAt { get; set; }
}

public class RegisterMovementDto
{
    public Guid ArticleId { get; set; }
    public Guid WarehouseId { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public Guid UnitId { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public class RegisterInitialInventoryDto
{
    public List<InitialInventoryItemDto> Items { get; set; } = [];
}

public class InitialInventoryItemDto
{
    public Guid ArticleId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal Quantity { get; set; }
    public Guid UnitId { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Notes { get; set; }
}

// ── Alertas ───────────────────────────────────────────────────────────────

public class StockAlertDto
{
    public Guid ArticleId { get; set; }
    public string ArticleName { get; set; } = string.Empty;
    public string? InternalCode { get; set; }
    public string UnitSymbol { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal MinStock { get; set; }
}
