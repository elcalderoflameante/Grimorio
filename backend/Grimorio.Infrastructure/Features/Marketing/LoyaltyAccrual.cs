using System.Text.Json;
using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.Marketing;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Marketing;

public static class LoyaltyAccrual
{
    public static async Task RecordAsync(GrimorioDbContext db, OrderPayment payment, CancellationToken ct)
    {
        if (!payment.LoyaltyMemberId.HasValue) return;
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Fidelizacion requiere la transaccion del cobro.");
        var program = await db.LoyaltyPrograms.AsNoTracking().SingleOrDefaultAsync(p => p.BranchId == payment.BranchId && p.IsActive, ct)
            ?? throw new PaymentRejectedException("El programa de fidelizacion no esta activo. Retira la tarjeta de este cobro o revisa la configuracion.");
        var member = await db.LoyaltyMembers.FromSqlInterpolated($"""
            SELECT * FROM marketing."Members" WHERE "Id" = {payment.LoyaltyMemberId.Value}
            AND "BranchId" = {payment.BranchId} AND NOT "IsDeleted" FOR UPDATE
            """).AsNoTracking().SingleOrDefaultAsync(ct);
        if (member is null || !member.IsActive)
            throw new PaymentRejectedException("La tarjeta no pertenece a un miembro activo de esta sucursal.");
        if (await db.LoyaltyContributions.AnyAsync(c => c.OrderPaymentId == payment.Id, ct)) return;
        var excluded = JsonSerializer.Deserialize<List<Guid>>(program.ExcludedItemIdsJson, LoyaltyHandlers.Json) ?? [];
        var eligibleItems = payment.Items.Where(i => !i.IsDeleted && i.OrderItem != null && !excluded.Contains(i.OrderItem.MenuItemId)).ToList();
        var eligibleTotal = eligibleItems.Sum(i => i.Total);
        var eligibleSubtotal = eligibleItems.Sum(i => i.Subtotal ?? 0m);
        var eligible = program.IncludeTax ? eligibleTotal : eligibleSubtotal;
        var timeZone = await db.Branches.Where(b => b.Id == payment.BranchId).Select(b => b.TimeZoneId).SingleAsync(ct);
        var localDate = BranchTimeZone.DateFromUtc(payment.PaidAt, timeZone);
        var accumulated = await db.LoyaltyContributions.Where(c => c.MemberId == member.Id && c.OrderId == payment.OrderId
            && c.BusinessDate == localDate).SumAsync(c => program.IncludeTax ? c.EligibleAmount : c.EligibleSubtotal, ct) + eligible;
        db.Add(new LoyaltyContribution { BranchId = payment.BranchId, MemberId = member.Id, OrderId = payment.OrderId,
            OrderPaymentId = payment.Id, BusinessDate = localDate, EligibleAmount = eligibleTotal, EligibleSubtotal = eligibleSubtotal });
        if (accumulated >= program.MinimumSpend && !await db.LoyaltyVisits.AnyAsync(v => v.MemberId == member.Id && v.BusinessDate == localDate, ct)) {
            var number = await db.LoyaltyVisits.CountAsync(v => v.MemberId == member.Id, ct) + 1;
            db.Add(new LoyaltyVisit { BranchId = payment.BranchId, MemberId = member.Id, OrderId = payment.OrderId,
                BusinessDate = localDate, QualifiedSpend = accumulated, MinimumSpend = program.MinimumSpend });
            var tier = LoyaltyHandlers.Tiers(program).SingleOrDefault(t => t.IsEnabled && (number == t.VisitNumber
                || (t.RepeatEvery.HasValue && number > t.VisitNumber && (number - t.VisitNumber) % t.RepeatEvery.Value == 0)));
            if (tier != null) {
                var nextDay = localDate.AddDays(1).ToDateTime(TimeOnly.MinValue);
                var zone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
                db.Add(new LoyaltyReward { BranchId = payment.BranchId, MemberId = member.Id, VisitNumber = number, Name = tier.Name,
                    IssuedAt = payment.PaidAt, AvailableAt = TimeZoneInfo.ConvertTimeToUtc(nextDay, zone),
                    ExpiresAt = payment.PaidAt.AddDays(program.RewardValidityDays), RuleSnapshotJson = JsonSerializer.Serialize(tier, LoyaltyHandlers.Json) });
            }
        }
        await db.SaveChangesAsync(ct);
    }
}
