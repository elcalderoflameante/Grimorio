using Grimorio.SharedKernel;

namespace Grimorio.Domain.Entities.Billing;

// ── Enums ─────────────────────────────────────────────────────────────────────

public enum TaxIdType { Cedula = 1, Ruc = 2, Passport = 3, FinalConsumer = 4 }

public enum CashSessionStatus { Open = 1, Closed = 2 }

public enum DocumentType { NotaDeVenta = 1, Factura = 2 }

public enum CardPaymentType { Credit = 1, Debit = 2 }

// ── TaxRate ───────────────────────────────────────────────────────────────────
// Tarifas de IVA configurables por sucursal (15%, 5%, 0% según SRI Ecuador)

public class TaxRate : BaseEntity
{
    public string Name { get; set; } = string.Empty;        // "IVA 15%", "IVA 0%"
    public decimal Percentage { get; set; }                  // 15, 5, 0
    public string SriCode { get; set; } = string.Empty;     // codigoPorcentaje SRI: "4"=15%, "8"=8%(feriado), "0"=0%, "6"=exento
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

// ── BranchTaxConfig ───────────────────────────────────────────────────────────
// Datos del emisor requeridos por el SRI para comprobantes electrónicos

public class BranchTaxConfig : BaseEntity
{
    public string Ruc { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public string CodigoEstablecimiento { get; set; } = "001";
    public string PuntoEmision { get; set; } = "001";
    public string Ambiente { get; set; } = "1";             // "1"=pruebas, "2"=producción
    public string? ContribuyenteEspecial { get; set; }      // número de resolución si aplica
    public bool ObligadoContabilidad { get; set; }
    public long SecuencialInicial { get; set; } = 1;       // primer secuencial a emitir (configurable)
    public long Secuencial { get; set; }                    // último secuencial emitido
}

// ── SriCertificate ────────────────────────────────────────────────────────────
// Certificado .p12 cifrado para la firma electrónica XAdES-BES del SRI Ecuador

public class SriCertificate : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public byte[] CertificateEncrypted { get; set; } = [];
    public string PasswordEncrypted { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
}

// ── ElectronicDocument ────────────────────────────────────────────────────────
// Comprobante electrónico SRI: factura electrónica vinculada a un cobro

public enum ElectronicDocumentStatus
{
    Pending = 1,       // Generado localmente, no enviado
    Sent = 2,          // Enviado al SRI, pendiente de autorización
    Authorized = 3,    // Autorizado por el SRI
    Rejected = 4,      // Rechazado por el SRI
    Cancelled = 5,     // Anulado
}

public class ElectronicDocument : BaseEntity
{
    public Guid OrderPaymentId { get; set; }
    public string ClaveAcceso { get; set; } = string.Empty;        // 49 dígitos
    public string NumeroFactura { get; set; } = string.Empty;      // 001-001-000000001
    public long Secuencial { get; set; }
    public string Environment { get; set; } = "1";
    public ElectronicDocumentStatus Status { get; set; } = ElectronicDocumentStatus.Pending;

    public decimal TotalSinImpuestos { get; set; }
    public decimal TotalDescuento { get; set; }
    public decimal TotalIva { get; set; }
    public decimal ImporteTotal { get; set; }

    public string? XmlSigned { get; set; }
    public string? XmlAuthorized { get; set; }
    public string? XmlResponseSri { get; set; }     // respuesta XML cruda del SRI al rechazar
    public string? NumeroAutorizacion { get; set; }
    public DateTime? FechaAutorizacion { get; set; }
    public byte[]? RidePdf { get; set; }

    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }
    public int RetryCount { get; set; }

    public virtual OrderPayment? OrderPayment { get; set; }
}

// ── SmtpConfig ────────────────────────────────────────────────────────────────
// Configuración SMTP por sucursal para envío de facturas electrónicas por correo

public class SmtpConfig : BaseEntity
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string PasswordEncrypted { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

// ── PaymentMethodConfig ───────────────────────────────────────────────────────
// Medios de pago configurables (Efectivo, Tarjeta, Transferencia, etc.)
// No hereda BaseEntity porque no es por sucursal ni requiere auditoría completa.

public class PaymentMethodConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#1677ff";
    public bool IsCash { get; set; }
    public bool IsCard { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public bool IsDeleted { get; set; }
}

public class CardBank : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public class CashRegister : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<CashSession> Sessions { get; set; } = [];
}

// ── Customer ──────────────────────────────────────────────────────────────────

public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public TaxIdType TaxIdType { get; set; } = TaxIdType.FinalConsumer;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
}

