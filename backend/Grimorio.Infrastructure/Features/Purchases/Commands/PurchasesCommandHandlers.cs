using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Purchases.Commands;
using Grimorio.Application.Features.Inventory.Commands;
using Grimorio.Domain.Entities.Purchases;
using Grimorio.Domain.Entities.Inventory;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Purchases.Commands;

// ── Suppliers ──────────────────────────────────────────────────────────────────

public class CreateSupplierHandler : IRequestHandler<CreateSupplierCommand, SupplierDto>
{
    private readonly GrimorioDbContext _db;
    public CreateSupplierHandler(GrimorioDbContext db) => _db = db;

    public async Task<SupplierDto> Handle(CreateSupplierCommand req, CancellationToken ct)
    {
        var entity = new Supplier
        {
            Id = Guid.NewGuid(), BranchId = req.BranchId,
            Name = req.Name.Trim(), TaxId = req.TaxId?.Trim(),
            Phone = req.Phone?.Trim(), Email = req.Email?.Trim(),
            Address = req.Address?.Trim(), ContactName = req.ContactName?.Trim(),
        };
        _db.Suppliers.Add(entity);
        await _db.SaveChangesAsync(ct);
        return PurchasesMapper.MapSupplier(entity, 0);
    }
}

public class UpdateSupplierHandler : IRequestHandler<UpdateSupplierCommand, SupplierDto>
{
    private readonly GrimorioDbContext _db;
    public UpdateSupplierHandler(GrimorioDbContext db) => _db = db;

    public async Task<SupplierDto> Handle(UpdateSupplierCommand req, CancellationToken ct)
    {
        var entity = await _db.Suppliers
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Supplier no encontrado.");
        entity.Name = req.Name.Trim(); entity.TaxId = req.TaxId?.Trim();
        entity.Phone = req.Phone?.Trim(); entity.Email = req.Email?.Trim();
        entity.Address = req.Address?.Trim(); entity.ContactName = req.ContactName?.Trim();
        entity.IsActive = req.IsActive;
        await _db.SaveChangesAsync(ct);
        return PurchasesMapper.MapSupplier(entity, 0);
    }
}

