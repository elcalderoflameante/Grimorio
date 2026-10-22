using System.ComponentModel.DataAnnotations;

namespace Grimorio.Application.DTOs;

public record PublicLoyaltyInfoDto(bool Enabled, bool TestMode, string ProgramName, decimal MinimumSpend,
    bool IncludeTax, int RewardValidityDays, string TermsVersion, List<PublicLoyaltyTierDto> Rewards);
public record PublicLoyaltyTierDto(int VisitNumber, int? RepeatEvery, string Name);
public class RequestLoyaltyCodeDto
{
    [Required, StringLength(40)] public string Phone { get; set; } = string.Empty;
    [Required, RegularExpression("^(Enroll|Recover)$")] public string Purpose { get; set; } = "Enroll";
}
public class VerifyLoyaltyCodeDto
{
    public Guid ChallengeId { get; set; }
    [Required, RegularExpression("^[0-9]{6}$")] public string Code { get; set; } = string.Empty;
    [StringLength(200)] public string? Name { get; set; }
    [StringLength(254)] public string? Email { get; set; }
    public int? BirthDay { get; set; }
    public int? BirthMonth { get; set; }
    public bool AcceptsProgram { get; set; }
    public bool MarketingConsent { get; set; }
    [StringLength(40)] public string? TermsVersion { get; set; }
}
public record LoyaltyCodeSentDto(Guid ChallengeId, DateTime ExpiresAt, int RetryAfterSeconds);
public record PublicLoyaltyAccessDto(string PublicToken);
