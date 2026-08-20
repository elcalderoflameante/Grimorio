namespace Grimorio.Application.DTOs;

// ── Estaciones de trabajo ─────────────────────────────────────────────────

public class WorkStationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateWorkStationDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class UpdateWorkStationDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

// ── Órdenes ───────────────────────────────────────────────────────────────

public class OrderDto
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? TableId { get; set; }
    public string? TableCode { get; set; }
    public string? CustomerName { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? Notes { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxableBase15 { get; set; }
    public decimal TaxableBase0 { get; set; }
    public decimal TaxableBaseExempt { get; set; }
    public decimal Iva15 { get; set; }
    public decimal Ice { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public int TotalItems { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];
}

public class ActiveOrderSummaryDto
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? TableCode { get; set; }
    public string? CustomerName { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public int TotalItems { get; set; }
}

public class ModifierSelectionDto
{
    public Guid ModifierGroupId { get; set; }
    public Guid ModifierOptionId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string OptionName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPriceDelta { get; set; }
    public decimal TotalPriceDelta { get; set; }
}

public class CreateModifierSelectionDto
{
    public Guid ModifierOptionId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid MenuItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? ItemCode { get; set; }
    public Guid? StationId { get; set; }
    public string? StationName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPct { get; set; }
    public decimal DiscountAmount { get; set; }
    public Guid? PromotionId { get; set; }
    public string? PromotionName { get; set; }
    public Guid? TaxRateId { get; set; }
    public string? TaxRateName { get; set; }
    public decimal? TaxRatePercentage { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
    public bool IsTakeout { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<ModifierSelectionDto> ModifierSelections { get; set; } = [];
}

public class CreateOrderDto
{
    public string Type { get; set; } = string.Empty;
    public Guid? TableId { get; set; }
    public string? CustomerName { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? Notes { get; set; }
    public List<CreateOrderItemDto> Items { get; set; } = [];
}

public class CreateOrderItemDto
{
    public Guid MenuItemId { get; set; }
    public int Quantity { get; set; }
    public decimal DiscountPct { get; set; }
    public Guid? PromotionId { get; set; }
    public string? Notes { get; set; }
    public bool IsTakeout { get; set; }
    public List<CreateModifierSelectionDto> ModifierSelections { get; set; } = [];
}

public class UpdateOrderItemsDto
{
    public List<CreateOrderItemDto> Items { get; set; } = [];
}

public class UpdateOrderItemNotesDto
{
    public string? Notes { get; set; }
}

public class PromotionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateOnly? StartsOn { get; set; }
    public DateOnly? EndsOn { get; set; }
    public TimeOnly? StartsAt { get; set; }
    public TimeOnly? EndsAt { get; set; }
    public int DaysOfWeekMask { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? FixedPrice { get; set; }
    public string PaymentPolicy { get; set; } = string.Empty;
    public decimal? CardPrice { get; set; }
    public int? BuyQuantity { get; set; }
    public int? PayQuantity { get; set; }
    public int Priority { get; set; }
    public List<Guid> MenuItemIds { get; set; } = [];
    public List<Guid> MenuCategoryIds { get; set; } = [];
    public bool IsCurrentlyActive { get; set; }
}

public class UpsertPromotionDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateOnly? StartsOn { get; set; }
    public DateOnly? EndsOn { get; set; }
    public TimeOnly? StartsAt { get; set; }
    public TimeOnly? EndsAt { get; set; }
    public int DaysOfWeekMask { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? FixedPrice { get; set; }
    public string PaymentPolicy { get; set; } = "AnyPayment";
    public decimal? CardPrice { get; set; }
    public int? BuyQuantity { get; set; }
    public int? PayQuantity { get; set; }
    public int Priority { get; set; }
    public List<Guid> MenuItemIds { get; set; } = [];
    public List<Guid> MenuCategoryIds { get; set; } = [];
}

public class AlexaKitchenCommandDto
{
    public Guid BranchId { get; set; }
    public string? RawText { get; set; }
    public string? Action { get; set; }
    public string? TableCode { get; set; }
    public int? OrderNumber { get; set; }
    public string? ItemText { get; set; }
    public bool AllItems { get; set; }
}

public class AlexaKitchenCommandResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Status { get; set; }
    public int UpdatedCount { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];
}

public class AlexaOrderRepeatRequestDto
{
    public Guid BranchId { get; set; }
    public string? TableCode { get; set; }
    public int? OrderNumber { get; set; }
    public string? StationText { get; set; }
    public string? ExcludeStationText { get; set; }
}

public class AlexaOrderRepeatResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? OrderId { get; set; }
    public int? OrderNumber { get; set; }
    public string? TableCode { get; set; }
    public string? StationName { get; set; }
    public string? ExcludedStationName { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];
}

// ── Items por estación (monitor) ──────────────────────────────────────────

public class StationItemDto
{
    public Guid OrderItemId { get; set; }
    public Guid OrderId { get; set; }
    public int OrderNumber { get; set; }
    public string OrderType { get; set; } = string.Empty;
    public string? TableCode { get; set; }
    public string? CustomerName { get; set; }
    public string? OrderNotes { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Notes { get; set; }
    public bool IsTakeout { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ConfirmedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<ModifierSelectionDto> ModifierSelections { get; set; } = [];
}
