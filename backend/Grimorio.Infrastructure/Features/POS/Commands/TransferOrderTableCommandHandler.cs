using Grimorio.Application.DTOs;
using Grimorio.Application.Features.POS.Commands;
using Grimorio.Application.Features.POS.Queries;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.POS.Commands;

public class TransferOrderTableCommandHandler(GrimorioDbContext db, IMediator mediator) : IRequestHandler<TransferOrderTableCommand, OrderDto>
{
    public async Task<OrderDto> Handle(TransferOrderTableCommand req, CancellationToken ct)
    {
        var data = req.Data;
        if (data.RequestId == Guid.Empty || data.FromTableId == Guid.Empty || data.ToTableId == Guid.Empty
            || data.FromTableId == data.ToTableId || req.ActorUserId == Guid.Empty)
            throw new InvalidOperationException("Selecciona una mesa de destino diferente.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Share the creation lock with staff and QR orders so a free table cannot be claimed twice.
        await PosOrderCreationLock.AcquireAsync(db, req.BranchId, ct);
        await PosOrderLock.AcquireAsync(db, req.BranchId, req.OrderId, ct);
        var applied = await db.OrderTableTransfers.SingleOrDefaultAsync(t => t.BranchId == req.BranchId && t.RequestId == data.RequestId, ct);
        if (applied != null)
        {
            if (applied.OrderId != req.OrderId || applied.FromTableId != data.FromTableId || applied.ToTableId != data.ToTableId)
                throw new InvalidOperationException("Este traslado ya se utilizo con otros datos. Actualiza el pedido.");
            await tx.CommitAsync(ct);
            return (await mediator.Send(new GetOrderDetailQuery { BranchId = req.BranchId, OrderId = req.OrderId }, ct))!;
        }
        var order = await db.Orders.SingleAsync(o => o.Id == req.OrderId && o.BranchId == req.BranchId && !o.IsDeleted, ct);
        if (order.Type != OrderType.DineIn || !order.TableId.HasValue || order.PaidAt.HasValue || order.Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("Solo se pueden trasladar pedidos de mesa abiertos y sin pagar por completo.");
        if (order.TableId != data.FromTableId)
            throw new InvalidOperationException("El pedido ya cambio de mesa. Actualizalo antes de continuar.");
        var target = await db.RestaurantTables.FromSqlInterpolated($"""
            SELECT * FROM pos."RestaurantTables" WHERE "Id" = {data.ToTableId}
            AND "BranchId" = {req.BranchId} AND NOT "IsDeleted" FOR UPDATE
            """).AsNoTracking().SingleOrDefaultAsync(ct);
        if (target == null || !target.IsActive)
            throw new InvalidOperationException("La mesa de destino no esta disponible en esta sucursal.");
        if (await db.Orders.AnyAsync(o => o.BranchId == req.BranchId && o.TableId == target.Id
            && !o.IsDeleted && o.PaidAt == null && o.Status != OrderStatus.Cancelled, ct))
            throw new InvalidOperationException("La mesa de destino ya esta ocupada. Selecciona otra mesa.");
        order.TableId = target.Id;
        if (order.Status == OrderStatus.Draft && order.PublicSubmissionId.HasValue)
        {
            var notifications = await db.TableServiceRequests.Where(r => r.BranchId == req.BranchId
                && r.RestaurantTableId == data.FromTableId && !r.IsDeleted
                && r.CustomMessage == $"Pedido #{order.Number} pendiente de confirmar"
                && r.Status != TableServiceRequestStatus.Completed && r.Status != TableServiceRequestStatus.Cancelled).ToListAsync(ct);
            foreach (var notification in notifications) notification.RestaurantTableId = target.Id;
        }
        db.OrderTableTransfers.Add(new() { BranchId = req.BranchId, OrderId = order.Id,
            RequestId = data.RequestId, FromTableId = data.FromTableId, ToTableId = target.Id, ActorUserId = req.ActorUserId });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return (await mediator.Send(new GetOrderDetailQuery { BranchId = req.BranchId, OrderId = order.Id }, ct))!;
    }
}
