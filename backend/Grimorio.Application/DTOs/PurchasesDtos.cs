namespace Grimorio.Application.DTOs;

// ── Suppliers ───────────────────────────────────────────────────────────

public class SupplierDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? ContactName { get; set; }
    public bool IsActive { get; set; }
    public int TotalOrders { get; set; }
}

public class CreateSupplierDto
{
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? ContactName { get; set; }
}

public class UpdateSupplierDto : CreateSupplierDto
{
    public bool IsActive { get; set; }
}

// ── Órdenes de compra ─────────────────────────────────────────────────────

public class PurchaseOrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime? ExpectedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? Notes { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public Guid? DestinationWarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public int TotalItems { get; set; }
    public List<PurchaseOrderItemDto> Items { get; set; } = [];
}

public class PurchaseOrderItemDto
{
    public Guid Id { get; set; }
    public Guid ArticleId { get; set; }
    public string ArticleName { get; set; } = string.Empty;
    public string? InternalCode { get; set; }
    public Guid UnitId { get; set; }
    public string UnitSymbol { get; set; } = string.Empty;
    public decimal QuantityOrdered { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
}

public class CreatePurchaseOrderDto
{
    public Guid SupplierId { get; set; }
    public DateTime? ExpectedAt { get; set; }
    public string? Notes { get; set; }
    public Guid? DestinationWarehouseId { get; set; }
    public List<PurchaseOrderItemInputDto> Items { get; set; } = [];
}

public class UpdatePurchaseOrderDto
{
    public Guid SupplierId { get; set; }
    public DateTime? ExpectedAt { get; set; }
    public string? Notes { get; set; }
    public Guid? DestinationWarehouseId { get; set; }
    public List<PurchaseOrderItemInputDto> Items { get; set; } = [];
}

public class PurchaseOrderItemInputDto
{
    public Guid ArticleId { get; set; }
    public Guid UnitId { get; set; }
    public decimal QuantityOrdered { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Notes { get; set; }
}

public class ReceivePurchaseOrderDto
{
    public Guid WarehouseId { get; set; }
    public List<ReceptionItemDto> Items { get; set; } = [];
}

public class ReceptionItemDto
{
    public Guid PurchaseOrderItemId { get; set; }
    public decimal QuantityReceived { get; set; }
}
