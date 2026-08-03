using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Inventory.Commands;
using Grimorio.Domain.Entities.Inventory;
using Grimorio.Infrastructure.Features.Inventory.Queries;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Grimorio.Infrastructure.Features.Inventory.Commands;

// ── Unidades de medida ────────────────────────────────────────────────────

public class CreateMeasurementUnitHandler : IRequestHandler<CreateMeasurementUnitCommand, MeasurementUnitDto>
{
    private readonly GrimorioDbContext _db;
    public CreateMeasurementUnitHandler(GrimorioDbContext db) => _db = db;

    public async Task<MeasurementUnitDto> Handle(CreateMeasurementUnitCommand req, CancellationToken ct)
    {
        if (await _db.MeasurementUnits.AnyAsync(x => x.BranchId == req.BranchId && x.Name == req.Name, ct))
            throw new InvalidOperationException("Ya existe una unidad de medida con ese nombre.");

        var entity = new MeasurementUnit { Id = Guid.NewGuid(), BranchId = req.BranchId, Name = req.Name.Trim(), Symbol = req.Symbol.Trim() };
        _db.MeasurementUnits.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new MeasurementUnitDto { Id = entity.Id, Name = entity.Name, Symbol = entity.Symbol };
    }
}

public class UpdateMeasurementUnitHandler : IRequestHandler<UpdateMeasurementUnitCommand, MeasurementUnitDto>
{
    private readonly GrimorioDbContext _db;
    public UpdateMeasurementUnitHandler(GrimorioDbContext db) => _db = db;

    public async Task<MeasurementUnitDto> Handle(UpdateMeasurementUnitCommand req, CancellationToken ct)
    {
        var entity = await _db.MeasurementUnits.FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId, ct)
            ?? throw new InvalidOperationException("Unit de medida no encontrada.");
        entity.Name = req.Name.Trim();
        entity.Symbol = req.Symbol.Trim();
        await _db.SaveChangesAsync(ct);
        return new MeasurementUnitDto { Id = entity.Id, Name = entity.Name, Symbol = entity.Symbol };
    }
}

public class DeleteMeasurementUnitHandler : IRequestHandler<DeleteMeasurementUnitCommand, bool>
{
    private readonly GrimorioDbContext _db;
    public DeleteMeasurementUnitHandler(GrimorioDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteMeasurementUnitCommand req, CancellationToken ct)
    {
        var entity = await _db.MeasurementUnits.FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId, ct)
            ?? throw new InvalidOperationException("Unit de medida no encontrada.");
        _db.MeasurementUnits.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

// ── Conversiones ──────────────────────────────────────────────────────────

public class CreateUnitConversionHandler : IRequestHandler<CreateUnitConversionCommand, UnitConversionDto>
{
    private readonly GrimorioDbContext _db;
    public CreateUnitConversionHandler(GrimorioDbContext db) => _db = db;

    public async Task<UnitConversionDto> Handle(CreateUnitConversionCommand req, CancellationToken ct)
    {
        if (await _db.UnitConversions.AnyAsync(
            x => x.BranchId == req.BranchId && x.OriginUnitId == req.OriginUnitId && x.DestinationUnitId == req.DestinationUnitId, ct))
            throw new InvalidOperationException("Ya existe esa conversión.");

        var entity = new UnitConversion
        {
            Id = Guid.NewGuid(), BranchId = req.BranchId,
            OriginUnitId = req.OriginUnitId, DestinationUnitId = req.DestinationUnitId, Factor = req.Factor,
        };
        _db.UnitConversions.Add(entity);
        await _db.SaveChangesAsync(ct);

        await _db.Entry(entity).Reference(x => x.OriginUnit).LoadAsync(ct);
        await _db.Entry(entity).Reference(x => x.DestinationUnit).LoadAsync(ct);

        return new UnitConversionDto
        {
            Id = entity.Id, OriginUnitId = entity.OriginUnitId,
            OriginUnitName = entity.OriginUnit!.Name, OriginUnitSymbol = entity.OriginUnit.Symbol,
            DestinationUnitId = entity.DestinationUnitId,
            DestinationUnitName = entity.DestinationUnit!.Name, DestinationUnitSymbol = entity.DestinationUnit.Symbol,
            Factor = entity.Factor,
        };
    }
}

public class DeleteUnitConversionHandler : IRequestHandler<DeleteUnitConversionCommand, bool>
{
    private readonly GrimorioDbContext _db;
    public DeleteUnitConversionHandler(GrimorioDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteUnitConversionCommand req, CancellationToken ct)
    {
        var entity = await _db.UnitConversions.FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId, ct)
            ?? throw new InvalidOperationException("Conversión no encontrada.");
        _db.UnitConversions.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

// ── Categorías ────────────────────────────────────────────────────────────

public class CreateInventoryCategoryHandler : IRequestHandler<CreateInventoryCategoryCommand, InventoryCategoryDto>
{
    private readonly GrimorioDbContext _db;
    public CreateInventoryCategoryHandler(GrimorioDbContext db) => _db = db;

