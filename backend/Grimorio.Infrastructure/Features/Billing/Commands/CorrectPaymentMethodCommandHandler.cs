using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Infrastructure.Features.POS.Commands;
using Grimorio.Infrastructure.Features.POS;
using Grimorio.Infrastructure.Persistence;
using Grimorio.SharedKernel.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Billing.Commands;

public class CorrectPaymentMethodCommandHandler(GrimorioDbContext db) : IRequestHandler<CorrectPaymentMethodCommand, bool>
{
    public async Task<bool> Handle(CorrectPaymentMethodCommand request, CancellationToken ct)
    {
        var data = request.Data;
        var reason = data.Reason?.Trim() ?? "";
        var replaceAll = data.ExpectedLines != null;
        if (data.RequestId == Guid.Empty || reason.Length is < 5 or > 500
            || (!replaceAll && (data.PaymentLineId == Guid.Empty || data.ExpectedMethodId == Guid.Empty
                || (data.Lines == null && data.MethodId == Guid.Empty)))
            || (replaceAll && (data.Lines == null || data.ExpectedLines!.Count is < 1 or > 100
                || data.ExpectedLines.Any(l => l == null || l.Id == Guid.Empty || l.MethodId == Guid.Empty)
                || data.ExpectedLines.Select(l => l.Id).Distinct().Count() != data.ExpectedLines.Count)))
            throw new InvalidOperationException("Selecciona el medio de pago e indica un motivo de 5 a 500 caracteres.");
        if (data.Lines != null && (data.Lines.Count is < 1 or > 10 || data.Lines.Any(l =>
            l == null || l.MethodId == Guid.Empty || l.Amount <= 0 || decimal.Round(l.Amount, 2) != l.Amount)))
            throw new InvalidOperationException("Indica de 1 a 10 medios con importes positivos de hasta dos decimales.");
        var actor = await db.UserRoles.Where(r => r.UserId == request.ActorUserId && r.BranchId == request.BranchId
            && r.User != null && r.User.IsActive && r.User.BranchId == request.BranchId
            && r.Role != null && r.Role.IsActive && r.Role.BranchId == request.BranchId
            && (r.Role.Name == AppConstants.Roles.Admin || r.Role.RolePermissions.Any(p => p.BranchId == request.BranchId
                && p.Permission != null && p.Permission.IsActive && p.Permission.BranchId == request.BranchId
                && p.Permission.Code == AppConstants.Permissions.BillingPaymentsCorrectMethod)))
            .Select(r => new { r.User!.FirstName, r.User.LastName }).FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("No tienes un rol activo con permiso para corregir medios de pago.");
        var legacyPayload = JsonSerializer.Serialize(new {
            request.PaymentId, data.PaymentLineId, data.ExpectedMethodId, data.ExpectedUpdatedAt, data.MethodId,
            data.CardPaymentType, data.CardBankId, CardBrand = data.CardBrand?.Trim(),
            AuthorizationNumber = data.AuthorizationNumber?.Trim(), Reason = reason,
        });
        var hashPayload = data.Lines == null ? legacyPayload : JsonSerializer.Serialize(new { Legacy = legacyPayload, data.Lines });
        if (replaceAll) hashPayload = JsonSerializer.Serialize(new { Payload = hashPayload, data.ExpectedLines });
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hashPayload)));
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var orderId = await db.OrderPayments.Where(p => p.Id == request.PaymentId && p.BranchId == request.BranchId)
            .Select(p => (Guid?)p.OrderId).SingleOrDefaultAsync(ct) ?? throw new KeyNotFoundException("Cobro no encontrado.");
        await PosOrderLock.AcquireAsync(db, request.BranchId, orderId, ct);
        var applied = await db.PaymentMethodCorrections.SingleOrDefaultAsync(c => c.BranchId == request.BranchId && c.RequestId == data.RequestId, ct);
        if (applied != null)
        {
            if (applied.RequestHash != hash) throw new InvalidOperationException("Este intento ya se utilizo con otros datos. Actualiza la venta.");
            await tx.CommitAsync(ct);
            return true;
        }
        var payment = await db.OrderPayments.Include(p => p.Lines).ThenInclude(l => l.Config)
            .Include(p => p.Lines).ThenInclude(l => l.EmployeeConsumption)
            .SingleAsync(p => p.Id == request.PaymentId && p.BranchId == request.BranchId, ct);
        if (!payment.CashSessionId.HasValue) throw new InvalidOperationException("El cobro no tiene una caja asociada.");
        // Match payment/close lock order: order first, then its cash session.
        var session = await db.CashSessions.FromSqlInterpolated($"""
            SELECT * FROM billing."CashSessions" WHERE "Id" = {payment.CashSessionId.Value}
            AND "BranchId" = {request.BranchId} AND NOT "IsDeleted" FOR UPDATE
            """).SingleOrDefaultAsync(ct);
        if (session == null || session.Status != CashSessionStatus.Open)
            throw new InvalidOperationException("La caja ya esta cerrada. Esta correccion solo admite cajas abiertas.");
        if (payment.Lines.Any(l => l.EmployeeConsumption != null || l.Config == null || l.Config.Purpose != PaymentMethodPurpose.Standard))
            throw new InvalidOperationException("Los cobros con consumos de empleados requieren una revision separada.");
        if (payment.Lines.Sum(l => l.AmountTendered - l.Change) != payment.OrderAmount)
            throw new InvalidOperationException("El detalle de medios no coincide con el importe del cobro. Requiere revision manual.");
        if (replaceAll && (data.ExpectedLines!.Count != payment.Lines.Count || data.ExpectedLines.Any(expected =>
            !payment.Lines.Any(l => l.Id == expected.Id && l.PaymentMethodConfigId == expected.MethodId
                && l.UpdatedAt == expected.UpdatedAt && l.AmountTendered - l.Change == expected.NetAmount))))
            throw new InvalidOperationException("Los pagos de esta venta cambiaron. Actualiza la venta antes de corregirlos.");
        var line = replaceAll ? payment.Lines.OrderBy(l => l.Id).First() : payment.Lines.SingleOrDefault(l => l.Id == data.PaymentLineId)
            ?? throw new InvalidOperationException("La linea seleccionada no pertenece a este cobro.");
        if (!replaceAll && (line.PaymentMethodConfigId != data.ExpectedMethodId || line.UpdatedAt != data.ExpectedUpdatedAt))
            throw new InvalidOperationException("Este medio de pago cambio desde que abriste la venta. Actualiza antes de corregirlo.");
        var sourceLines = replaceAll ? payment.Lines.ToList() : [line];
        var net = sourceLines.Sum(l => l.AmountTendered - l.Change);
        if (net <= 0 || sourceLines.Any(l => l.Change < 0 || l.AmountTendered - l.Change <= 0))
            throw new InvalidOperationException("El importe de las lineas no es valido.");
        var replacements = data.Lines ?? [new PaymentMethodCorrectionLineDto {
            MethodId = data.MethodId, Amount = net, CardPaymentType = data.CardPaymentType, CardBankId = data.CardBankId,
            CardBrand = data.CardBrand, AuthorizationNumber = data.AuthorizationNumber,
        }];
        if (replacements.Sum(l => l.Amount) != net)
            throw new InvalidOperationException("La suma de los medios debe coincidir exactamente con el importe a reclasificar.");
        var replacementLines = new List<PaymentLine>();
        foreach (var replacement in replacements)
        {
            var method = await db.PaymentMethodConfigs.SingleOrDefaultAsync(m => m.Id == replacement.MethodId && m.IsActive, ct)
                ?? throw new InvalidOperationException("El medio de pago no esta disponible.");
            if (method.Purpose != PaymentMethodPurpose.Standard || (method.IsCash && method.IsCard))
                throw new InvalidOperationException("Selecciona un medio de pago normal, no consumo de empleados.");
            CardPaymentType? cardType = null;
            CardBank? bank = null;
            if (method.IsCard)
            {
                if (!Enum.TryParse<CardPaymentType>(replacement.CardPaymentType, true, out var parsed) || !Enum.IsDefined(parsed))
                    throw new InvalidOperationException("Selecciona credito o debito.");
                cardType = parsed;
                bank = await db.CardBanks.SingleOrDefaultAsync(b => b.Id == replacement.CardBankId && b.BranchId == request.BranchId && b.IsActive, ct)
                    ?? throw new InvalidOperationException("Selecciona un banco valido para la tarjeta.");
                if (string.IsNullOrWhiteSpace(replacement.CardBrand) || replacement.CardBrand.Trim().Length > 40
                    || string.IsNullOrWhiteSpace(replacement.AuthorizationNumber) || replacement.AuthorizationNumber.Trim().Length > 50)
                    throw new InvalidOperationException("Completa la marca y autorizacion de la tarjeta (maximo 40 y 50 caracteres).");
            }
            else if (replacement.CardBankId.HasValue || replacement.CardPaymentType != null || replacement.CardBrand != null || replacement.AuthorizationNumber != null)
                throw new InvalidOperationException("Los datos de tarjeta solo corresponden a pagos con tarjeta.");
            replacementLines.Add(new PaymentLine {
                BranchId = request.BranchId, OrderPaymentId = payment.Id, Config = method, PaymentMethodConfigId = method.Id,
                AmountTendered = replacement.Amount, Change = 0, CardPaymentType = cardType, CardBankId = bank?.Id,
                CardBankName = bank?.Name, CardBrand = method.IsCard ? replacement.CardBrand!.Trim() : null,
                AuthorizationNumber = method.IsCard ? replacement.AuthorizationNumber!.Trim() : null,
            });
        }
        var first = replacementLines[0];
        var unmatched = replacementLines.ToList();
        foreach (var source in sourceLines)
        {
            var match = unmatched.FindIndex(target => source.PaymentMethodConfigId == target.PaymentMethodConfigId
                && source.AmountTendered - source.Change == target.AmountTendered - target.Change
                && source.CardPaymentType == target.CardPaymentType && source.CardBankId == target.CardBankId
                && source.CardBrand == target.CardBrand && source.AuthorizationNumber == target.AuthorizationNumber);
            if (match >= 0) unmatched.RemoveAt(match);
        }
        if (sourceLines.Count == replacementLines.Count && unmatched.Count == 0)
            throw new InvalidOperationException("No hay cambios en el medio de pago.");
        var before = sourceLines.Select(Snapshot).ToList();
        payment.OriginalPaymentLinesJson ??= JsonSerializer.Serialize(payment.Lines.Select(l => new PaymentLineCommand {
            MethodId = l.PaymentMethodConfigId, AmountTendered = l.AmountTendered, CardPaymentType = l.CardPaymentType?.ToString(),
            CardBankId = l.CardBankId, CardBrand = l.CardBrand, AuthorizationNumber = l.AuthorizationNumber,
        }).ToList());
        foreach (var removed in sourceLines.Where(l => l.Id != line.Id))
        {
            removed.IsDeleted = true;
            removed.DeletedAt = DateTime.UtcNow;
        }
        line.PaymentMethodConfigId = first.PaymentMethodConfigId;
        line.Config = first.Config;
        line.AmountTendered = first.AmountTendered;
        line.Change = 0;
        line.CardPaymentType = first.CardPaymentType;
        line.CardBankId = first.CardBankId;
        line.CardBankName = first.CardBankName;
        line.CardBrand = first.CardBrand;
        line.AuthorizationNumber = first.AuthorizationNumber;
        replacementLines[0] = line;
        foreach (var added in replacementLines.Skip(1))
        {
            payment.Lines.Add(added);
            db.PaymentLines.Add(added);
        }
        db.PaymentMethodCorrections.Add(new PaymentMethodCorrection {
            BranchId = request.BranchId, OrderPaymentId = payment.Id, PaymentLineId = line.Id, CashSessionId = session.Id,
            RequestId = data.RequestId, RequestHash = hash, ActorUserId = request.ActorUserId,
            ActorName = $"{actor.FirstName} {actor.LastName}".Trim(), Reason = reason,
            BeforeJson = replaceAll ? JsonSerializer.Serialize(before) : JsonSerializer.Serialize(before[0]), AfterJson = replacementLines.Count == 1
                ? JsonSerializer.Serialize(Snapshot(line)) : JsonSerializer.Serialize(replacementLines.Select(Snapshot)),
        });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    private static PaymentLineDto Snapshot(PaymentLine line) => new() {
        Id = line.Id, MethodId = line.PaymentMethodConfigId, MethodName = line.Config!.Name, MethodColor = line.Config.Color,
        IsCash = line.Config.IsCash, IsCard = line.Config.IsCard, AmountTendered = line.AmountTendered, Change = line.Change,
        CardPaymentType = line.CardPaymentType?.ToString(), CardBankId = line.CardBankId, CardBankName = line.CardBankName,
        CardBrand = line.CardBrand, AuthorizationNumber = line.AuthorizationNumber,
    };
}
