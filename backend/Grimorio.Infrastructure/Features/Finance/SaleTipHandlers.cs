using Grimorio.Application.Features.Finance;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.Finance;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Finance;

public class SaleTipHandlers(GrimorioDbContext db) :
    IRequestHandler<GetSaleTipQuery, SaleTipDto?>,
    IRequestHandler<SaveSaleTipCommand, SaleTipDto>,
    IRequestHandler<DeleteSaleTipCommand, bool>,
    IRequestHandler<GetSaleTipReportQuery, SaleTipReportDto>
{
    private static TipMethod ParseMethod(string value)
    {
        if (Enum.TryParse<TipMethod>(value, false, out var method) && Enum.IsDefined(method) && method.ToString() == value)
            return method;
        throw new InvalidOperationException("Selecciona efectivo, tarjeta o transferencia.");
    }

    private IQueryable<SaleTip> Tips(Guid branchId) => db.SaleTips.AsNoTracking()
        .Where(t => t.BranchId == branchId && t.OrderPayment != null && !t.OrderPayment.IsDeleted && t.OrderPayment.BranchId == branchId);

    private static IQueryable<SaleTipDto> Map(IQueryable<SaleTip> query) => query.Select(t => new SaleTipDto(
        t.Id, t.OrderPaymentId, t.OrderPayment!.Order!.Number, t.OrderPayment.PaidAt, t.RegisteredAt,
        t.Amount, t.Method.ToString(), t.RegisteredByName, t.UpdatedByName));

    public async Task<SaleTipDto?> Handle(GetSaleTipQuery req, CancellationToken ct)
    {
        if (!await db.OrderPayments.AnyAsync(p => p.Id == req.OrderPaymentId && p.BranchId == req.BranchId, ct))
            throw new KeyNotFoundException("Venta no encontrada.");
        return await Map(Tips(req.BranchId).Where(t => t.OrderPaymentId == req.OrderPaymentId)).SingleOrDefaultAsync(ct);
    }

    private async Task<OrderPayment> LockPayment(Guid branchId, Guid paymentId, CancellationToken ct) =>
        await db.OrderPayments.FromSqlInterpolated($"""
            SELECT * FROM billing."OrderPayments"
            WHERE "Id" = {paymentId} AND "BranchId" = {branchId} AND "IsDeleted" = false
            FOR UPDATE
            """).SingleOrDefaultAsync(ct) ?? throw new KeyNotFoundException("Venta no encontrada.");

    public async Task<SaleTipDto> Handle(SaveSaleTipCommand req, CancellationToken ct)
    {
        if (req.Amount <= 0 || req.Amount > 99999999.99m || decimal.Round(req.Amount, 2) != req.Amount)
            throw new InvalidOperationException("La propina debe ser mayor a cero, con un maximo de dos decimales.");
        var method = ParseMethod(req.Method);
        if (string.IsNullOrWhiteSpace(req.UserName) || req.UserName.Length > 150)
            throw new InvalidOperationException("Usuario no valido.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Serialize saves on the sale so retries cannot create duplicate tips.
        await LockPayment(req.BranchId, req.OrderPaymentId, ct);
        var tip = await db.SaleTips.SingleOrDefaultAsync(t => t.BranchId == req.BranchId && t.OrderPaymentId == req.OrderPaymentId, ct);
        if (tip == null)
        {
            tip = new SaleTip { BranchId = req.BranchId, OrderPaymentId = req.OrderPaymentId, RegisteredByName = req.UserName.Trim() };
            db.SaleTips.Add(tip);
        }
        else if (tip.Amount != req.Amount || tip.Method != method)
            tip.UpdatedByName = req.UserName.Trim();
        tip.Amount = req.Amount;
        tip.Method = method;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return (await Map(Tips(req.BranchId).Where(t => t.Id == tip.Id)).SingleAsync(ct));
    }

    public async Task<bool> Handle(DeleteSaleTipCommand req, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockPayment(req.BranchId, req.OrderPaymentId, ct);
        var tip = await db.SaleTips.SingleOrDefaultAsync(t => t.BranchId == req.BranchId && t.OrderPaymentId == req.OrderPaymentId, ct);
        if (tip != null) db.SaleTips.Remove(tip);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task<SaleTipReportDto> Handle(GetSaleTipReportQuery req, CancellationToken ct)
    {
        if (req.FromUtc > req.ToUtc) throw new InvalidOperationException("El rango de fechas no es valido.");
        var query = Tips(req.BranchId);
        if (req.FromUtc.HasValue) query = query.Where(t => t.RegisteredAt >= req.FromUtc.Value);
        if (req.ToUtc.HasValue) query = query.Where(t => t.RegisteredAt <= req.ToUtc.Value);
        if (!string.IsNullOrEmpty(req.Method))
        {
            var method = ParseMethod(req.Method);
            query = query.Where(t => t.Method == method);
        }
        // Totals cover the entire filter, not just the visible page.
        var groups = await query.GroupBy(t => t.Method)
            .Select(g => new { Method = g.Key, Amount = g.Sum(t => t.Amount), Count = g.Count() }).ToListAsync(ct);
        var page = Math.Clamp(req.Page, 1, 1000000);
        var pageSize = Math.Clamp(req.PageSize, 1, 100);
        var items = await Map(query.OrderByDescending(t => t.RegisteredAt).ThenBy(t => t.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)).ToListAsync(ct);
        return new SaleTipReportDto(groups.Sum(g => g.Amount), groups.Sum(g => g.Count),
            groups.Select(g => new TipMethodTotalDto(g.Method.ToString(), g.Amount, g.Count)).ToList(), items, page, pageSize);
    }
}