    public async Task<InventoryCategoryDto> Handle(CreateInventoryCategoryCommand req, CancellationToken ct)
    {
        if (await _db.InventoryCategories.AnyAsync(x => x.BranchId == req.BranchId && x.Name == req.Name, ct))
            throw new InvalidOperationException("Ya existe una categoría con ese nombre.");

        var entity = new InventoryCategory
        {
            Id = Guid.NewGuid(), BranchId = req.BranchId,
            Name = req.Name.Trim(), Description = req.Description?.Trim(), Color = req.Color?.Trim(),
        };
        _db.InventoryCategories.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new InventoryCategoryDto { Id = entity.Id, Name = entity.Name, Description = entity.Description, Color = entity.Color };
    }
}

public class UpdateInventoryCategoryHandler : IRequestHandler<UpdateInventoryCategoryCommand, InventoryCategoryDto>
{
    private readonly GrimorioDbContext _db;
    public UpdateInventoryCategoryHandler(GrimorioDbContext db) => _db = db;

    public async Task<InventoryCategoryDto> Handle(UpdateInventoryCategoryCommand req, CancellationToken ct)
    {
        var entity = await _db.InventoryCategories.FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId, ct)
            ?? throw new InvalidOperationException("Categoría no encontrada.");
        entity.Name = req.Name.Trim();
        entity.Description = req.Description?.Trim();
        entity.Color = req.Color?.Trim();
        await _db.SaveChangesAsync(ct);
        return new InventoryCategoryDto { Id = entity.Id, Name = entity.Name, Description = entity.Description, Color = entity.Color };
    }
}

public class DeleteInventoryCategoryHandler : IRequestHandler<DeleteInventoryCategoryCommand, bool>
{
    private readonly GrimorioDbContext _db;
    public DeleteInventoryCategoryHandler(GrimorioDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteInventoryCategoryCommand req, CancellationToken ct)
    {
        var entity = await _db.InventoryCategories.FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId, ct)
            ?? throw new InvalidOperationException("Categoría no encontrada.");
        if (await _db.InventoryArticles.AnyAsync(x => x.CategoryId == req.Id && !x.IsDeleted, ct))
            throw new InvalidOperationException("No se puede eliminar una categoría con artículos asociados.");
        _db.InventoryCategories.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

// ── Artículos ─────────────────────────────────────────────────────────────

public class CreateInventoryArticleHandler : IRequestHandler<CreateInventoryArticleCommand, InventoryArticleDto>
{
    private readonly GrimorioDbContext _db;
    public CreateInventoryArticleHandler(GrimorioDbContext db) => _db = db;

    public async Task<InventoryArticleDto> Handle(CreateInventoryArticleCommand req, CancellationToken ct)
    {
        var entity = new InventoryArticle
        {
            Id = Guid.NewGuid(), BranchId = req.BranchId,
            Name = req.Name.Trim(), Description = req.Description?.Trim(),
            InternalCode = string.IsNullOrWhiteSpace(req.InternalCode) ? null : req.InternalCode.Trim(),
            Type = req.Type, CategoryId = req.CategoryId, BaseUnitId = req.BaseUnitId,
            MinStock = req.MinStock, StockAlertActive = req.StockAlertActive, IsActive = true,
        };
        _db.InventoryArticles.Add(entity);
        await _db.SaveChangesAsync(ct);

        return await LoadAndMap(entity.Id, req.BranchId, ct);
    }

    private async Task<InventoryArticleDto> LoadAndMap(Guid id, Guid branchId, CancellationToken ct)
    {
        var x = await _db.InventoryArticles
            .Include(a => a.Category).Include(a => a.BaseUnit)
            .Include(a => a.Stocks.Where(s => !s.IsDeleted))
            .FirstAsync(a => a.Id == id && a.BranchId == branchId, ct);
        return GetInventoryArticlesHandler.MapArticulo(x);
    }
}

public class UpdateInventoryArticleHandler : IRequestHandler<UpdateInventoryArticleCommand, InventoryArticleDto>
{
    private readonly GrimorioDbContext _db;
    public UpdateInventoryArticleHandler(GrimorioDbContext db) => _db = db;

