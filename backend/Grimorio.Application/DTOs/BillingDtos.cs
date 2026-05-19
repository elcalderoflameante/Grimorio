namespace Grimorio.Application.DTOs;

// ── TaxRate ───────────────────────────────────────────────────────────────────

public class TaxRateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public string SriCode { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}

public class UpsertTaxRateDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public string SriCode { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

// ── BranchTaxConfig ───────────────────────────────────────────────────────────

public class BranchTaxConfigDto
{
    public Guid? Id { get; set; }
    public string Ruc { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public string CodigoEstablecimiento { get; set; } = "001";
    public string PuntoEmision { get; set; } = "001";
    public string Ambiente { get; set; } = "1";
    public string? ContribuyenteEspecial { get; set; }
    public bool ObligadoContabilidad { get; set; }
    public long SecuencialInicial { get; set; } = 1;
    public long Secuencial { get; set; }
}

// ── SRI Certificado ───────────────────────────────────────────────────────────

public class SriCertificateStatusDto
{
    public bool HasCertificate { get; set; }
    public string? FileName { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
    public string? UploadedAt { get; set; }
}

// ── Documento Electrónico ─────────────────────────────────────────────────────

public class ElectronicDocumentBytesDto
{
    public Guid Id { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
    public byte[]? RidePdf { get; set; }
    public string? XmlSigned { get; set; }
    public string? XmlAuthorized { get; set; }
    public string? XmlResponseSri { get; set; }
}

public class ElectronicDocumentDto
{
    public Guid Id { get; set; }
    public Guid OrderPaymentId { get; set; }
    public string ClaveAcceso { get; set; } = string.Empty;
    public string NumeroFactura { get; set; } = string.Empty;
    public long Secuencial { get; set; }
    public string Environment { get; set; } = "1";
    public string Status { get; set; } = string.Empty;

    public decimal TotalSinImpuestos { get; set; }
    public decimal TotalDescuento { get; set; }
    public decimal TotalIva { get; set; }
    public decimal ImporteTotal { get; set; }

