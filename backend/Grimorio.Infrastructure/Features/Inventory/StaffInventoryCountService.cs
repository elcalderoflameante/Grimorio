using Grimorio.Application.Abstractions;
using Grimorio.Application.DTOs;
using Grimorio.Domain.Entities.Inventory;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Security;
using Grimorio.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Inventory;

/// <summary>Records observations only. This service never writes stock, reservations or movements.</summary>
public class StaffInventoryCountService(GrimorioDbContext db, ICurrentUserContext user)
{
    private Task<int> Lock(Guid branch, CancellationToken ct) => db.Database.ExecuteSqlInterpolatedAsync(
        $"SELECT pg_advisory_xact_lock(hashtext({$"inventory-counts:{branch}"}))", ct);

    private async Task<DateOnly> Today(Guid branch, CancellationToken ct) => BranchTimeZone.DateFromUtc(DateTime.UtcNow,
        await db.Branches.Where(x => x.Id == branch && x.IsActive).Select(x => x.TimeZoneId).SingleAsync(ct));

    private async Task<string> ActorName(Guid branch, CancellationToken ct) => await db.Users.Where(UserAccessPolicy.ActiveAccount)
        .Where(x => x.Id == user.UserId && x.BranchId == branch).Select(x => x.FirstName + " " + x.LastName).SingleAsync(ct);

    private async Task<InventoryCountArea> Area(Guid branch, Guid id, bool mine, CancellationToken ct) =>
        await db.InventoryCountAreas.Include(x => x.Members).Include(x => x.Rules).AsSplitQuery()
            .SingleOrDefaultAsync(x => x.BranchId == branch && x.Id == id && (!mine || x.IsActive && x.Members.Any(m => m.UserId == user.UserId)), ct)
            ?? throw new InvalidOperationException("Area no disponible para tu usuario.");

    public async Task<InventoryCountCatalogDto> Catalog(Guid branch, CancellationToken ct)
    {
        var areas = await db.InventoryCountAreas.Where(x => x.BranchId == branch).Include(x => x.Members).Include(x => x.Rules).AsSplitQuery().OrderBy(x => x.Name).ToListAsync(ct);
        return new(areas.Select(x => new InventoryCountAreaDto { Id = x.Id, Name = x.Name, IsActive = x.IsActive,
                UserIds = x.Members.Select(m => m.UserId).ToList(), Rules = x.Rules.Select(r => new InventoryCountRuleDto(r.ArticleId, r.WarehouseId, r.Frequency, r.WeekDay)).ToList() }).ToList(),
            await db.Users.Where(UserAccessPolicy.ActiveAccount).Where(x => x.BranchId == branch).OrderBy(x => x.FirstName).Select(x => new CountOptionDto(x.Id, x.FirstName + " " + x.LastName)).ToListAsync(ct),
            await db.InventoryArticles.Where(x => x.BranchId == branch && x.IsActive).OrderBy(x => x.Name).Select(x => new CountArticleOptionDto(x.Id, x.Name, x.BaseUnit!.Symbol)).ToListAsync(ct),
            await db.Warehouses.Where(x => x.BranchId == branch && x.IsActive).OrderBy(x => x.Name).Select(x => new CountOptionDto(x.Id, x.Name)).ToListAsync(ct));
    }

