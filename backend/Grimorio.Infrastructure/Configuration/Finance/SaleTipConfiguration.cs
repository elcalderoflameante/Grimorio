using Grimorio.Domain.Entities.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grimorio.Infrastructure.Configuration.Finance;

public class SaleTipConfiguration : BaseEntityConfiguration<SaleTip>
{
    public override void Configure(EntityTypeBuilder<SaleTip> builder)
    {
        base.Configure(builder);
        builder.ToTable("SaleTips", "finance", table => table.HasCheckConstraint("CK_SaleTips_Amount", "\"Amount\" > 0"));
        builder.Property(x => x.Amount).HasColumnType("numeric(18,2)");
        builder.Property(x => x.Method).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.RegisteredByName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.UpdatedByName).HasMaxLength(150);
        builder.HasOne(x => x.OrderPayment).WithMany().HasForeignKey(x => x.OrderPaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.BranchId, x.OrderPaymentId }).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => new { x.BranchId, x.RegisteredAt });
    }
}
