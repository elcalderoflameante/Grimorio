using Grimorio.SharedKernel;

namespace Grimorio.Domain.Entities.Purchases;

public class Supplier : BaseEntity
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? ContactName { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<PurchaseOrder> Orders { get; set; } = [];
}

public enum PurchaseOrderStatus { Draft = 1, Sent = 2, Received = 3, Cancelled = 4 }

public class PurchaseOrder : BaseEntity
{
    public Guid BranchId { get; set; }
    public Guid SupplierId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? Notes { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public Guid? DestinationWarehouseId { get; set; }

    public virtual Supplier? Supplier { get; set; }
    public virtual ICollection<PurchaseOrderItem> Items { get; set; } = [];
}

public class PurchaseOrderItem : BaseEntity
{
    public Guid BranchId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid ArticleId { get; set; }
    public Guid UnitId { get; set; }
    public decimal QuantityOrdered { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }

    public virtual PurchaseOrder? PurchaseOrder { get; set; }
    public virtual Inventory.InventoryArticle? Article { get; set; }
    public virtual Inventory.MeasurementUnit? Unit { get; set; }
}
