namespace Grimorio.Application.DTOs;

public class CorrectPaymentMethodDto
{
    public Guid RequestId { get; set; }
    public Guid PaymentLineId { get; set; }
    public Guid ExpectedMethodId { get; set; }
    public DateTime? ExpectedUpdatedAt { get; set; }
    public Guid MethodId { get; set; }
    public string? CardPaymentType { get; set; }
    public Guid? CardBankId { get; set; }
    public string? CardBrand { get; set; }
    public string? AuthorizationNumber { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class PaymentMethodCorrectionDto
{
    public Guid Id { get; set; }
    public Guid PaymentLineId { get; set; }
    public DateTime CorrectedAt { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public PaymentLineDto Before { get; set; } = new();
    public PaymentLineDto After { get; set; } = new();
}