public class DeleteSupplierHandler : IRequestHandler<DeleteSupplierCommand, bool>
{
    private readonly GrimorioDbContext _db;
    public DeleteSupplierHandler(GrimorioDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteSupplierCommand req, CancellationToken ct)
    {
        var entity = await _db.Suppliers
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Supplier no encontrado.");
        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

// ── Compras directas ──────────────────────────────────────────────────────────

public class CreatePurchaseHandler : IRequestHandler<CreatePurchaseCommand, PurchaseDto>
{
    private readonly GrimorioDbContext _db;
    private readonly IMediator _mediator;
    public CreatePurchaseHandler(GrimorioDbContext db, IMediator mediator) { _db = db; _mediator = mediator; }

    public async Task<PurchaseDto> Handle(CreatePurchaseCommand req, CancellationToken ct)
    {
        var purchase = new Purchase
        {
            Id = Guid.NewGuid(), BranchId = req.BranchId,
            DocumentType = (PurchaseDocumentType)req.DocumentType,
            DocumentNumber = req.DocumentNumber?.Trim(),
            DocumentDate = req.DocumentDate,
            SupplierId = req.SupplierId,
            Status = PurchaseStatus.Registrada,
            Notes = req.Notes?.Trim(),
            DestinationWarehouseId = req.DestinationWarehouseId,
        };

        var taxInfo = await PurchasesHelper.LoadTaxRateInfo(
            req.Items.Select(i => i.TaxRateId).OfType<Guid>(), _db, ct);

        var newItems = PurchasesHelper.BuildItems(purchase.Id, req.BranchId, req.Items, taxInfo);
        PurchasesHelper.ApplyTotalsWithInfo(purchase, newItems, req.Items, taxInfo);
        foreach (var item in newItems) purchase.Items.Add(item);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        _db.Purchases.Add(purchase);
        await _db.SaveChangesAsync(ct);

        if (req.DestinationWarehouseId.HasValue)
            await RegisterStockMovements(newItems, req.DestinationWarehouseId.Value, req.BranchId,
                MovementType.PurchaseEntry, DocRef(purchase), ct);

        await tx.CommitAsync(ct);
        return await LoadDto(purchase.Id, ct);
    }

    private async Task RegisterStockMovements(List<PurchaseItem> items, Guid warehouseId, Guid branchId,
        MovementType type, string reference, CancellationToken ct)
    {
        foreach (var item in items)
        {
            await _mediator.Send(new RegisterMovementCommand
            {
                BranchId = branchId, ArticleId = item.ArticleId,
                WarehouseId = warehouseId, Type = type,
                Quantity = item.Quantity, UnitId = item.UnitId,
                Reference = reference,
            }, ct);
        }
    }

    internal static string DocRef(Purchase p) =>
        $"Compra {p.DocumentNumber ?? p.Id.ToString("N")[..8]}";

    private async Task<PurchaseDto> LoadDto(Guid id, CancellationToken ct)
    {
        var p = await _db.Purchases
            .Include(x => x.Supplier)
            .Include(x => x.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Article)
            .Include(x => x.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Unit)
            .Include(x => x.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.TaxRate)
            .FirstAsync(x => x.Id == id, ct);

        var warehouse = p.DestinationWarehouseId.HasValue
            ? await _db.Warehouses.FindAsync([p.DestinationWarehouseId.Value], ct)
            : null;

        return PurchasesMapper.MapPurchase(p, warehouse?.Name);
    }
}

public class UpdatePurchaseHandler : IRequestHandler<UpdatePurchaseCommand, PurchaseDto>
{
    private readonly GrimorioDbContext _db;
    private readonly IMediator _mediator;
    public UpdatePurchaseHandler(GrimorioDbContext db, IMediator mediator) { _db = db; _mediator = mediator; }

    public async Task<PurchaseDto> Handle(UpdatePurchaseCommand req, CancellationToken ct)
    {
        var purchase = await _db.Purchases
            .Include(x => x.Items.Where(i => !i.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Compra no encontrada.");

        if (purchase.Status != PurchaseStatus.Registrada)
            throw new InvalidOperationException("Solo se pueden modificar compras con estado Registrada.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Reversar stock anterior
        if (purchase.DestinationWarehouseId.HasValue)
        {
            var oldRef = $"Corrección {CreatePurchaseHandler.DocRef(purchase)}";
            foreach (var item in purchase.Items)
            {
                await _mediator.Send(new RegisterMovementCommand
                {
                    BranchId = req.BranchId, ArticleId = item.ArticleId,
                    WarehouseId = purchase.DestinationWarehouseId.Value,
                    Type = MovementType.NegativeAdjustment,
                    Quantity = item.Quantity, UnitId = item.UnitId,
                    Reference = oldRef,
                }, ct);
            }
        }

        // Soft-delete ítems anteriores
        foreach (var item in purchase.Items) item.IsDeleted = true;

        // Actualizar cabecera
        purchase.DocumentType = (PurchaseDocumentType)req.DocumentType;
        purchase.DocumentNumber = req.DocumentNumber?.Trim();
        purchase.DocumentDate = req.DocumentDate;
        purchase.SupplierId = req.SupplierId;
        purchase.Notes = req.Notes?.Trim();
        purchase.DestinationWarehouseId = req.DestinationWarehouseId;

        // Crear nuevos ítems directamente en el DbSet para evitar conflictos de tracking
        var taxInfo = await PurchasesHelper.LoadTaxRateInfo(
            req.Items.Select(i => i.TaxRateId).OfType<Guid>(), _db, ct);

        var newItems = PurchasesHelper.BuildItems(purchase.Id, req.BranchId, req.Items, taxInfo);
        PurchasesHelper.ApplyTotalsWithInfo(purchase, newItems, req.Items, taxInfo);
        foreach (var item in newItems) _db.PurchaseItems.Add(item);

        await _db.SaveChangesAsync(ct);

        // Registrar nuevo stock
        if (req.DestinationWarehouseId.HasValue)
        {
            var newRef = CreatePurchaseHandler.DocRef(purchase);
            foreach (var item in newItems)
            {
                await _mediator.Send(new RegisterMovementCommand
                {
                    BranchId = req.BranchId, ArticleId = item.ArticleId,
                    WarehouseId = req.DestinationWarehouseId.Value,
                    Type = MovementType.PurchaseEntry,
                    Quantity = item.Quantity, UnitId = item.UnitId,
                    Reference = newRef,
                }, ct);
            }
        }

        await tx.CommitAsync(ct);

        var updated = await _db.Purchases
            .Include(x => x.Supplier)
            .Include(x => x.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Article)
            .Include(x => x.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Unit)
            .Include(x => x.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.TaxRate)
            .FirstAsync(x => x.Id == purchase.Id, ct);

        var warehouse = updated.DestinationWarehouseId.HasValue
            ? await _db.Warehouses.FindAsync([updated.DestinationWarehouseId.Value], ct)
            : null;

        return PurchasesMapper.MapPurchase(updated, warehouse?.Name);
    }
}

public class AnularPurchaseHandler : IRequestHandler<AnularPurchaseCommand, PurchaseDto>
{
    private readonly GrimorioDbContext _db;
    private readonly IMediator _mediator;
    public AnularPurchaseHandler(GrimorioDbContext db, IMediator mediator) { _db = db; _mediator = mediator; }

    public async Task<PurchaseDto> Handle(AnularPurchaseCommand req, CancellationToken ct)
    {
        var purchase = await _db.Purchases
            .Include(x => x.Supplier)
            .Include(x => x.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Article)
            .Include(x => x.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Unit)
            .Include(x => x.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.TaxRate)
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Compra no encontrada.");

        if (purchase.Status == PurchaseStatus.Anulada)
            throw new InvalidOperationException("La compra ya está anulada.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Reversar stock
        if (purchase.DestinationWarehouseId.HasValue)
        {
            var docRef = $"Anulación {CreatePurchaseHandler.DocRef(purchase)}";
            foreach (var item in purchase.Items.Where(i => !i.IsDeleted))
            {
                await _mediator.Send(new RegisterMovementCommand
                {
                    BranchId = req.BranchId, ArticleId = item.ArticleId,
                    WarehouseId = purchase.DestinationWarehouseId.Value,
                    Type = MovementType.NegativeAdjustment,
                    Quantity = item.Quantity, UnitId = item.UnitId,
                    Reference = docRef,
                }, ct);
            }
        }

        purchase.Status = PurchaseStatus.Anulada;
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        var warehouse = purchase.DestinationWarehouseId.HasValue
            ? await _db.Warehouses.FindAsync([purchase.DestinationWarehouseId.Value], ct)
            : null;

        return PurchasesMapper.MapPurchase(purchase, warehouse?.Name);
    }
}

public class DeletePurchaseHandler : IRequestHandler<DeletePurchaseCommand, bool>
{
    private readonly GrimorioDbContext _db;
    public DeletePurchaseHandler(GrimorioDbContext db) => _db = db;

    public async Task<bool> Handle(DeletePurchaseCommand req, CancellationToken ct)
    {
        var purchase = await _db.Purchases
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Compra no encontrada.");

        if (purchase.Status == PurchaseStatus.Registrada)
            throw new InvalidOperationException("Anule la compra antes de eliminarla.");

        purchase.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

// ── Helpers ───────────────────────────────────────────────────────────────────

internal static class PurchasesHelper
{
    internal record TaxInfo(decimal Percentage, string SriCode);

    internal record PurchaseTotals(
        decimal Subtotal, decimal DiscountTotal,
        decimal TaxableBase15, decimal TaxableBase0, decimal TaxableBaseExempt,
        decimal Iva15);

    internal static async Task<Dictionary<Guid, TaxInfo>> LoadTaxRateInfo(
        IEnumerable<Guid> taxRateIds, GrimorioDbContext db, CancellationToken ct)
    {
        var ids = taxRateIds.Distinct().ToList();
        if (ids.Count == 0) return [];
        return await db.TaxRates
            .Where(t => ids.Contains(t.Id) && !t.IsDeleted)
            .ToDictionaryAsync(t => t.Id, t => new TaxInfo(t.Percentage, t.SriCode), ct);
    }

    internal static List<PurchaseItem> BuildItems(
        Guid purchaseId, Guid branchId,
        List<PurchaseItemInputDto> items,
        Dictionary<Guid, TaxInfo> taxInfo)
    {
        var result = new List<PurchaseItem>();
        foreach (var item in items)
        {
            var gross = item.UnitPrice * item.Quantity;
            var discountAmt = Math.Round(gross * (item.DiscountPct / 100m), 2);
            var taxableBase = gross - discountAmt;
            var info = item.TaxRateId.HasValue ? taxInfo.GetValueOrDefault(item.TaxRateId.Value) : null;
            var taxAmt = info != null ? Math.Round(taxableBase * (info.Percentage / 100m), 2) : 0m;

            result.Add(new PurchaseItem
            {
                Id = Guid.NewGuid(), BranchId = branchId, PurchaseId = purchaseId,
                ArticleId = item.ArticleId, UnitId = item.UnitId,
                Quantity = item.Quantity, UnitPrice = item.UnitPrice,
                DiscountPct = item.DiscountPct, DiscountAmount = discountAmt,
                TaxRateId = item.TaxRateId, TaxAmount = taxAmt,
                TotalPrice = taxableBase + taxAmt, Notes = item.Notes?.Trim(),
            });
        }
        return result;
    }

    internal static void ApplyTotals(Purchase purchase, List<PurchaseItem> items)
    {
        decimal subtotal = 0, discountTotal = 0;
        decimal taxableBase15 = 0, taxableBase0 = 0, taxableBaseExempt = 0, iva15 = 0;

        // Para el desglose fiscal necesitamos el SriCode; lo derivamos de los ítems ya calculados
        // (TaxAmount > 0 && DiscountPct contribuye a la base; clasificamos por TaxRateId si lo tenemos)
        // Nota: la clasificación fiscal la hacemos pasando de vuelta por los inputs junto con la info de tarifa.
        // Como ya tenemos los ítems construidos, recalculamos directamente aquí.
        foreach (var item in items)
        {
            var gross = item.UnitPrice * item.Quantity;
            var taxableBase = gross - item.DiscountAmount;

            subtotal += gross;
            discountTotal += item.DiscountAmount;

            // Inferimos la categoría fiscal por el TaxAmount y el porcentaje
            if (item.TaxAmount == 0 && item.TaxRateId == null)
            {
                taxableBaseExempt += taxableBase;
            }
            else if (item.TaxAmount > 0 && taxableBase > 0)
            {
                // Si el porcentaje efectivo es ~15% → IVA 15%
                var effectivePct = Math.Round(item.TaxAmount / taxableBase * 100m, 1);
                if (effectivePct >= 14m)
                {
                    taxableBase15 += taxableBase;
                    iva15 += item.TaxAmount;
                }
                else
                {
                    taxableBase0 += taxableBase;
                }
            }
            else
            {
                taxableBase0 += taxableBase;
            }
        }

        purchase.Subtotal = subtotal;
        purchase.DiscountTotal = discountTotal;
        purchase.TaxableBase15 = taxableBase15;
        purchase.TaxableBase0 = taxableBase0;
        purchase.TaxableBaseExempt = taxableBaseExempt;
        purchase.Iva15 = iva15;
        purchase.Ice = 0;
        purchase.Total = taxableBase15 + taxableBase0 + taxableBaseExempt + iva15;
    }

    // Versión con TaxInfo disponible para clasificación fiscal precisa (usada en Create/Update)
    internal static void ApplyTotalsWithInfo(
        Purchase purchase, List<PurchaseItem> items,
        List<Application.DTOs.PurchaseItemInputDto> inputs,
        Dictionary<Guid, TaxInfo> taxInfo)
    {
        decimal subtotal = 0, discountTotal = 0;
        decimal taxableBase15 = 0, taxableBase0 = 0, taxableBaseExempt = 0, iva15 = 0;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var input = inputs[i];
            var gross = item.UnitPrice * item.Quantity;
            var taxableBase = gross - item.DiscountAmount;
            var info = input.TaxRateId.HasValue ? taxInfo.GetValueOrDefault(input.TaxRateId.Value) : null;

            subtotal += gross;
            discountTotal += item.DiscountAmount;

            if (info == null || info.SriCode is "5" or "6" or "7")
                taxableBaseExempt += taxableBase;
            else if (info.Percentage > 0) { taxableBase15 += taxableBase; iva15 += item.TaxAmount; }
            else taxableBase0 += taxableBase; // "0"
        }

        purchase.Subtotal = subtotal;
        purchase.DiscountTotal = discountTotal;
        purchase.TaxableBase15 = taxableBase15;
        purchase.TaxableBase0 = taxableBase0;
        purchase.TaxableBaseExempt = taxableBaseExempt;
        purchase.Iva15 = iva15;
        purchase.Ice = 0;
        purchase.Total = taxableBase15 + taxableBase0 + taxableBaseExempt + iva15;
    }
}

// ── Mapper ────────────────────────────────────────────────────────────────────

internal static class PurchasesMapper
{
    internal static SupplierDto MapSupplier(Supplier p, int totalPurchases) => new()
    {
        Id = p.Id, Name = p.Name, TaxId = p.TaxId,
        Phone = p.Phone, Email = p.Email, Address = p.Address,
        ContactName = p.ContactName, IsActive = p.IsActive,
        TotalPurchases = totalPurchases,
    };

    internal static PurchaseDto MapPurchase(Purchase p, string? warehouseName) => new()
    {
        Id = p.Id,
        DocumentType = p.DocumentType.ToString(),
        DocumentNumber = p.DocumentNumber,
        DocumentDate = p.DocumentDate,
        Status = p.Status.ToString(),
        SupplierId = p.SupplierId,
        SupplierName = p.Supplier?.Name,
        Notes = p.Notes,
        DestinationWarehouseId = p.DestinationWarehouseId,
        WarehouseName = warehouseName,
        Subtotal = p.Subtotal,
        DiscountTotal = p.DiscountTotal,
        TaxableBase15 = p.TaxableBase15,
        TaxableBase0 = p.TaxableBase0,
        TaxableBaseExempt = p.TaxableBaseExempt,
        Iva15 = p.Iva15,
        Ice = p.Ice,
        Total = p.Total,
        TotalItems = p.Items.Count(i => !i.IsDeleted),
        Items = p.Items.Where(i => !i.IsDeleted).Select(i => new PurchaseItemDto
        {
            Id = i.Id, ArticleId = i.ArticleId,
            ArticleName = i.Article?.Name ?? string.Empty,
            InternalCode = i.Article?.InternalCode,
            UnitId = i.UnitId, UnitSymbol = i.Unit?.Symbol ?? string.Empty,
            Quantity = i.Quantity, UnitPrice = i.UnitPrice,
            DiscountPct = i.DiscountPct, DiscountAmount = i.DiscountAmount,
            TaxRateId = i.TaxRateId, TaxRateName = i.TaxRate?.Name,
            TaxRatePercentage = i.TaxRate?.Percentage,
            TaxAmount = i.TaxAmount, TotalPrice = i.TotalPrice,
            Notes = i.Notes,
        }).ToList(),
    };
}
