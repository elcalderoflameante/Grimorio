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
    public string? TableName { get; set; }
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

public class IngredientChoiceDto
{
    public Guid RecipeIngredientId { get; set; }
    public Guid ChosenArticleId { get; set; }
    public string ChosenArticleName { get; set; } = string.Empty;
}

public class CreateIngredientChoiceDto
{
    public Guid RecipeIngredientId { get; set; }
    public Guid ChosenArticleId { get; set; }
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
    public Guid? TaxRateId { get; set; }
    public string? TaxRateName { get; set; }
    public decimal? TaxRatePercentage { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<IngredientChoiceDto> IngredientChoices { get; set; } = [];
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
    public string? Notes { get; set; }
    public List<CreateIngredientChoiceDto> IngredientChoices { get; set; } = [];
}

public class UpdateOrderItemsDto
{
    public List<CreateOrderItemDto> Items { get; set; } = [];
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
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ConfirmedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<IngredientChoiceDto> IngredientChoices { get; set; } = [];
}
