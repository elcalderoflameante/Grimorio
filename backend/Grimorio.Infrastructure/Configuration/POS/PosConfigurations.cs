using Grimorio.Domain.Entities.POS;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grimorio.Infrastructure.Configuration.POS;

public class EstacionTrabajoConfiguration : IEntityTypeConfiguration<WorkStation>
{
    public void Configure(EntityTypeBuilder<WorkStation> builder)
    {
        builder.ToTable("WorkStations", "pos");

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.HasIndex(x => new { x.BranchId, x.Name })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
    }
}

public class OrdenConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", "pos");

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.CustomerName).HasMaxLength(200);
        builder.Property(x => x.DeliveryAddress).HasMaxLength(400);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.Subtotal).HasColumnType("numeric(18,2)");
        builder.Property(x => x.Total).HasColumnType("numeric(18,2)");

        builder.HasOne(x => x.Table)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.TableId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BranchId, x.Status })
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => new { x.BranchId, x.Number });
    }
}

public class OrdenItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems", "pos");

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Notes).HasMaxLength(300);
        builder.Property(x => x.UnitPrice).HasColumnType("numeric(18,2)");
        builder.Property(x => x.TotalPrice).HasColumnType("numeric(18,2)");

        builder.HasOne(x => x.Order)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Station)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.StationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.BranchId, x.OrderId });
        builder.HasIndex(x => new { x.StationId, x.Status })
            .HasFilter("\"IsDeleted\" = false");
    }
}


public class RestaurantTableConfiguration : IEntityTypeConfiguration<RestaurantTable>
{
    public void Configure(EntityTypeBuilder<RestaurantTable> builder)
    {
        builder.ToTable("RestaurantTables", "pos");

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(x => x.Area)
            .HasMaxLength(120);

        builder.Property(x => x.PublicToken)
            .IsRequired()
            .HasMaxLength(80);

        builder.HasIndex(x => new { x.BranchId, x.Code })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => x.PublicToken)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.Property(x => x.PosX).HasDefaultValue(0);
        builder.Property(x => x.PosY).HasDefaultValue(0);
    }
}

public class TableServiceRequestConfiguration : IEntityTypeConfiguration<TableServiceRequest>
{
    public void Configure(EntityTypeBuilder<TableServiceRequest> builder)
    {
        builder.ToTable("TableServiceRequests", "pos");

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.CustomMessage)
            .HasMaxLength(400);

        builder.Property(x => x.TakenByName)
            .HasMaxLength(180);

        builder.Property(x => x.ClientFingerprint)
            .HasMaxLength(120);

        builder.Property(x => x.SourceIp)
            .HasMaxLength(120);

        builder.HasOne(x => x.RestaurantTable)
            .WithMany(x => x.ServiceRequests)
            .HasForeignKey(x => x.RestaurantTableId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BranchId, x.RestaurantTableId, x.RequestedAt })
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => new { x.BranchId, x.Status, x.RequestedAt })
            .HasFilter("\"IsDeleted\" = false");
    }
}
