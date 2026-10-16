using Grimorio.Domain.Entities.Billing;
using Grimorio.SharedKernel;

namespace Grimorio.Domain.Entities.Finance;

public enum TipMethod { Cash, Card, Transfer }

public class SaleTip : BaseEntity
{
    public Guid OrderPaymentId { get; set; }
    public decimal Amount { get; set; }
    public TipMethod Method { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public string RegisteredByName { get; set; } = string.Empty;
    public string? UpdatedByName { get; set; }
    public virtual OrderPayment? OrderPayment { get; set; }
}
