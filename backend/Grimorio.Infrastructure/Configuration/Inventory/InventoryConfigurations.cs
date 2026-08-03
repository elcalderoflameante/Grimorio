using Grimorio.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grimorio.Infrastructure.Configuration.Inventory;

public class UnidadMedidaConfiguration : BaseEntityConfiguration<MeasurementUnit>
{
    public override void Configure(EntityTypeBuilder<MeasurementUnit> builder)
    {
        base.Configure(builder);
        builder.ToTable("MeasurementUnits", "inv");

        builder.Property(x => x.Name).IsRequired().HasMaxLength(80);
        builder.Property(x => x.Symbol).IsRequired().HasMaxLength(20);

        builder.HasIndex(x => new { x.BranchId, x.Name })
            .IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}

public class UnitConversionConfiguration : BaseEntityConfiguration<UnitConversion>
{
    public override void Configure(EntityTypeBuilder<UnitConversion> builder)
    {
        base.Configure(builder);
        builder.ToTable("UnitConversions", "inv");

        builder.Property(x => x.Factor).HasColumnType("numeric(18,6)").IsRequired();

        builder.HasOne(x => x.OriginUnit)
            .WithMany(x => x.OriginConversions)
            .HasForeignKey(x => x.OriginUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DestinationUnit)
            .WithMany(x => x.DestinationConversions)
            .HasForeignKey(x => x.DestinationUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BranchId, x.OriginUnitId, x.DestinationUnitId })
            .IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}

public class CategoriaInventarioConfiguration : BaseEntityConfiguration<InventoryCategory>
{
    public override void Configure(EntityTypeBuilder<InventoryCategory> builder)
    {
        base.Configure(builder);
        builder.ToTable("InventoryCategories", "inv");

        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(300);
        builder.Property(x => x.Color).HasMaxLength(20);

        builder.HasIndex(x => new { x.BranchId, x.Name })
            .IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}

public class ArticuloInventarioConfiguration : BaseEntityConfiguration<InventoryArticle>
{
    public override void Configure(EntityTypeBuilder<InventoryArticle> builder)
    {
        base.Configure(builder);
        builder.ToTable("InventoryArticles", "inv");

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.InternalCode).HasMaxLength(80);
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.MinStock).HasColumnType("numeric(18,4)").IsRequired();

        builder.HasOne(x => x.Category)
            .WithMany(x => x.Articles)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.BaseUnit)
            .WithMany(x => x.BaseArticles)
            .HasForeignKey(x => x.BaseUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BranchId, x.InternalCode })
            .HasFilter("\"IsDeleted\" = false AND \"InternalCode\" IS NOT NULL");

        builder.HasIndex(x => new { x.BranchId, x.Type, x.IsActive });
    }
}

public class BodegaConfiguration : BaseEntityConfiguration<Warehouse>
{
    public override void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        base.Configure(builder);
        builder.ToTable("Warehouses", "inv");

        builder.Property(x => x.Name).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Description).HasMaxLength(300);
        builder.Property(x => x.Location).HasMaxLength(200);

        builder.HasIndex(x => new { x.BranchId, x.Name })
            .IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}

public class WarehouseStockConfiguration : BaseEntityConfiguration<WarehouseStock>
{
    public override void Configure(EntityTypeBuilder<WarehouseStock> builder)
    {
        base.Configure(builder);
        builder.ToTable("WarehouseStock", "inv");

        builder.Property(x => x.Quantity).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.LastUpdatedAt)
            .HasColumnType("timestamp with time zone").IsRequired();

        builder.HasOne(x => x.Article)
            .WithMany(x => x.Stocks)
            .HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Warehouse)
            .WithMany(x => x.Stocks)
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BranchId, x.ArticleId, x.WarehouseId })
            .IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}

public class MovimientoStockConfiguration : BaseEntityConfiguration<StockMovement>
{
    public override void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        base.Configure(builder);
        builder.ToTable("StockMovements", "inv");

        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.Quantity).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.BaseQuantity).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.UnitCost).HasColumnType("numeric(18,4)");
        builder.Property(x => x.TotalCost).HasColumnType("numeric(18,4)");
        builder.Property(x => x.Reference).HasMaxLength(120);
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasOne(x => x.Article)
            .WithMany(x => x.Movements)
            .HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Warehouse)
            .WithMany(x => x.Movements)
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Unit)
            .WithMany(x => x.Movements)
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BranchId, x.ArticleId, x.CreatedAt });
        builder.HasIndex(x => new { x.BranchId, x.Type, x.CreatedAt });
    }
}

public class StockReservationConfiguration : BaseEntityConfiguration<StockReservation>
{
    public override void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        base.Configure(builder);
        builder.ToTable("StockReservations", "inv");

