namespace Grimorio.Application.DTOs;

// ── Suppliers ──────────────────────────────────────────────────────────────────

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
    public int TotalPurchases { get; set; }
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

// ── Compras directas ──────────────────────────────────────────────────────────

public class PurchaseDto
{
    public Guid Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
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
    public string Status { get; set; } = string.Empty;
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? Notes { get; set; }
    public Guid? DestinationWarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    // Desglose fiscal SRI
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
    public int TotalItems { get; set; }
    public List<PurchaseItemDto> Items { get; set; } = [];
}

public class PurchaseItemDto
{
    public Guid Id { get; set; }
    public Guid ArticleId { get; set; }
    public string ArticleName { get; set; } = string.Empty;
    public string? InternalCode { get; set; }
    public string? SupplierMainCode { get; set; }
    public string? SupplierAuxCode { get; set; }
    public string? SupplierDescription { get; set; }
    public string? AdditionalDetail { get; set; }
    public Guid UnitId { get; set; }
    public string UnitSymbol { get; set; } = string.Empty;
    public decimal? InventoryQuantity { get; set; }
    public Guid? InventoryUnitId { get; set; }
    public string? InventoryUnitSymbol { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPct { get; set; }
    public decimal DiscountAmount { get; set; }
    public Guid? TaxRateId { get; set; }
    public string? TaxRateName { get; set; }
    public decimal? TaxRatePercentage { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
}

public class CreatePurchaseDto
{
    public int DocumentType { get; set; }
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
    public decimal? Ice { get; set; }
    public decimal? Irbpnr { get; set; }
    public decimal? Tip { get; set; }
    public string? XmlFileUrl { get; set; }
    public string? PdfFileUrl { get; set; }
    public Guid? SupplierId { get; set; }
    public string? Notes { get; set; }
    public Guid? DestinationWarehouseId { get; set; }
    public List<PurchaseItemInputDto> Items { get; set; } = [];
}

public class UpdatePurchaseDto
{
    public int DocumentType { get; set; }
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
    public decimal? Ice { get; set; }
    public decimal? Irbpnr { get; set; }
    public decimal? Tip { get; set; }
    public string? XmlFileUrl { get; set; }
    public string? PdfFileUrl { get; set; }
    public Guid? SupplierId { get; set; }
    public string? Notes { get; set; }
    public Guid? DestinationWarehouseId { get; set; }
    public List<PurchaseItemInputDto> Items { get; set; } = [];
}

public class PurchaseItemInputDto
{
    public Guid ArticleId { get; set; }
    public Guid UnitId { get; set; }
    public string? SupplierMainCode { get; set; }
    public string? SupplierAuxCode { get; set; }
    public string? SupplierDescription { get; set; }
    public string? AdditionalDetail { get; set; }
    public decimal Quantity { get; set; }
    public decimal? InventoryQuantity { get; set; }
    public Guid? InventoryUnitId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPct { get; set; }
    public decimal? DiscountAmount { get; set; }
    public Guid? TaxRateId { get; set; }
    public string? Notes { get; set; }
}

public class PurchaseAttachmentDto
{
    public string FileUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}

public class PurchaseInvoiceImportDto
{
    public int DocumentType { get; set; } = 1;
    public string? DocumentNumber { get; set; }
    public DateTime? DocumentDate { get; set; }
    public string? AccessKey { get; set; }
    public string? AuthorizationNumber { get; set; }
    public DateTime? AuthorizationDate { get; set; }
    public string? Environment { get; set; }
    public string? EmissionType { get; set; }
    public string? SupplierTaxId { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierCommercialName { get; set; }
    public string? SupplierMatrixAddress { get; set; }
    public string? SupplierBranchAddress { get; set; }
    public string? SupplierSpecialTaxpayerNumber { get; set; }
    public bool? SupplierObligatedAccounting { get; set; }
    public string? PaymentMethodSriCode { get; set; }
    public string? PaymentMethodName { get; set; }
    public decimal? PaymentAmount { get; set; }
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
    public string? XmlFileUrl { get; set; }
    public List<PurchaseInvoiceImportItemDto> Items { get; set; } = [];
}

public class PurchaseInvoiceImportItemDto
{
    public string? SupplierMainCode { get; set; }
    public string? SupplierAuxCode { get; set; }
    public string SupplierDescription { get; set; } = string.Empty;
    public string? AdditionalDetail { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercentage { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalPrice { get; set; }
}
