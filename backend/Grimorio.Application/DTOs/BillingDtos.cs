namespace Grimorio.Application.DTOs;

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

    // Totales calculados
    public decimal TotalCash { get; set; }
    public decimal TotalCard { get; set; }
    public decimal TotalTransfer { get; set; }
    public decimal TotalQr { get; set; }
    public decimal TotalSales { get; set; }
    public int TotalOrders { get; set; }
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

public class OrderPaymentDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public int OrderNumber { get; set; }
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerTaxId { get; set; }
    public string Method { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public decimal Change { get; set; }
    public decimal OrderTotal { get; set; }
    public DateTime PaidAt { get; set; }
}

public class PayOrderDto
{
    public string Method { get; set; } = "Cash";
    public decimal AmountPaid { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? CashSessionId { get; set; }
}
