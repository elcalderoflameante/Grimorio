using Grimorio.Application.DTOs;
using Grimorio.Application.Features.TableService.Queries;
using Grimorio.Domain.Entities.Inventory;
using Grimorio.Domain.Entities.Menu;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Features.POS.Commands;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Grimorio.Infrastructure.Features.TableService.Queries;

public class GetRestaurantTablesQueryHandler : IRequestHandler<GetRestaurantTablesQuery, List<RestaurantTableDto>>
{
    private readonly GrimorioDbContext _context;

    public GetRestaurantTablesQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<List<RestaurantTableDto>> Handle(GetRestaurantTablesQuery request, CancellationToken cancellationToken)
    {
        var tables = await _context.RestaurantTables
            .Where(x => x.BranchId == request.BranchId && !x.IsDeleted)
            .Include(x => x.Orders.Where(o =>
                !o.IsDeleted &&
                o.Status != OrderStatus.Cancelled &&
                o.Status != OrderStatus.Delivered &&
                o.PaidAt == null))
                .ThenInclude(o => o.Payments.Where(p => !p.IsDeleted))
            .ToListAsync(cancellationToken);

        return tables
            .OrderBy(x => GetTableNumber(x.Code) ?? int.MaxValue)
            .ThenBy(x => x.Code, StringComparer.Create(CultureInfo.GetCultureInfo("es-EC"), ignoreCase: true))
            .ThenBy(x => x.Area ?? string.Empty, StringComparer.Create(CultureInfo.GetCultureInfo("es-EC"), ignoreCase: true))
            .Select(x =>
        {
            var activeOrder = x.Orders
                .OrderBy(o => o.Status == OrderStatus.Draft ? 0 : 1)
                .ThenByDescending(o => o.CreatedAt)
                .FirstOrDefault();
            var paidAmount = activeOrder?.Payments.Where(p => !p.IsDeleted).Sum(p => p.OrderAmount) ?? 0m;
            var pendingPayment = activeOrder == null ? 0m : Math.Max(0m, activeOrder.Total - paidAmount);
            return new RestaurantTableDto
            {
                Id = x.Id,
                BranchId = x.BranchId,
                Code = x.Code,
                Area = x.Area,
                Capacity = x.Capacity,
                PublicToken = x.PublicToken,
                IsActive = x.IsActive,
                PublicUrl = $"/mesa/{x.PublicToken}",
                CurrentStatus = activeOrder == null
                    ? "Free"
                    : activeOrder.Status == OrderStatus.Draft ? "Draft" : "Occupied",
                CurrentOrderId = activeOrder?.Id,
                CurrentOrderStartedAt = activeOrder?.ConfirmedAt ?? activeOrder?.CreatedAt,
                CurrentOrderTotal = activeOrder?.Total ?? 0m,
                PendingPaymentTotal = pendingPayment,
            };
        }).ToList();
    }

    private static int? GetTableNumber(string? code)
        => int.TryParse(code?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;
}

public class GetRestaurantTableByTokenQueryHandler : IRequestHandler<GetRestaurantTableByTokenQuery, PublicTableInfoDto?>
{
    private readonly GrimorioDbContext _context;

    public GetRestaurantTableByTokenQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<PublicTableInfoDto?> Handle(GetRestaurantTableByTokenQuery request, CancellationToken cancellationToken)
    {
        return await _context.RestaurantTables
            .Where(x => x.PublicToken == request.Token && !x.IsDeleted)
            .Select(x => new PublicTableInfoDto
            {
                TableId = x.Id,
                Code = x.Code,
                Area = x.Area,
                IsActive = x.IsActive,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public class GetPublicTableMenuQueryHandler : IRequestHandler<GetPublicTableMenuQuery, PublicTableMenuDto>
{
    private readonly GrimorioDbContext _context;

    public GetPublicTableMenuQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<PublicTableMenuDto> Handle(GetPublicTableMenuQuery request, CancellationToken cancellationToken)
    {
        var table = await _context.RestaurantTables
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PublicToken == request.TableToken && !x.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Mesa no válida.");

        if (!table.IsActive)
            throw new InvalidOperationException("La mesa no está habilitada.");

        var categories = await _context.MenuCategories
            .AsNoTracking()
            .Where(x => x.BranchId == table.BranchId && x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Name)
            .Select(x => new PublicMenuCategoryDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Color = x.Color,
                Order = x.Order,
            })
            .ToListAsync(cancellationToken);

        var items = await _context.MenuItems
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Recipe.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.Article)
                    .ThenInclude(a => a!.BaseUnit)
            .Include(x => x.Recipe.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.Unit)
            .Include(x => x.ModifierGroups.Where(g => !g.IsDeleted && g.IsActive))
                .ThenInclude(g => g.Options.Where(o => !o.IsDeleted && o.IsActive))
                    .ThenInclude(o => o.Article)
            .Include(x => x.ModifierGroups.Where(g => !g.IsDeleted && g.IsActive))
                .ThenInclude(g => g.Options.Where(o => !o.IsDeleted && o.IsActive))
                    .ThenInclude(o => o.Unit)
            .Where(x =>
                x.BranchId == table.BranchId &&
                x.IsActive &&
                x.AvailableForSale &&
                !x.IsDeleted &&
                x.Category != null &&
                x.Category.IsActive &&
                !x.Category.IsDeleted)
            .AsSplitQuery()
            .OrderBy(x => x.Category!.Order)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var availability = await PublicMenuAvailability.BuildAsync(_context, table.BranchId, items, cancellationToken);

        return new PublicTableMenuDto
        {
            Categories = categories
                .Where(category => items.Any(item => item.MenuCategoryId == category.Id))
                .ToList(),
            Items = items.Select(item => new PublicMenuItemDto
            {
                Id = item.Id,
                MenuCategoryId = item.MenuCategoryId,
                CategoryName = item.Category?.Name ?? string.Empty,
                CategoryColor = item.Category?.Color,
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                IsAvailable = availability.MenuItems.GetValueOrDefault(item.Id, true),
                HasModifiers = item.ModifierGroups.Any(g => !g.IsDeleted && g.IsActive),
                ModifierGroups = item.ModifierGroups
                    .Where(g => !g.IsDeleted && g.IsActive)
                    .OrderBy(g => g.DisplayOrder)
                    .ThenBy(g => g.Name)
                    .Select(group => new PublicMenuItemModifierGroupDto
                    {
                        Id = group.Id,
                        MenuItemId = group.MenuItemId,
                        Name = group.Name,
                        MinSelections = group.MinSelections,
                        MaxSelections = group.MaxSelections,
                        IsRequired = group.IsRequired,
                        AllowDuplicates = group.AllowDuplicates,
                        DisplayOrder = group.DisplayOrder,
                        Options = group.Options
                            .Where(o => !o.IsDeleted && o.IsActive)
                            .OrderBy(o => o.DisplayOrder)
                            .ThenBy(o => o.Name)
                            .Select(option => new PublicMenuItemModifierOptionDto
                            {
                                Id = option.Id,
                                ModifierGroupId = option.ModifierGroupId,
                                Name = option.Name,
                                PriceDelta = option.PriceDelta,
                                DisplayOrder = option.DisplayOrder,
                                IsAvailable = availability.ModifierOptions.GetValueOrDefault(option.Id, true),
                            })
                            .ToList(),
                    })
                    .ToList(),
            }).ToList(),
        };
    }
}

public class GetActivePublicTableOrderQueryHandler : IRequestHandler<GetActivePublicTableOrderQuery, OrderDto?>
{
    private readonly GrimorioDbContext _context;

    public GetActivePublicTableOrderQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<OrderDto?> Handle(GetActivePublicTableOrderQuery request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.Table != null &&
                x.Table.PublicToken == request.TableToken &&
                x.Table.IsActive &&
                !x.Table.IsDeleted &&
                x.Status != OrderStatus.Cancelled &&
                x.Status != OrderStatus.Delivered &&
                x.PaidAt == null)
            .Include(x => x.Table)
            .Include(x => x.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.MenuItem)
            .Include(x => x.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Station)
            .Include(x => x.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.TaxRate)
            .Include(x => x.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.ModifierSelections.Where(s => !s.IsDeleted))
            .AsSplitQuery()
            .OrderBy(x => x.Status == OrderStatus.Draft ? 0 : 1)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return order is null ? null : PosMapper.MapOrder(order);
    }
}

public class GetTableServiceRequestsQueryHandler : IRequestHandler<GetTableServiceRequestsQuery, List<TableServiceRequestDto>>
{
    private readonly GrimorioDbContext _context;

    public GetTableServiceRequestsQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<List<TableServiceRequestDto>> Handle(GetTableServiceRequestsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.TableServiceRequests
            .Where(x => x.BranchId == request.BranchId && !x.IsDeleted)
            .Include(x => x.RestaurantTable)
            .AsQueryable();

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }

        if (request.FromUtc.HasValue)
        {
            query = query.Where(x => x.RequestedAt >= request.FromUtc.Value);
        }

        if (request.ToUtc.HasValue)
        {
            query = query.Where(x => x.RequestedAt <= request.ToUtc.Value);
        }

