using Grimorio.Application.DTOs;
using Grimorio.Domain.Entities.Inventory;
using MediatR;

namespace Grimorio.Application.Features.Inventory.Commands;

// ── Unidades de medida ────────────────────────────────────────────────────

public class CreateMeasurementUnitCommand : IRequest<MeasurementUnitDto>
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
}

public class UpdateMeasurementUnitCommand : IRequest<MeasurementUnitDto>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
}

public class DeleteMeasurementUnitCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
}

// ── Conversiones ──────────────────────────────────────────────────────────

public class CreateUnitConversionCommand : IRequest<UnitConversionDto>
{
    public Guid BranchId { get; set; }
    public Guid OriginUnitId { get; set; }
    public Guid DestinationUnitId { get; set; }
    public decimal Factor { get; set; }
}

public class DeleteUnitConversionCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
}

// ── Categorías ────────────────────────────────────────────────────────────

public class CreateInventoryCategoryCommand : IRequest<InventoryCategoryDto>
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
}

public class UpdateInventoryCategoryCommand : IRequest<InventoryCategoryDto>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
}

public class DeleteInventoryCategoryCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
}

// ── Artículos ─────────────────────────────────────────────────────────────

public class CreateInventoryArticleCommand : IRequest<InventoryArticleDto>
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? InternalCode { get; set; }
    public ArticleType Type { get; set; }
    public Guid CategoryId { get; set; }
    public Guid BaseUnitId { get; set; }
    public decimal MinStock { get; set; }
    public bool StockAlertActive { get; set; } = true;
}

public class UpdateInventoryArticleCommand : IRequest<InventoryArticleDto>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? InternalCode { get; set; }
    public ArticleType Type { get; set; }
    public Guid CategoryId { get; set; }
    public Guid BaseUnitId { get; set; }
    public decimal MinStock { get; set; }
    public bool StockAlertActive { get; set; }
    public bool IsActive { get; set; }
}

public class DeleteInventoryArticleCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
}

// ── Warehouses ───────────────────────────────────────────────────────────────

public class CreateWarehouseCommand : IRequest<WarehouseDto>
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
}

public class UpdateWarehouseCommand : IRequest<WarehouseDto>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public bool IsActive { get; set; }
}

public class DeleteWarehouseCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
}

// ── Movements ───────────────────────────────────────────────────────────

public class RegisterMovementCommand : IRequest<StockMovementDto>
{
    public Guid BranchId { get; set; }
    public Guid ArticleId { get; set; }
    public Guid WarehouseId { get; set; }
    public MovementType Type { get; set; }
    public decimal Quantity { get; set; }
    public Guid UnitId { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public class RegisterInitialInventoryCommand : IRequest<List<StockMovementDto>>
{
    public Guid BranchId { get; set; }
    public List<InitialInventoryItemDto> Items { get; set; } = [];
}