    public async Task<InventoryArticleDto> Handle(UpdateInventoryArticleCommand req, CancellationToken ct)
    {
        var entity = await _db.InventoryArticles.FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId, ct)
            ?? throw new InvalidOperationException("Artículo no encontrado.");

        entity.Name = req.Name.Trim();
        entity.Description = req.Description?.Trim();
        entity.InternalCode = string.IsNullOrWhiteSpace(req.InternalCode) ? null : req.InternalCode.Trim();
        entity.Type = req.Type;
        entity.CategoryId = req.CategoryId;
        entity.BaseUnitId = req.BaseUnitId;
        entity.MinStock = req.MinStock;
        entity.StockAlertActive = req.StockAlertActive;
        entity.IsActive = req.IsActive;
        await _db.SaveChangesAsync(ct);

        var x = await _db.InventoryArticles
            .Include(a => a.Category).Include(a => a.BaseUnit)
            .Include(a => a.Stocks.Where(s => !s.IsDeleted))
            .FirstAsync(a => a.Id == req.Id, ct);
        return GetInventoryArticlesHandler.MapArticulo(x);
    }
}

public class DeleteInventoryArticleHandler : IRequestHandler<DeleteInventoryArticleCommand, bool>
{
    private readonly GrimorioDbContext _db;
    public DeleteInventoryArticleHandler(GrimorioDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteInventoryArticleCommand req, CancellationToken ct)
    {
        var entity = await _db.InventoryArticles.FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId, ct)
            ?? throw new InvalidOperationException("Artículo no encontrado.");
        _db.InventoryArticles.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

// ── Warehouses ───────────────────────────────────────────────────────────────

public class CreateWarehouseHandler : IRequestHandler<CreateWarehouseCommand, WarehouseDto>
{
    private readonly GrimorioDbContext _db;
    public CreateWarehouseHandler(GrimorioDbContext db) => _db = db;

    public async Task<WarehouseDto> Handle(CreateWarehouseCommand req, CancellationToken ct)
    {
        if (await _db.Warehouses.AnyAsync(x => x.BranchId == req.BranchId && x.Name == req.Name, ct))
            throw new InvalidOperationException("Ya existe una bodega con ese nombre.");
        var entity = new Warehouse
        {
            Id = Guid.NewGuid(), BranchId = req.BranchId,
            Name = req.Name.Trim(), Description = req.Description?.Trim(),
            Location = req.Location?.Trim(), IsActive = true,
        };
        _db.Warehouses.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new WarehouseDto { Id = entity.Id, Name = entity.Name, Description = entity.Description, Location = entity.Location, IsActive = entity.IsActive };
    }
}

public class UpdateWarehouseHandler : IRequestHandler<UpdateWarehouseCommand, WarehouseDto>
{
    private readonly GrimorioDbContext _db;
    public UpdateWarehouseHandler(GrimorioDbContext db) => _db = db;

    public async Task<WarehouseDto> Handle(UpdateWarehouseCommand req, CancellationToken ct)
    {
        var entity = await _db.Warehouses.FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId, ct)
            ?? throw new InvalidOperationException("Warehouse no encontrada.");
        entity.Name = req.Name.Trim();
        entity.Description = req.Description?.Trim();
        entity.Location = req.Location?.Trim();
        entity.IsActive = req.IsActive;
        await _db.SaveChangesAsync(ct);
        return new WarehouseDto { Id = entity.Id, Name = entity.Name, Description = entity.Description, Location = entity.Location, IsActive = entity.IsActive };
    }
}

public class DeleteWarehouseHandler : IRequestHandler<DeleteWarehouseCommand, bool>
{
    private readonly GrimorioDbContext _db;
    public DeleteWarehouseHandler(GrimorioDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteWarehouseCommand req, CancellationToken ct)
    {
        var entity = await _db.Warehouses.FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId, ct)
            ?? throw new InvalidOperationException("Warehouse no encontrada.");
        _db.Warehouses.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

// ── Movements de stock ──────────────────────────────────────────────────

public class RegisterMovementHandler : IRequestHandler<RegisterMovementCommand, StockMovementDto>
{
    private readonly GrimorioDbContext _db;
    public RegisterMovementHandler(GrimorioDbContext db) => _db = db;

