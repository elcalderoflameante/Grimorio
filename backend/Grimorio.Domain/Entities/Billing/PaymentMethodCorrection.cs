using Grimorio.SharedKernel;

namespace Grimorio.Domain.Entities.Billing;

public class PaymentMethodCorrection : BaseEntity
{
    public Guid OrderPaymentId { get; set; }
    public Guid PaymentLineId { get; set; }
    public Guid CashSessionId { get; set; }
    public Guid RequestId { get; set; }
    public string RequestHash { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string BeforeJson { get; set; } = string.Empty;
    public string AfterJson { get; set; } = string.Empty;
}
