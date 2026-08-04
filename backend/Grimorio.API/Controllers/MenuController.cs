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
    private readonly IWebHostEnvironment _environment;

    public MenuController(IMediator mediator, IWebHostEnvironment environment)
    {
        _mediator = mediator;
        _environment = environment;
    }

    // â”€â”€ CategorÃ­as â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Authorize(Policy = "Menu.Categories.View")]
    [HttpGet("categorias")]
    public async Task<IActionResult> GetCategories()
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetMenuCategoriesQuery { BranchId = branchId }));
    }

    [Authorize(Policy = "Menu.Categories.Manage")]
    [HttpPost("categorias")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateMenuCategoryDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new CreateMenuCategoryCommand
        {
            BranchId = branchId, Name = dto.Name, Description = dto.Description,
            Color = dto.Color, Order = dto.Order, CostCenterId = dto.CostCenterId,
        });
        return Ok(result);
    }

    [Authorize(Policy = "Menu.Categories.Manage")]
    [HttpPut("categorias/{id:guid}")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] MenuCategoryDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new UpdateMenuCategoryCommand
        {
            Id = id, BranchId = branchId, Name = dto.Name, Description = dto.Description,
            Color = dto.Color, Order = dto.Order, IsActive = dto.IsActive,
            CostCenterId = dto.CostCenterId,
        });
        return Ok(result);
    }

    [Authorize(Policy = "Menu.Categories.Manage")]
    [HttpDelete("categorias/{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        await _mediator.Send(new DeleteMenuCategoryCommand { Id = id, BranchId = branchId });
        return NoContent();
    }

    // â”€â”€ Items â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Authorize(Policy = "Menu.Items.View")]
    [HttpGet("items")]
    public async Task<IActionResult> GetItems(
        [FromQuery] Guid? categoryId,
        [FromQuery] bool? activeOnly,
        [FromQuery] bool? availableOnly,
        [FromQuery] bool lightweight = false)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetMenuItemsQuery
        {
            BranchId = branchId, CategoryId = categoryId,
            ActiveOnly = activeOnly, AvailableOnly = availableOnly,
            Lightweight = lightweight,
        }));
    }

    [Authorize(Policy = "Menu.Items.View")]
    [HttpGet("items/{id:guid}")]
    public async Task<IActionResult> GetItem(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new GetMenuItemDetailQuery { Id = id, BranchId = branchId });
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = "Menu.Items.View")]
    [HttpGet("items/{id:guid}/ficha-operativa/pdf")]
    public async Task<IActionResult> GetItemOperationalSheetPdf(Guid id, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();

        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var pdf = await _mediator.Send(new GenerateMenuItemOperationalSheetPdfQuery
        {
            Id = id,
            BranchId = branchId,
            WebRootPath = webRoot,
        }, ct);

        return pdf is null
            ? NotFound()
            : File(pdf, "application/pdf", $"ficha-operativa-{id:N}.pdf");
    }

    [Authorize(Policy = "Menu.Items.View")]
    [HttpGet("items/disponibilidad")]
    public async Task<IActionResult> GetAvailability(
        [FromQuery] Guid? categoryId,
        [FromQuery] bool activeOnly = true,
        [FromQuery] bool availableOnly = true)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetMenuAvailabilityQuery
        {
            BranchId = branchId,
            CategoryId = categoryId,
            ActiveOnly = activeOnly,
            AvailableOnly = availableOnly,
        }));
    }

    [Authorize(Policy = "Menu.Items.View")]
    [HttpGet("rentabilidad")]
    public async Task<IActionResult> GetProfitability(
        [FromQuery] Guid? categoryId,
        [FromQuery] bool activeOnly = true,
        [FromQuery] bool availableOnly = false)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetMenuProfitabilityQuery
        {
            BranchId = branchId,
            CategoryId = categoryId,
            ActiveOnly = activeOnly,
            AvailableOnly = availableOnly,
        }));
    }

    [Authorize(Policy = "Menu.Items.Manage")]
    [HttpPost("items")]
    public async Task<IActionResult> CreateItem([FromBody] CreateMenuItemDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new CreateMenuItemCommand
        {
            BranchId = branchId, MenuCategoryId = dto.MenuCategoryId,
            Name = dto.Name, Description = dto.Description,
            InternalCode = dto.InternalCode, ImageUrl = dto.ImageUrl, Price = dto.Price,
            StationId = dto.StationId, TaxRateId = dto.TaxRateId,
        });
        return Ok(result);
    }

    [Authorize(Policy = "Menu.Items.Manage")]
    [HttpPut("items/{id:guid}")]
    public async Task<IActionResult> UpdateItem(Guid id, [FromBody] UpdateMenuItemDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new UpdateMenuItemCommand
        {
            Id = id, BranchId = branchId, MenuCategoryId = dto.MenuCategoryId,
            Name = dto.Name, Description = dto.Description,
            InternalCode = dto.InternalCode, ImageUrl = dto.ImageUrl, Price = dto.Price,
            IsActive = dto.IsActive, AvailableForSale = dto.AvailableForSale,
            StationId = dto.StationId, TaxRateId = dto.TaxRateId,
        });
        return Ok(result);
    }

    [Authorize(Policy = "Menu.Items.Manage")]
    [HttpPost("items/{id:guid}/image")]
    [RequestSizeLimit(4 * 1024 * 1024)]
    public async Task<IActionResult> UploadItemImage(Guid id, [FromForm] IFormFile image, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        if (image.Length == 0) return BadRequest("Imagen no válida.");
        if (image.Length > 3 * 1024 * 1024) return BadRequest("La imagen no puede superar 3 MB.");

        var oldItem = await _mediator.Send(new GetMenuItemDetailQuery { Id = id, BranchId = branchId }, ct);
        if (oldItem is null) return NotFound();

        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowedExtensions.Contains(extension)) return BadRequest("Formato no permitido. Usa JPG, PNG o WEBP.");

        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var relativeFolder = Path.Combine("uploads", "menu-items", branchId.ToString("N"));
        var targetFolder = Path.Combine(webRoot, relativeFolder);
        Directory.CreateDirectory(targetFolder);

        var fileName = $"{id:N}-{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(targetFolder, fileName);
        await using (var stream = System.IO.File.Create(fullPath))
        {
            await image.CopyToAsync(stream, ct);
        }

        var imageUrl = "/" + Path.Combine(relativeFolder, fileName).Replace('\\', '/');
        var result = await _mediator.Send(new UpdateMenuItemImageCommand
        {
            Id = id,
            BranchId = branchId,
            ImageUrl = imageUrl,
        }, ct);

        DeleteLocalUpload(oldItem.ImageUrl);
        return Ok(result);
    }

    [Authorize(Policy = "Menu.Items.Manage")]
    [HttpDelete("items/{id:guid}/image")]
    public async Task<IActionResult> DeleteItemImage(Guid id, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();

        var oldItem = await _mediator.Send(new GetMenuItemDetailQuery { Id = id, BranchId = branchId }, ct);
        if (oldItem is null) return NotFound();

        var result = await _mediator.Send(new UpdateMenuItemImageCommand
        {
            Id = id,
            BranchId = branchId,
            ImageUrl = null,
        }, ct);

        DeleteLocalUpload(oldItem.ImageUrl);
        return Ok(result);
    }

    [Authorize(Policy = "Menu.Items.Manage")]
    [HttpDelete("items/{id:guid}")]
    public async Task<IActionResult> DeleteItem(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        await _mediator.Send(new DeleteMenuItemCommand { Id = id, BranchId = branchId });
        return NoContent();
    }

    // â”€â”€ Recipe â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Authorize(Policy = "Menu.Items.Manage")]
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

    [Authorize(Policy = "Menu.Items.Manage")]
    [HttpDelete("receta/{id:guid}")]
    public async Task<IActionResult> DeleteIngredient(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        await _mediator.Send(new DeleteRecipeIngredientCommand { Id = id, BranchId = branchId });
        return NoContent();
    }

    [Authorize(Policy = "Menu.Items.View")]
    [HttpGet("subrecetas")]
    public async Task<IActionResult> GetSubRecipes([FromQuery] bool activeOnly = false)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetSubRecipesQuery { BranchId = branchId, ActiveOnly = activeOnly }));
    }

    [Authorize(Policy = "Menu.Items.Manage")]
    [HttpPost("subrecetas")]
    public async Task<IActionResult> CreateSubRecipe([FromBody] UpsertSubRecipeDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new UpsertSubRecipeCommand { BranchId = branchId, SubRecipe = dto });
        return Ok(result);
    }

    [Authorize(Policy = "Menu.Items.Manage")]
    [HttpPut("subrecetas/{id:guid}")]
    public async Task<IActionResult> UpdateSubRecipe(Guid id, [FromBody] UpsertSubRecipeDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new UpsertSubRecipeCommand { Id = id, BranchId = branchId, SubRecipe = dto });
        return Ok(result);
    }

    [Authorize(Policy = "Menu.Items.Manage")]
    [HttpDelete("subrecetas/{id:guid}")]
    public async Task<IActionResult> DeleteSubRecipe(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        await _mediator.Send(new DeleteSubRecipeCommand { Id = id, BranchId = branchId });
        return NoContent();
    }

    [Authorize(Policy = "Menu.Items.Manage")]
    [HttpPut("items/{id:guid}/modifiers")]
    public async Task<IActionResult> UpsertModifiers(Guid id, [FromBody] List<UpsertMenuItemModifierGroupDto> groups)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new UpsertMenuItemModifiersCommand
        {
            MenuItemId = id,
            BranchId = branchId,
            Groups = groups,
        });
        return Ok(result);
    }

    [Authorize(Policy = "Menu.Items.Manage")]
    [HttpPut("items/{id:guid}/preparacion")]
    public async Task<IActionResult> UpsertPreparation(Guid id, [FromBody] UpsertMenuItemPreparationDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new UpsertMenuItemPreparationCommand
        {
            MenuItemId = id,
            BranchId = branchId,
            Preparation = dto,
        });
        return Ok(result);
    }

    // â”€â”€ Descuento por venta â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    // â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private bool TryGetBranchId(out Guid branchId)
    {
        var claim = User.FindFirst(AppConstants.Claims.BranchId)?.Value;
        return Guid.TryParse(claim, out branchId);
    }

    private void DeleteLocalUpload(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith("/uploads/menu-items/", StringComparison.OrdinalIgnoreCase))
            return;

        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var relativePath = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(webRoot, relativePath));
        var uploadsRoot = Path.GetFullPath(Path.Combine(webRoot, "uploads", "menu-items"));

        if (!fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase)) return;
        if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
    }
}
