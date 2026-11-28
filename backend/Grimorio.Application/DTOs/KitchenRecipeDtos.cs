namespace Grimorio.Application.DTOs;

public class KitchenRecipeDto
{
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public List<KitchenRecipeIngredientDto> Ingredients { get; set; } = [];
    public MenuItemPreparationDto? Preparation { get; set; }
}

public class KitchenRecipeIngredientDto
{
    public string Name { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<KitchenRecipeIngredientDto> Ingredients { get; set; } = [];
}
