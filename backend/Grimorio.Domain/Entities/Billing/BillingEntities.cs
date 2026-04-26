using Grimorio.SharedKernel;

namespace Grimorio.Domain.Entities.Billing;

// ── Enums ─────────────────────────────────────────────────────────────────────

public enum PaymentMethod { Cash = 1, Card = 2, Transfer = 3, QR = 4 }

public enum TaxIdType { Cedula = 1, Ruc = 2, Passport = 3, FinalConsumer = 4 }

public enum CashSessionStatus { Open = 1, Closed = 2 }

// ── Customer ──────────────────────────────────────────────────────────────────

public class Customer : BaseEntity
{
    public Guid BranchId { get; set; }
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
    public Guid BranchId { get; set; }
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
    public Guid BranchId { get; set; }
    public Guid OrderId { get; set; }
    public Guid? CashSessionId { get; set; }
    public Guid? CustomerId { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Change { get; set; }
    public decimal OrderTotal { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;

    public virtual POS.Order? Order { get; set; }
    public virtual CashSession? CashSession { get; set; }
    public virtual Customer? Customer { get; set; }
}
