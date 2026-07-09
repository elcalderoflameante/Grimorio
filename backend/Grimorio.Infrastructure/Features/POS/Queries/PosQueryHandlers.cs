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
            .AsNoTracking()
            .Where(o => o.BranchId == req.BranchId && !o.IsDeleted)
            .Include(o => o.Table)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.MenuItem)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Station)
            .Include(o => o.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.IngredientChoices.Where(c => !c.IsDeleted))
                    .ThenInclude(c => c.ChosenArticle)
            .AsQueryable();

        if (req.ActiveOnly)
            query = query.Where(o =>
                o.PaidAt == null &&
                o.Status != OrderStatus.Cancelled);

        if (!string.IsNullOrEmpty(req.Status) && Enum.TryParse<OrderStatus>(req.Status, out var orderStatus))
            query = query.Where(o => o.Status == orderStatus);

        if (!string.IsNullOrEmpty(req.Type) && Enum.TryParse<OrderType>(req.Type, out var orderType))
            query = query.Where(o => o.Type == orderType);

        if (req.TableId.HasValue)
            query = query.Where(o => o.TableId == req.TableId);

        var orders = await query.AsSplitQuery().OrderByDescending(o => o.CreatedAt).ToListAsync(ct);
        return orders.Select(PosMapper.MapOrder).ToList();
    }
}

public class GetActiveOrderSummariesQueryHandler : IRequestHandler<GetActiveOrderSummariesQuery, List<ActiveOrderSummaryDto>>
{
    private readonly GrimorioDbContext _db;
    public GetActiveOrderSummariesQueryHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<ActiveOrderSummaryDto>> Handle(GetActiveOrderSummariesQuery req, CancellationToken ct)
    {
        return await _db.Orders
            .AsNoTracking()
            .Where(o =>
                o.BranchId == req.BranchId &&
                !o.IsDeleted &&
                o.PaidAt == null &&
                o.Status != OrderStatus.Cancelled)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new ActiveOrderSummaryDto
            {
                Id = o.Id,
                Number = o.Number,
                Type = o.Type.ToString(),
                Status = o.Status.ToString(),
                TableCode = o.Table != null ? o.Table.Code : null,
                CustomerName = o.CustomerName,
                Total = o.Total,
                CreatedAt = o.CreatedAt,
                ConfirmedAt = o.ConfirmedAt,
                TotalItems = o.Items.Where(i => !i.IsDeleted).Sum(i => i.Quantity),
            })
            .ToListAsync(ct);
    }
}

public class GetOrderDetailQueryHandler : IRequestHandler<GetOrderDetailQuery, OrderDto?>
{
    private readonly GrimorioDbContext _db;
    public GetOrderDetailQueryHandler(GrimorioDbContext db) => _db = db;

    public async Task<OrderDto?> Handle(GetOrderDetailQuery req, CancellationToken ct)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Where(o => o.Id == req.OrderId && o.BranchId == req.BranchId && !o.IsDeleted)
            .Include(o => o.Table)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.MenuItem)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Station)
            .Include(o => o.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.IngredientChoices.Where(c => !c.IsDeleted))
                    .ThenInclude(c => c.ChosenArticle)
            .AsSplitQuery()
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
        // Include explícito necesario para cargar ChosenArticle.
        // No se usa .Select() porque EF Core ignora los .Include() cuando hay proyección SQL.
        var entities = await _db.OrderItems
            .AsNoTracking()
            .Where(i =>
                i.StationId == req.StationId &&
                i.BranchId == req.BranchId &&
                !i.IsDeleted &&
                (i.Status == OrderItemStatus.Pending || i.Status == OrderItemStatus.InPreparation))
            .Include(i => i.MenuItem)
            .Include(i => i.Order).ThenInclude(o => o!.Table)
            .Include(i => i.IngredientChoices.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.ChosenArticle)
            .Where(i => i.Order != null &&
                i.Order.Status != OrderStatus.Cancelled &&
                i.Order.Status != OrderStatus.Delivered &&
                i.Order.Status != OrderStatus.Draft)
            .OrderBy(i => i.Order!.ConfirmedAt)
            .ToListAsync(ct);

        // Mapeo en memoria: en este punto ChosenArticle ya está cargado
        return entities.Select(i => MapStationItem(i)).ToList();
    }

    internal static StationItemDto MapStationItem(OrderItem i) => new()
    {
        OrderItemId = i.Id,
        OrderId = i.OrderId,
        OrderNumber = i.Order!.Number,
        OrderType = i.Order.Type.ToString(),
        TableCode = i.Order.Table?.Code,
        CustomerName = i.Order.CustomerName,
        OrderNotes = i.Order.Notes,
        ItemName = i.MenuItem!.Name,
        Quantity = i.Quantity,
        Notes = i.Notes,
        Status = i.Status.ToString(),
        ConfirmedAt = i.Order.ConfirmedAt ?? i.Order.CreatedAt,
        UpdatedAt = i.UpdatedAt,
        IngredientChoices = i.IngredientChoices
            .Where(c => !c.IsDeleted)
            .Select(c => new IngredientChoiceDto
            {
                RecipeIngredientId = c.RecipeIngredientId,
                ChosenArticleId = c.ChosenArticleId,
                ChosenArticleName = c.ChosenArticle?.Name ?? string.Empty,
            }).ToList(),
    };
}

public class GetCompletedStationItemsQueryHandler
    : IRequestHandler<GetCompletedStationItemsQuery, List<StationItemDto>>
{
    private readonly GrimorioDbContext _db;
    public GetCompletedStationItemsQueryHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<StationItemDto>> Handle(GetCompletedStationItemsQuery req, CancellationToken ct)
    {
        // req.Date está en hora de Ecuador (UTC-5, sin DST).
        // Convertimos a UTC: medianoche Ecuador = 05:00 UTC del mismo día.
        // SpecifyKind(Utc) es requerido por Npgsql para columnas timestamptz.
        var dayStartUtc = DateTime.SpecifyKind(
            req.Date.ToDateTime(TimeOnly.MinValue).AddHours(5),
            DateTimeKind.Utc);
        var dayEnd = dayStartUtc.AddDays(1);
        var dayStart = dayStartUtc;

        var entities = await _db.OrderItems
            .AsNoTracking()
            .Where(i =>
                i.StationId == req.StationId &&
                i.BranchId == req.BranchId &&
                !i.IsDeleted &&
                i.Status == OrderItemStatus.Ready &&
                i.Order!.ConfirmedAt >= dayStart &&
                i.Order.ConfirmedAt < dayEnd)
            .Include(i => i.MenuItem)
            .Include(i => i.Order).ThenInclude(o => o!.Table)
            .Include(i => i.IngredientChoices.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.ChosenArticle)
            .Where(i => i.Order!.Status != OrderStatus.Cancelled)
            .OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt)
            .ToListAsync(ct);

        return entities.Select(GetItemsByStationQueryHandler.MapStationItem).ToList();
    }
}
