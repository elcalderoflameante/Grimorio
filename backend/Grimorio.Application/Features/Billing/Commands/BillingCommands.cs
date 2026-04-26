using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.Billing.Commands;

// ── Customers ─────────────────────────────────────────────────────────────────

public class CreateCustomerCommand : IRequest<CustomerDto>
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string TaxIdType { get; set; } = "FinalConsumer";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public class UpdateCustomerCommand : IRequest<CustomerDto>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string TaxIdType { get; set; } = "FinalConsumer";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
}

public class DeleteCustomerCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
}

// ── CashSession ───────────────────────────────────────────────────────────────

public class OpenCashSessionCommand : IRequest<CashSessionDto>
{
    public Guid BranchId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
}

public class CloseCashSessionCommand : IRequest<CashSessionDto>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public decimal ActualCash { get; set; }
    public string? Notes { get; set; }
}

// ── Payment ───────────────────────────────────────────────────────────────────

public class PayOrderCommand : IRequest<OrderPaymentDto>
{
    public Guid OrderId { get; set; }
    public Guid BranchId { get; set; }
    public string Method { get; set; } = "Cash";
    public decimal AmountPaid { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? CashSessionId { get; set; }
}
