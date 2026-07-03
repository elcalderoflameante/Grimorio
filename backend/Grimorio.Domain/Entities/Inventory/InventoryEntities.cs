using Grimorio.SharedKernel;

namespace Grimorio.Domain.Entities.Inventory;

public enum ArticleType
{
    Ingredient = 1,
    FinishedProduct = 2,
    Supply = 3,
}

public enum MovementType
{
    InitialInventory = 1,
    PurchaseEntry = 2,
    ManualEntry = 3,
    ManualExit = 4,
    Waste = 5,
    Spoilage = 6,
    SaleDeduction = 7,
    SaleRestoration = 8,
    TransferIn = 9,
    TransferOut = 10,
    PositiveAdjustment = 11,
    NegativeAdjustment = 12,
}

public enum StockReservationStatus
{
    Active = 1,
    Consumed = 2,
    Released = 3,
}

public class MeasurementUnit : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;

    public virtual ICollection<UnitConversion> OriginConversions { get; set; } = [];
    public virtual ICollection<UnitConversion> DestinationConversions { get; set; } = [];
    public virtual ICollection<InventoryArticle> BaseArticles { get; set; } = [];
    public virtual ICollection<StockMovement> Movements { get; set; } = [];
}

// Permite convertir entre unidades: 1 unidad origen = Factor unidades destino
public class UnitConversion : BaseEntity
{
    public Guid OriginUnitId { get; set; }
    public Guid DestinationUnitId { get; set; }
    public decimal Factor { get; set; }

    public virtual MeasurementUnit? OriginUnit { get; set; }
    public virtual MeasurementUnit? DestinationUnit { get; set; }
}

public class InventoryCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }

    public virtual ICollection<InventoryArticle> Articles { get; set; } = [];
}

public class InventoryArticle : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? InternalCode { get; set; }
    public ArticleType Type { get; set; }
    public Guid CategoryId { get; set; }
    public Guid BaseUnitId { get; set; }
    public decimal MinStock { get; set; } = 0;
    public bool StockAlertActive { get; set; } = true;
    public bool IsActive { get; set; } = true;

    public virtual InventoryCategory? Category { get; set; }
    public virtual MeasurementUnit? BaseUnit { get; set; }
    public virtual ICollection<WarehouseStock> Stocks { get; set; } = [];
    public virtual ICollection<StockMovement> Movements { get; set; } = [];
    public virtual ICollection<StockReservation> Reservations { get; set; } = [];
}

public class Warehouse : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<WarehouseStock> Stocks { get; set; } = [];
    public virtual ICollection<StockMovement> Movements { get; set; } = [];
    public virtual ICollection<StockReservation> Reservations { get; set; } = [];
}

public class WarehouseStock : BaseEntity
{
    public Guid ArticleId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal Quantity { get; set; } = 0;
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual InventoryArticle? Article { get; set; }
    public virtual Warehouse? Warehouse { get; set; }
}

public class StockMovement : BaseEntity
{
    public Guid ArticleId { get; set; }
    public Guid WarehouseId { get; set; }
    public MovementType Type { get; set; }
    public decimal Quantity { get; set; }
    public Guid UnitId { get; set; }
    public decimal BaseQuantity { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public Guid? OrderItemId { get; set; }

    public virtual InventoryArticle? Article { get; set; }
    public virtual Warehouse? Warehouse { get; set; }
    public virtual MeasurementUnit? Unit { get; set; }
}

public class StockReservation : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid OrderItemId { get; set; }
    public Guid ArticleId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal Quantity { get; set; }
    public Guid UnitId { get; set; }
    public decimal BaseQuantity { get; set; }
    public StockReservationStatus Status { get; set; } = StockReservationStatus.Active;
    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConsumedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }

    public virtual InventoryArticle? Article { get; set; }
    public virtual Warehouse? Warehouse { get; set; }
    public virtual MeasurementUnit? Unit { get; set; }
}
