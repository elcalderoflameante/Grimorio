using Grimorio.Domain.Entities.POS;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grimorio.Infrastructure.Configuration.POS;

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
