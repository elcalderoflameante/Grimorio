using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Inventory.Commands;
using Grimorio.Application.Features.Inventory.Queries;
using Grimorio.Domain.Entities.Inventory;
using Grimorio.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grimorio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public InventoryController(IMediator mediator) => _mediator = mediator;

    // ── Unidades de medida ────────────────────────────────────────────────

    [HttpGet("unidades")]
    public async Task<IActionResult> GetUnits()
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetMeasurementUnitsQuery { BranchId = branchId }));
    }

    [HttpPost("unidades")]
    public async Task<IActionResult> CreateUnit([FromBody] CreateMeasurementUnitDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new CreateMeasurementUnitCommand { BranchId = branchId, Name = dto.Name, Symbol = dto.Symbol });
        return Ok(result);
    }

    [HttpPut("unidades/{id:guid}")]
    public async Task<IActionResult> UpdateUnit(Guid id, [FromBody] CreateMeasurementUnitDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new UpdateMeasurementUnitCommand { Id = id, BranchId = branchId, Name = dto.Name, Symbol = dto.Symbol });
        return Ok(result);
    }

    [HttpDelete("unidades/{id:guid}")]
    public async Task<IActionResult> DeleteUnit(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        await _mediator.Send(new DeleteMeasurementUnitCommand { Id = id, BranchId = branchId });
        return NoContent();
    }

    // ── Conversiones de unidad ────────────────────────────────────────────

    [HttpGet("conversiones")]
    public async Task<IActionResult> GetConversions()
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetUnitConversionsQuery { BranchId = branchId }));
    }

    [HttpPost("conversiones")]
    public async Task<IActionResult> CreateConversion([FromBody] CreateUnitConversionDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new CreateUnitConversionCommand
        {
            BranchId = branchId, OriginUnitId = dto.OriginUnitId,
            DestinationUnitId = dto.DestinationUnitId, Factor = dto.Factor,
        });
        return Ok(result);
    }

    [HttpDelete("conversiones/{id:guid}")]
    public async Task<IActionResult> DeleteConversion(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        await _mediator.Send(new DeleteUnitConversionCommand { Id = id, BranchId = branchId });
        return NoContent();
    }

    // ── Categorías ────────────────────────────────────────────────────────

    [HttpGet("categorias")]
    public async Task<IActionResult> GetCategories()
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetInventoryCategoriesQuery { BranchId = branchId }));
    }

    [HttpPost("categorias")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateInventoryCategoryDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new CreateInventoryCategoryCommand
        {
            BranchId = branchId, Name = dto.Name, Description = dto.Description, Color = dto.Color,
        });
        return Ok(result);
    }

    [HttpPut("categorias/{id:guid}")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] CreateInventoryCategoryDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new UpdateInventoryCategoryCommand
        {
            Id = id, BranchId = branchId, Name = dto.Name, Description = dto.Description, Color = dto.Color,
        });
        return Ok(result);
    }

    [HttpDelete("categorias/{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        await _mediator.Send(new DeleteInventoryCategoryCommand { Id = id, BranchId = branchId });
        return NoContent();
    }

    // ── Artículos ─────────────────────────────────────────────────────────

    [HttpGet("articulos")]
    public async Task<IActionResult> GetArticles([FromQuery] bool? activeOnly, [FromQuery] string? type, [FromQuery] Guid? categoryId)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetInventoryArticlesQuery
        {
            BranchId = branchId, ActiveOnly = activeOnly, Type = type, CategoryId = categoryId,
        }));
    }

    [HttpGet("articulos/{id:guid}")]
    public async Task<IActionResult> GetArticle(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new GetInventoryArticleQuery { Id = id, BranchId = branchId });
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("articulos")]
    public async Task<IActionResult> CreateArticle([FromBody] CreateInventoryArticleDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        if (!Enum.TryParse<ArticleType>(dto.Type, out var articleType))
            return BadRequest($"Type de artículo inválido: {dto.Type}");
        var result = await _mediator.Send(new CreateInventoryArticleCommand
        {
            BranchId = branchId, Name = dto.Name, Description = dto.Description,
            InternalCode = dto.InternalCode, Type = articleType, CategoryId = dto.CategoryId,
            BaseUnitId = dto.BaseUnitId, MinStock = dto.MinStock, StockAlertActive = dto.StockAlertActive,
        });
        return Ok(result);
    }

    [HttpPut("articulos/{id:guid}")]
    public async Task<IActionResult> UpdateArticle(Guid id, [FromBody] UpdateInventoryArticleDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        if (!Enum.TryParse<ArticleType>(dto.Type, out var articleType))
            return BadRequest($"Type de artículo inválido: {dto.Type}");
        var result = await _mediator.Send(new UpdateInventoryArticleCommand
        {
            Id = id, BranchId = branchId, Name = dto.Name, Description = dto.Description,
            InternalCode = dto.InternalCode, Type = articleType, CategoryId = dto.CategoryId,
            BaseUnitId = dto.BaseUnitId, MinStock = dto.MinStock,
            StockAlertActive = dto.StockAlertActive, IsActive = dto.IsActive,
        });
        return Ok(result);
    }

    [HttpDelete("articulos/{id:guid}")]
    public async Task<IActionResult> DeleteArticle(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        await _mediator.Send(new DeleteInventoryArticleCommand { Id = id, BranchId = branchId });
        return NoContent();
    }

    // ── Warehouses ───────────────────────────────────────────────────────────

    [HttpGet("bodegas")]
    public async Task<IActionResult> GetWarehouses([FromQuery] bool? activeOnly)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetWarehousesQuery { BranchId = branchId, ActiveOnly = activeOnly }));
    }

    [HttpPost("bodegas")]
    public async Task<IActionResult> CreateWarehouse([FromBody] CreateWarehouseDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new CreateWarehouseCommand
        {
            BranchId = branchId, Name = dto.Name, Description = dto.Description, Location = dto.Location,
        });
        return Ok(result);
    }

    [HttpPut("bodegas/{id:guid}")]
    public async Task<IActionResult> UpdateWarehouse(Guid id, [FromBody] WarehouseDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new UpdateWarehouseCommand
        {
            Id = id, BranchId = branchId, Name = dto.Name,
            Description = dto.Description, Location = dto.Location, IsActive = dto.IsActive,
        });
        return Ok(result);
    }

    [HttpDelete("bodegas/{id:guid}")]
    public async Task<IActionResult> DeleteWarehouse(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        await _mediator.Send(new DeleteWarehouseCommand { Id = id, BranchId = branchId });
        return NoContent();
    }

    // ── Stock ─────────────────────────────────────────────────────────────

    [HttpGet("stock")]
    public async Task<IActionResult> GetStock([FromQuery] Guid? warehouseId, [FromQuery] Guid? categoryId, [FromQuery] bool? lowStockOnly)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetCurrentStockQuery
        {
            BranchId = branchId, WarehouseId = warehouseId, CategoryId = categoryId, LowStockOnly = lowStockOnly,
        }));
    }

    [HttpGet("alertas")]
    public async Task<IActionResult> GetAlertas()
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetStockAlertsQuery { BranchId = branchId }));
    }

    // ── Movements ───────────────────────────────────────────────────────

    [HttpGet("movimientos")]
    public async Task<IActionResult> GetMovimientos(
        [FromQuery] Guid? articleId, [FromQuery] Guid? warehouseId,
        [FromQuery] string? type, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int pageSize = 50)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetStockMovementsQuery
        {
            BranchId = branchId, ArticleId = articleId, WarehouseId = warehouseId,
            Type = type, FromUtc = from, ToUtc = to, PageSize = pageSize,
        }));
    }

    [HttpPost("movimientos")]
    public async Task<IActionResult> RegistrarMovimiento([FromBody] RegisterMovementDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        if (!Enum.TryParse<MovementType>(dto.Type, out var movementType))
            return BadRequest($"Type de movimiento inválido: {dto.Type}");
        var result = await _mediator.Send(new RegisterMovementCommand
        {
            BranchId = branchId, ArticleId = dto.ArticleId, WarehouseId = dto.WarehouseId,
            Type = movementType, Quantity = dto.Quantity, UnitId = dto.UnitId,
            Reference = dto.Reference, Notes = dto.Notes,
        });
        return Ok(result);
    }

    [HttpPost("movimientos/inventario-inicial")]
    public async Task<IActionResult> RegisterInitialInventory([FromBody] RegisterInitialInventoryDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new RegisterInitialInventoryCommand
        {
            BranchId = branchId, Items = dto.Items,
        });
        return Ok(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private bool TryGetBranchId(out Guid branchId)
    {
        var claim = User.FindFirst(AppConstants.Claims.BranchId)?.Value;
        return Guid.TryParse(claim, out branchId);
    }
}

