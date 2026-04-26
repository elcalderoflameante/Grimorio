using Grimorio.Application.DTOs;
using Grimorio.Application.Features.POS.Queries;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Features.POS.Commands;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.POS.Queries;

public class GetWorkStationsQueryHandler : IRequestHandler<GetWorkStationsQuery, List<WorkStationDto>>
{
    private readonly GrimorioDbContext _db;
    public GetWorkStationsQueryHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<WorkStationDto>> Handle(GetWorkStationsQuery req, CancellationToken ct)
    {
        return await _db.WorkStations
            .Where(e => e.BranchId == req.BranchId && !e.IsDeleted)
            .OrderBy(e => e.Type).ThenBy(e => e.Name)
            .Select(e => new WorkStationDto
            {
                Id = e.Id,
                Name = e.Name,
                Type = e.Type.ToString(),
                IsActive = e.IsActive,
            })
            .ToListAsync(ct);
    }
}

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, List<OrderDto>>
{
    private readonly GrimorioDbContext _db;
    public GetOrdersQueryHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<OrderDto>> Handle(GetOrdersQuery req, CancellationToken ct)
    {
        var query = _db.Orders
            .Where(o => o.BranchId == req.BranchId && !o.IsDeleted)
            .Include(o => o.Table)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.MenuItem)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Station)
            .AsQueryable();

        if (req.ActiveOnly)
            query = query.Where(o =>
                o.Status != OrderStatus.Delivered &&
                o.Status != OrderStatus.Cancelled);

        if (!string.IsNullOrEmpty(req.Status) && Enum.TryParse<OrderStatus>(req.Status, out var orderStatus))
            query = query.Where(o => o.Status == orderStatus);

        if (!string.IsNullOrEmpty(req.Type) && Enum.TryParse<OrderType>(req.Type, out var orderType))
            query = query.Where(o => o.Type == orderType);

        if (req.TableId.HasValue)
            query = query.Where(o => o.TableId == req.TableId);

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync(ct);
        return orders.Select(PosMapper.MapOrder).ToList();
    }
}

public class GetOrderDetailQueryHandler : IRequestHandler<GetOrderDetailQuery, OrderDto?>
{
    private readonly GrimorioDbContext _db;
    public GetOrderDetailQueryHandler(GrimorioDbContext db) => _db = db;

    public async Task<OrderDto?> Handle(GetOrderDetailQuery req, CancellationToken ct)
    {
        var order = await _db.Orders
            .Where(o => o.Id == req.OrderId && o.BranchId == req.BranchId && !o.IsDeleted)
            .Include(o => o.Table)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.MenuItem)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Station)
            .FirstOrDefaultAsync(ct);

        return order == null ? null : PosMapper.MapOrder(order);
    }
}

public class GetItemsByStationQueryHandler : IRequestHandler<GetItemsByStationQuery, List<StationItemDto>>
{
    private readonly GrimorioDbContext _db;
    public GetItemsByStationQueryHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<StationItemDto>> Handle(GetItemsByStationQuery req, CancellationToken ct)
    {
        return await _db.OrderItems
            .Where(i =>
                i.StationId == req.StationId &&
                i.BranchId == req.BranchId &&
                !i.IsDeleted &&
                (i.Status == OrderItemStatus.Pending || i.Status == OrderItemStatus.InPreparation))
            .Include(i => i.MenuItem)
            .Include(i => i.Order).ThenInclude(o => o!.Table)
            .Where(i => i.Order != null &&
                i.Order.Status != OrderStatus.Cancelled &&
                i.Order.Status != OrderStatus.Delivered &&
                i.Order.Status != OrderStatus.Draft)
            .OrderBy(i => i.Order!.ConfirmedAt)
            .Select(i => new StationItemDto
            {
                OrderItemId = i.Id,
                OrderId = i.OrderId,
                OrderNumber = i.Order!.Number,
                OrderType = i.Order.Type.ToString(),
                TableCode = i.Order.Table != null ? i.Order.Table.Code : null,
                CustomerName = i.Order.CustomerName,
                ItemName = i.MenuItem!.Name,
                Quantity = i.Quantity,
                Notes = i.Notes,
                Status = i.Status.ToString(),
                ConfirmedAt = i.Order.ConfirmedAt ?? i.Order.CreatedAt,
            })
            .ToListAsync(ct);
    }
}

