using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Inventory.Commands;
using Grimorio.Domain.Entities.Inventory;
using Grimorio.Infrastructure.Features.Inventory.Queries;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
            or MovementType.NegativeAdjustment;

        var effectiveQuantity = isExit ? -Math.Abs(baseQuantity) : Math.Abs(baseQuantity);

        // Actualizar o crear WarehouseStock
        var stock = await _db.WarehouseStock.FirstOrDefaultAsync(
            x => x.BranchId == req.BranchId && x.ArticleId == req.ArticleId && x.WarehouseId == req.WarehouseId, ct);

        if (stock is null)
        {
            stock = new WarehouseStock
            {
                Id = Guid.NewGuid(), BranchId = req.BranchId,
                ArticleId = req.ArticleId, WarehouseId = req.WarehouseId,
                Quantity = effectiveQuantity, LastUpdatedAt = DateTime.UtcNow,
            };
            _db.WarehouseStock.Add(stock);
        }
        else
        {
            stock.Quantity += effectiveQuantity;
            stock.LastUpdatedAt = DateTime.UtcNow;
        }

        var movement = new StockMovement
        {
            Id = Guid.NewGuid(), BranchId = req.BranchId,
            ArticleId = req.ArticleId, WarehouseId = req.WarehouseId,
            Type = req.Type, Quantity = req.Quantity, UnitId = req.UnitId,
            BaseQuantity = effectiveQuantity, Reference = req.Reference?.Trim(), Notes = req.Notes?.Trim(),
        };
        _db.StockMovements.Add(movement);
        await _db.SaveChangesAsync(ct);

        return new StockMovementDto
        {
            Id = movement.Id, ArticleId = article.Id, ArticleName = article.Name,
            WarehouseId = req.WarehouseId, WarehouseName = warehouse.Name,
            Type = movement.Type.ToString(), Quantity = movement.Quantity,
            UnitSymbol = movementUnit.Symbol, BaseQuantity = effectiveQuantity,
            BaseUnitSymbol = article.BaseUnit!.Symbol,
            Reference = movement.Reference, Notes = movement.Notes,
            MovedAt = movement.CreatedAt,
        };
    }
}

public class RegisterInitialInventoryHandler : IRequestHandler<RegisterInitialInventoryCommand, List<StockMovementDto>>
{
    private readonly IMediator _mediator;
    public RegisterInitialInventoryHandler(IMediator mediator) => _mediator = mediator;

    public async Task<List<StockMovementDto>> Handle(RegisterInitialInventoryCommand req, CancellationToken ct)
    {
        var resultados = new List<StockMovementDto>();
        foreach (var item in req.Items)
        {
            var resultado = await _mediator.Send(new RegisterMovementCommand
            {
                BranchId = req.BranchId, ArticleId = item.ArticleId, WarehouseId = item.WarehouseId,
                Type = MovementType.InitialInventory, Quantity = item.Quantity,
                UnitId = item.UnitId, Notes = item.Notes,
            }, ct);
            resultados.Add(resultado);
        }
        return resultados;
    }
}
