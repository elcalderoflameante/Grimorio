using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Purchases.Queries;
using Grimorio.Infrastructure.Features.Purchases.Commands;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Purchases.Queries;

public class GetSuppliersHandler : IRequestHandler<GetSuppliersQuery, List<SupplierDto>>
{
    private readonly GrimorioDbContext _db;
    public GetSuppliersHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<SupplierDto>> Handle(GetSuppliersQuery req, CancellationToken ct)
    {
        var query = _db.Suppliers.Where(p => p.BranchId == req.BranchId && !p.IsDeleted);
        if (req.ActiveOnly == true) query = query.Where(p => p.IsActive);

        var list = await query.OrderBy(p => p.Name).ToListAsync(ct);

        var totales = await _db.Purchases
            .Where(p => p.BranchId == req.BranchId && !p.IsDeleted && p.SupplierId != null)
            .GroupBy(p => p.SupplierId!.Value)
            .Select(g => new { SupplierId = g.Key, Total = g.Count() })
            .ToListAsync(ct);

        var totalesDict = totales.ToDictionary(t => t.SupplierId, t => t.Total);

        return list.Select(p => PurchasesMapper.MapSupplier(p, totalesDict.GetValueOrDefault(p.Id))).ToList();
    }
}

public class GetPurchasesHandler : IRequestHandler<GetPurchasesQuery, List<PurchaseDto>>
{
    private readonly GrimorioDbContext _db;
    public GetPurchasesHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<PurchaseDto>> Handle(GetPurchasesQuery req, CancellationToken ct)
    {
        var query = _db.Purchases
            .Include(p => p.Supplier)
            .Include(p => p.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Article)
            .Include(p => p.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Unit)
            .Where(p => p.BranchId == req.BranchId && !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(req.Status) &&
            Enum.TryParse<Domain.Entities.Purchases.PurchaseStatus>(req.Status, out var status))
            query = query.Where(p => p.Status == status);

        if (req.SupplierId.HasValue)
            query = query.Where(p => p.SupplierId == req.SupplierId.Value);

        if (req.DateFrom.HasValue)
            query = query.Where(p => p.DocumentDate >= req.DateFrom.Value);

        if (req.DateTo.HasValue)
            query = query.Where(p => p.DocumentDate <= req.DateTo.Value);

        var purchases = await query.OrderByDescending(p => p.DocumentDate).ToListAsync(ct);

        var warehouseIds = purchases
            .Where(p => p.DestinationWarehouseId.HasValue)
            .Select(p => p.DestinationWarehouseId!.Value)
            .Distinct().ToList();

        var warehouses = await _db.Warehouses
            .Where(w => warehouseIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, w => w.Name, ct);

        return purchases.Select(p =>
            PurchasesMapper.MapPurchase(p,
                p.DestinationWarehouseId.HasValue ? warehouses.GetValueOrDefault(p.DestinationWarehouseId.Value) : null)
        ).ToList();
    }
}

public class GetPurchaseDetailHandler : IRequestHandler<GetPurchaseDetailQuery, PurchaseDto?>
{
    private readonly GrimorioDbContext _db;
    public GetPurchaseDetailHandler(GrimorioDbContext db) => _db = db;

    public async Task<PurchaseDto?> Handle(GetPurchaseDetailQuery req, CancellationToken ct)
    {
        var purchase = await _db.Purchases
            .Include(p => p.Supplier)
            .Include(p => p.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Article)
            .Include(p => p.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Unit)
            .Include(p => p.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.TaxRate)
            .FirstOrDefaultAsync(p => p.Id == req.Id && p.BranchId == req.BranchId && !p.IsDeleted, ct);

        if (purchase == null) return null;

        var warehouse = purchase.DestinationWarehouseId.HasValue
            ? await _db.Warehouses.FindAsync([purchase.DestinationWarehouseId.Value], ct)
            : null;

        return PurchasesMapper.MapPurchase(purchase, warehouse?.Name);
    }
}
