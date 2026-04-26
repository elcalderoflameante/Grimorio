using Grimorio.Domain.Entities.Billing;
using Grimorio.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grimorio.Infrastructure.Configuration.Billing;

public class CustomerConfiguration : BaseEntityConfiguration<Customer>
{
    public override void Configure(EntityTypeBuilder<Customer> builder)
    {
        base.Configure(builder);
        builder.ToTable("Customers", "billing");

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.TaxId).HasMaxLength(20);
        builder.Property(x => x.Address).HasMaxLength(300);
        builder.Property(x => x.Phone).HasMaxLength(30);
        builder.Property(x => x.Email).HasMaxLength(150);

        builder.HasIndex(x => new { x.BranchId, x.TaxId })
            .HasFilter("\"TaxId\" IS NOT NULL AND \"IsDeleted\" = false");
    }
}

public class CashSessionConfiguration : BaseEntityConfiguration<CashSession>
{
    public override void Configure(EntityTypeBuilder<CashSession> builder)
    {
        base.Configure(builder);
        builder.ToTable("CashSessions", "billing");

        builder.Property(x => x.OpenedByName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.ClosedByName).HasMaxLength(150);
        builder.Property(x => x.OpeningBalance).HasColumnType("numeric(18,2)");
        builder.Property(x => x.ActualCash).HasColumnType("numeric(18,2)");
        builder.Property(x => x.CloseNotes).HasMaxLength(500);

        builder.HasIndex(x => new { x.BranchId, x.Status });
    }
}

public class OrderPaymentConfiguration : BaseEntityConfiguration<OrderPayment>
{
    public override void Configure(EntityTypeBuilder<OrderPayment> builder)
    {
        base.Configure(builder);
        builder.ToTable("OrderPayments", "billing");

        builder.Property(x => x.AmountPaid).HasColumnType("numeric(18,2)");
        builder.Property(x => x.Change).HasColumnType("numeric(18,2)");
        builder.Property(x => x.OrderTotal).HasColumnType("numeric(18,2)");

        builder.HasOne(x => x.Order)
            .WithOne(o => o.Payment)
            .HasForeignKey<OrderPayment>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CashSession)
            .WithMany(s => s.Payments)
            .HasForeignKey(x => x.CashSessionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.OrderId).IsUnique();
        builder.HasIndex(x => new { x.BranchId, x.PaidAt });
    }
}
