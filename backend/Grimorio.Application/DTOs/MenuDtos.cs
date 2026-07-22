namespace Grimorio.Application.DTOs;

// ── Categorías de menú ────────────────────────────────────────────────────

public class MenuCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public int TotalItems { get; set; }
}

public class CreateMenuCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public int Order { get; set; }
}

// ── Items del menú ────────────────────────────────────────────────────────

public class MenuItemDto
{
    public Guid Id { get; set; }
    public Guid MenuCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? CategoryColor { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? InternalCode { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public bool AvailableForSale { get; set; }
    public int TotalIngredients { get; set; }
    public Guid? StationId { get; set; }
    public string? StationName { get; set; }
    public Guid? TaxRateId { get; set; }
    public string? TaxRateName { get; set; }
    public decimal? TaxRatePercentage { get; set; }
    public string? TaxRateSriCode { get; set; }
    public bool HasModifiers { get; set; }
    public List<MenuItemModifierGroupDto> ModifierGroups { get; set; } = [];
}

public class MenuItemDetailDto : MenuItemDto
{
    public List<RecipeIngredientDto> Recipe { get; set; } = [];
}

public class MenuItemAvailabilityDto
{
    public Guid MenuItemId { get; set; }
    public bool IsTracked { get; set; }
    public bool IsAvailable { get; set; }
    public decimal? AvailableQuantity { get; set; }
    public string UnitLabel { get; set; } = "u";
    public string? LimitingArticleName { get; set; }
    public List<MenuItemAvailabilityComponentDto> Components { get; set; } = [];
}

public class MenuItemAvailabilityComponentDto
{
    public Guid? RecipeIngredientId { get; set; }
    public Guid ArticleId { get; set; }
    public string ArticleName { get; set; } = string.Empty;
    public decimal RequiredQuantity { get; set; }
    public string RequiredUnitSymbol { get; set; } = string.Empty;
    public decimal StockQuantity { get; set; }
    public string StockUnitSymbol { get; set; } = string.Empty;
    public decimal AvailableServings { get; set; }
}

public class CreateMenuItemDto
{
    public Guid MenuCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? InternalCode { get; set; }
    public decimal Price { get; set; }
    public Guid? StationId { get; set; }
    public Guid? TaxRateId { get; set; }
}

public class UpdateMenuItemDto
{
    public Guid MenuCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? InternalCode { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public bool AvailableForSale { get; set; }
    public Guid? StationId { get; set; }
    public Guid? TaxRateId { get; set; }
}

// ── Recipe ────────────────────────────────────────────────────────────────

public class RecipeIngredientDto
{
    public Guid Id { get; set; }
    public Guid ArticleId { get; set; }
    public string ArticleName { get; set; } = string.Empty;
    public string? InternalCode { get; set; }
    public Guid UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public string UnitSymbol { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
}

public class MenuItemModifierOptionDto
{
    public Guid Id { get; set; }
    public Guid ModifierGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ArticleId { get; set; }
    public string? ArticleName { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitName { get; set; }
    public string? UnitSymbol { get; set; }
    public decimal Quantity { get; set; }
    public decimal PriceDelta { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsTracked { get; set; }
    public bool IsAvailable { get; set; } = true;
    public decimal? AvailableQuantity { get; set; }
}

public class MenuItemModifierGroupDto
{
    public Guid Id { get; set; }
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MinSelections { get; set; }
    public int MaxSelections { get; set; }
    public bool IsRequired { get; set; }
    public bool AllowDuplicates { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public List<MenuItemModifierOptionDto> Options { get; set; } = [];
}

public class UpsertMenuItemModifierOptionDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ArticleId { get; set; }
    public Guid? UnitId { get; set; }
    public decimal Quantity { get; set; }
    public decimal PriceDelta { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpsertMenuItemModifierGroupDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MinSelections { get; set; }
    public int MaxSelections { get; set; } = 1;
    public bool IsRequired { get; set; } = true;
    public bool AllowDuplicates { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public List<UpsertMenuItemModifierOptionDto> Options { get; set; } = [];
}

// -- Rentabilidad de platos -----------------------------------------------

public class MenuItemProfitabilityDto
{
    public Guid MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public string? InternalCode { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? CategoryColor { get; set; }
    public decimal GrossSalePrice { get; set; }
    public decimal TaxPercentage { get; set; }
    public decimal NetSalePrice { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal RecipeCost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal FoodCostPercentage { get; set; }
    public decimal GrossMarginPercentage { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string CostMethod { get; set; } = "Promedio neto de compras";
    public bool HasRecipe { get; set; }
    public bool HasMissingCosts { get; set; }
    public bool HasConversionWarnings { get; set; }
    public List<MenuItemProfitabilityIngredientDto> Ingredients { get; set; } = [];
}

public class MenuItemProfitabilityIngredientDto
{
    public Guid RecipeIngredientId { get; set; }
    public Guid ArticleId { get; set; }
    public string ArticleName { get; set; } = string.Empty;
    public string? InternalCode { get; set; }
    public decimal Quantity { get; set; }
    public Guid UnitId { get; set; }
    public string UnitSymbol { get; set; } = string.Empty;
    public decimal BaseQuantity { get; set; }
    public string BaseUnitSymbol { get; set; } = string.Empty;
    public decimal AverageUnitCost { get; set; }
    public decimal? LastUnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal CostSharePercentage { get; set; }
    public bool HasCost { get; set; }
    public string? Warning { get; set; }
}

public class UpsertRecipeIngredientDto
{
    public Guid ArticleId { get; set; }
    public Guid UnitId { get; set; }
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
}

// ── Descuento por venta ───────────────────────────────────────────────────

public class DeductStockFromSaleDto
{
    public Guid WarehouseId { get; set; }
    public List<SaleItemDto> Items { get; set; } = [];
}

public class SaleItemDto
{
    public Guid MenuItemId { get; set; }
    public decimal Quantity { get; set; }
    public Guid? OrderItemId { get; set; }
}
