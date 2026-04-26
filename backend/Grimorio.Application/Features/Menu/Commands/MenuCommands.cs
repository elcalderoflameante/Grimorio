using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.Menu.Commands;

// ── Categorías ────────────────────────────────────────────────────────────

public class CreateMenuCategoryCommand : IRequest<MenuCategoryDto>
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public int Order { get; set; }
}

public class UpdateMenuCategoryCommand : IRequest<MenuCategoryDto>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}

public class DeleteMenuCategoryCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
}

// ── Items ─────────────────────────────────────────────────────────────────

public class CreateMenuItemCommand : IRequest<MenuItemDto>
{
    public Guid BranchId { get; set; }
    public Guid MenuCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? InternalCode { get; set; }
    public decimal Price { get; set; }
    public Guid? StationId { get; set; }
}

public class UpdateMenuItemCommand : IRequest<MenuItemDto>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid MenuCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? InternalCode { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public bool AvailableForSale { get; set; }
    public Guid? StationId { get; set; }
}

public class DeleteMenuItemCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
}

// ── Recipe ────────────────────────────────────────────────────────────────

public class UpsertRecipeCommand : IRequest<List<RecipeIngredientDto>>
{
    public Guid MenuItemId { get; set; }
    public Guid BranchId { get; set; }
    public List<UpsertRecipeIngredientDto> Ingredients { get; set; } = [];
}

public class DeleteRecipeIngredientCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
}

// ── Descuento por venta ───────────────────────────────────────────────────

public class DeductStockFromSaleCommand : IRequest<bool>
{
    public Guid BranchId { get; set; }
    public Guid WarehouseId { get; set; }
    public List<SaleItemDto> Items { get; set; } = [];
}
