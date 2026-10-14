using Grimorio.Domain.Entities.Menu;
using Grimorio.Domain.Entities.POS;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grimorio.Infrastructure.Configuration.Menu;

public class MenuComboComponentConfiguration : BaseEntityConfiguration<MenuComboComponent>
{
    public override void Configure(EntityTypeBuilder<MenuComboComponent> builder)
    {
        base.Configure(builder);
        builder.ToTable("MenuComboComponents", "menu", table => table.HasCheckConstraint("CK_MenuComboComponents_Quantity", "\"Quantity\" > 0"));
        builder.HasOne(x => x.Combo).WithMany(x => x.ComboComponents).HasForeignKey(x => x.ComboId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ComponentItem).WithMany().HasForeignKey(x => x.ComponentItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ComboId, x.ComponentItemId }).IsUnique().HasFilter("NOT \"IsDeleted\"");
    }
}

public class OrderItemPreparationTaskConfiguration : BaseEntityConfiguration<OrderItemPreparationTask>
{
    public override void Configure(EntityTypeBuilder<OrderItemPreparationTask> builder)
    {
        base.Configure(builder);
        builder.ToTable("OrderItemPreparationTasks", "pos", table => table.HasCheckConstraint("CK_OrderItemPreparationTasks_Quantity", "\"QuantityPerCombo\" > 0"));
        builder.Property(x => x.ComponentName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.ComboName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.RecipeSnapshot).HasColumnType("jsonb").IsRequired();
        builder.HasOne(x => x.OrderItem).WithMany(x => x.PreparationTasks).HasForeignKey(x => x.OrderItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ComponentItem).WithMany().HasForeignKey(x => x.ComponentItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Station).WithMany().HasForeignKey(x => x.StationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.OrderItemId, x.ComponentItemId }).IsUnique();
        builder.HasIndex(x => new { x.BranchId, x.StationId, x.Status });
    }
}
