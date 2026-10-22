using Grimorio.SharedKernel;

namespace Grimorio.Domain.Entities.Marketing;

public class LoyaltyProgram : BaseEntity
{
    public string Name { get; set; } = "Pasaporte del aventurero";
    public bool IsActive { get; set; }
    public decimal MinimumSpend { get; set; } = 10m;
    public bool IncludeTax { get; set; } = true;
    public int RewardValidityDays { get; set; } = 30;
    public string TiersJson { get; set; } = "[]";
    public string ExcludedItemIdsJson { get; set; } = "[]";
}

public class LoyaltyMember : BaseEntity
{
    public Guid? CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int? BirthDay { get; set; }
    public int? BirthMonth { get; set; }
    public DateTime? MarketingConsentUpdatedAt { get; set; }
    public string CardCode { get; set; } = string.Empty;
    public string PublicToken { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime AcceptedAt { get; set; }
    public DateTime? PhoneVerifiedAt { get; set; }
    public string? ProgramTermsVersion { get; set; }
    public bool MarketingConsent { get; set; }
    public virtual Billing.Customer? Customer { get; set; }
}

public class LoyaltyPhoneChallenge : BaseEntity
{
    public string Phone { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public int Attempts { get; set; }
    public bool Sent { get; set; }
    public string? MessageId { get; set; }
    public string? DeliveryStatus { get; set; }
}

public class LoyaltyContribution : BaseEntity
{
    public Guid MemberId { get; set; }
    public Guid OrderPaymentId { get; set; }
    public Guid OrderId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public decimal EligibleAmount { get; set; }
    public decimal EligibleSubtotal { get; set; }
}

public class LoyaltyVisit : BaseEntity
{
    public Guid MemberId { get; set; }
    public Guid OrderId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public decimal QualifiedSpend { get; set; }
    public decimal MinimumSpend { get; set; }
}

public class LoyaltyReward : BaseEntity
{
    public Guid MemberId { get; set; }
    public int VisitNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RuleSnapshotJson { get; set; } = "{}";
    public DateTime IssuedAt { get; set; }
    public DateTime AvailableAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
