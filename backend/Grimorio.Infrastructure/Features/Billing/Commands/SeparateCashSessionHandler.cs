using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Application.Features.Billing.Queries;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.Finance;
using Grimorio.Infrastructure.Persistence;
using Grimorio.SharedKernel.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Billing.Commands;

public class SeparateCashSessionHandler(GrimorioDbContext db) : IRequestHandler<SeparateCashSessionCommand, CashSessionSeparationResultDto>
{
    public async Task<CashSessionSeparationResultDto> Handle(SeparateCashSessionCommand req, CancellationToken ct)
    {
        var actor = await CashSessionSeparationWorkflow.Authorize(db, req.BranchId, req.ActorUserId, ct);
        CashSessionSeparationWorkflow.Validate(req.Data);
        var requestHash = CashSessionSeparationWorkflow.Hash(new { req.SessionId, req.ActorUserId, req.Data });
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // All cash writers take this same session lock before saving their changes.
        // Do not lock orders here: payment handlers lock order first, then session.
        var session = await db.CashSessions.FromSqlInterpolated($"""
            SELECT * FROM billing."CashSessions" WHERE "Id" = {req.SessionId}
            AND "BranchId" = {req.BranchId} AND NOT "IsDeleted" FOR UPDATE
            """).SingleOrDefaultAsync(ct) ?? throw new KeyNotFoundException("Sesion de caja no encontrada.");
        await db.Entry(session).ReloadAsync(ct);
        var applied = await db.CashSessionSeparations.AsNoTracking()
            .SingleOrDefaultAsync(s => s.BranchId == req.BranchId && s.RequestId == req.Data.RequestId, ct);
        if (applied != null)
        {
            if (applied.RequestHash != requestHash)
                throw new InvalidOperationException("Este intento ya se uso con otros datos. Revisa la caja.");
            await tx.CommitAsync(ct);
            return Result(applied);
        }
        var state = await CashSessionSeparationWorkflow.Load(db, session, req.Data, ct);
        var preview = CashSessionSeparationWorkflow.Preview(state, req.Data);
        if (!string.Equals(req.Data.PreviewToken, preview.Token, StringComparison.Ordinal))
            throw new InvalidOperationException("Los movimientos o los datos del corte cambiaron. Vuelve a revisar la vista previa.");
        var now = DateTime.UtcNow;
        var next = new CashSession {
            BranchId = session.BranchId, CashRegisterId = session.CashRegisterId,
            OpenedBy = session.OpenedBy, OpenedByName = session.OpenedByName,
            OpenedAt = preview.CutoffUtc, OpeningBalance = req.Data.NewOpeningBalance, Status = CashSessionStatus.Open,
        };
        session.Status = CashSessionStatus.Closed;
        session.ClosedAt = preview.CutoffUtc;
        session.ClosedBy = req.ActorUserId;
        session.ClosedByName = actor[..Math.Min(actor.Length, 150)];
        session.ActualCash = req.Data.PreviousActualCash;
        session.CloseNotes = req.Data.Reason.Trim();
        db.CashSessions.Add(next);
        foreach (var payment in state.Payments.Where(p => p.PaidAt >= preview.CutoffUtc))
            payment.CashSessionId = next.Id;
        foreach (var expense in state.Expenses.Where(e => e.RegisteredAt >= preview.CutoffUtc))
            expense.CashSessionId = next.Id;
        var audit = new CashSessionSeparation {
            BranchId = req.BranchId, PreviousSessionId = session.Id, NewSessionId = next.Id,
            RequestId = req.Data.RequestId, RequestHash = requestHash, CutoffUtc = preview.CutoffUtc,
            ActorUserId = req.ActorUserId, ActorName = actor, Reason = req.Data.Reason.Trim(),
            CreatedAt = now, CreatedBy = req.ActorUserId, PreviewJson = JsonSerializer.Serialize(preview),
        };
        preview.NewSession.Id = next.Id;
        preview.PreviousSession.ClosedByName = session.ClosedByName;
        audit.PreviewJson = JsonSerializer.Serialize(preview);
        db.CashSessionSeparations.Add(audit);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Result(audit);
    }

    private static CashSessionSeparationResultDto Result(CashSessionSeparation audit) => new() {
        Id = audit.Id, PreviousSessionId = audit.PreviousSessionId, NewSessionId = audit.NewSessionId,
    };
}

