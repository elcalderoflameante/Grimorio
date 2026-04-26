using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Purchases.Commands;
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

        var totales = await _db.PurchaseOrders
            .Where(o => o.BranchId == req.BranchId && !o.IsDeleted)
            .GroupBy(o => o.SupplierId)
            .Select(g => new { SupplierId = g.Key, Total = g.Count() })
            .ToListAsync(ct);

        var totalesDict = totales.ToDictionary(t => t.SupplierId, t => t.Total);

        return list.Select(p => PurchasesMapper.MapSupplier(p, totalesDict.GetValueOrDefault(p.Id))).ToList();
    }
}

public class GetPurchaseOrdersHandler : IRequestHandler<GetPurchaseOrdersQuery, List<PurchaseOrderDto>>
{
    private readonly GrimorioDbContext _db;
    public GetPurchaseOrdersHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<PurchaseOrderDto>> Handle(GetPurchaseOrdersQuery req, CancellationToken ct)
    {
        var query = _db.PurchaseOrders
            .Include(o => o.Supplier)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Article)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Unit)
            .Where(o => o.BranchId == req.BranchId && !o.IsDeleted);

        if (!string.IsNullOrWhiteSpace(req.Status) &&
            Enum.TryParse<Domain.Entities.Purchases.PurchaseOrderStatus>(req.Status, out var status))
            query = query.Where(o => o.Status == status);

        if (req.SupplierId.HasValue)
            query = query.Where(o => o.SupplierId == req.SupplierId.Value);

        var orders = await query.OrderByDescending(o => o.IssuedAt).ToListAsync(ct);

        var warehouseIds = orders.Where(o => o.DestinationWarehouseId.HasValue).Select(o => o.DestinationWarehouseId!.Value).Distinct().ToList();
        var warehouses = await _db.Warehouses.Where(b => warehouseIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, b => b.Name, ct);

        return orders.Select(o => PurchasesMapper.MapOrder(o, o.DestinationWarehouseId.HasValue ? warehouses.GetValueOrDefault(o.DestinationWarehouseId.Value) : null)).ToList();
    }
}

public class GetPurchaseOrderDetailHandler : IRequestHandler<GetPurchaseOrderDetailQuery, PurchaseOrderDto?>
{
    private readonly GrimorioDbContext _db;
    public GetPurchaseOrderDetailHandler(GrimorioDbContext db) => _db = db;

    public async Task<PurchaseOrderDto?> Handle(GetPurchaseOrderDetailQuery req, CancellationToken ct)
    {
        var order = await _db.PurchaseOrders
            .Include(o => o.Supplier)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Article)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Unit)
            .FirstOrDefaultAsync(o => o.Id == req.Id && o.BranchId == req.BranchId && !o.IsDeleted, ct);

        if (order == null) return null;

        var warehouse = order.DestinationWarehouseId.HasValue
            ? await _db.Warehouses.FindAsync([order.DestinationWarehouseId.Value], ct)
            : null;

        return PurchasesMapper.MapOrder(order, warehouse?.Name);
    }
}
