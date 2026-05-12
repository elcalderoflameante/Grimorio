using Grimorio.SharedKernel;

namespace Grimorio.Domain.Entities.Billing;

// ── Enums ─────────────────────────────────────────────────────────────────────

public enum TaxIdType { Cedula = 1, Ruc = 2, Passport = 3, FinalConsumer = 4 }

public enum CashSessionStatus { Open = 1, Closed = 2 }

public enum DocumentType { NotaDeVenta = 1, Factura = 2 }

// ── TaxRate ───────────────────────────────────────────────────────────────────
// Tarifas de IVA configurables por sucursal (15%, 5%, 0% según SRI Ecuador)

public class TaxRate : BaseEntity
{
    public string Name { get; set; } = string.Empty;        // "IVA 15%", "IVA 0%"
    public decimal Percentage { get; set; }                  // 15, 5, 0
    public string SriCode { get; set; } = string.Empty;     // "10"=15%, "8"=5%, "0"=0% (códigos SRI)
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

// ── PaymentMethodConfig ───────────────────────────────────────────────────────
// Medios de pago configurables (Efectivo, Tarjeta, Transferencia, etc.)
// No hereda BaseEntity porque no es por sucursal ni requiere auditoría completa.

public class PaymentMethodConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#1677ff";
    public bool IsCash { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public bool IsDeleted { get; set; }
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
}

// ── PaymentLine ───────────────────────────────────────────────────────────────

public class PaymentLine : BaseEntity
{
    public Guid OrderPaymentId { get; set; }
    public Guid PaymentMethodConfigId { get; set; }
    public decimal AmountTendered { get; set; }
    public decimal Change { get; set; }

    public virtual OrderPayment? Payment { get; set; }
    public virtual PaymentMethodConfig? Config { get; set; }
}
