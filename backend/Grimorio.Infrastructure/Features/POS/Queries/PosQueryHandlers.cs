using Grimorio.Application.DTOs;
using Grimorio.Application.Features.POS.Queries;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Features.POS.Commands;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

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
                .ThenInclude(i => i.ModifierSelections.Where(s => !s.IsDeleted))
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
                .ThenInclude(i => i.ModifierSelections.Where(s => !s.IsDeleted))
            .AsSplitQuery()
            .FirstOrDefaultAsync(ct);

        return order == null ? null : PosMapper.MapOrder(order);
    }
}

public class GetAlexaOrderRepeatQueryHandler
    : IRequestHandler<GetAlexaOrderRepeatQuery, AlexaOrderRepeatResultDto>
{
    private readonly GrimorioDbContext _db;

    public GetAlexaOrderRepeatQueryHandler(GrimorioDbContext db) => _db = db;

    public async Task<AlexaOrderRepeatResultDto> Handle(
        GetAlexaOrderRepeatQuery req,
        CancellationToken ct)
    {
        var tableCode = NormalizeText(req.TableCode ?? string.Empty);
        if (string.IsNullOrWhiteSpace(tableCode) && req.OrderNumber == null)
        {
            return Fail("No escuche la mesa o el numero de pedido.");
        }

        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o =>
                o.BranchId == req.BranchId &&
                !o.IsDeleted &&
                o.PaidAt == null &&
                o.Status != OrderStatus.Cancelled &&
                o.Status != OrderStatus.Delivered &&
                o.Status != OrderStatus.Draft)
            .Include(o => o.Table)
            .Include(o => o.Items.Where(i => !i.IsDeleted && i.Status != OrderItemStatus.Cancelled))
                .ThenInclude(i => i.MenuItem)
            .Include(o => o.Items.Where(i => !i.IsDeleted && i.Status != OrderItemStatus.Cancelled))
                .ThenInclude(i => i.Station)
            .Include(o => o.Items.Where(i => !i.IsDeleted && i.Status != OrderItemStatus.Cancelled))
                .ThenInclude(i => i.ModifierSelections.Where(s => !s.IsDeleted))
            .AsSplitQuery()
            .OrderByDescending(o => o.ConfirmedAt ?? o.CreatedAt)
            .ToListAsync(ct);

        var order = orders.FirstOrDefault(o => MatchesTarget(o, tableCode, req.OrderNumber));
        if (order == null)
        {
            return Fail("No encontre un pedido activo para esa mesa.");
        }

        var items = order.Items
            .Where(i => !i.IsDeleted && i.Status != OrderItemStatus.Cancelled)
            .OrderBy(i => i.CreatedAt)
            .ToList();

        if (items.Count == 0)
        {
            return Fail("Ese pedido no tiene platos activos.");
        }

        var stationText = NormalizeStationAlias(NormalizeText(req.StationText ?? string.Empty));
        var excludeStationText = NormalizeStationAlias(NormalizeText(req.ExcludeStationText ?? string.Empty));

        if (!string.IsNullOrWhiteSpace(stationText) && !string.IsNullOrWhiteSpace(excludeStationText))
        {
            return Fail("Dime solo una estacion, o dime sin una estacion.");
        }

        string? stationName = null;
        string? excludedStationName = null;

        if (!string.IsNullOrWhiteSpace(stationText))
        {
            var matchingStation = FindStation(items, stationText);
            if (matchingStation == null)
            {
                return Fail($"No encontre items para la estacion {req.StationText} en ese pedido.");
            }

            stationName = matchingStation;
            items = items
                .Where(i => MatchesStation(i, stationText))
                .ToList();
        }
        else if (!string.IsNullOrWhiteSpace(excludeStationText))
        {
            var matchingStation = FindStation(items, excludeStationText);
            if (matchingStation == null)
            {
                return Fail($"No encontre la estacion {req.ExcludeStationText} en ese pedido.");
            }

            excludedStationName = matchingStation;
            items = items
                .Where(i => !MatchesStation(i, excludeStationText))
                .ToList();
        }

        if (items.Count == 0)
        {
            var emptyScope = stationName != null
                ? $"para {stationName}"
                : $"sin {excludedStationName}";
            return Fail($"No hay items {emptyScope} en ese pedido.");
        }

        var itemDtos = items.Select(PosMapper.MapOrderItem).ToList();
        var orderLabel = !string.IsNullOrWhiteSpace(order.Table?.Code)
            ? FormatTableLabel(order.Table.Code)
            : $"pedido {order.Number}";
        var itemText = string.Join("; ", items.Select(BuildItemText));
        var scope = stationName != null
            ? $", {stationName}"
            : excludedStationName != null
                ? $", sin {excludedStationName}"
                : string.Empty;
        var notes = string.IsNullOrWhiteSpace(order.Notes)
            ? string.Empty
            : $" Observacion general: {order.Notes.Trim()}.";

        return new AlexaOrderRepeatResultDto
        {
            Success = true,
            OrderId = order.Id,
            OrderNumber = order.Number,
            TableCode = order.Table?.Code,
            StationName = stationName,
            ExcludedStationName = excludedStationName,
            Items = itemDtos,
            Message = $"Pedido {orderLabel}{scope}: {itemText}.{notes}",
        };
    }

    private static AlexaOrderRepeatResultDto Fail(string message) => new()
    {
        Success = false,
        Message = message,
    };

    private static bool MatchesTarget(Order order, string tableCode, int? orderNumber)
    {
        var orderMatches = orderNumber.HasValue && order.Number == orderNumber.Value;
        var tableMatches = !string.IsNullOrWhiteSpace(tableCode) &&
            MatchesTable(NormalizeText(order.Table?.Code ?? string.Empty), tableCode);
        return orderMatches || tableMatches;
    }

    private static bool MatchesTable(string orderTable, string spokenTable)
    {
        if (orderTable == spokenTable) return true;
        if (orderTable == $"mesa {spokenTable}" || orderTable == $"m {spokenTable}") return true;

        var orderDigits = Regex.Match(orderTable, @"\d+").Value;
        var spokenDigits = Regex.Match(spokenTable, @"\d+").Value;
        return !string.IsNullOrWhiteSpace(orderDigits) &&
            !string.IsNullOrWhiteSpace(spokenDigits) &&
            orderDigits == spokenDigits;
    }

    private static string? FindStation(List<OrderItem> items, string stationText)
    {
        return items
            .Select(i => i.Station?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .FirstOrDefault(name => MatchesStationName(name!, stationText));
    }

    private static bool MatchesStation(OrderItem item, string stationText)
    {
        return MatchesStationName(item.Station?.Name ?? string.Empty, stationText);
    }

    private static bool MatchesStationName(string stationName, string stationText)
    {
        var normalizedStation = NormalizeText(stationName);
        if (string.IsNullOrWhiteSpace(normalizedStation) || string.IsNullOrWhiteSpace(stationText))
            return false;

        return normalizedStation == stationText ||
            normalizedStation.Contains(stationText) ||
            stationText.Contains(normalizedStation);
    }

    private static string NormalizeStationAlias(string value)
    {
        return value switch
        {
            "bebida" or "bebidas" => "bar",
            "frito" or "freidora" => "fritos",
            "grill" or "asado" or "asados" => "parrilla",
            "pase" or "despacho" => "emplatado",
            _ => value,
        };
    }

    private static string BuildItemText(OrderItem item)
    {
        var parts = new List<string>
        {
            $"{item.Quantity} {item.MenuItem?.Name ?? "plato"}"
        };

        var modifiers = item.ModifierSelections
            .Where(s => !s.IsDeleted)
            .Select(s => s.Quantity > 1 ? $"{s.OptionName} x{s.Quantity}" : s.OptionName)
            .ToList();
        if (modifiers.Count > 0)
            parts.Add($"con {string.Join(", ", modifiers)}");

        if (!string.IsNullOrWhiteSpace(item.Notes))
            parts.Add($"nota {item.Notes.Trim()}");

        parts.Add(item.Status switch
        {
            OrderItemStatus.Pending => "pendiente",
            OrderItemStatus.InPreparation => "en preparacion",
            OrderItemStatus.Ready => "listo",
            _ => item.Status.ToString(),
        });

        return string.Join(", ", parts);
    }

    private static string FormatTableLabel(string tableCode)
    {
        var normalized = NormalizeText(tableCode);
        if (normalized.StartsWith("mesa ")) return tableCode.Trim();
        if (Regex.IsMatch(normalized, @"^\d+$")) return $"mesa {tableCode.Trim()}";
        return tableCode.Trim();
    }

    private static string NormalizeText(string value)
    {
        var normalized = NormalizeNumbers(value)
            .ToLowerInvariant()
            .Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }

        return Regex.Replace(
                Regex.Replace(builder.ToString().Normalize(NormalizationForm.FormC), @"[^a-z0-9\s]", " "),
                @"\s+",
                " ")
            .Trim();
    }

    private static string NormalizeNumbers(string text)
    {
        var map = new Dictionary<string, string>
        {
            ["cero"] = "0",
            ["uno"] = "1",
            ["dos"] = "2",
            ["tres"] = "3",
            ["cuatro"] = "4",
            ["cinco"] = "5",
            ["seis"] = "6",
            ["siete"] = "7",
            ["ocho"] = "8",
            ["nueve"] = "9",
            ["diez"] = "10",
            ["once"] = "11",
            ["doce"] = "12",
            ["trece"] = "13",
            ["catorce"] = "14",
            ["quince"] = "15",
            ["dieciseis"] = "16",
            ["diecisiete"] = "17",
            ["dieciocho"] = "18",
            ["diecinueve"] = "19",
            ["veinte"] = "20",
            ["veintiuno"] = "21",
            ["veintidos"] = "22",
            ["veintitres"] = "23",
            ["veinticuatro"] = "24",
            ["veinticinco"] = "25",
            ["veintiseis"] = "26",
            ["veintisiete"] = "27",
            ["veintiocho"] = "28",
            ["veintinueve"] = "29",
            ["treinta"] = "30",
        };

        var result = text.ToLowerInvariant();
        foreach (var (word, digit) in map)
            result = Regex.Replace(result, $@"\b{word}\b", digit);

        return Regex.Replace(result, @"\bveinti\s+(\d)\b", "2$1");
    }
}

