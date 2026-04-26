using Grimorio.Domain.Entities.Purchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grimorio.Infrastructure.Configuration.Purchases;

public class ProveedorConfiguration : IEntityTypeConfiguration<Supplier>
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

public class OrdenCompraConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders", "purchases");

        builder.Property(x => x.OrderNumber).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.Subtotal).HasColumnType("numeric(18,2)");
        builder.Property(x => x.Total).HasColumnType("numeric(18,2)");

        builder.HasOne(x => x.Supplier)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BranchId, x.OrderNumber })
            .IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => new { x.BranchId, x.Status });
    }
}

public class OrdenCompraItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
    {
        builder.ToTable("PurchaseOrderItems", "purchases");

        builder.Property(x => x.QuantityOrdered).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.QuantityReceived).HasColumnType("numeric(18,4)");
        builder.Property(x => x.UnitPrice).HasColumnType("numeric(18,4)");
        builder.Property(x => x.TotalPrice).HasColumnType("numeric(18,2)");
        builder.Property(x => x.Notes).HasMaxLength(300);

        builder.HasOne(x => x.PurchaseOrder)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Article)
            .WithMany()
            .HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Unit)
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.PurchaseOrderId);
    }
}
