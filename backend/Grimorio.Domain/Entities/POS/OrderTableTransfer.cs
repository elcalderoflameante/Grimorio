using Grimorio.SharedKernel;

namespace Grimorio.Domain.Entities.POS;

public class OrderTableTransfer : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid RequestId { get; set; }
    public Guid FromTableId { get; set; }
    public Guid ToTableId { get; set; }
    public Guid ActorUserId { get; set; }
}
