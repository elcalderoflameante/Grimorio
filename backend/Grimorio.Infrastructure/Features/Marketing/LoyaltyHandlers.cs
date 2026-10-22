using System.Security.Cryptography;
using System.Text.Json;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Marketing;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.Marketing;
using Grimorio.Domain.Entities.Menu;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Marketing;

public class LoyaltyHandlers(GrimorioDbContext db) :
    IRequestHandler<GetLoyaltyProgramQuery, LoyaltyProgramDto>,
    IRequestHandler<SaveLoyaltyProgramCommand, LoyaltyProgramDto>,
    IRequestHandler<GetLoyaltyMembersQuery, List<LoyaltyMemberDto>>,
    IRequestHandler<EnrollLoyaltyMemberCommand, LoyaltyMemberDetailDto>,
    IRequestHandler<UpdateLoyaltyMemberCommand, LoyaltyMemberDetailDto>,
    IRequestHandler<GetLoyaltyMemberQuery, LoyaltyMemberDetailDto?>,
    IRequestHandler<GetPublicLoyaltyCardQuery, PublicLoyaltyCardDto?>,
    IRequestHandler<GetLoyaltySummaryQuery, LoyaltySummaryDto>
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    internal static List<LoyaltyTierDto> Tiers(LoyaltyProgram program) => JsonSerializer.Deserialize<List<LoyaltyTierDto>>(program.TiersJson, Json) ?? [];
    private static LoyaltyProgramDto Map(LoyaltyProgram p) => new() { Name = p.Name, IsActive = p.IsActive, MinimumSpend = p.MinimumSpend,
        IncludeTax = p.IncludeTax, RewardValidityDays = p.RewardValidityDays, Tiers = Tiers(p),
        ExcludedItemIds = JsonSerializer.Deserialize<List<Guid>>(p.ExcludedItemIdsJson, Json) ?? [] };

    public async Task<LoyaltyProgramDto> Handle(GetLoyaltyProgramQuery q, CancellationToken ct) {
        var program = await db.LoyaltyPrograms.AsNoTracking().SingleOrDefaultAsync(p => p.BranchId == q.BranchId, ct);
        return program is null ? new() : Map(program);
    }

    public async Task<LoyaltyProgramDto> Handle(SaveLoyaltyProgramCommand q, CancellationToken ct) {
        var p = q.Program;
        if (string.IsNullOrWhiteSpace(p.Name) || p.Name.Trim().Length > 120 || p.MinimumSpend <= 0 || p.MinimumSpend > 10000
            || decimal.Round(p.MinimumSpend, 2) != p.MinimumSpend || p.RewardValidityDays is < 1 or > 365
            || p.Tiers is null || p.ExcludedItemIds is null || p.Tiers.Any(t => t is null)
            || p.Tiers.Count is < 1 or > 30 || p.Tiers.Select(t => t.VisitNumber).Distinct().Count() != p.Tiers.Count)
            throw new InvalidOperationException("Revisa el nombre, consumo minimo, vigencia y los hitos de la escalera.");
        foreach (var tier in p.Tiers) {
            if (tier.VisitNumber is < 1 or > 10000 || tier.RepeatEvery is <= 0 or > 1000
                || string.IsNullOrWhiteSpace(tier.Name) || tier.Name.Length > 120 || tier.Kind is not ("Bundle" or "Choice")
                || tier.MaximumPrice is <= 0 || tier.Products is null || tier.Products.Count > 30 || tier.Products.Any(i => i is null || i.Quantity is < 1 or > 20)
                || tier.Products.Select(i => i.MenuItemId).Distinct().Count() != tier.Products.Count)
                throw new InvalidOperationException("Cada hito debe tener visitas, nombre, tipo y productos validos.");
            if (tier.IsEnabled && tier.Products.Count == 0)
                throw new InvalidOperationException($"Selecciona los productos de '{tier.Name}' antes de habilitarlo.");
            if (tier.Kind == "Choice" && tier.Products.Any(i => i.Quantity != 1))
                throw new InvalidOperationException("En los premios a elegir, cada opcion debe tener cantidad uno.");
        }
        var repeating = p.Tiers.Where(t => t.RepeatEvery.HasValue).ToList();
        if (repeating.Count > 1 || repeating.Any(t => p.Tiers.Any(other => other != t && other.VisitNumber >= t.VisitNumber)))
            throw new InvalidOperationException("Solo el ultimo hito puede repetirse.");
        if (p.IsActive && !p.Tiers.Any(t => t.IsEnabled)) throw new InvalidOperationException("Habilita al menos un premio antes de activar el programa.");
        var ids = p.Tiers.SelectMany(t => t.Products).Select(i => i.MenuItemId).Distinct().ToList();
        var products = await db.MenuItems.AsNoTracking().Where(i => i.BranchId == q.BranchId && ids.Contains(i.Id)
            && i.IsActive && i.SaleVisibility == MenuItemSaleVisibility.RewardsOnly).ToDictionaryAsync(i => i.Id, ct);
        if (ids.Any(id => !products.ContainsKey(id))) throw new InvalidOperationException("Los premios deben usar productos activos del catalogo Recompensas de esta sucursal.");
        foreach (var tier in p.Tiers) foreach (var item in tier.Products) {
            var product = products[item.MenuItemId]; item.Name = product.Name;
            if (tier.MaximumPrice.HasValue && product.Price > tier.MaximumPrice)
                throw new InvalidOperationException($"{product.Name} supera el valor maximo de {tier.Name}.");
        }
        var excluded = p.ExcludedItemIds.Distinct().ToList();
        if (await db.MenuItems.CountAsync(i => i.BranchId == q.BranchId && excluded.Contains(i.Id), ct) != excluded.Count)
            throw new InvalidOperationException("Hay exclusiones de productos que no pertenecen a esta sucursal.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Serialize first-time setup as well as edits without deleting old reward snapshots.
        await db.Branches.FromSqlInterpolated($"SELECT * FROM organization.\"Branches\" WHERE \"Id\" = {q.BranchId} FOR UPDATE").AsNoTracking().SingleAsync(ct);
        var entity = await db.LoyaltyPrograms.SingleOrDefaultAsync(x => x.BranchId == q.BranchId, ct);
        if (entity is null) { entity = new() { BranchId = q.BranchId }; db.Add(entity); }
        entity.Name = p.Name.Trim(); entity.MinimumSpend = p.MinimumSpend; entity.IncludeTax = p.IncludeTax;
        entity.IsActive = p.IsActive; entity.RewardValidityDays = p.RewardValidityDays;
        entity.TiersJson = JsonSerializer.Serialize(p.Tiers.OrderBy(t => t.VisitNumber), Json);
        entity.ExcludedItemIdsJson = JsonSerializer.Serialize(excluded, Json);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Map(entity);
    }

    public async Task<List<LoyaltyMemberDto>> Handle(GetLoyaltyMembersQuery q, CancellationToken ct) {
        var query = db.LoyaltyMembers.AsNoTracking().Where(m => m.BranchId == q.BranchId);
        if (!string.IsNullOrWhiteSpace(q.Search)) {
            var search = q.Search.Trim();
            var phone = LoyaltyContact.NormalizePhone(search);
            var digits = string.Concat(search.Where(c => c is >= '0' and <= '9'));
            var card = search.ToUpperInvariant();
            query = query.Where(m => m.CardCode == card || m.PublicToken == search || m.Name.ToLower().Contains(search.ToLower())
                || (phone != null && m.Phone == phone) || (digits.Length >= 3 && m.Phone != null && m.Phone.Contains(digits)));
        }
        return await query.OrderBy(m => m.Name).Take(100).Select(m => new LoyaltyMemberDto {
            Id = m.Id, CustomerId = m.CustomerId, CustomerName = m.Name, BillingCustomerName = m.Customer != null ? m.Customer.Name : null,
            Phone = m.Phone, Email = m.Email, BirthDay = m.BirthDay, BirthMonth = m.BirthMonth, MarketingConsent = m.MarketingConsent,
            CardCode = m.CardCode, IsActive = m.IsActive,
            Visits = db.LoyaltyVisits.Count(v => v.MemberId == m.Id),
            Rewards = db.LoyaltyRewards.Count(r => r.MemberId == m.Id && r.AvailableAt <= DateTime.UtcNow && r.ExpiresAt > DateTime.UtcNow),
        }).ToListAsync(ct);
    }

    public async Task<LoyaltyMemberDetailDto> Handle(EnrollLoyaltyMemberCommand q, CancellationToken ct) {
        if (!q.Member.AcceptsProgram) throw new InvalidOperationException("Confirma que el cliente acepta participar en el programa.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockBranch(q.BranchId, ct);
        var customer = await Customer(q.BranchId, q.Member.CustomerId, ct);
        var member = new LoyaltyMember { BranchId = q.BranchId, CustomerId = customer?.Id, AcceptedAt = DateTime.UtcNow,
            MarketingConsentUpdatedAt = DateTime.UtcNow, CardCode = "CF-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(8)),
            PublicToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant() };
        q.Member.Name ??= customer?.Name; q.Member.Phone ??= customer?.Phone; q.Member.Email ??= customer?.Email;
        LoyaltyContact.Apply(member, q.Member);
        var existing = customer is null ? null : await db.LoyaltyMembers.SingleOrDefaultAsync(m => m.BranchId == q.BranchId && m.CustomerId == customer.Id, ct);
        if (existing is not null) {
            if (existing.Phone != member.Phone)
                throw new InvalidOperationException("Ese cliente ya tiene una tarjeta. Actualiza su ficha existente para cambiar el telefono.");
            await tx.CommitAsync(ct);
            return (await Handle(new GetLoyaltyMemberQuery(q.BranchId, existing.Id), ct))!;
        }
        await EnsureUniqueProfile(member, ct);
        db.Add(member); await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return (await Handle(new GetLoyaltyMemberQuery(q.BranchId, member.Id), ct))!;
    }

    public async Task<LoyaltyMemberDetailDto> Handle(UpdateLoyaltyMemberCommand q, CancellationToken ct) {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockBranch(q.BranchId, ct);
        var member = await db.LoyaltyMembers.FromSqlInterpolated($"SELECT * FROM marketing.\"Members\" WHERE \"Id\" = {q.MemberId} AND \"BranchId\" = {q.BranchId} AND NOT \"IsDeleted\" FOR UPDATE")
            .SingleOrDefaultAsync(ct) ?? throw new InvalidOperationException("Miembro no encontrado.");
        if (q.Member.CustomerId != member.CustomerId) await Customer(q.BranchId, q.Member.CustomerId, ct);
        LoyaltyContact.Apply(member, q.Member);
        member.CustomerId = q.Member.CustomerId;
        await EnsureUniqueProfile(member, ct);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return (await Handle(new GetLoyaltyMemberQuery(q.BranchId, member.Id), ct))!;
    }

    private async Task LockBranch(Guid branchId, CancellationToken ct) =>
        _ = await db.Branches.FromSqlInterpolated($"SELECT * FROM organization.\"Branches\" WHERE \"Id\" = {branchId} FOR UPDATE").AsNoTracking().SingleAsync(ct);

    private async Task<Customer?> Customer(Guid branchId, Guid? customerId, CancellationToken ct) {
        if (customerId is null) return null;
        var customer = await db.Customers.AsNoTracking().SingleOrDefaultAsync(c => c.Id == customerId && c.BranchId == branchId, ct);
        if (customer is null || !customer.IsActive || customer.TaxIdType == TaxIdType.FinalConsumer)
            throw new InvalidOperationException("Selecciona una ficha de facturacion individual activa de esta sucursal, no Consumidor final.");
        return customer;
    }

    private async Task EnsureUniqueProfile(LoyaltyMember member, CancellationToken ct) {
        if (await db.LoyaltyMembers.AnyAsync(m => m.BranchId == member.BranchId && m.Id != member.Id && m.Phone == member.Phone, ct))
            throw new InvalidOperationException("Ese telefono ya tiene una tarjeta. Busca el miembro existente para no duplicar su historial.");
        if (member.CustomerId.HasValue && await db.LoyaltyMembers.AnyAsync(m => m.BranchId == member.BranchId && m.Id != member.Id && m.CustomerId == member.CustomerId, ct))
            throw new InvalidOperationException("La ficha de facturacion ya esta vinculada a otra tarjeta. No se fusionaran historiales automaticamente.");
    }

    public async Task<LoyaltyMemberDetailDto?> Handle(GetLoyaltyMemberQuery q, CancellationToken ct) {
        var member = await db.LoyaltyMembers.AsNoTracking().Include(m => m.Customer)
            .SingleOrDefaultAsync(m => m.BranchId == q.BranchId && m.Id == q.MemberId, ct);
        if (member is null) return null;
        var visits = await db.LoyaltyVisits.AsNoTracking().Where(v => v.MemberId == member.Id).OrderByDescending(v => v.BusinessDate)
            .Select(v => new LoyaltyVisitDto(v.Id, v.BusinessDate, v.QualifiedSpend, v.MinimumSpend)).ToListAsync(ct);
        var rewards = await Rewards(member.Id, ct);
        return new(new() { Id = member.Id, CustomerId = member.CustomerId, CustomerName = member.Name, BillingCustomerName = member.Customer?.Name,
            Phone = member.Phone, Email = member.Email, BirthDay = member.BirthDay, BirthMonth = member.BirthMonth, MarketingConsent = member.MarketingConsent, CardCode = member.CardCode,
            IsActive = member.IsActive, Visits = visits.Count, Rewards = rewards.Count(r => r.Status == "Available") }, member.PublicToken, visits, rewards);
    }

    private async Task<List<LoyaltyRewardDto>> Rewards(Guid memberId, CancellationToken ct) {
        var now = DateTime.UtcNow;
        var rewards = await db.LoyaltyRewards.AsNoTracking().Where(r => r.MemberId == memberId).OrderByDescending(r => r.IssuedAt).ToListAsync(ct);
        return rewards.Select(r => new LoyaltyRewardDto(r.Id, r.VisitNumber, r.Name, r.IssuedAt, r.AvailableAt, r.ExpiresAt,
            r.ExpiresAt <= now ? "Expired" : r.AvailableAt > now ? "Upcoming" : "Available",
            JsonSerializer.Deserialize<LoyaltyTierDto>(r.RuleSnapshotJson, Json)!)).ToList();
    }

    public async Task<PublicLoyaltyCardDto?> Handle(GetPublicLoyaltyCardQuery q, CancellationToken ct) {
        if (q.Token.Length != 64) return null;
        var member = await db.LoyaltyMembers.AsNoTracking().SingleOrDefaultAsync(m => m.PublicToken == q.Token && m.IsActive, ct);
        if (member is null) return null;
        var program = await Handle(new GetLoyaltyProgramQuery(member.BranchId), ct);
        var visits = await db.LoyaltyVisits.CountAsync(v => v.MemberId == member.Id, ct);
        var next = program.Tiers.Where(t => t.IsEnabled).Select(t => new { Rule = t, Visit = t.VisitNumber > visits ? t.VisitNumber
            : t.RepeatEvery.HasValue ? t.VisitNumber + ((visits - t.VisitNumber) / t.RepeatEvery.Value + 1) * t.RepeatEvery.Value : int.MaxValue })
            .Where(t => t.Visit != int.MaxValue).OrderBy(t => t.Visit).FirstOrDefault();
        var name = member.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Cliente";
        return new(program.Name, name, member.CardCode, visits, next?.Visit, next?.Rule.Name, await Rewards(member.Id, ct));
    }

    public async Task<LoyaltySummaryDto> Handle(GetLoyaltySummaryQuery q, CancellationToken ct) => new(
        await db.LoyaltyMembers.CountAsync(m => m.BranchId == q.BranchId, ct),
        await db.LoyaltyMembers.CountAsync(m => m.BranchId == q.BranchId && m.IsActive, ct),
        await db.LoyaltyVisits.CountAsync(v => v.BranchId == q.BranchId, ct),
        await db.LoyaltyRewards.CountAsync(r => r.BranchId == q.BranchId && r.AvailableAt <= DateTime.UtcNow && r.ExpiresAt > DateTime.UtcNow, ct),
        await db.LoyaltyRewards.CountAsync(r => r.BranchId == q.BranchId && r.ExpiresAt <= DateTime.UtcNow, ct));
}
