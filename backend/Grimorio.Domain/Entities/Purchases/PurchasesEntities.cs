using Grimorio.SharedKernel;

namespace Grimorio.Domain.Entities.Purchases;

public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? ContactName { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<Purchase> Purchases { get; set; } = [];
}

public enum PurchaseDocumentType
{
    Factura          = 1,
    NotaDeVenta      = 2,
    Comprobante      = 3,   // Comprobante de venta sin valor tributario
    LiquidacionCompra = 4,  // Para proveedores no obligados a llevar contabilidad
    Otro             = 5,
}

public enum PurchaseStatus
{
    Registrada = 1,
    Anulada    = 2,
}

public class Purchase : BaseEntity
{
    public Guid? SupplierId { get; set; }          // Nullable: proveedor informal o sin registro

    public PurchaseDocumentType DocumentType { get; set; }
    public string? DocumentNumber { get; set; }    // Número del comprobante del proveedor
    public DateTime DocumentDate { get; set; }     // Fecha que figura en el comprobante

    public PurchaseStatus Status { get; set; } = PurchaseStatus.Registrada;
    public string? Notes { get; set; }

    public Guid? DestinationWarehouseId { get; set; }

    // Desglose fiscal (pre-calculado para informes rápidos)
    public decimal Subtotal { get; set; }              // Suma bruta antes de descuentos
    public decimal DiscountTotal { get; set; }         // Total descuentos
    public decimal TaxableBase15 { get; set; }         // Base imponible IVA 15%
    public decimal TaxableBase0 { get; set; }          // Base IVA 0%
    public decimal TaxableBaseExempt { get; set; }     // No objeto / exento de IVA
    public decimal Iva15 { get; set; }                 // IVA 15%
    public decimal Ice { get; set; }                   // ICE (reservado, 0 por ahora)
    public decimal Total { get; set; }

    public virtual Supplier? Supplier { get; set; }
    public virtual ICollection<PurchaseItem> Items { get; set; } = [];
}

public class PurchaseItem : BaseEntity
{
    public Guid PurchaseId { get; set; }
    public Guid ArticleId { get; set; }
    public Guid UnitId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPct { get; set; }
    public decimal DiscountAmount { get; set; }
    public Guid? TaxRateId { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }

    public virtual Purchase? Purchase { get; set; }
    public virtual Inventory.InventoryArticle? Article { get; set; }
    public virtual Inventory.MeasurementUnit? Unit { get; set; }
    public virtual Billing.TaxRate? TaxRate { get; set; }
}
