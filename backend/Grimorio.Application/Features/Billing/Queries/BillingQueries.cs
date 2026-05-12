using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.Billing.Queries;

public class GetTaxRatesQuery : IRequest<List<TaxRateDto>>
{
    public Guid BranchId { get; set; }
    public bool ActiveOnly { get; set; } = false;
}

public class GetBranchTaxConfigQuery : IRequest<BranchTaxConfigDto?>
{
    public Guid BranchId { get; set; }
}

public class GetPaymentMethodsQuery : IRequest<List<PaymentMethodConfigDto>>
{
    public bool ActiveOnly { get; set; } = true;
}

public class GetCustomersQuery : IRequest<List<CustomerDto>>
{
    public Guid BranchId { get; set; }
    public bool? ActiveOnly { get; set; }
    public string? Search { get; set; }
}

public class GetActiveCashSessionQuery : IRequest<CashSessionDto?>
{
    public Guid BranchId { get; set; }
}

public class GetCashSessionsQuery : IRequest<List<CashSessionDto>>
{
    public Guid BranchId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int PageSize { get; set; } = 30;
}

public class GetCashSessionDetailQuery : IRequest<CashSessionDto?>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
}

public class GetOrderPaymentsQuery : IRequest<List<OrderPaymentDto>>
{
    public Guid OrderId { get; set; }
    public Guid BranchId { get; set; }
}

public class GetSalesQuery : IRequest<List<OrderPaymentDto>>
{
    public Guid BranchId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int PageSize { get; set; } = 100;
}

// ── SRI ───────────────────────────────────────────────────────────────────────

public class GetSriCertificateStatusQuery : IRequest<SriCertificateStatusDto>
{
    public Guid BranchId { get; set; }
}

public class GetElectronicDocumentsQuery : IRequest<List<ElectronicDocumentDto>>
{
    public Guid BranchId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public string? Status { get; set; }
    public int PageSize { get; set; } = 50;
}

public class GetElectronicDocumentDetailQuery : IRequest<ElectronicDocumentDto?>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
}