    public async Task<InventoryCountAreaDto> SaveArea(Guid branch, InventoryCountAreaDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length > 120 || dto.UserIds.Count > 200 || dto.Rules.Count > 1000
            || dto.UserIds.Distinct().Count() != dto.UserIds.Count || dto.Rules.Select(x => (x.ArticleId, x.WarehouseId)).Distinct().Count() != dto.Rules.Count
            || dto.Rules.Any(x => x.Frequency is not ("Daily" or "Weekly") || x.WeekDay is < 0 or > 6))
            throw new InvalidOperationException("Revisa el nombre, responsables, frecuencias y articulos duplicados.");
        await using var tx = await db.Database.BeginTransactionAsync(ct); await Lock(branch, ct);
        if (await db.InventoryCountAreas.AnyAsync(x => x.BranchId == branch && x.Id != dto.Id && x.Name.ToLower() == dto.Name.Trim().ToLower(), ct))
            throw new InvalidOperationException("Ya existe un area con ese nombre.");
        if (await db.Users.Where(UserAccessPolicy.ActiveAccount).CountAsync(x => x.BranchId == branch && dto.UserIds.Contains(x.Id), ct) != dto.UserIds.Count)
            throw new InvalidOperationException("Los responsables deben ser usuarios activos de esta sucursal.");
        var articleIds = dto.Rules.Select(x => x.ArticleId).Distinct().ToList(); var warehouseIds = dto.Rules.Select(x => x.WarehouseId).Distinct().ToList();
        if (await db.InventoryArticles.CountAsync(x => x.BranchId == branch && x.IsActive && articleIds.Contains(x.Id), ct) != articleIds.Count
            || await db.Warehouses.CountAsync(x => x.BranchId == branch && x.IsActive && warehouseIds.Contains(x.Id), ct) != warehouseIds.Count)
            throw new InvalidOperationException("Selecciona articulos y bodegas activos de esta sucursal.");
        var otherRules = await db.InventoryCountRules.Where(x => x.BranchId == branch && x.AreaId != dto.Id).ToListAsync(ct);
        if (dto.Rules.Any(r => otherRules.Any(x => x.ArticleId == r.ArticleId && x.WarehouseId == r.WarehouseId)))
            throw new InvalidOperationException("Ese articulo y bodega ya tienen un area responsable. Edita esa area para reasignarlos.");
        var area = dto.Id == Guid.Empty ? new InventoryCountArea { BranchId = branch } : await Area(branch, dto.Id, false, ct);
        if (dto.Id == Guid.Empty) db.InventoryCountAreas.Add(area);
        area.Name = dto.Name.Trim(); area.IsActive = dto.IsActive;
        foreach (var member in area.Members.Where(x => !dto.UserIds.Contains(x.UserId)).ToList()) db.Remove(member);
        foreach (var id in dto.UserIds.Where(id => !area.Members.Any(x => x.UserId == id)))
            area.Members.Add(new() { BranchId = branch, UserId = id });
        foreach (var rule in area.Rules.Where(x => !dto.Rules.Any(r => r.ArticleId == x.ArticleId && r.WarehouseId == x.WarehouseId)).ToList()) db.Remove(rule);
        foreach (var rule in dto.Rules)
        {
            var entity = area.Rules.SingleOrDefault(x => x.ArticleId == rule.ArticleId && x.WarehouseId == rule.WarehouseId);
            if (entity == null) { entity = new() { BranchId = branch, ArticleId = rule.ArticleId, WarehouseId = rule.WarehouseId }; area.Rules.Add(entity); }
            entity.Frequency = rule.Frequency; entity.WeekDay = rule.WeekDay;
        }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); dto.Id = area.Id; return dto;
    }

    public static bool IsDue(string frequency, int weekDay, DateOnly date, IEnumerable<DateOnly> completed)
    {
        if (frequency == "Daily") return !completed.Contains(date);
        // Weekly items remain pending after their scheduled day until counted within the same week.
        var monday = date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
        var scheduled = monday.AddDays((weekDay + 6) % 7);
        return date >= scheduled && !completed.Any(x => x >= monday && x <= date);
    }

    private async Task<List<InventoryCountRule>> DueRules(Guid branch, InventoryCountArea area, DateOnly date, CancellationToken ct)
    {
        var from = date.AddDays(-7);
        var completed = await db.StaffInventoryCounts.Where(x => x.BranchId == branch && x.Status != "Draft" && x.Status != "Dismissed"
                && x.BusinessDate >= from && x.BusinessDate <= date).SelectMany(x => x.Lines.Select(l => new { l.ArticleId, l.WarehouseId, x.BusinessDate })).ToListAsync(ct);
        var articles = await db.InventoryArticles.Where(x => x.BranchId == branch && x.IsActive).Select(x => x.Id).ToListAsync(ct);
        var warehouses = await db.Warehouses.Where(x => x.BranchId == branch && x.IsActive).Select(x => x.Id).ToListAsync(ct);
        return area.Rules.Where(r => articles.Contains(r.ArticleId) && warehouses.Contains(r.WarehouseId)
            && IsDue(r.Frequency, r.WeekDay, date, completed.Where(x => x.ArticleId == r.ArticleId && x.WarehouseId == r.WarehouseId).Select(x => x.BusinessDate))).ToList();
    }

    public async Task<List<InventoryCountDueDto>> Mine(Guid branch, CancellationToken ct)
    {
        var date = await Today(branch, ct);
        var areas = await db.InventoryCountAreas.Where(x => x.BranchId == branch && x.IsActive && x.Members.Any(m => m.UserId == user.UserId))
            .Include(x => x.Rules).OrderBy(x => x.Name).ToListAsync(ct);
        var result = new List<InventoryCountDueDto>();
        foreach (var area in areas)
        {
            var draft = await db.StaffInventoryCounts.Include(x => x.Lines).SingleOrDefaultAsync(x => x.BranchId == branch && x.AreaId == area.Id && x.Status == "Draft", ct);
            var recount = await db.StaffInventoryCounts.AnyAsync(x => x.BranchId == branch && x.AreaId == area.Id && x.Status == "RecountRequested", ct);
            var pending = draft?.Lines.Count(x => x.Quantity == null) ?? (await DueRules(branch, area, date, ct)).Count;
            result.Add(new(area.Id, area.Name, pending, draft?.Id, draft?.CounterName, recount));
        }
        return result;
    }

    public async Task<StaffInventoryCountDto> Start(Guid branch, Guid areaId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); await Lock(branch, ct);
        var area = await Area(branch, areaId, true, ct); var date = await Today(branch, ct);
        var draft = await db.StaffInventoryCounts.Include(x => x.Lines).SingleOrDefaultAsync(x => x.BranchId == branch && x.AreaId == areaId && x.Status == "Draft", ct);
        if (draft != null) { await tx.CommitAsync(ct); return Map(draft, false); }
        var recount = await db.StaffInventoryCounts.Include(x => x.Lines).Where(x => x.BranchId == branch && x.AreaId == areaId && x.Status == "RecountRequested").OrderBy(x => x.SubmittedAt).FirstOrDefaultAsync(ct);
        var rules = await DueRules(branch, area, date, ct);
        var count = new StaffInventoryCount { BranchId = branch, AreaId = area.Id, AreaName = area.Name, BusinessDate = date,
            CounterUserId = user.UserId, CounterName = await ActorName(branch, ct), RecountOfId = recount?.Id };
        var articles = await db.InventoryArticles.Where(x => x.BranchId == branch && x.IsActive).Include(x => x.BaseUnit).ToDictionaryAsync(x => x.Id, ct);
        var warehouses = await db.Warehouses.Where(x => x.BranchId == branch && x.IsActive).ToDictionaryAsync(x => x.Id, ct);
        foreach (var r in rules)
        {
            var a = articles[r.ArticleId]; var w = warehouses[r.WarehouseId];
            count.Lines.Add(new() { BranchId = branch, ArticleId = a.Id, ArticleName = a.Name, WarehouseId = w.Id, WarehouseName = w.Name,
                UnitId = a.BaseUnitId, UnitSymbol = a.BaseUnit!.Symbol, Frequency = r.Frequency });
        }
        // Recounts preserve the original article/unit snapshot even if the configuration changed.
        foreach (var l in recount?.Lines ?? [])
            if (!count.Lines.Any(x => x.ArticleId == l.ArticleId && x.WarehouseId == l.WarehouseId))
                count.Lines.Add(new() { BranchId = branch, ArticleId = l.ArticleId, ArticleName = l.ArticleName, WarehouseId = l.WarehouseId,
                    WarehouseName = l.WarehouseName, UnitId = l.UnitId, UnitSymbol = l.UnitSymbol, Frequency = l.Frequency });
        if (count.Lines.Count == 0) throw new InvalidOperationException("No hay articulos pendientes para esta area.");
        db.StaffInventoryCounts.Add(count); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Map(count, false);
    }

    private async Task<StaffInventoryCount> Get(Guid branch, Guid id, bool mine, CancellationToken ct)
    {
        var count = await db.StaffInventoryCounts.Include(x => x.Lines).SingleOrDefaultAsync(x => x.BranchId == branch && x.Id == id, ct)
            ?? throw new InvalidOperationException("Conteo no encontrado.");
        if (mine) await Area(branch, count.AreaId, true, ct);
        return count;
    }
    public async Task<StaffInventoryCountDto> Detail(Guid branch, Guid id, bool mine, CancellationToken ct) => Map(await Get(branch, id, mine, ct), !mine);

    public async Task<StaffInventoryCountDto> SaveLine(Guid branch, Guid id, Guid lineId, SaveInventoryCountLineDto dto, CancellationToken ct)
    {
        if (dto.Quantity is < 0 or > 99999999999999.9999m || dto.Quantity != decimal.Round(dto.Quantity ?? 0, 4) && dto.Quantity != null || dto.Notes?.Length > 1000)
            throw new InvalidOperationException("La cantidad debe ser positiva o cero, con hasta 4 decimales. Observacion: maximo 1000 caracteres.");
        await using var tx = await db.Database.BeginTransactionAsync(ct); await Lock(branch, ct);
        var count = await Get(branch, id, true, ct);
        if (count.Status != "Draft") throw new InvalidOperationException("El conteo ya fue enviado.");
        var line = count.Lines.SingleOrDefault(x => x.Id == lineId) ?? throw new InvalidOperationException("Articulo no encontrado en este conteo.");
        if (line.CountedAt?.Ticks / 10 != dto.PreviousCountedAt?.Ticks / 10 && (line.Quantity != dto.Quantity || line.Notes != dto.Notes?.Trim()))
            throw new InvalidOperationException("Otra persona actualizo este articulo. Cierra la ventana y actualiza el conteo antes de volver a contar.");
        var article = await db.InventoryArticles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == line.ArticleId && x.BranchId == branch, ct);
        if (article == null || article.BaseUnitId != line.UnitId) throw new InvalidOperationException("La unidad del articulo cambio. Solicita revision al administrador.");
        // A note-only save must not move the comparison time or replace its stored balance.
        if (line.Quantity != dto.Quantity)
        {
            line.Quantity = dto.Quantity;
            line.ExpectedQuantity = dto.Quantity == null ? null : await db.WarehouseStock.AsNoTracking().Where(x => x.BranchId == branch && x.ArticleId == line.ArticleId && x.WarehouseId == line.WarehouseId).Select(x => (decimal?)x.Quantity).SingleOrDefaultAsync(ct) ?? 0;
            line.CountedAt = dto.Quantity == null ? null : DateTime.UtcNow; line.CountedBy = dto.Quantity == null ? null : user.UserId;
        }
        line.Notes = dto.Notes?.Trim(); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Map(count, false);
    }

    public async Task<StaffInventoryCountDto> Submit(Guid branch, Guid id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); await Lock(branch, ct);
        var count = await Get(branch, id, true, ct);
        if (count.Status != "Draft") { await tx.CommitAsync(ct); return Map(count, false); }
        if (count.Lines.Count == 0 || count.Lines.Any(x => x.Quantity == null)) throw new InvalidOperationException("Cuenta todos los articulos. Ingresa cero cuando no haya existencias.");
        count.Status = "Submitted"; count.SubmittedAt = DateTime.UtcNow;
        count.CounterUserId = user.UserId; count.CounterName = await ActorName(branch, ct);
        if (count.RecountOfId is { } priorId)
        {
            var prior = await Get(branch, priorId, false, ct); prior.Status = "Recounted";
        }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Map(count, false);
    }

    public async Task<List<StaffInventoryCountDto>> List(Guid branch, DateOnly? from, DateOnly? to, string? status, CancellationToken ct)
    {
        var today = await Today(branch, ct); from ??= today.AddDays(-30); to ??= today;
        if (to < from || to.Value.DayNumber - from.Value.DayNumber > 366) throw new InvalidOperationException("Consulta un periodo de hasta un ano.");
        var counts = await db.StaffInventoryCounts.AsNoTracking().Include(x => x.Lines).Where(x => x.BranchId == branch && x.BusinessDate >= from && x.BusinessDate <= to && (status == null || x.Status == status))
            .OrderByDescending(x => x.CreatedAt).Take(500).ToListAsync(ct);
        return counts.Select(x => Map(x, true)).ToList();
    }

    public async Task<StaffInventoryCountDto> Review(Guid branch, Guid id, ReviewInventoryCountDto dto, CancellationToken ct)
    {
        if (dto.Decision is not ("Reviewed" or "Dismissed" or "RecountRequested") || string.IsNullOrWhiteSpace(dto.Notes) || dto.Notes.Length > 2000)
            throw new InvalidOperationException("Selecciona una decision y escribe el motivo (hasta 2000 caracteres).");
        await using var tx = await db.Database.BeginTransactionAsync(ct); await Lock(branch, ct);
        var count = await Get(branch, id, false, ct);
        if (count.Status != "Submitted") throw new InvalidOperationException("Este conteo ya fue revisado o todavia no fue enviado.");
        count.Status = dto.Decision; count.ReviewNotes = dto.Notes.Trim(); count.ReviewedBy = user.UserId;
        count.ReviewerName = await ActorName(branch, ct); count.ReviewedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Map(count, true);
    }

    private static StaffInventoryCountDto Map(StaffInventoryCount x, bool supervisor) => new(x.Id, x.AreaId, x.AreaName, x.BusinessDate,
        x.CounterName, x.Status, x.SubmittedAt, x.ReviewedAt, x.ReviewerName, x.ReviewNotes, x.RecountOfId,
        x.Lines.OrderBy(l => l.WarehouseName).ThenBy(l => l.ArticleName).Select(l => new InventoryCountLineDto(l.Id, l.ArticleId, l.ArticleName,
            l.WarehouseName, l.UnitSymbol, l.Quantity, supervisor ? l.ExpectedQuantity : null, supervisor ? l.Quantity - l.ExpectedQuantity : null, l.CountedAt, l.Notes)).ToList());
}
