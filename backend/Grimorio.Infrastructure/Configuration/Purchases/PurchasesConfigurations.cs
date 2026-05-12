using Grimorio.Domain.Entities.Purchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grimorio.Infrastructure.Configuration.Purchases;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers", "purchases");

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.TaxId).HasMaxLength(20);
        builder.Property(x => x.Phone).HasMaxLength(30);
        builder.Property(x => x.Email).HasMaxLength(150);
        builder.Property(x => x.Address).HasMaxLength(400);
        builder.Property(x => x.ContactName).HasMaxLength(150);

        builder.HasIndex(x => new { x.BranchId, x.Name })
            .IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}

public class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("Purchases", "purchases");

        builder.Property(x => x.DocumentType).HasConversion<int>().IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.DocumentNumber).HasMaxLength(50);
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.Property(x => x.Subtotal).HasColumnType("numeric(18,2)");
        builder.Property(x => x.DiscountTotal).HasColumnType("numeric(18,2)");
        builder.Property(x => x.TaxableBase15).HasColumnType("numeric(18,2)");
        builder.Property(x => x.TaxableBase0).HasColumnType("numeric(18,2)");
        builder.Property(x => x.TaxableBaseExempt).HasColumnType("numeric(18,2)");
        builder.Property(x => x.Iva15).HasColumnType("numeric(18,2)");
        builder.Property(x => x.Ice).HasColumnType("numeric(18,2)");
        builder.Property(x => x.Total).HasColumnType("numeric(18,2)");

        builder.HasOne(x => x.Supplier)
            .WithMany(x => x.Purchases)
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(x => new { x.BranchId, x.DocumentDate });
        builder.HasIndex(x => new { x.BranchId, x.Status });
        builder.HasIndex(x => new { x.BranchId, x.SupplierId });
    }
}

public class PurchaseItemConfiguration : IEntityTypeConfiguration<PurchaseItem>
{
    public void Configure(EntityTypeBuilder<PurchaseItem> builder)
    {
        builder.ToTable("PurchaseItems", "purchases");

        builder.Property(x => x.Quantity).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.UnitPrice).HasColumnType("numeric(18,4)");
        builder.Property(x => x.DiscountPct).HasColumnType("numeric(5,2)");
        builder.Property(x => x.DiscountAmount).HasColumnType("numeric(18,2)");
        builder.Property(x => x.TaxAmount).HasColumnType("numeric(18,2)");
        builder.Property(x => x.TotalPrice).HasColumnType("numeric(18,2)");
        builder.Property(x => x.Notes).HasMaxLength(300);

        builder.HasOne(x => x.Purchase)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.PurchaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Article)
            .WithMany()
            .HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Unit)
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TaxRate)
            .WithMany()
            .HasForeignKey(x => x.TaxRateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.PurchaseId);
    }
}