        return await query
            .OrderByDescending(x => x.CompletedAt ?? x.RequestedAt)
            .Select(x => new TableServiceRequestDto
            {
                Id = x.Id,
                BranchId = x.BranchId,
                RestaurantTableId = x.RestaurantTableId,
                TableCode = x.RestaurantTable != null ? x.RestaurantTable.Code : string.Empty,
                TableArea = x.RestaurantTable != null ? x.RestaurantTable.Area : null,
                Type = x.Type,
                CustomMessage = x.CustomMessage,
                Status = x.Status,
                RequestedAt = x.RequestedAt,
                TakenAt = x.TakenAt,
                CompletedAt = x.CompletedAt,
                TakenByUserId = x.TakenByUserId,
                TakenByName = x.TakenByName,
            })
            .ToListAsync(cancellationToken);
    }
}

    public class GetPublicRequestStatusQueryHandler : IRequestHandler<GetPublicRequestStatusQuery, PublicRequestStatusDto?>
    {
        private readonly GrimorioDbContext _context;

        public GetPublicRequestStatusQueryHandler(GrimorioDbContext context) => _context = context;

        public async Task<PublicRequestStatusDto?> Handle(GetPublicRequestStatusQuery request, CancellationToken cancellationToken)
        {
            return await _context.TableServiceRequests
                .Where(x => x.Id == request.RequestId && !x.IsDeleted)
                .Select(x => new PublicRequestStatusDto
                {
                    Id = x.Id,
                    Status = x.Status,
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
    }

    public class GetActivePublicTableRequestQueryHandler : IRequestHandler<GetActivePublicTableRequestQuery, PublicActiveTableRequestDto?>
    {
        private readonly GrimorioDbContext _context;

        public GetActivePublicTableRequestQueryHandler(GrimorioDbContext context) => _context = context;

        public async Task<PublicActiveTableRequestDto?> Handle(GetActivePublicTableRequestQuery request, CancellationToken cancellationToken)
        {
            return await _context.TableServiceRequests
                .Where(x =>
                    !x.IsDeleted &&
                    x.RestaurantTable != null &&
                    x.RestaurantTable.PublicToken == request.TableToken &&
                    (x.Status == TableServiceRequestStatus.Pending ||
                     x.Status == TableServiceRequestStatus.Taken ||
                     x.Status == TableServiceRequestStatus.InProgress))
                .OrderByDescending(x => x.RequestedAt)
                .Select(x => new PublicActiveTableRequestDto
                {
                    Id = x.Id,
                    Status = x.Status,
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
    }

internal sealed class PublicMenuAvailabilityResult
{
    public Dictionary<Guid, bool> MenuItems { get; init; } = [];
    public Dictionary<Guid, bool> ModifierOptions { get; init; } = [];
}

internal static class PublicMenuAvailability
{
    public static async Task<PublicMenuAvailabilityResult> BuildAsync(
        GrimorioDbContext context,
        Guid branchId,
        IReadOnlyCollection<MenuItem> menuItems,
        CancellationToken cancellationToken)
    {
        var articleIds = menuItems
            .SelectMany(item => item.Recipe.Where(r => !r.IsDeleted).Select(r => r.ArticleId))
            .Concat(menuItems.SelectMany(item => item.ModifierGroups
                .Where(g => !g.IsDeleted && g.IsActive)
                .SelectMany(g => g.Options
                    .Where(o => !o.IsDeleted && o.IsActive && o.ArticleId.HasValue)
                    .Select(o => o.ArticleId!.Value))))
            .Distinct()
            .ToList();

        var stockByArticle = articleIds.Count == 0
            ? new Dictionary<Guid, decimal>()
            : await context.WarehouseStock
                .AsNoTracking()
                .Where(x => x.BranchId == branchId && !x.IsDeleted && articleIds.Contains(x.ArticleId))
                .GroupBy(x => x.ArticleId)
                .Select(g => new { ArticleId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.ArticleId, x => x.Quantity, cancellationToken);

        var reservedByArticle = articleIds.Count == 0
            ? new Dictionary<Guid, decimal>()
            : await context.StockReservations
                .AsNoTracking()
                .Where(x =>
                    x.BranchId == branchId &&
                    !x.IsDeleted &&
                    articleIds.Contains(x.ArticleId) &&
                    x.Status == StockReservationStatus.Active)
                .GroupBy(x => x.ArticleId)
                .Select(g => new { ArticleId = g.Key, Quantity = g.Sum(x => x.BaseQuantity) })
                .ToDictionaryAsync(x => x.ArticleId, x => x.Quantity, cancellationToken);

        foreach (var (articleId, reservedQuantity) in reservedByArticle)
            stockByArticle[articleId] = Math.Max(0, stockByArticle.GetValueOrDefault(articleId) - reservedQuantity);

        var conversions = await context.UnitConversions
            .AsNoTracking()
            .Where(x => x.BranchId == branchId && !x.IsDeleted)
            .Select(x => new UnitConversionInfo(x.OriginUnitId, x.DestinationUnitId, x.Factor))
            .ToListAsync(cancellationToken);

        var result = new PublicMenuAvailabilityResult();
        foreach (var item in menuItems)
        {
            result.MenuItems[item.Id] = IsMenuItemAvailable(item, stockByArticle, conversions);

            foreach (var option in item.ModifierGroups
                .Where(g => !g.IsDeleted && g.IsActive)
                .SelectMany(g => g.Options.Where(o => !o.IsDeleted && o.IsActive)))
            {
                result.ModifierOptions[option.Id] = IsModifierOptionAvailable(option, stockByArticle, conversions);
            }
        }

        return result;
    }

    public static bool IsMenuItemAvailable(
        MenuItem item,
        IReadOnlyDictionary<Guid, decimal> stockByArticle,
        IReadOnlyCollection<UnitConversionInfo> conversions,
        int quantity = 1)
    {
        if (!item.IsActive || !item.AvailableForSale || item.IsDeleted) return false;

        var recipe = item.Recipe.Where(r => !r.IsDeleted).ToList();
        if (recipe.Count == 0) return true;

        foreach (var ingredient in recipe)
        {
            var article = ingredient.Article;
            if (article is null) return false;

            var requiredBase = ConvertQuantity(ingredient.Quantity * quantity, ingredient.UnitId, article.BaseUnitId, conversions);
            if (requiredBase <= 0) return false;

            var stock = stockByArticle.GetValueOrDefault(ingredient.ArticleId);
            if (stock < requiredBase) return false;
        }

        return true;
    }

    public static bool IsModifierOptionAvailable(
        MenuItemModifierOption option,
        IReadOnlyDictionary<Guid, decimal> stockByArticle,
        IReadOnlyCollection<UnitConversionInfo> conversions,
        int quantity = 1)
    {
        if (!option.IsActive || option.IsDeleted) return false;
        if (!option.ArticleId.HasValue) return true;
        if (!option.UnitId.HasValue || option.Quantity <= 0 || option.Article is null) return false;

        var requiredBase = ConvertQuantity(option.Quantity * quantity, option.UnitId.Value, option.Article.BaseUnitId, conversions);
        if (requiredBase <= 0) return false;

        return stockByArticle.GetValueOrDefault(option.ArticleId.Value) >= requiredBase;
    }

    public static async Task<(Dictionary<Guid, decimal> StockByArticle, List<UnitConversionInfo> Conversions)> LoadStockInputsAsync(
        GrimorioDbContext context,
        Guid branchId,
        IReadOnlyCollection<Guid> articleIds,
        CancellationToken cancellationToken)
    {
        var stockByArticle = articleIds.Count == 0
            ? new Dictionary<Guid, decimal>()
            : await context.WarehouseStock
                .AsNoTracking()
                .Where(x => x.BranchId == branchId && !x.IsDeleted && articleIds.Contains(x.ArticleId))
                .GroupBy(x => x.ArticleId)
                .Select(g => new { ArticleId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.ArticleId, x => x.Quantity, cancellationToken);

        var reservedByArticle = articleIds.Count == 0
            ? new Dictionary<Guid, decimal>()
            : await context.StockReservations
                .AsNoTracking()
                .Where(x =>
                    x.BranchId == branchId &&
                    !x.IsDeleted &&
                    articleIds.Contains(x.ArticleId) &&
                    x.Status == StockReservationStatus.Active)
                .GroupBy(x => x.ArticleId)
                .Select(g => new { ArticleId = g.Key, Quantity = g.Sum(x => x.BaseQuantity) })
                .ToDictionaryAsync(x => x.ArticleId, x => x.Quantity, cancellationToken);

        foreach (var (articleId, reservedQuantity) in reservedByArticle)
            stockByArticle[articleId] = Math.Max(0, stockByArticle.GetValueOrDefault(articleId) - reservedQuantity);

        var conversions = await context.UnitConversions
            .AsNoTracking()
            .Where(x => x.BranchId == branchId && !x.IsDeleted)
            .Select(x => new UnitConversionInfo(x.OriginUnitId, x.DestinationUnitId, x.Factor))
            .ToListAsync(cancellationToken);

        return (stockByArticle, conversions);
    }

    public sealed record UnitConversionInfo(Guid OriginUnitId, Guid DestinationUnitId, decimal Factor);

    private static decimal ConvertQuantity(
        decimal quantity,
        Guid originUnitId,
        Guid destinationUnitId,
        IEnumerable<UnitConversionInfo> conversions)
    {
        if (originUnitId == destinationUnitId) return quantity;

        var direct = conversions.FirstOrDefault(x => x.OriginUnitId == originUnitId && x.DestinationUnitId == destinationUnitId);
        if (direct is not null) return quantity * direct.Factor;

        var reverse = conversions.FirstOrDefault(x => x.OriginUnitId == destinationUnitId && x.DestinationUnitId == originUnitId);
        if (reverse is not null && reverse.Factor != 0) return quantity / reverse.Factor;

        return 0m;
    }
}
