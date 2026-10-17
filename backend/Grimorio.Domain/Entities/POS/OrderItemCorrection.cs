using Grimorio.SharedKernel;

namespace Grimorio.Domain.Entities.POS;

public class OrderItemCorrection : BaseEntity
{
    public Guid RequestId { get; set; }
    public string RequestHash { get; set; } = string.Empty;
    public Guid OriginalItemId { get; set; }
    public Guid? ReplacementItemId { get; set; }
    public string Disposition { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public OrderItemStatus OriginalStatus { get; set; }
    public virtual OrderItem? OriginalItem { get; set; }
    public virtual OrderItem? ReplacementItem { get; set; }
}
