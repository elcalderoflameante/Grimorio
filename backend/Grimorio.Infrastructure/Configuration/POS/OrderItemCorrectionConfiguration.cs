using Grimorio.Domain.Entities.POS;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grimorio.Infrastructure.Configuration.POS;

public class OrderItemCorrectionConfiguration : BaseEntityConfiguration<OrderItemCorrection>
{
    public override void Configure(EntityTypeBuilder<OrderItemCorrection> builder)
    {
        base.Configure(builder);
        builder.ToTable("OrderItemCorrections", "pos");
        builder.Property(c => c.RequestHash).HasMaxLength(64).IsRequired();
        builder.Property(c => c.Disposition).HasMaxLength(30).IsRequired();
        builder.Property(c => c.Reason).HasMaxLength(500).IsRequired();
        builder.Property(c => c.ActorName).HasMaxLength(201).IsRequired();
        builder.Property(c => c.OriginalStatus).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(c => new { c.BranchId, c.RequestId }).IsUnique();
        builder.HasOne(c => c.ReplacementItem).WithMany().HasForeignKey(c => c.ReplacementItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
