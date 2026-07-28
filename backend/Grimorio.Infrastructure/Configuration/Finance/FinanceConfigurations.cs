using Grimorio.Domain.Entities.Finance;
using Grimorio.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grimorio.Infrastructure.Configuration.Finance;

public class CostCenterConfiguration : BaseEntityConfiguration<CostCenter>
{
    public override void Configure(EntityTypeBuilder<CostCenter> builder)
    {
        base.Configure(builder);
        builder.ToTable("CostCenters", "finance");

        builder.Property(x => x.Name).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Code).HasMaxLength(30);
        builder.Property(x => x.Description).HasMaxLength(300);

        builder.HasIndex(x => new { x.BranchId, x.Name })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => new { x.BranchId, x.Code })
            .IsUnique()
            .HasFilter("\"Code\" IS NOT NULL AND \"IsDeleted\" = false");
        builder.HasIndex(x => new { x.BranchId, x.DisplayOrder });
    }
}

public class ExpenseCategoryConfiguration : BaseEntityConfiguration<ExpenseCategory>
{
    public override void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        base.Configure(builder);
        builder.ToTable("ExpenseCategories", "finance");

        builder.Property(x => x.Name).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Description).HasMaxLength(300);
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.HasIndex(x => new { x.BranchId, x.Name })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => new { x.BranchId, x.Type });
        builder.HasIndex(x => new { x.BranchId, x.DisplayOrder });
    }
}

public class ExpenseConfiguration : BaseEntityConfiguration<Expense>
{
    public override void Configure(EntityTypeBuilder<Expense> builder)
    {
        base.Configure(builder);
        builder.ToTable("Expenses", "finance");

        builder.Property(x => x.SupplierName).HasMaxLength(200);
        builder.Property(x => x.DocumentNumber).HasMaxLength(80);
        builder.Property(x => x.Amount).HasColumnType("numeric(18,2)");
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.RegisteredByName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.CancelledByName).HasMaxLength(150);
        builder.Property(x => x.CancellationReason).HasMaxLength(300);

        builder.HasOne(x => x.CostCenter)
            .WithMany()
            .HasForeignKey(x => x.CostCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ExpenseCategory)
            .WithMany()
            .HasForeignKey(x => x.ExpenseCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PaymentMethodConfig)
            .WithMany()
            .HasForeignKey(x => x.PaymentMethodConfigId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CashSession)
            .WithMany()
            .HasForeignKey(x => x.CashSessionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.BranchId, x.ExpenseDate });
        builder.HasIndex(x => new { x.BranchId, x.Status });
        builder.HasIndex(x => new { x.BranchId, x.CostCenterId });
        builder.HasIndex(x => new { x.BranchId, x.ExpenseCategoryId });
        builder.HasIndex(x => x.CashSessionId);
    }
}
