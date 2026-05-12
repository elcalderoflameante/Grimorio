using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Menu.Commands;
using Grimorio.Application.Features.Menu.Queries;
using Grimorio.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grimorio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MenuController : ControllerBase
{
    private readonly IMediator _mediator;
    public MenuController(IMediator mediator) => _mediator = mediator;

    // â”€â”€ CategorÃ­as â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [HttpGet("categorias")]
    public async Task<IActionResult> GetCategories()
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetMenuCategoriesQuery { BranchId = branchId }));
    }

    [HttpPost("categorias")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateMenuCategoryDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new CreateMenuCategoryCommand
        {
            BranchId = branchId, Name = dto.Name, Description = dto.Description,
            Color = dto.Color, Order = dto.Order,
        });
        return Ok(result);
    }

    [HttpPut("categorias/{id:guid}")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] MenuCategoryDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new UpdateMenuCategoryCommand
        {
            Id = id, BranchId = branchId, Name = dto.Name, Description = dto.Description,
            Color = dto.Color, Order = dto.Order, IsActive = dto.IsActive,
        });
        return Ok(result);
    }

    [HttpDelete("categorias/{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        await _mediator.Send(new DeleteMenuCategoryCommand { Id = id, BranchId = branchId });
        return NoContent();
    }

    // â”€â”€ Items â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [HttpGet("items")]
    public async Task<IActionResult> GetItems(
        [FromQuery] Guid? categoryId,
        [FromQuery] bool? activeOnly,
        [FromQuery] bool? availableOnly)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetMenuItemsQuery
        {
            BranchId = branchId, CategoryId = categoryId,
            ActiveOnly = activeOnly, AvailableOnly = availableOnly,
        }));
    }

    [HttpGet("items/{id:guid}")]
    public async Task<IActionResult> GetItem(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new GetMenuItemDetailQuery { Id = id, BranchId = branchId });
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("items")]
    public async Task<IActionResult> CreateItem([FromBody] CreateMenuItemDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new CreateMenuItemCommand
        {
            BranchId = branchId, MenuCategoryId = dto.MenuCategoryId,
            Name = dto.Name, Description = dto.Description,
            InternalCode = dto.InternalCode, Price = dto.Price,
            StationId = dto.StationId, TaxRateId = dto.TaxRateId,
        });
        return Ok(result);
    }

    [HttpPut("items/{id:guid}")]
    public async Task<IActionResult> UpdateItem(Guid id, [FromBody] UpdateMenuItemDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new UpdateMenuItemCommand
        {
            Id = id, BranchId = branchId, MenuCategoryId = dto.MenuCategoryId,
            Name = dto.Name, Description = dto.Description,
            InternalCode = dto.InternalCode, Price = dto.Price,
            IsActive = dto.IsActive, AvailableForSale = dto.AvailableForSale,
            StationId = dto.StationId, TaxRateId = dto.TaxRateId,
        });
        return Ok(result);
    }

    [HttpDelete("items/{id:guid}")]
    public async Task<IActionResult> DeleteItem(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        await _mediator.Send(new DeleteMenuItemCommand { Id = id, BranchId = branchId });
        return NoContent();
    }

    // â”€â”€ Recipe â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [HttpPut("items/{id:guid}/receta")]
    public async Task<IActionResult> UpsertRecipe(Guid id, [FromBody] List<UpsertRecipeIngredientDto> ingredients)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new UpsertRecipeCommand
        {
            MenuItemId = id, BranchId = branchId, Ingredients = ingredients,
        });
        return Ok(result);
    }

    [HttpDelete("receta/{id:guid}")]
    public async Task<IActionResult> DeleteIngredient(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        await _mediator.Send(new DeleteRecipeIngredientCommand { Id = id, BranchId = branchId });
        return NoContent();
    }

    // â”€â”€ Descuento por venta â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [HttpPost("venta/descontar-stock")]
    public async Task<IActionResult> DeductStock([FromBody] DeductStockFromSaleDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        await _mediator.Send(new DeductStockFromSaleCommand
        {
            BranchId = branchId, WarehouseId = dto.WarehouseId, Items = dto.Items,
        });
        return Ok();
    }

    // â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private bool TryGetBranchId(out Guid branchId)
    {
        var claim = User.FindFirst(AppConstants.Claims.BranchId)?.Value;
        return Guid.TryParse(claim, out branchId);
    }
}

