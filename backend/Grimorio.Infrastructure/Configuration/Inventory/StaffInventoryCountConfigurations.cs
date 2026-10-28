using Grimorio.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grimorio.Infrastructure.Configuration.Inventory;

public class InventoryCountAreaConfiguration : BaseEntityConfiguration<InventoryCountArea>
{
    public override void Configure(EntityTypeBuilder<InventoryCountArea> b)
    {
        base.Configure(b); b.ToTable("InventoryCountAreas", "inv");
        b.Property(x => x.Name).HasMaxLength(120);
        b.HasIndex(x => new { x.BranchId, x.Name }).IsUnique().HasFilter("\"IsDeleted\" = false");
        b.HasMany(x => x.Members).WithOne().HasForeignKey(x => x.AreaId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Rules).WithOne().HasForeignKey(x => x.AreaId).OnDelete(DeleteBehavior.Restrict);
    }
}
public class InventoryCountMemberConfiguration : BaseEntityConfiguration<InventoryCountMember>
{
    public override void Configure(EntityTypeBuilder<InventoryCountMember> b)
    {
        base.Configure(b); b.ToTable("InventoryCountMembers", "inv");
        b.HasIndex(x => new { x.AreaId, x.UserId }).IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}
public class InventoryCountRuleConfiguration : BaseEntityConfiguration<InventoryCountRule>
{
    public override void Configure(EntityTypeBuilder<InventoryCountRule> b)
    {
        base.Configure(b); b.ToTable("InventoryCountRules", "inv");
        b.Property(x => x.Frequency).HasMaxLength(20);
        b.HasIndex(x => new { x.BranchId, x.ArticleId, x.WarehouseId }).IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}
public class StaffInventoryCountConfiguration : BaseEntityConfiguration<StaffInventoryCount>
{
    public override void Configure(EntityTypeBuilder<StaffInventoryCount> b)
    {
        base.Configure(b); b.ToTable("StaffInventoryCounts", "inv");
        b.Property(x => x.AreaName).HasMaxLength(120); b.Property(x => x.CounterName).HasMaxLength(250);
        b.Property(x => x.Status).HasMaxLength(30); b.Property(x => x.ReviewerName).HasMaxLength(250);
        b.Property(x => x.ReviewNotes).HasMaxLength(2000);
        b.HasIndex(x => new { x.BranchId, x.AreaId, x.BusinessDate });
        b.HasIndex(x => new { x.BranchId, x.AreaId }).IsUnique().HasFilter("\"IsDeleted\" = false AND \"Status\" = 'Draft'");
        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.CountId).OnDelete(DeleteBehavior.Restrict);
    }
}
public class StaffInventoryCountLineConfiguration : BaseEntityConfiguration<StaffInventoryCountLine>
{
    public override void Configure(EntityTypeBuilder<StaffInventoryCountLine> b)
    {
        base.Configure(b); b.ToTable("StaffInventoryCountLines", "inv");
        b.Property(x => x.ArticleName).HasMaxLength(200); b.Property(x => x.WarehouseName).HasMaxLength(200);
        b.Property(x => x.UnitSymbol).HasMaxLength(20); b.Property(x => x.Frequency).HasMaxLength(20);
        b.Property(x => x.Quantity).HasPrecision(18, 4); b.Property(x => x.ExpectedQuantity).HasPrecision(18, 4);
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.HasIndex(x => new { x.CountId, x.ArticleId, x.WarehouseId }).IsUnique();
    }
}
