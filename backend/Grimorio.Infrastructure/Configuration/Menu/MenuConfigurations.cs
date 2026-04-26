using Grimorio.Domain.Entities.Menu;
using Grimorio.Domain.Entities.POS;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grimorio.Infrastructure.Configuration.Menu;

public class CategoriaMenuConfiguration : BaseEntityConfiguration<MenuCategory>
{
    public override void Configure(EntityTypeBuilder<MenuCategory> builder)
    {
        base.Configure(builder);
        builder.ToTable("MenuCategories", "menu");

        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(300);
        builder.Property(x => x.Color).HasMaxLength(20);

        builder.HasIndex(x => new { x.BranchId, x.Name })
            .IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => new { x.BranchId, x.Order });
    }
}

public class ItemMenuConfiguration : BaseEntityConfiguration<MenuItem>
{
    public override void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        base.Configure(builder);
        builder.ToTable("MenuItems", "menu");

        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.InternalCode).HasMaxLength(50);
        builder.Property(x => x.Price).HasColumnType("numeric(18,4)").IsRequired();

        builder.HasOne(x => x.Category)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.MenuCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Station)
            .WithMany()
            .HasForeignKey(x => x.StationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.BranchId, x.MenuCategoryId });
        builder.HasIndex(x => new { x.BranchId, x.IsActive });
    }
}

public class RecipeIngredientConfiguration : BaseEntityConfiguration<RecipeIngredient>
{
    public override void Configure(EntityTypeBuilder<RecipeIngredient> builder)
    {
        base.Configure(builder);
        builder.ToTable("RecipeIngredients", "menu");

        builder.Property(x => x.Quantity).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(200);

        builder.HasOne(x => x.MenuItem)
            .WithMany(x => x.Recipe)
            .HasForeignKey(x => x.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Article)
            .WithMany()
            .HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Unit)
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.MenuItemId, x.ArticleId })
            .IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}
