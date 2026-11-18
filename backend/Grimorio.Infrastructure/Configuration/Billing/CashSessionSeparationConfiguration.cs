using Grimorio.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grimorio.Infrastructure.Configuration.Billing;

public class CashSessionSeparationConfiguration : BaseEntityConfiguration<CashSessionSeparation>
{
    public override void Configure(EntityTypeBuilder<CashSessionSeparation> builder)
    {
        base.Configure(builder);
        builder.ToTable("CashSessionSeparations", "billing");
        builder.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ActorName).HasMaxLength(201).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.PreviewJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => new { x.BranchId, x.RequestId }).IsUnique();
        builder.HasIndex(x => x.PreviousSessionId).IsUnique();
        builder.HasIndex(x => x.NewSessionId).IsUnique();
        builder.HasOne<CashSession>().WithMany().HasForeignKey(x => x.PreviousSessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CashSession>().WithMany().HasForeignKey(x => x.NewSessionId).OnDelete(DeleteBehavior.Restrict);
    }
}