    public async Task<StockMovementDto> Handle(RegisterMovementCommand req, CancellationToken ct)
    {
        var article = await _db.InventoryArticles
            .Include(x => x.BaseUnit)
            .FirstOrDefaultAsync(x => x.Id == req.ArticleId && x.BranchId == req.BranchId, ct)
            ?? throw new InvalidOperationException("Artículo no encontrado.");

        var warehouse = await _db.Warehouses
            .FirstOrDefaultAsync(x => x.Id == req.WarehouseId && x.BranchId == req.BranchId, ct)
            ?? throw new InvalidOperationException("Bodega no encontrada.");

        var movementUnit = await _db.MeasurementUnits
            .FirstOrDefaultAsync(x => x.Id == req.UnitId && x.BranchId == req.BranchId, ct)
            ?? throw new InvalidOperationException("Unit de medida no encontrada.");

        // Convertir a unidad base
        decimal baseQuantity = req.Quantity;
        if (req.UnitId != article.BaseUnitId)
        {
            // Dirección directa: unidad_movimiento → unidad_base
            var conversion = await _db.UnitConversions.FirstOrDefaultAsync(
                x => x.BranchId == req.BranchId && x.OriginUnitId == req.UnitId && x.DestinationUnitId == article.BaseUnitId, ct);

            if (conversion != null)
            {
                baseQuantity = req.Quantity * conversion.Factor;
            }
            else
            {
                // Dirección inversa: unidad_base → unidad_movimiento (se invierte el factor)
                var reverseConversion = await _db.UnitConversions.FirstOrDefaultAsync(
                    x => x.BranchId == req.BranchId && x.OriginUnitId == article.BaseUnitId && x.DestinationUnitId == req.UnitId, ct);

                if (reverseConversion is null)
                    throw new InvalidOperationException(
                        $"No existe conversión entre {movementUnit.Name} y {article.BaseUnit!.Name}.");

                baseQuantity = req.Quantity / reverseConversion.Factor;
            }
        }

        // Determinar si suma o resta según tipo de movimiento
        var isExit = req.Type is MovementType.ManualExit or MovementType.Waste
            or MovementType.Spoilage or MovementType.SaleDeduction or MovementType.TransferOut
            or MovementType.NegativeAdjustment or MovementType.ProductionInput;

        var effectiveQuantity = isExit ? -Math.Abs(baseQuantity) : Math.Abs(baseQuantity);
        var allowsManualCost = req.Type is MovementType.InitialInventory
            or MovementType.ManualEntry
            or MovementType.PositiveAdjustment
            or MovementType.ProductionInput
            or MovementType.ProductionOutput;
        decimal? unitCost = allowsManualCost ? req.UnitCost : null;
        decimal? totalCost = unitCost.HasValue ? Math.Abs(effectiveQuantity) * unitCost.Value : null;

        var movement = new StockMovement
        {
            Id = Guid.NewGuid(), BranchId = req.BranchId,
            ArticleId = req.ArticleId, WarehouseId = req.WarehouseId,
            Type = req.Type, Quantity = req.Quantity, UnitId = req.UnitId,
            BaseQuantity = effectiveQuantity, UnitCost = unitCost, TotalCost = totalCost,
            Reference = req.Reference?.Trim(), Notes = req.Notes?.Trim(),
        };
        _db.StockMovements.Add(movement);

        var ownsTransaction = _db.Database.CurrentTransaction is null;
        IDbContextTransaction? transaction = null;
        if (ownsTransaction)
            transaction = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            await _db.SaveChangesAsync(ct);

            // Recalcular stock real desde todos los movimientos (corrige divergencias históricas)
            var trueStock = await _db.StockMovements
                .Where(m => m.BranchId == req.BranchId && m.ArticleId == req.ArticleId && m.WarehouseId == req.WarehouseId)
                .SumAsync(m => m.BaseQuantity, ct);

            var stock = await _db.WarehouseStock.FirstOrDefaultAsync(
                x => x.BranchId == req.BranchId && x.ArticleId == req.ArticleId && x.WarehouseId == req.WarehouseId, ct);

            if (stock is null)
            {
                stock = new WarehouseStock
                {
                    Id = Guid.NewGuid(), BranchId = req.BranchId,
                    ArticleId = req.ArticleId, WarehouseId = req.WarehouseId,
                    Quantity = trueStock, LastUpdatedAt = DateTime.UtcNow,
                };
                _db.WarehouseStock.Add(stock);
            }
            else
            {
                stock.Quantity = trueStock;
                stock.LastUpdatedAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync(ct);

            if (transaction is not null)
                await transaction.CommitAsync(ct);
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(ct);
            throw;
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }

        return new StockMovementDto
        {
            Id = movement.Id, ArticleId = article.Id, ArticleName = article.Name,
            WarehouseId = req.WarehouseId, WarehouseName = warehouse.Name,
            Type = movement.Type.ToString(), Quantity = movement.Quantity,
            UnitSymbol = movementUnit.Symbol, BaseQuantity = effectiveQuantity,
            BaseUnitSymbol = article.BaseUnit!.Symbol,
            UnitCost = movement.UnitCost,
            TotalCost = movement.TotalCost,
            Reference = movement.Reference, Notes = movement.Notes,
            MovedAt = movement.CreatedAt,
        };
    }
}

public class RegisterInitialInventoryHandler : IRequestHandler<RegisterInitialInventoryCommand, List<StockMovementDto>>
{
    private readonly IMediator _mediator;
    private readonly GrimorioDbContext _db;
    public RegisterInitialInventoryHandler(IMediator mediator, GrimorioDbContext db)
    {
        _mediator = mediator;
        _db = db;
    }

