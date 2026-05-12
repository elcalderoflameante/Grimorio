using Grimorio.SharedKernel;

namespace Grimorio.Domain.Entities.POS;

// ── Enums de servicio de mesa ─────────────────────────────────────────────

public enum TableServiceRequestType
{
    Napkins = 1,
    Salt = 2,
    TomatoSauce = 3,
    Mayonnaise = 4,
    Chili = 5,
    Container = 6,
    Bill = 7,
    CallWaiter = 8,
    Custom = 99,
}

public enum TableServiceRequestStatus
{
    Pending = 1,
    Taken = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5,
}

public class RestaurantTable : Grimorio.SharedKernel.BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Area { get; set; }
    public int Capacity { get; set; } = 2;
    public string PublicToken { get; set; } = Guid.NewGuid().ToString("N");
    public bool IsActive { get; set; } = true;

    public int PosX { get; set; }
    public int PosY { get; set; }

    public virtual ICollection<TableServiceRequest> ServiceRequests { get; set; } = new List<TableServiceRequest>();
    public virtual ICollection<Order> Orders { get; set; } = [];
}

public class TableServiceRequest : Grimorio.SharedKernel.BaseEntity
{
    public Guid RestaurantTableId { get; set; }
    public TableServiceRequestType Type { get; set; }
    public string? CustomMessage { get; set; }
    public TableServiceRequestStatus Status { get; set; } = TableServiceRequestStatus.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? TakenAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? TakenByUserId { get; set; }
    public string? TakenByName { get; set; }
    public string? ClientFingerprint { get; set; }
    public string? SourceIp { get; set; }

    public virtual RestaurantTable? RestaurantTable { get; set; }
}

// ── POS: Pedidos ──────────────────────────────────────────────────────────

public enum OrderType { DineIn = 1, Takeout = 2, Delivery = 3 }

public enum OrderStatus
{
    Draft = 1,
    Confirmed = 2,
    InPreparation = 3,
    Ready = 4,
    Delivered = 5,
    Cancelled = 6,
}

public enum OrderItemStatus { Pending = 1, InPreparation = 2, Ready = 3, Cancelled = 4 }

public enum StationType { Kitchen = 1, Bar = 2, Beverages = 3, HotKitchen = 4, Fries = 5 }

public class WorkStation : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public StationType Type { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<OrderItem> Items { get; set; } = [];
}

public class Order : BaseEntity
{
    public int Number { get; set; }
    public OrderType Type { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public Guid? TableId { get; set; }
    public Guid? WaiterId { get; set; }
    public Guid? CustomerId { get; set; }
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
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? PaidAt { get; set; }

    public virtual RestaurantTable? Table { get; set; }
    public virtual Billing.Customer? Customer { get; set; }
    public virtual ICollection<OrderItem> Items { get; set; } = [];
    public virtual ICollection<Billing.OrderPayment> Payments { get; set; } = [];
}

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid MenuItemId { get; set; }
    public Guid? StationId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPct { get; set; }
    public decimal DiscountAmount { get; set; }
    public Guid? TaxRateId { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
    public OrderItemStatus Status { get; set; } = OrderItemStatus.Pending;

    public virtual Order? Order { get; set; }
    public virtual Menu.MenuItem? MenuItem { get; set; }
    public virtual WorkStation? Station { get; set; }
    public virtual Billing.TaxRate? TaxRate { get; set; }
    public virtual ICollection<OrderItemIngredientChoice> IngredientChoices { get; set; } = [];
}

public class OrderItemIngredientChoice : BaseEntity
{
    public Guid OrderItemId { get; set; }
    public Guid RecipeIngredientId { get; set; }
    public Guid ChosenArticleId { get; set; }

    public virtual OrderItem? OrderItem { get; set; }
    public virtual Menu.RecipeIngredient? RecipeIngredient { get; set; }
    public virtual Inventory.InventoryArticle? ChosenArticle { get; set; }
}
