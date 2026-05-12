using Grimorio.Domain.Entities.Billing;
using Grimorio.Infrastructure.Configuration;
using Grimorio.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grimorio.Infrastructure.Configuration.Billing;

public class TaxRateConfiguration : BaseEntityConfiguration<TaxRate>
{
    public override void Configure(EntityTypeBuilder<TaxRate> builder)
    {
        base.Configure(builder);
        builder.ToTable("TaxRates", "billing");

        builder.Property(x => x.Name).IsRequired().HasMaxLength(60);
        builder.Property(x => x.SriCode).IsRequired().HasMaxLength(10);
        builder.Property(x => x.Percentage).HasColumnType("numeric(5,2)");

        builder.HasIndex(x => new { x.BranchId, x.SriCode })
            .HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => new { x.BranchId, x.IsDefault })
            .HasFilter("\"IsDeleted\" = false AND \"IsDefault\" = true");
    }
}

public class BranchTaxConfigConfiguration : BaseEntityConfiguration<BranchTaxConfig>
{
    public override void Configure(EntityTypeBuilder<BranchTaxConfig> builder)
    {
        base.Configure(builder);
        builder.ToTable("BranchTaxConfigs", "billing");

        builder.Property(x => x.Ruc).IsRequired().HasMaxLength(13);
        builder.Property(x => x.RazonSocial).IsRequired().HasMaxLength(300);
        builder.Property(x => x.NombreComercial).HasMaxLength(300);
        builder.Property(x => x.Direccion).IsRequired().HasMaxLength(300);
        builder.Property(x => x.CodigoEstablecimiento).IsRequired().HasMaxLength(3);
        builder.Property(x => x.PuntoEmision).IsRequired().HasMaxLength(3);
        builder.Property(x => x.Ambiente).IsRequired().HasMaxLength(1);

        builder.HasIndex(x => x.BranchId).IsUnique()
            .HasFilter("\"IsDeleted\" = false");
    }
}

public class PaymentMethodConfigConfiguration : IEntityTypeConfiguration<PaymentMethodConfig>
{
    public void Configure(EntityTypeBuilder<PaymentMethodConfig> builder)
    {
        builder.ToTable("PaymentMethodConfigs", "billing");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.Name).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Color).HasMaxLength(32);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.SortOrder);
    }
}

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

        builder.Property(x => x.OrderAmount).HasColumnType("numeric(18,2)");

        builder.HasOne(x => x.Order)
            .WithMany(o => o.Payments)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CashSession)
            .WithMany(s => s.Payments)
            .HasForeignKey(x => x.CashSessionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => new { x.BranchId, x.PaidAt });
    }
}

public class PaymentLineConfiguration : BaseEntityConfiguration<PaymentLine>
{
    public override void Configure(EntityTypeBuilder<PaymentLine> builder)
    {
        base.Configure(builder);
        builder.ToTable("PaymentLines", "billing");

        builder.Property(x => x.AmountTendered).HasColumnType("numeric(18,2)");
        builder.Property(x => x.Change).HasColumnType("numeric(18,2)");

        builder.HasOne(x => x.Payment)
            .WithMany(p => p.Lines)
            .HasForeignKey(x => x.OrderPaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Config)
            .WithMany()
            .HasForeignKey(x => x.PaymentMethodConfigId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
