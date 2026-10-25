using System.Globalization;
using System.Security.Cryptography;
using Grimorio.Application.DTOs;
using Grimorio.Domain.Entities.Marketing;
using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Grimorio.Infrastructure.Features.Marketing;

public class PublicLoyaltyService(GrimorioDbContext db, ILoyaltyCodeSender sender, IOptions<LoyaltyWhatsAppOptions> options, TimeProvider time)
{
    public const string TermsVersion = "loyalty-v1";
    private LoyaltyWhatsAppOptions Settings => options.Value;

    public async Task<PublicLoyaltyInfoDto> Info(CancellationToken ct) {
        var p = await db.LoyaltyPrograms.AsNoTracking().SingleOrDefaultAsync(p => p.BranchId == Settings.PublicBranchId, ct);
        return new(Settings.Ready && p?.IsActive == true, Settings.IsRestricted, p?.Name ?? "Pasaporte del aventurero", p?.MinimumSpend ?? 10,
            p?.IncludeTax ?? true, p?.RewardValidityDays ?? 30, TermsVersion,
            p is null ? [] : LoyaltyHandlers.Tiers(p).Where(t => t.IsEnabled)
                .Select(t => new PublicLoyaltyTierDto(t.VisitNumber, t.RepeatEvery, t.Name)).OrderBy(t => t.VisitNumber).ToList());
    }

    private async Task RequireAvailable(CancellationToken ct) {
        if (!(await Info(ct)).Enabled) throw new InvalidOperationException("La afiliacion en linea no esta disponible por el momento.");
    }

    private async Task LockBranch(CancellationToken ct) => _ = await db.Branches.FromSqlInterpolated(
        $"SELECT * FROM organization.\"Branches\" WHERE \"Id\" = {Settings.PublicBranchId} AND NOT \"IsDeleted\" FOR UPDATE")
        .AsNoTracking().SingleAsync(ct);

    public async Task<LoyaltyCodeSentDto> Request(RequestLoyaltyCodeDto input, CancellationToken ct) {
        await RequireAvailable(ct);
        var phone = LoyaltyContact.NormalizePhone(input.Phone) ?? throw new InvalidOperationException("Ingresa un celular valido con codigo de pais.");
        if (input.Purpose is not ("Enroll" or "Recover")) throw new InvalidOperationException("Solicitud no valida.");
        if (!Settings.CanSendTo(phone))
            throw new InvalidOperationException("La afiliacion esta en pruebas y este telefono no esta habilitado.");
        var now = time.GetUtcNow().UtcDateTime;
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        var challenge = new LoyaltyPhoneChallenge { BranchId = Settings.PublicBranchId, Phone = phone,
            Purpose = input.Purpose, ExpiresAt = now.AddMinutes(5) };
        challenge.CodeHash = LoyaltyCodeSecurity.Hash(challenge.Id, code, Settings.CodeHashKey);
        await using (var tx = await db.Database.BeginTransactionAsync(ct)) {
            await LockBranch(ct);
            if (await db.LoyaltyPhoneChallenges.CountAsync(c => c.BranchId == Settings.PublicBranchId && c.CreatedAt > now.AddDays(-1), ct) >= Settings.DailySendLimit)
                throw new InvalidOperationException("El envio de codigos alcanzo su limite temporal. Consulta en caja o intenta mas tarde.");
            var recent = await db.LoyaltyPhoneChallenges.Where(c => c.BranchId == Settings.PublicBranchId && c.Phone == phone
                && c.CreatedAt > now.AddDays(-1)).ToListAsync(ct);
            if (recent.Count >= 5 || recent.Any(c => c.CreatedAt > now.AddSeconds(-60)))
                throw new InvalidOperationException("Espera antes de solicitar otro codigo. Maximo cinco envios por telefono al dia.");
            foreach (var old in recent.Where(c => c.ConsumedAt is null)) old.ConsumedAt = now;
            db.Add(challenge); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        }
        // The durable unsent challenge counts toward send limits even if Meta times out.
        try {
            challenge.MessageId = await sender.Send(phone, code, ct);
            challenge.Sent = true; challenge.DeliveryStatus = "accepted";
            await db.SaveChangesAsync(ct);
        } catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or KeyNotFoundException) {
            throw new InvalidOperationException("No pudimos confirmar el envio. Espera un minuto e intenta nuevamente.");
        }
        return new(challenge.Id, challenge.ExpiresAt, 60);
    }

    public async Task<PublicLoyaltyAccessDto> Verify(VerifyLoyaltyCodeDto input, CancellationToken ct) {
        await RequireAvailable(ct);
        var now = time.GetUtcNow().UtcDateTime;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockBranch(ct);
        var challenge = await db.LoyaltyPhoneChallenges.SingleOrDefaultAsync(c => c.Id == input.ChallengeId
            && c.BranchId == Settings.PublicBranchId, ct);
        if (challenge is null || !challenge.Sent || challenge.ConsumedAt.HasValue || challenge.ExpiresAt <= now || challenge.Attempts >= 5)
            throw new InvalidOperationException("Codigo invalido o vencido. Solicita uno nuevo.");
        challenge.Attempts++;
        if (input.Code.Length != 6 || !input.Code.All(char.IsAsciiDigit)
            || !LoyaltyCodeSecurity.Equal(challenge.CodeHash, LoyaltyCodeSecurity.Hash(challenge.Id, input.Code, Settings.CodeHashKey))) {
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            throw new InvalidOperationException("Codigo invalido o vencido. Solicita uno nuevo si agotaste tus intentos.");
        }
        // Reuse the card only after proof of ownership; never overwrite its profile or fiscal link.
        var member = await db.LoyaltyMembers.SingleOrDefaultAsync(m => m.BranchId == Settings.PublicBranchId && m.Phone == challenge.Phone, ct);
        if (member is not null && !member.IsActive) {
            challenge.ConsumedAt = now; await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            throw new InvalidOperationException("Consulta en caja para habilitar tu tarjeta.");
        }
        if (member is null) {
            if (challenge.Purpose == "Recover") {
                challenge.ConsumedAt = now; await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
                throw new InvalidOperationException("No encontramos una tarjeta. Puedes afiliarte al programa.");
            }
            if (!input.AcceptsProgram || input.TermsVersion != TermsVersion)
                throw new InvalidOperationException("Acepta las condiciones vigentes del programa.");
            member = new() { BranchId = Settings.PublicBranchId, AcceptedAt = now, ProgramTermsVersion = TermsVersion,
                MarketingConsentUpdatedAt = now, CardCode = "CF-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(8)),
                PublicToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant() };
            LoyaltyContact.Apply(member, new() { Name = input.Name, Phone = challenge.Phone, Email = input.Email,
                BirthDay = input.BirthDay, BirthMonth = input.BirthMonth, MarketingConsent = input.MarketingConsent });
            db.Add(member);
        }
        member.PhoneVerifiedAt = now;
        challenge.ConsumedAt = now;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return new(member.PublicToken);
    }
}
