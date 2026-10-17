using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.Billing.Queries;

public class GetPaymentMethodCorrectionsQuery : IRequest<List<PaymentMethodCorrectionDto>>
{
    public Guid BranchId { get; set; }
    public Guid PaymentId { get; set; }
}
