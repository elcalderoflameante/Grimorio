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
        if (data.RequestId == Guid.Empty || data.PaymentLineId == Guid.Empty || data.MethodId == Guid.Empty
            || data.ExpectedMethodId == Guid.Empty || reason.Length is < 5 or > 500)
            throw new InvalidOperationException("Selecciona el medio de pago e indica un motivo de 5 a 500 caracteres.");
        var actor = await db.UserRoles.Where(r => r.UserId == request.ActorUserId && r.BranchId == request.BranchId
            && r.User != null && r.User.IsActive && r.User.BranchId == request.BranchId
            && r.Role != null && r.Role.IsActive && r.Role.BranchId == request.BranchId
            && (r.Role.Name == AppConstants.Roles.Admin || r.Role.RolePermissions.Any(p => p.BranchId == request.BranchId
                && p.Permission != null && p.Permission.IsActive && p.Permission.BranchId == request.BranchId
                && p.Permission.Code == AppConstants.Permissions.BillingPaymentsCorrectMethod)))
            .Select(r => new { r.User!.FirstName, r.User.LastName }).FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("No tienes un rol activo con permiso para corregir medios de pago.");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {
            request.PaymentId, data.PaymentLineId, data.ExpectedMethodId, data.ExpectedUpdatedAt, data.MethodId,
            data.CardPaymentType, data.CardBankId, CardBrand = data.CardBrand?.Trim(),
            AuthorizationNumber = data.AuthorizationNumber?.Trim(), Reason = reason,
        }))));
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
        if (payment.DocumentType != DocumentType.NotaDeVenta
            || await db.ElectronicDocuments.AnyAsync(d => d.OrderPaymentId == payment.Id, ct))
            throw new InvalidOperationException("Solo se pueden corregir notas de venta sin comprobante electronico.");
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
        var line = payment.Lines.SingleOrDefault(l => l.Id == data.PaymentLineId)
            ?? throw new InvalidOperationException("La linea seleccionada no pertenece a este cobro.");
        if (line.PaymentMethodConfigId != data.ExpectedMethodId || line.UpdatedAt != data.ExpectedUpdatedAt)
            throw new InvalidOperationException("Este medio de pago cambio desde que abriste la venta. Actualiza antes de corregirlo.");
        var method = await db.PaymentMethodConfigs.SingleOrDefaultAsync(m => m.Id == data.MethodId && m.IsActive, ct)
            ?? throw new InvalidOperationException("El medio de pago no esta disponible.");
        if (method.Purpose != PaymentMethodPurpose.Standard || (method.IsCash && method.IsCard))
            throw new InvalidOperationException("Selecciona un medio de pago normal, no consumo de empleados.");
        CardPaymentType? cardType = null;
        CardBank? bank = null;
        if (method.IsCard)
        {
            if (!Enum.TryParse<CardPaymentType>(data.CardPaymentType, true, out var parsed) || !Enum.IsDefined(parsed))
                throw new InvalidOperationException("Selecciona credito o debito.");
            cardType = parsed;
            bank = await db.CardBanks.SingleOrDefaultAsync(b => b.Id == data.CardBankId && b.BranchId == request.BranchId && b.IsActive, ct)
                ?? throw new InvalidOperationException("Selecciona un banco valido para la tarjeta.");
            if (string.IsNullOrWhiteSpace(data.CardBrand) || data.CardBrand.Trim().Length > 40
                || string.IsNullOrWhiteSpace(data.AuthorizationNumber) || data.AuthorizationNumber.Trim().Length > 50)
                throw new InvalidOperationException("Completa la marca y autorizacion de la tarjeta (maximo 40 y 50 caracteres).");
        }
        else if (data.CardBankId.HasValue || data.CardPaymentType != null || data.CardBrand != null || data.AuthorizationNumber != null)
            throw new InvalidOperationException("Los datos de tarjeta solo corresponden a pagos con tarjeta.");
        var brand = method.IsCard ? data.CardBrand!.Trim() : null;
        var authorization = method.IsCard ? data.AuthorizationNumber!.Trim() : null;
        if (line.PaymentMethodConfigId == method.Id && line.CardPaymentType == cardType && line.CardBankId == bank?.Id
            && line.CardBrand == brand && line.AuthorizationNumber == authorization)
            throw new InvalidOperationException("No hay cambios en el medio de pago.");
        var net = line.AmountTendered - line.Change;
        if (net <= 0 || line.Change < 0) throw new InvalidOperationException("El importe de la linea no es valido.");
        var before = Snapshot(line);
        payment.OriginalPaymentLinesJson ??= JsonSerializer.Serialize(payment.Lines.Select(l => new PaymentLineCommand {
            MethodId = l.PaymentMethodConfigId, AmountTendered = l.AmountTendered, CardPaymentType = l.CardPaymentType?.ToString(),
            CardBankId = l.CardBankId, CardBrand = l.CardBrand, AuthorizationNumber = l.AuthorizationNumber,
        }).ToList());
        line.PaymentMethodConfigId = method.Id;
        line.Config = method;
        line.AmountTendered = net;
        line.Change = 0;
        line.CardPaymentType = cardType;
        line.CardBankId = bank?.Id;
        line.CardBankName = bank?.Name;
        line.CardBrand = brand;
        line.AuthorizationNumber = authorization;
        db.PaymentMethodCorrections.Add(new PaymentMethodCorrection {
            BranchId = request.BranchId, OrderPaymentId = payment.Id, PaymentLineId = line.Id, CashSessionId = session.Id,
            RequestId = data.RequestId, RequestHash = hash, ActorUserId = request.ActorUserId,
            ActorName = $"{actor.FirstName} {actor.LastName}".Trim(), Reason = reason,
            BeforeJson = JsonSerializer.Serialize(before), AfterJson = JsonSerializer.Serialize(Snapshot(line)),
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
