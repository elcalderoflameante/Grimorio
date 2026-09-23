namespace Grimorio.Application.DTOs;

public class InventoryReconciliationDto
{
    public DateTime CheckedAt { get; set; }
    public int CheckedBalances { get; set; }
    public int ConfirmedCount { get; set; }
    public int ReviewCount { get; set; }
    public int IncompleteCount { get; set; }
    public int TotalFindings { get; set; }
    public List<InventoryReconciliationFindingDto> Findings { get; set; } = [];
}

public class InventoryReconciliationFindingDto
{
    public string Code { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Guid ArticleId { get; set; }
    public string ArticleName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string? UnitSymbol { get; set; }
    public decimal? ExpectedQuantity { get; set; }
    public decimal? ActualQuantity { get; set; }
    public string? Reference { get; set; }
    public Guid? MovementId { get; set; }
    public Guid? SourceId { get; set; }
}

public class StockMovementTraceDto
{
    public Guid MovementId { get; set; }
    public string Origin { get; set; } = string.Empty;
    public Guid? OrderId { get; set; }
    public int? OrderNumber { get; set; }
    public Guid? OrderItemId { get; set; }
    public Guid? OrderPaymentId { get; set; }
    public Guid? OrderPaymentItemId { get; set; }
    public decimal? PaidItemQuantity { get; set; }
    public DateTime? PaidAt { get; set; }
    public Guid? StockReservationId { get; set; }
    public Guid? PurchaseId { get; set; }
    public Guid? PurchaseItemId { get; set; }
    public string? DocumentNumber { get; set; }
    public Guid? ProductionOrderId { get; set; }
    public string? ProductionNumber { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