        builder.Property(x => x.Quantity).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.BaseQuantity).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.ReservedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(x => x.ConsumedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.ReleasedAt).HasColumnType("timestamp with time zone");

        builder.HasOne(x => x.Article)
            .WithMany(x => x.Reservations)
            .HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Warehouse)
            .WithMany(x => x.Reservations)
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Unit)
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BranchId, x.OrderId });
        builder.HasIndex(x => new { x.BranchId, x.OrderItemId });
        builder.HasIndex(x => new { x.BranchId, x.ArticleId, x.WarehouseId, x.Status })
            .HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => new { x.BranchId, x.OrderItemId, x.ArticleId, x.WarehouseId, x.Status })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false AND \"Status\" = 'Active'");
    }
}

public class ProductionRecipeConfiguration : BaseEntityConfiguration<ProductionRecipe>
{
    public override void Configure(EntityTypeBuilder<ProductionRecipe> builder)
    {
        base.Configure(builder);
        builder.ToTable("ProductionRecipes", "inv");

        builder.Property(x => x.OutputQuantity).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasOne(x => x.OutputArticle)
            .WithOne(x => x.ProductionRecipe)
            .HasForeignKey<ProductionRecipe>(x => x.OutputArticleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.OutputUnit)
            .WithMany()
            .HasForeignKey(x => x.OutputUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BranchId, x.OutputArticleId })
            .IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}

public class ProductionRecipeIngredientConfiguration : BaseEntityConfiguration<ProductionRecipeIngredient>
{
    public override void Configure(EntityTypeBuilder<ProductionRecipeIngredient> builder)
    {
        base.Configure(builder);
        builder.ToTable("ProductionRecipeIngredients", "inv");

        builder.Property(x => x.Quantity).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(300);

        builder.HasOne(x => x.ProductionRecipe)
            .WithMany(x => x.Ingredients)
            .HasForeignKey(x => x.ProductionRecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Article)
            .WithMany(x => x.ProductionRecipeIngredients)
            .HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Unit)
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BranchId, x.ProductionRecipeId, x.ArticleId })
            .HasFilter("\"IsDeleted\" = false");
    }
}

public class ProductionOrderConfiguration : BaseEntityConfiguration<ProductionOrder>
{
    public override void Configure(EntityTypeBuilder<ProductionOrder> builder)
    {
        base.Configure(builder);
        builder.ToTable("ProductionOrders", "inv");

        builder.Property(x => x.Number).IsRequired().HasMaxLength(30);
        builder.Property(x => x.OutputQuantity).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.OutputBaseQuantity).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.TotalCost).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.UnitCost).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasOne(x => x.ProductionRecipe)
            .WithMany(x => x.ProductionOrders)
            .HasForeignKey(x => x.ProductionRecipeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.OutputArticle)
            .WithMany()
            .HasForeignKey(x => x.OutputArticleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SourceWarehouse)
            .WithMany()
            .HasForeignKey(x => x.SourceWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DestinationWarehouse)
            .WithMany()
            .HasForeignKey(x => x.DestinationWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.OutputUnit)
            .WithMany()
            .HasForeignKey(x => x.OutputUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BranchId, x.Number })
            .IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => new { x.BranchId, x.CreatedAt });
        builder.HasIndex(x => new { x.BranchId, x.OutputArticleId, x.CreatedAt });
    }
}

public class ProductionOrderIngredientConfiguration : BaseEntityConfiguration<ProductionOrderIngredient>
{
    public override void Configure(EntityTypeBuilder<ProductionOrderIngredient> builder)
    {
        base.Configure(builder);
        builder.ToTable("ProductionOrderIngredients", "inv");

        builder.Property(x => x.Quantity).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.BaseQuantity).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.UnitCost).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.TotalCost).HasColumnType("numeric(18,4)").IsRequired();

        builder.HasOne(x => x.ProductionOrder)
            .WithMany(x => x.Ingredients)
            .HasForeignKey(x => x.ProductionOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Article)
            .WithMany()
            .HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Unit)
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BranchId, x.ProductionOrderId });
        builder.HasIndex(x => new { x.BranchId, x.ArticleId });
    }
}

public class ProductionOrderMovementConfiguration : BaseEntityConfiguration<ProductionOrderMovement>
{
    public override void Configure(EntityTypeBuilder<ProductionOrderMovement> builder)
    {
        base.Configure(builder);
        builder.ToTable("ProductionOrderMovements", "inv");

        builder.HasOne(x => x.ProductionOrder)
            .WithMany(x => x.Movements)
            .HasForeignKey(x => x.ProductionOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.StockMovement)
            .WithMany()
            .HasForeignKey(x => x.StockMovementId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BranchId, x.ProductionOrderId });
        builder.HasIndex(x => new { x.BranchId, x.StockMovementId })
            .IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}