public class PreviewCashSessionSeparationHandler(GrimorioDbContext db) : IRequestHandler<PreviewCashSessionSeparationQuery, CashSessionSeparationPreviewDto>
{
    public async Task<CashSessionSeparationPreviewDto> Handle(PreviewCashSessionSeparationQuery req, CancellationToken ct)
    {
        await CashSessionSeparationWorkflow.Authorize(db, req.BranchId, req.ActorUserId, ct);
        CashSessionSeparationWorkflow.Validate(req.Data);
        var session = await db.CashSessions.SingleOrDefaultAsync(s => s.Id == req.SessionId
            && s.BranchId == req.BranchId && !s.IsDeleted, ct) ?? throw new KeyNotFoundException("Sesion de caja no encontrada.");
        return CashSessionSeparationWorkflow.Preview(await CashSessionSeparationWorkflow.Load(db, session, req.Data, ct), req.Data);
    }
}

public class GetCashSessionSeparationsHandler(GrimorioDbContext db) : IRequestHandler<GetCashSessionSeparationsQuery, List<CashSessionSeparationRecordDto>>
{
    public async Task<List<CashSessionSeparationRecordDto>> Handle(GetCashSessionSeparationsQuery req, CancellationToken ct)
    {
        var audits = await db.CashSessionSeparations.AsNoTracking().Where(s => s.BranchId == req.BranchId
            && (s.PreviousSessionId == req.SessionId || s.NewSessionId == req.SessionId)).OrderBy(s => s.CreatedAt).ToListAsync(ct);
        return audits.Select(s => new CashSessionSeparationRecordDto {
            Id = s.Id, PreviousSessionId = s.PreviousSessionId, NewSessionId = s.NewSessionId,
            CutoffUtc = s.CutoffUtc, RecordedAt = s.CreatedAt, ActorName = s.ActorName, Reason = s.Reason,
            Preview = JsonSerializer.Deserialize<CashSessionSeparationPreviewDto>(s.PreviewJson)!,
        }).ToList();
    }
}

internal static class CashSessionSeparationWorkflow
{
    internal sealed record State(CashSession Session, List<OrderPayment> Payments, List<Expense> Expenses);

    internal static async Task<string> Authorize(GrimorioDbContext db, Guid branchId, Guid actorId, CancellationToken ct) =>
        await db.UserRoles.Where(r => r.BranchId == branchId && r.UserId == actorId
            && r.User != null && r.User.IsActive && r.User.BranchId == branchId
            && r.Role != null && r.Role.IsActive && r.Role.BranchId == branchId
            && (r.Role.Name == AppConstants.Roles.Admin || r.Role.RolePermissions.Any(p => p.BranchId == branchId
                && p.Permission != null && p.Permission.IsActive && p.Permission.BranchId == branchId
                && p.Permission.Code == AppConstants.Permissions.BillingCashSeparate)))
            .Select(r => r.User!.FirstName + " " + r.User.LastName).FirstOrDefaultAsync(ct)
        ?? throw new InvalidOperationException("No tienes un rol activo con permiso para separar jornadas de caja.");

    internal static void Validate(SeparateCashSessionDto data)
    {
        bool Money(decimal amount) => amount >= 0 && amount <= 999999999.99m && Math.Round(amount, 2) == amount;
        if (data.RequestId == Guid.Empty || data.Reason?.Trim().Length is not (>= 5 and <= 500)
            || !Money(data.NewOpeningBalance) || (data.PreviousActualCash.HasValue && !Money(data.PreviousActualCash.Value)))
            throw new InvalidOperationException("Indica un motivo de 5 a 500 caracteres e importes no negativos de hasta dos decimales.");
    }

    internal static async Task<State> Load(GrimorioDbContext db, CashSession session, SeparateCashSessionDto data, CancellationToken ct)
    {
        if (session.Status != CashSessionStatus.Open)
            throw new InvalidOperationException("La sesion ya esta cerrada. Actualiza la caja.");
        if (data.Cutoff.UtcDateTime <= session.OpenedAt || data.Cutoff.UtcDateTime > DateTime.UtcNow)
            throw new InvalidOperationException("El corte debe ser posterior a la apertura y no puede estar en el futuro.");
        if (await db.CashSessions.AnyAsync(s => s.BranchId == session.BranchId && !s.IsDeleted
            && s.Status == CashSessionStatus.Open && s.Id != session.Id
            && (s.CashRegisterId == session.CashRegisterId || s.OpenedBy == session.OpenedBy), ct))
            throw new InvalidOperationException("Existe otra caja abierta para el mismo cajero o caja. Requiere revision.");
        await db.Entry(session).Reference(s => s.CashRegister).LoadAsync(ct);
        var payments = await db.OrderPayments.Where(p => p.BranchId == session.BranchId && p.CashSessionId == session.Id && !p.IsDeleted)
            .Include(p => p.Order).Include(p => p.Lines.Where(l => !l.IsDeleted)).ThenInclude(l => l.Config)
            .OrderBy(p => p.Id).ToListAsync(ct);
        var expenses = await db.Expenses.Where(e => e.BranchId == session.BranchId && e.CashSessionId == session.Id && !e.IsDeleted)
            .Include(e => e.PaymentMethodConfig).OrderBy(e => e.Id).ToListAsync(ct);
        if (!payments.Any(p => p.PaidAt >= data.Cutoff.UtcDateTime) && !expenses.Any(e => e.RegisteredAt >= data.Cutoff.UtcDateTime))
            throw new InvalidOperationException("No hay movimientos desde el corte para trasladar. Usa el cierre normal.");
        if (payments.Any(p => p.Lines.Any(l => l.Config == null) || p.Lines.Sum(l => l.AmountTendered - l.Change) != p.OrderAmount))
            throw new InvalidOperationException("Hay cobros cuyos medios no cuadran con el total. Revisa las ventas antes de separar.");
        return new State(session, payments, expenses);
    }