public class GetItemsByStationQueryHandler : IRequestHandler<GetItemsByStationQuery, List<StationItemDto>>
{
    private readonly GrimorioDbContext _db;
    public GetItemsByStationQueryHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<StationItemDto>> Handle(GetItemsByStationQuery req, CancellationToken ct)
    {
        var entities = await _db.OrderItems
            .AsNoTracking()
            .Where(i =>
                i.StationId == req.StationId &&
                i.BranchId == req.BranchId &&
                !i.IsDeleted &&
                (i.Status == OrderItemStatus.Pending || i.Status == OrderItemStatus.InPreparation))
            .Include(i => i.MenuItem)
            .Include(i => i.Order).ThenInclude(o => o!.Table)
            .Include(i => i.ModifierSelections.Where(s => !s.IsDeleted))
            .Where(i => i.Order != null &&
                i.Order.Status != OrderStatus.Cancelled &&
                i.Order.Status != OrderStatus.Delivered &&
                i.Order.Status != OrderStatus.Draft)
            .OrderBy(i => i.Order!.ConfirmedAt)
            .ToListAsync(ct);

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
        IsTakeout = i.IsTakeout,
        Status = i.Status.ToString(),
        ConfirmedAt = i.Order.ConfirmedAt ?? i.Order.CreatedAt,
        UpdatedAt = i.UpdatedAt,
        ModifierSelections = i.ModifierSelections
            .Where(s => !s.IsDeleted)
            .Select(s => new ModifierSelectionDto
            {
                ModifierGroupId = s.ModifierGroupId,
                ModifierOptionId = s.ModifierOptionId,
                GroupName = s.GroupName,
                OptionName = s.OptionName,
                Quantity = s.Quantity,
                UnitPriceDelta = s.UnitPriceDelta,
                TotalPriceDelta = s.UnitPriceDelta * s.Quantity,
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
            .Include(i => i.ModifierSelections.Where(s => !s.IsDeleted))
            .Where(i => i.Order!.Status != OrderStatus.Cancelled)
            .OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt)
            .ToListAsync(ct);

        return entities.Select(GetItemsByStationQueryHandler.MapStationItem).ToList();
    }
}