    public async Task<List<StockMovementDto>> Handle(RegisterInitialInventoryCommand req, CancellationToken ct)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var resultados = new List<StockMovementDto>();
        try
        {
            foreach (var item in req.Items)
            {
                var resultado = await _mediator.Send(new RegisterMovementCommand
                {
                    BranchId = req.BranchId, ArticleId = item.ArticleId, WarehouseId = item.WarehouseId,
                    Type = MovementType.InitialInventory, Quantity = item.Quantity,
                    UnitId = item.UnitId, UnitCost = item.UnitCost, Notes = item.Notes,
                }, ct);
                resultados.Add(resultado);
            }

            await transaction.CommitAsync(ct);
            return resultados;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}

public class UpsertProductionRecipeHandler : IRequestHandler<UpsertProductionRecipeCommand, ProductionRecipeDto>
{
    private readonly GrimorioDbContext _db;
    public UpsertProductionRecipeHandler(GrimorioDbContext db) => _db = db;

    public async Task<ProductionRecipeDto> Handle(UpsertProductionRecipeCommand req, CancellationToken ct)
    {
        if (req.OutputQuantity <= 0)
            throw new InvalidOperationException("La cantidad resultante debe ser mayor a cero.");
        if (req.Ingredients.Count == 0)
            throw new InvalidOperationException("La receta de producción debe tener al menos un insumo.");

        var outputArticle = await _db.InventoryArticles
            .Include(x => x.BaseUnit)
            .FirstOrDefaultAsync(x => x.Id == req.OutputArticleId && x.BranchId == req.BranchId && x.IsActive, ct)
            ?? throw new InvalidOperationException("Artículo producido no encontrado.");

        if (outputArticle.Type != ArticleType.FinishedProduct)
            throw new InvalidOperationException("El artículo producido debe ser de tipo FinishedProduct.");

        await InventoryProductionHelper.ToBaseQuantity(_db, req.BranchId, outputArticle, req.OutputQuantity, req.OutputUnitId, ct);

        var ingredientArticleIds = req.Ingredients.Select(x => x.ArticleId).Distinct().ToList();
        if (ingredientArticleIds.Contains(req.OutputArticleId))
            throw new InvalidOperationException("El producto resultante no puede ser insumo de su propia receta.");

        var ingredientArticles = await _db.InventoryArticles
            .Include(x => x.BaseUnit)
            .Where(x => x.BranchId == req.BranchId && ingredientArticleIds.Contains(x.Id) && x.IsActive)
            .ToDictionaryAsync(x => x.Id, ct);

        if (ingredientArticles.Count != ingredientArticleIds.Count)
            throw new InvalidOperationException("Uno o más insumos no existen o están inactivos.");

        foreach (var ingredient in req.Ingredients)
        {
            if (ingredient.Quantity <= 0)
                throw new InvalidOperationException("La cantidad de cada insumo debe ser mayor a cero.");
            await InventoryProductionHelper.ToBaseQuantity(
                _db, req.BranchId, ingredientArticles[ingredient.ArticleId], ingredient.Quantity, ingredient.UnitId, ct);
        }

        var recipe = await _db.ProductionRecipes
            .Include(x => x.Ingredients)
            .FirstOrDefaultAsync(x => x.BranchId == req.BranchId && x.OutputArticleId == req.OutputArticleId, ct);

        if (recipe is null)
        {
            recipe = new ProductionRecipe
            {
                Id = Guid.NewGuid(),
                BranchId = req.BranchId,
                OutputArticleId = req.OutputArticleId,
            };
            _db.ProductionRecipes.Add(recipe);
        }

        recipe.OutputQuantity = req.OutputQuantity;
        recipe.OutputUnitId = req.OutputUnitId;
        recipe.Notes = req.Notes?.Trim();
        recipe.IsActive = req.IsActive;

        foreach (var oldIngredient in recipe.Ingredients.Where(x => !x.IsDeleted))
            oldIngredient.IsDeleted = true;

        foreach (var ingredient in req.Ingredients)
        {
            _db.ProductionRecipeIngredients.Add(new ProductionRecipeIngredient
            {
                Id = Guid.NewGuid(),
                BranchId = req.BranchId,
                ProductionRecipeId = recipe.Id,
                ArticleId = ingredient.ArticleId,
                Quantity = ingredient.Quantity,
                UnitId = ingredient.UnitId,
                Notes = ingredient.Notes?.Trim(),
            });
        }

        await _db.SaveChangesAsync(ct);

        var saved = await _db.ProductionRecipes
            .Include(x => x.OutputArticle).ThenInclude(x => x!.BaseUnit)
            .Include(x => x.OutputUnit)
            .Include(x => x.Ingredients.Where(i => !i.IsDeleted)).ThenInclude(x => x.Article).ThenInclude(x => x!.BaseUnit)
            .Include(x => x.Ingredients.Where(i => !i.IsDeleted)).ThenInclude(x => x.Unit)
            .FirstAsync(x => x.Id == recipe.Id, ct);

        return InventoryProductionMapper.MapRecipe(saved);
    }
}

public class RegisterProductionHandler : IRequestHandler<RegisterProductionCommand, ProductionOrderDto>
{
    private readonly GrimorioDbContext _db;