    internal static CashSessionSeparationPreviewDto Preview(State state, SeparateCashSessionDto data)
    {
        var cutoff = data.Cutoff.UtcDateTime;
        var session = state.Session;
        var previous = new CashSession {
            Id = session.Id, CashRegisterId = session.CashRegisterId, CashRegister = session.CashRegister,
            OpenedByName = session.OpenedByName, OpenedAt = session.OpenedAt, OpeningBalance = session.OpeningBalance,
            ClosedAt = cutoff, ActualCash = data.PreviousActualCash, Status = CashSessionStatus.Closed,
        };
        var next = new CashSession {
            CashRegisterId = session.CashRegisterId, CashRegister = session.CashRegister,
            OpenedByName = session.OpenedByName, OpenedAt = cutoff, OpeningBalance = data.NewOpeningBalance,
            Status = CashSessionStatus.Open,
        };
        var preview = new CashSessionSeparationPreviewDto {
            CutoffUtc = cutoff,
            PreviousSession = BillingMapper.MapSession(previous, state.Payments.Where(p => p.PaidAt < cutoff).ToList(),
                state.Expenses.Where(e => e.RegisteredAt < cutoff && e.Status == ExpenseStatus.Registered).ToList()),
            NewSession = BillingMapper.MapSession(next, state.Payments.Where(p => p.PaidAt >= cutoff).ToList(),
                state.Expenses.Where(e => e.RegisteredAt >= cutoff && e.Status == ExpenseStatus.Registered).ToList()),
            Movements = state.Payments.Select(p => new CashSessionSeparationMovementDto {
                Id = p.Id, Kind = "Payment", Reference = p.Order != null ? $"Orden #{p.Order.Number}" : $"Cobro {p.Id.ToString()[..8]}", Date = p.PaidAt,
                Amount = p.OrderAmount, MovesToNewSession = p.PaidAt >= cutoff,
            }).Concat(state.Expenses.Select(e => new CashSessionSeparationMovementDto {
                Id = e.Id, Kind = "Expense", Reference = e.DocumentNumber ?? e.SupplierName ?? "Gasto",
                Date = e.RegisteredAt, Amount = e.Amount, IsCancelled = e.Status == ExpenseStatus.Cancelled,
                MovesToNewSession = e.RegisteredAt >= cutoff,
            })).OrderBy(m => m.Date).ThenBy(m => m.Id).ToList(),
        };
        // Hash exact ledger contents, not just counts/totals: same-value edits must invalidate a preview too.
        preview.Token = Hash(new {
            session.Id, session.UpdatedAt, session.OpeningBalance, session.OpenedAt, session.OpenedBy,
            Cutoff = cutoff, data.NewOpeningBalance, data.PreviousActualCash, Reason = data.Reason.Trim(),
            Payments = state.Payments.Select(p => new { p.Id, p.OrderId, p.PaidAt, p.OrderAmount, p.UpdatedAt,
                Lines = p.Lines.OrderBy(l => l.Id).Select(l => new { l.Id, l.PaymentMethodConfigId, l.AmountTendered, l.Change,
                    l.UpdatedAt, l.Config!.IsCash, l.CardPaymentType, l.CardBankId, l.CardBrand, l.AuthorizationNumber }) }),
            Expenses = state.Expenses.Select(e => new { e.Id, e.RegisteredAt, e.Amount, e.Status, e.UpdatedAt,
                e.PaymentMethodConfigId, IsCash = e.PaymentMethodConfig?.IsCash }),
        });
        return preview;
    }

    internal static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));
}
