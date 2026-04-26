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
}

public class MenuItemDetailDto : MenuItemDto
{
    public List<RecipeIngredientDto> Recipe { get; set; } = [];
}

public class CreateMenuItemDto
{
    public Guid MenuCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? InternalCode { get; set; }
    public decimal Price { get; set; }
    public Guid? StationId { get; set; }
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
}
