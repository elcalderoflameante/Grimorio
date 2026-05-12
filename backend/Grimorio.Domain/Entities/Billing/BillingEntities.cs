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