    public string? NumeroAutorizacion { get; set; }
    public DateTime? FechaAutorizacion { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool HasRide { get; set; }
    public bool HasXml { get; set; }
    public bool HasXmlResponse { get; set; }
}

// ── SmtpConfig ────────────────────────────────────────────────────────────────

public class SmtpConfigDto
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public bool HasPassword { get; set; }
}

public class UpsertSmtpConfigDto
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; }   // null = mantener contraseña actual
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

// ── PaymentMethodConfig ───────────────────────────────────────────────────────

public class PaymentMethodConfigDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#1677ff";
    public bool IsCash { get; set; }
    public bool IsCard { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

public class CreatePaymentMethodConfigDto
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#1677ff";
    public bool IsCash { get; set; }
    public bool IsCard { get; set; }
    public int SortOrder { get; set; }
}

public class UpdatePaymentMethodConfigDto
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#1677ff";
    public bool IsCash { get; set; }
    public bool IsCard { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

public class CardBankDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

public class CreateCardBankDto
{
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class UpdateCardBankDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

// ── Customer ──────────────────────────────────────────────────────────────────

public class CustomerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string TaxIdType { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
}

public class CreateCustomerDto
{
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string TaxIdType { get; set; } = "FinalConsumer";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public class UpdateCustomerDto
{
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string TaxIdType { get; set; } = "FinalConsumer";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
}

// ── CashSession ───────────────────────────────────────────────────────────────

public class PaymentMethodTotalDto
{
    public Guid MethodId { get; set; }
    public string MethodName { get; set; } = string.Empty;
    public string MethodColor { get; set; } = "#1677ff";
    public bool IsCash { get; set; }
    public decimal Total { get; set; }
}

public class CashSessionDto
{
    public Guid Id { get; set; }
    public string OpenedByName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? ClosedByName { get; set; }
    public decimal? ActualCash { get; set; }
    public string? CloseNotes { get; set; }
    public string Status { get; set; } = string.Empty;

    public List<PaymentMethodTotalDto> Totals { get; set; } = [];
    public decimal TotalSales { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalCash => Totals.Where(t => t.IsCash).Sum(t => t.Total);
    public decimal ExpectedCash { get; set; }
    public decimal? CashDifference { get; set; }
}

public class OpenCashSessionDto
{
    public decimal OpeningBalance { get; set; }
}

public class CloseCashSessionDto
{
    public decimal ActualCash { get; set; }
    public string? Notes { get; set; }
}

// ── Payment ───────────────────────────────────────────────────────────────────

public class PaymentLineDto
{
    public Guid Id { get; set; }
    public Guid MethodId { get; set; }
    public string MethodName { get; set; } = string.Empty;
    public string MethodColor { get; set; } = "#1677ff";
    public bool IsCash { get; set; }
    public bool IsCard { get; set; }
    public decimal AmountTendered { get; set; }
    public decimal Change { get; set; }
    public decimal NetAmount => AmountTendered - Change;
    public string? CardPaymentType { get; set; }
    public Guid? CardBankId { get; set; }
    public string? CardBankName { get; set; }
    public string? CardBrand { get; set; }
    public string? AuthorizationNumber { get; set; }
}

public class OrderPaymentDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public int OrderNumber { get; set; }
    public string? OrderType { get; set; }
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerTaxId { get; set; }
    public string? TableCode { get; set; }
    public string? TableName { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public decimal OrderAmount { get; set; }
    public DateTime PaidAt { get; set; }
    public List<PaymentLineDto> Lines { get; set; } = [];
    public Guid? ElectronicDocumentId { get; set; }
    public string? ElectronicDocumentStatus { get; set; }
}

public class AddPaymentLineDto
{
    public Guid MethodId { get; set; }
    public decimal AmountTendered { get; set; }
    public string? CardPaymentType { get; set; }
    public Guid? CardBankId { get; set; }
    public string? CardBrand { get; set; }
    public string? AuthorizationNumber { get; set; }
}

public class AddOrderPaymentDto
{
    public decimal OrderAmount { get; set; }
    public string DocumentType { get; set; } = "NotaDeVenta";
    public Guid? CustomerId { get; set; }
    public Guid? CashSessionId { get; set; }
    public List<AddPaymentLineDto> Lines { get; set; } = [];
}

// ── InvoiceTemplate ───────────────────────────────────────────────────────────

public class PdfBlockDto
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public bool Visible { get; set; } = true;
    public string Label { get; set; } = "";
    // header
    public string PrimaryColor { get; set; } = "#1677ff";
    public bool ShowLogo { get; set; } = true;
    // customer
    public bool ShowEmail { get; set; } = true;
    public bool ShowPhone { get; set; } = true;
    public bool ShowAddress { get; set; } = true;
    // items
    public bool ShowAuxCode { get; set; } = false;
    public bool ShowDiscount { get; set; } = true;
    // totals
    public bool ShowZeroLines { get; set; } = true;
    // footer
    public string? CustomText { get; set; }
}

public class EmailBlockDto
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public bool Visible { get; set; } = true;
    public string Label { get; set; } = "";
    // header
    public string BgColor { get; set; } = "#1677ff";
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    // text blocks (greeting, message, legal_note, footer)
    public string? Text { get; set; }
}

public class InvoiceTemplateDto
{
    public string? LogoBase64 { get; set; }
    public string PrimaryColor { get; set; } = "#1677ff";
    public string AccentColor { get; set; } = "#e6f4ff";
    public List<PdfBlockDto> PdfBlocks { get; set; } = [];
    public string EmailSubject { get; set; } = "Factura Electrónica {numeroFactura} — {razonSocial}";
    public List<EmailBlockDto> EmailBlocks { get; set; } = [];
}

public class UpsertInvoiceTemplateDto
{
    public string? LogoBase64 { get; set; }
    public string PrimaryColor { get; set; } = "#1677ff";
    public string AccentColor { get; set; } = "#e6f4ff";
    public List<PdfBlockDto> PdfBlocks { get; set; } = [];
    public string EmailSubject { get; set; } = "Factura Electrónica {numeroFactura} — {razonSocial}";
    public List<EmailBlockDto> EmailBlocks { get; set; } = [];
}
