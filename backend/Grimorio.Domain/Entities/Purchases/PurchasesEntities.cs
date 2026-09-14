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
    Factura = 1,
    NotaDeVenta = 2,
    Comprobante = 3,
    LiquidacionCompra = 4,
    Otro = 5,
}

public enum PurchaseStatus
{
    Registrada = 1,
    Anulada = 2,
}

public class Purchase : BaseEntity
{
    public Guid? SupplierId { get; set; }

    public PurchaseDocumentType DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public DateTime DocumentDate { get; set; }
    public string? AccessKey { get; set; }
    public string? AuthorizationNumber { get; set; }
    public DateTime? AuthorizationDate { get; set; }
    public string? Environment { get; set; }
    public string? EmissionType { get; set; }
    public string? SupplierCommercialName { get; set; }
    public string? SupplierMatrixAddress { get; set; }
    public string? SupplierBranchAddress { get; set; }
    public string? SupplierSpecialTaxpayerNumber { get; set; }
    public bool? SupplierObligatedAccounting { get; set; }
    public string? PaymentMethodSriCode { get; set; }
    public string? PaymentMethodName { get; set; }
    public decimal? PaymentAmount { get; set; }
    public string? XmlFileUrl { get; set; }
    public string? PdfFileUrl { get; set; }

    public PurchaseStatus Status { get; set; } = PurchaseStatus.Registrada;
    public string? Notes { get; set; }

    public Guid? DestinationWarehouseId { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxableBase15 { get; set; }
    public decimal TaxableBase0 { get; set; }
    public decimal TaxableBaseExempt { get; set; }
    public decimal TaxableBaseNotSubject { get; set; }
    public decimal Iva15 { get; set; }
    public decimal Ice { get; set; }
    public decimal Irbpnr { get; set; }
    public decimal Tip { get; set; }
    public decimal Total { get; set; }

    public virtual Supplier? Supplier { get; set; }
    public virtual ICollection<PurchaseItem> Items { get; set; } = [];
}

public class PurchaseItem : BaseEntity
{
    public Guid PurchaseId { get; set; }
    public Guid ArticleId { get; set; }
    public string? SupplierMainCode { get; set; }
    public string? SupplierAuxCode { get; set; }
    public string? SupplierDescription { get; set; }
    public string? AdditionalDetail { get; set; }
    public Guid UnitId { get; set; }
    public decimal? InventoryQuantity { get; set; }
    public Guid? InventoryUnitId { get; set; }
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
    public virtual Inventory.MeasurementUnit? InventoryUnit { get; set; }
    public virtual Billing.TaxRate? TaxRate { get; set; }
}