    public RegisterProductionHandler(GrimorioDbContext db) => _db = db;

    public async Task<ProductionOrderDto> Handle(RegisterProductionCommand req, CancellationToken ct)
    {
        if (req.OutputQuantity <= 0)
            throw new InvalidOperationException("La cantidad a producir debe ser mayor a cero.");

        var recipe = await _db.ProductionRecipes
            .Include(x => x.OutputArticle).ThenInclude(x => x!.BaseUnit)
            .Include(x => x.OutputUnit)
            .Include(x => x.Ingredients.Where(i => !i.IsDeleted)).ThenInclude(x => x.Article).ThenInclude(x => x!.BaseUnit)
            .Include(x => x.Ingredients.Where(i => !i.IsDeleted)).ThenInclude(x => x.Unit)
            .FirstOrDefaultAsync(x => x.Id == req.ProductionRecipeId && x.BranchId == req.BranchId && x.IsActive, ct)
            ?? throw new InvalidOperationException("Receta de producción no encontrada o inactiva.");

        if (recipe.Ingredients.Count == 0)
            throw new InvalidOperationException("La receta de producción no tiene insumos.");

        var sourceWarehouse = await _db.Warehouses
            .FirstOrDefaultAsync(x => x.Id == req.SourceWarehouseId && x.BranchId == req.BranchId && x.IsActive, ct)
            ?? throw new InvalidOperationException("Bodega de origen no encontrada.");
        var destinationWarehouse = await _db.Warehouses
            .FirstOrDefaultAsync(x => x.Id == req.DestinationWarehouseId && x.BranchId == req.BranchId && x.IsActive, ct)
            ?? throw new InvalidOperationException("Bodega de destino no encontrada.");

        var outputBaseQuantity = await InventoryProductionHelper.ToBaseQuantity(
            _db, req.BranchId, recipe.OutputArticle!, req.OutputQuantity, req.OutputUnitId, ct);
        var recipeOutputBaseQuantity = await InventoryProductionHelper.ToBaseQuantity(
            _db, req.BranchId, recipe.OutputArticle!, recipe.OutputQuantity, recipe.OutputUnitId, ct);
        var factor = outputBaseQuantity / recipeOutputBaseQuantity;

        var ingredientSnapshots = new List<ProductionOrderIngredient>();
        foreach (var ingredient in recipe.Ingredients)
        {
            var requiredQuantity = ingredient.Quantity * factor;
            var requiredBaseQuantity = await InventoryProductionHelper.ToBaseQuantity(
                _db, req.BranchId, ingredient.Article!, requiredQuantity, ingredient.UnitId, ct);
            var available = await InventoryProductionHelper.GetAvailableQuantity(
                _db, req.BranchId, ingredient.ArticleId, req.SourceWarehouseId, ct);

            if (available < requiredBaseQuantity)
                throw new InvalidOperationException(
                    $"Stock insuficiente para {ingredient.Article!.Name}. Disponible: {available} {ingredient.Article.BaseUnit!.Symbol}.");

            var unitCost = await InventoryProductionHelper.GetAverageUnitCost(
                _db, req.BranchId, ingredient.ArticleId, ct);
            ingredientSnapshots.Add(new ProductionOrderIngredient
            {
                Id = Guid.NewGuid(),
                BranchId = req.BranchId,
                ArticleId = ingredient.ArticleId,
                Quantity = requiredQuantity,
                UnitId = ingredient.UnitId,
                BaseQuantity = requiredBaseQuantity,
                UnitCost = unitCost,
                TotalCost = Math.Round(requiredBaseQuantity * unitCost, 4),
            });
        }

        var totalCost = ingredientSnapshots.Sum(x => x.TotalCost);
        var outputUnitCost = outputBaseQuantity == 0 ? 0 : Math.Round(totalCost / outputBaseQuantity, 4);
        var productionNumber = await InventoryProductionHelper.NextProductionNumber(_db, req.BranchId, ct);
        var reference = $"Producción {productionNumber}";

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var order = new ProductionOrder
        {
            Id = Guid.NewGuid(),
            BranchId = req.BranchId,
            Number = productionNumber,
            ProductionRecipeId = recipe.Id,
            OutputArticleId = recipe.OutputArticleId,
            SourceWarehouseId = req.SourceWarehouseId,
            DestinationWarehouseId = req.DestinationWarehouseId,
            OutputQuantity = req.OutputQuantity,
            OutputUnitId = req.OutputUnitId,
            OutputBaseQuantity = outputBaseQuantity,
            TotalCost = totalCost,
            UnitCost = outputUnitCost,
            Status = ProductionOrderStatus.Completed,
            Notes = req.Notes?.Trim(),
        };
        _db.ProductionOrders.Add(order);

        foreach (var item in ingredientSnapshots)
        {
            item.ProductionOrderId = order.Id;
            _db.ProductionOrderIngredients.Add(item);
        }

        await _db.SaveChangesAsync(ct);

        foreach (var item in ingredientSnapshots)
        {
            var movement = await InventoryProductionHelper.RegisterProductionMovement(
                _db,
                req.BranchId,
                item.ArticleId,
                req.SourceWarehouseId,
                MovementType.ProductionInput,
                item.Quantity,
                item.UnitId,
                item.UnitCost,
                reference,
                req.Notes,
                ct);

            _db.ProductionOrderMovements.Add(new ProductionOrderMovement
            {
                Id = Guid.NewGuid(),
                BranchId = req.BranchId,
                ProductionOrderId = order.Id,
                StockMovementId = movement.Id,
            });
        }

        var outputMovement = await InventoryProductionHelper.RegisterProductionMovement(
            _db,
            req.BranchId,
            recipe.OutputArticleId,
            req.DestinationWarehouseId,
            MovementType.ProductionOutput,
            req.OutputQuantity,
            req.OutputUnitId,
            outputUnitCost,
            reference,
            req.Notes,
            ct);

        _db.ProductionOrderMovements.Add(new ProductionOrderMovement
        {
            Id = Guid.NewGuid(),
            BranchId = req.BranchId,
            ProductionOrderId = order.Id,
            StockMovementId = outputMovement.Id,
        });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        var saved = await _db.ProductionOrders
            .Include(x => x.OutputArticle).ThenInclude(x => x!.BaseUnit)
            .Include(x => x.OutputUnit)
            .Include(x => x.SourceWarehouse)
            .Include(x => x.DestinationWarehouse)
            .Include(x => x.Ingredients.Where(i => !i.IsDeleted)).ThenInclude(x => x.Article).ThenInclude(x => x!.BaseUnit)
            .Include(x => x.Ingredients.Where(i => !i.IsDeleted)).ThenInclude(x => x.Unit)
            .FirstAsync(x => x.Id == order.Id, ct);

        return InventoryProductionMapper.MapOrder(saved);
    }
}

internal static class InventoryProductionHelper
{
    internal static async Task<decimal> ToBaseQuantity(
        GrimorioDbContext db,
        Guid branchId,
        InventoryArticle article,
        decimal quantity,
        Guid unitId,
        CancellationToken ct)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("La cantidad debe ser mayor a cero.");

