using Grimorio.Domain.Entities.POS;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grimorio.Infrastructure.Configuration.POS;

public class OrderTableTransferConfiguration : BaseEntityConfiguration<OrderTableTransfer>
{
    public override void Configure(EntityTypeBuilder<OrderTableTransfer> builder)
    {
        base.Configure(builder);
        builder.ToTable("OrderTableTransfers", "pos");
        builder.HasIndex(x => new { x.BranchId, x.RequestId }).IsUnique();
        builder.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RestaurantTable>().WithMany().HasForeignKey(x => x.FromTableId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RestaurantTable>().WithMany().HasForeignKey(x => x.ToTableId).OnDelete(DeleteBehavior.Restrict);
    }
}
