using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.Billing.Queries;

public class PreviewCashSessionSeparationQuery : IRequest<CashSessionSeparationPreviewDto>
{
    public Guid BranchId { get; set; }
    public Guid ActorUserId { get; set; }
    public Guid SessionId { get; set; }
    public SeparateCashSessionDto Data { get; set; } = new();
}

public class GetCashSessionSeparationsQuery : IRequest<List<CashSessionSeparationRecordDto>>
{
    public Guid BranchId { get; set; }
    public Guid SessionId { get; set; }
}
