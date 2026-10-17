using Grimorio.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grimorio.Infrastructure.Configuration.Billing;

public class PaymentMethodCorrectionConfiguration : BaseEntityConfiguration<PaymentMethodCorrection>
{
    public override void Configure(EntityTypeBuilder<PaymentMethodCorrection> builder)
    {
        base.Configure(builder);
        builder.ToTable("PaymentMethodCorrections", "billing");
        builder.Property(c => c.RequestHash).HasMaxLength(64).IsRequired();
        builder.Property(c => c.ActorName).HasMaxLength(201).IsRequired();
        builder.Property(c => c.Reason).HasMaxLength(500).IsRequired();
        builder.Property(c => c.BeforeJson).HasColumnType("jsonb").IsRequired();
        builder.Property(c => c.AfterJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(c => new { c.BranchId, c.RequestId }).IsUnique();
        builder.HasIndex(c => new { c.OrderPaymentId, c.CreatedAt });
        builder.HasOne<OrderPayment>().WithMany().HasForeignKey(c => c.OrderPaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PaymentLine>().WithMany().HasForeignKey(c => c.PaymentLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CashSession>().WithMany().HasForeignKey(c => c.CashSessionId).OnDelete(DeleteBehavior.Restrict);
    }
}
