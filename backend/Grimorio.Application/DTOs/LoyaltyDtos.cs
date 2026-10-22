namespace Grimorio.Application.DTOs;

public class LoyaltyProductDto
{
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
}
public class LoyaltyTierDto
{
    public int VisitNumber { get; set; }
    public int? RepeatEvery { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = "Bundle";
    public bool IsEnabled { get; set; }
    public decimal? MaximumPrice { get; set; }
    public List<LoyaltyProductDto> Products { get; set; } = [];
}
public class LoyaltyProgramDto
{
    public string Name { get; set; } = "Pasaporte del aventurero";
    public bool IsActive { get; set; }
    public decimal MinimumSpend { get; set; } = 10;
    public bool IncludeTax { get; set; } = true;
    public int RewardValidityDays { get; set; } = 30;
    public List<Guid> ExcludedItemIds { get; set; } = [];
    public List<LoyaltyTierDto> Tiers { get; set; } = [
        new() { VisitNumber = 5, Name = "Helado sundae" },
        new() { VisitNumber = 10, Name = "Papas y sundae" },
        new() { VisitNumber = 15, Name = "Entrada", Kind = "Choice", MaximumPrice = 5 },
        new() { VisitNumber = 20, Name = "Premio especial" },
        new() { VisitNumber = 25, RepeatEvery = 5, Name = "Plato del menu", Kind = "Choice", MaximumPrice = 10 },
    ];
}
public class LoyaltyMemberProfileDto
{
    public Guid? CustomerId { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int? BirthDay { get; set; }
    public int? BirthMonth { get; set; }
    public bool MarketingConsent { get; set; }
}
public class EnrollLoyaltyMemberDto : LoyaltyMemberProfileDto
{
    public bool AcceptsProgram { get; set; }
}
public class LoyaltyMemberDto
{
    public Guid Id { get; set; }
    public Guid? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? BillingCustomerName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int? BirthDay { get; set; }
    public int? BirthMonth { get; set; }
    public bool MarketingConsent { get; set; }
    public string CardCode { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int Visits { get; set; }
    public int Rewards { get; set; }
}
public record LoyaltyVisitDto(Guid Id, DateOnly BusinessDate, decimal QualifiedSpend, decimal MinimumSpend);
public record LoyaltyRewardDto(Guid Id, int VisitNumber, string Name, DateTime IssuedAt, DateTime AvailableAt, DateTime ExpiresAt,
    string Status, LoyaltyTierDto Rule);
public record LoyaltyMemberDetailDto(LoyaltyMemberDto Member, string PublicToken, List<LoyaltyVisitDto> Visits, List<LoyaltyRewardDto> Rewards);
public record PublicLoyaltyCardDto(string ProgramName, string CustomerName, string CardCode, int Visits, int? NextVisit,
    string? NextReward, List<LoyaltyRewardDto> Rewards);
public record LoyaltySummaryDto(int Members, int ActiveMembers, int Visits, int AvailableRewards, int ExpiredRewards);
