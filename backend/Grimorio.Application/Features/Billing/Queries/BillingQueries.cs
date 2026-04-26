using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.Billing.Queries;

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
