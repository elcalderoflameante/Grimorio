using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.Billing.Commands;

public class CorrectPaymentMethodCommand : IRequest<bool>
{
    public Guid BranchId { get; set; }
    public Guid ActorUserId { get; set; }
    public Guid PaymentId { get; set; }
    public CorrectPaymentMethodDto Data { get; set; } = new();
}