// ── CashSession ───────────────────────────────────────────────────────────────

public class CashSession : BaseEntity
{
    public Guid CashRegisterId { get; set; }
    public Guid OpenedBy { get; set; }
    public string OpenedByName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public Guid? ClosedBy { get; set; }
    public string? ClosedByName { get; set; }
    public decimal? ActualCash { get; set; }
    public string? CloseNotes { get; set; }
    public CashSessionStatus Status { get; set; } = CashSessionStatus.Open;

    public virtual CashRegister? CashRegister { get; set; }
    public virtual ICollection<OrderPayment> Payments { get; set; } = [];
}

// ── OrderPayment ──────────────────────────────────────────────────────────────

public class OrderPayment : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid? CashSessionId { get; set; }
    public Guid? CustomerId { get; set; }
    public DocumentType DocumentType { get; set; } = DocumentType.NotaDeVenta;
    public decimal OrderAmount { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;

    public virtual POS.Order? Order { get; set; }
    public virtual CashSession? CashSession { get; set; }
    public virtual Customer? Customer { get; set; }
    public virtual ICollection<PaymentLine> Lines { get; set; } = [];
    public virtual ICollection<OrderPaymentItem> Items { get; set; } = [];
}

public class OrderPaymentItem : BaseEntity
{
    public Guid OrderPaymentId { get; set; }
    public Guid OrderItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }

    public virtual OrderPayment? Payment { get; set; }
    public virtual POS.OrderItem? OrderItem { get; set; }
}

// ── PaymentLine ───────────────────────────────────────────────────────────────

public class PaymentLine : BaseEntity
{
    public Guid OrderPaymentId { get; set; }
    public Guid PaymentMethodConfigId { get; set; }
    public decimal AmountTendered { get; set; }
    public decimal Change { get; set; }
    public CardPaymentType? CardPaymentType { get; set; }
    public Guid? CardBankId { get; set; }
    public string? CardBankName { get; set; }
    public string? CardBrand { get; set; }
    public string? AuthorizationNumber { get; set; }

    public virtual OrderPayment? Payment { get; set; }
    public virtual PaymentMethodConfig? Config { get; set; }
}

// ── InvoiceTemplate ───────────────────────────────────────────────────────────
// Plantilla visual para el RIDE PDF y el correo de factura electrónica

public class InvoiceTemplate : BaseEntity
{
    public string? LogoBase64 { get; set; }
    public string PrimaryColor { get; set; } = "#1677ff";
    public string AccentColor { get; set; } = "#e6f4ff";
    public string PdfBlocksJson { get; set; } = InvoiceTemplate.DefaultPdfBlocks;
    public string EmailSubject { get; set; } = "Factura Electrónica {numeroFactura} — {razonSocial}";
    public string EmailBlocksJson { get; set; } = InvoiceTemplate.DefaultEmailBlocks;

    public const string DefaultPdfBlocks =
        """[{"id":"header","type":"header","visible":true,"label":"Encabezado","primaryColor":"#1677ff","showLogo":true},{"id":"customer","type":"customer","visible":true,"label":"Datos del comprador","showEmail":true,"showPhone":true,"showAddress":true},{"id":"items","type":"items","visible":true,"label":"Detalle de productos","showAuxCode":false,"showDiscount":true},{"id":"payments","type":"payments","visible":true,"label":"Forma de pago"},{"id":"totals","type":"totals","visible":true,"label":"Totales","showZeroLines":true},{"id":"footer","type":"footer","visible":true,"label":"Pie de página","customText":"¡Gracias por su compra!"}]""";

    public const string DefaultEmailBlocks =
        """[{"id":"header","type":"header","visible":true,"label":"Encabezado","bgColor":"#1677ff","title":"Factura Electrónica","subtitle":"Documento autorizado por el SRI Ecuador"},{"id":"greeting","type":"greeting","visible":true,"label":"Saludo","text":"Estimado/a {nombreCliente},"},{"id":"message","type":"message","visible":true,"label":"Mensaje principal","text":"Adjunto encontrará el RIDE (Representación Impresa del Documento Electrónico) de su factura autorizada por el SRI Ecuador."},{"id":"invoice_summary","type":"invoice_summary","visible":true,"label":"Resumen de factura"},{"id":"legal_note","type":"legal_note","visible":true,"label":"Nota legal","text":"Este documento tiene validez legal ante el Servicio de Rentas Internas (SRI) del Ecuador."},{"id":"footer","type":"footer","visible":true,"label":"Pie de correo","text":"Generado por Grimorio"}]""";
}
