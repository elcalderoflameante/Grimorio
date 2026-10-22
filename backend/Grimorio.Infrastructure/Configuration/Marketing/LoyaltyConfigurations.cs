using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.Marketing;
using Grimorio.Domain.Entities.POS;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grimorio.Infrastructure.Configuration.Marketing;

public class LoyaltyProgramConfiguration : BaseEntityConfiguration<LoyaltyProgram>
{
    public override void Configure(EntityTypeBuilder<LoyaltyProgram> b) {
        base.Configure(b); b.ToTable("Programs", "marketing");
        b.Property(x => x.Name).HasMaxLength(120); b.Property(x => x.MinimumSpend).HasPrecision(18, 2);
        b.Property(x => x.TiersJson).HasColumnType("jsonb"); b.Property(x => x.ExcludedItemIdsJson).HasColumnType("jsonb");
        b.HasIndex(x => x.BranchId).IsUnique();
    }
}
public class LoyaltyMemberConfiguration : BaseEntityConfiguration<LoyaltyMember>
{
    public override void Configure(EntityTypeBuilder<LoyaltyMember> b) {
        base.Configure(b); b.ToTable("Members", "marketing", t => t.HasCheckConstraint("CK_LoyaltyMembers_Birthday",
            "(\"BirthDay\" IS NULL AND \"BirthMonth\" IS NULL) OR (\"BirthDay\" IS NOT NULL AND \"BirthMonth\" IS NOT NULL AND \"BirthMonth\" BETWEEN 1 AND 12 AND \"BirthDay\" BETWEEN 1 AND CASE \"BirthMonth\" WHEN 2 THEN 29 WHEN 4 THEN 30 WHEN 6 THEN 30 WHEN 9 THEN 30 WHEN 11 THEN 30 ELSE 31 END)"));
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(16); b.Property(x => x.Email).HasMaxLength(254);
        b.Property(x => x.CardCode).HasMaxLength(24); b.Property(x => x.PublicToken).HasMaxLength(64);
        b.Property(x => x.ProgramTermsVersion).HasMaxLength(40);
        b.HasIndex(x => x.PublicToken).IsUnique(); b.HasIndex(x => x.CardCode).IsUnique();
        b.HasIndex(x => new { x.BranchId, x.CustomerId }).IsUnique().HasFilter("\"CustomerId\" IS NOT NULL AND NOT \"IsDeleted\"");
        b.HasIndex(x => new { x.BranchId, x.Phone }).IsUnique().HasFilter("\"Phone\" IS NOT NULL AND NOT \"IsDeleted\"");
        b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}
public class LoyaltyPhoneChallengeConfiguration : BaseEntityConfiguration<LoyaltyPhoneChallenge>
{
    public override void Configure(EntityTypeBuilder<LoyaltyPhoneChallenge> b) {
        base.Configure(b); b.ToTable("PhoneChallenges", "marketing");
        b.Property(x => x.Phone).HasMaxLength(16).IsRequired();
        b.Property(x => x.Purpose).HasMaxLength(12).IsRequired();
        b.Property(x => x.CodeHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.MessageId).HasMaxLength(512);
        b.Property(x => x.DeliveryStatus).HasMaxLength(20);
        b.HasIndex(x => new { x.BranchId, x.Phone, x.CreatedAt });
        b.HasIndex(x => x.MessageId);
    }
}
public class LoyaltyContributionConfiguration : BaseEntityConfiguration<LoyaltyContribution>
{
    public override void Configure(EntityTypeBuilder<LoyaltyContribution> b) {
        base.Configure(b); b.ToTable("Contributions", "marketing"); b.Property(x => x.EligibleAmount).HasPrecision(18, 2);
        b.Property(x => x.EligibleSubtotal).HasPrecision(18, 2);
        b.HasOne<LoyaltyMember>().WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OrderPayment>().WithMany().HasForeignKey(x => x.OrderPaymentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.OrderPaymentId).IsUnique(); b.HasIndex(x => new { x.MemberId, x.OrderId, x.BusinessDate });
    }
}
public class LoyaltyVisitConfiguration : BaseEntityConfiguration<LoyaltyVisit>
{
    public override void Configure(EntityTypeBuilder<LoyaltyVisit> b) {
        base.Configure(b); b.ToTable("Visits", "marketing");
        b.Property(x => x.QualifiedSpend).HasPrecision(18, 2); b.Property(x => x.MinimumSpend).HasPrecision(18, 2);
        b.HasOne<LoyaltyMember>().WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.MemberId, x.BusinessDate }).IsUnique();
    }
}
public class LoyaltyRewardConfiguration : BaseEntityConfiguration<LoyaltyReward>
{
    public override void Configure(EntityTypeBuilder<LoyaltyReward> b) {
        base.Configure(b); b.ToTable("Rewards", "marketing");
        b.Property(x => x.Name).HasMaxLength(120); b.Property(x => x.RuleSnapshotJson).HasColumnType("jsonb");
        b.HasOne<LoyaltyMember>().WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.MemberId, x.VisitNumber }).IsUnique();
    }
}