        if (unitId == article.BaseUnitId) return quantity;

        var conversion = await db.UnitConversions.FirstOrDefaultAsync(
            x => x.BranchId == branchId && x.OriginUnitId == unitId && x.DestinationUnitId == article.BaseUnitId, ct);

        if (conversion != null) return quantity * conversion.Factor;

        var reverseConversion = await db.UnitConversions.FirstOrDefaultAsync(
            x => x.BranchId == branchId && x.OriginUnitId == article.BaseUnitId && x.DestinationUnitId == unitId, ct);

        if (reverseConversion is null)
            throw new InvalidOperationException($"No existe conversión entre la unidad seleccionada y {article.BaseUnit!.Name}.");

        return quantity / reverseConversion.Factor;
    }

    internal static async Task<decimal> GetAvailableQuantity(
        GrimorioDbContext db,
        Guid branchId,
        Guid articleId,
        Guid warehouseId,
        CancellationToken ct)
    {
        var stock = await db.StockMovements
            .Where(x => x.BranchId == branchId && x.ArticleId == articleId && x.WarehouseId == warehouseId)
            .SumAsync(x => x.BaseQuantity, ct);

        var reserved = await db.StockReservations
            .Where(x => x.BranchId == branchId
                && x.ArticleId == articleId
                && x.WarehouseId == warehouseId
                && x.Status == StockReservationStatus.Active)
            .SumAsync(x => x.BaseQuantity, ct);

        return stock - reserved;
    }

