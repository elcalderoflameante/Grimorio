using Grimorio.Application.DTOs;
using Grimorio.Application.Features.POS.Commands;
using Grimorio.Application.Features.POS.Queries;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.POS.Commands;

public class RecalculateOrderPromotionsHandler(GrimorioDbContext db, IMediator mediator)
    : IRequestHandler<RecalculateOrderPromotionsCommand, OrderDto>
{
    public async Task<OrderDto> Handle(RecalculateOrderPromotionsCommand req, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await PosOrderLock.AcquireAsync(db, req.BranchId, req.OrderId, ct);
        var order = await db.Orders.Include(o => o.Items.Where(i => !i.IsDeleted))
            .SingleOrDefaultAsync(o => o.Id == req.OrderId && o.BranchId == req.BranchId && !o.IsDeleted, ct)
            ?? throw new InvalidOperationException("Pedido no encontrado.");
        if (order.PaidAt.HasValue || order.Status == OrderStatus.Cancelled
            || await db.OrderPayments.AnyAsync(p => p.OrderId == order.Id && !p.IsDeleted, ct))
            throw new InvalidOperationException("Solo se pueden recalcular promociones de pedidos sin cobros.");
        await SecondUnitPromotionGroups.RecalculateAsync(db, order, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return (await mediator.Send(new GetOrderDetailQuery { BranchId = req.BranchId, OrderId = req.OrderId }, ct))!;
    }
}
