using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.Billing.Commands;

public class SeparateCashSessionCommand : IRequest<CashSessionSeparationResultDto>
{
    public Guid BranchId { get; set; }
    public Guid ActorUserId { get; set; }
    public Guid SessionId { get; set; }
    public SeparateCashSessionDto Data { get; set; } = new();
}
