using Grimorio.SharedKernel;

namespace Grimorio.Domain.Entities.Menu;

public class MenuCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<MenuItem> Items { get; set; } = [];
}

public class MenuItem : BaseEntity
{
    public Guid MenuCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? InternalCode { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public bool AvailableForSale { get; set; } = true;
    public Guid? StationId { get; set; }
    public Guid? TaxRateId { get; set; }

    public virtual MenuCategory? Category { get; set; }
    public virtual ICollection<RecipeIngredient> Recipe { get; set; } = [];
    public virtual ICollection<MenuItemModifierGroup> ModifierGroups { get; set; } = [];
    public virtual POS.WorkStation? Station { get; set; }
    public virtual Billing.TaxRate? TaxRate { get; set; }
}

public class MenuItemModifierGroup : BaseEntity
{
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MinSelections { get; set; }
    public int MaxSelections { get; set; } = 1;
    public bool IsRequired { get; set; } = true;
    public bool AllowDuplicates { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual MenuItem? MenuItem { get; set; }
    public virtual ICollection<MenuItemModifierOption> Options { get; set; } = [];
}

public class MenuItemModifierOption : BaseEntity
{
    public Guid ModifierGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ArticleId { get; set; }
    public Guid? UnitId { get; set; }
    public decimal Quantity { get; set; }
    public decimal PriceDelta { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual MenuItemModifierGroup? ModifierGroup { get; set; }
    public virtual Inventory.InventoryArticle? Article { get; set; }
    public virtual Inventory.MeasurementUnit? Unit { get; set; }
}

public class RecipeIngredient : BaseEntity
{
    public Guid MenuItemId { get; set; }
    public Guid ArticleId { get; set; }
    public Guid UnitId { get; set; }
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }

    public virtual MenuItem? MenuItem { get; set; }
    public virtual Inventory.InventoryArticle? Article { get; set; }
    public virtual Inventory.MeasurementUnit? Unit { get; set; }
}