    internal static async Task<decimal> GetAverageUnitCost(
        GrimorioDbContext db,
        Guid branchId,
        Guid articleId,
        CancellationToken ct)
    {
        var costBase = await db.StockMovements
            .Where(x => x.BranchId == branchId
                && x.ArticleId == articleId
                && x.BaseQuantity > 0
                && x.TotalCost.HasValue
                && x.TotalCost.Value > 0)
            .GroupBy(x => x.ArticleId)
            .Select(g => new
            {
                Quantity = g.Sum(x => x.BaseQuantity),
                Cost = g.Sum(x => x.TotalCost!.Value),
            })
            .FirstOrDefaultAsync(ct);

        if (costBase is null || costBase.Quantity <= 0) return 0;
        return Math.Round(costBase.Cost / costBase.Quantity, 4);
    }

    internal static async Task<string> NextProductionNumber(GrimorioDbContext db, Guid branchId, CancellationToken ct)
    {
        var next = await db.ProductionOrders
            .Where(x => x.BranchId == branchId)
            .CountAsync(ct) + 1;
        return $"PROD-{DateTime.UtcNow:yyyyMMdd}-{next:0000}";
    }

    internal static async Task<StockMovement> RegisterProductionMovement(
        GrimorioDbContext db,
        Guid branchId,
        Guid articleId,
        Guid warehouseId,
        MovementType type,
        decimal quantity,
        Guid unitId,
        decimal unitCost,
        string reference,
        string? notes,
        CancellationToken ct)
    {
        var article = await db.InventoryArticles
            .Include(x => x.BaseUnit)
            .FirstOrDefaultAsync(x => x.Id == articleId && x.BranchId == branchId && x.IsActive, ct)
            ?? throw new InvalidOperationException("Artículo no encontrado.");

        var warehouseExists = await db.Warehouses
            .FirstOrDefaultAsync(x => x.Id == warehouseId && x.BranchId == branchId && x.IsActive, ct)
            ?? throw new InvalidOperationException("Bodega no encontrada.");

        var unitExists = await db.MeasurementUnits
            .FirstOrDefaultAsync(x => x.Id == unitId && x.BranchId == branchId, ct)
            ?? throw new InvalidOperationException("Unidad de medida no encontrada.");

        var baseQuantity = await ToBaseQuantity(db, branchId, article, quantity, unitId, ct);
        var isExit = type == MovementType.ProductionInput;
        var effectiveQuantity = isExit ? -Math.Abs(baseQuantity) : Math.Abs(baseQuantity);
        var totalCost = Math.Round(Math.Abs(effectiveQuantity) * unitCost, 4);

        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            BranchId = branchId,
            ArticleId = articleId,
            WarehouseId = warehouseId,
            Type = type,
            Quantity = quantity,
            UnitId = unitId,
            BaseQuantity = effectiveQuantity,
            UnitCost = unitCost,
            TotalCost = totalCost,
            Reference = reference,
            Notes = notes?.Trim(),
        };
        db.StockMovements.Add(movement);

        var stock = await db.WarehouseStock.FirstOrDefaultAsync(
            x => x.BranchId == branchId && x.ArticleId == articleId && x.WarehouseId == warehouseId, ct);

        if (stock is null)
        {
            var currentQuantity = await db.StockMovements
                .Where(x => x.BranchId == branchId && x.ArticleId == articleId && x.WarehouseId == warehouseId)
                .SumAsync(x => x.BaseQuantity, ct);

            stock = new WarehouseStock
            {
                Id = Guid.NewGuid(),
                BranchId = branchId,
                ArticleId = articleId,
                WarehouseId = warehouseId,
                Quantity = currentQuantity + effectiveQuantity,
                LastUpdatedAt = DateTime.UtcNow,
            };
            db.WarehouseStock.Add(stock);
        }
        else
        {
            stock.Quantity += effectiveQuantity;
            stock.LastUpdatedAt = DateTime.UtcNow;
        }

        return movement;
    }
}
