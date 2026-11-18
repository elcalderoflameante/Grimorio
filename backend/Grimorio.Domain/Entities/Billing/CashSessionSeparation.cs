using Grimorio.SharedKernel;

namespace Grimorio.Domain.Entities.Billing;

public class CashSessionSeparation : BaseEntity
{
    public Guid PreviousSessionId { get; set; }
    public Guid NewSessionId { get; set; }
    public Guid RequestId { get; set; }
    public string RequestHash { get; set; } = string.Empty;
    public DateTime CutoffUtc { get; set; }
    public Guid ActorUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string PreviewJson { get; set; } = string.Empty;
}
