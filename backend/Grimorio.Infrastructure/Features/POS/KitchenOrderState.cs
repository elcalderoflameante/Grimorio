using Grimorio.Domain.Entities.POS;

namespace Grimorio.Infrastructure.Features.POS;

internal static class KitchenOrderState
{
    public static bool CanAdvance(OrderItemStatus current, OrderItemStatus target) => target switch
    {
        OrderItemStatus.InPreparation => current == OrderItemStatus.Pending,
        OrderItemStatus.Ready => current is OrderItemStatus.Pending or OrderItemStatus.InPreparation,
        _ => false,
    };

    public static void Validate(Order order, OrderItem item, OrderItemStatus target)
    {
        if (target is not (OrderItemStatus.InPreparation or OrderItemStatus.Ready))
            throw new InvalidOperationException("Cocina solo permite marcar en preparacion o listo. Para cancelar utiliza la opcion de cancelacion del pedido.");
        if (order.IsDeleted || order.Status is not (OrderStatus.Confirmed or OrderStatus.InPreparation or OrderStatus.Ready))
            throw new InvalidOperationException("La orden no admite cambios de cocina en su estado actual.");
        if (item.IsDeleted || item.Status == OrderItemStatus.Cancelled)
            throw new InvalidOperationException("No se puede preparar un plato eliminado o cancelado.");

        // Repeating the same valid state is harmless after a lost response.
        if (item.Status != target && !CanAdvance(item.Status, target))
            throw new InvalidOperationException("No se puede retroceder el estado del plato.");
    }

    public static void Recalculate(Order order)
    {
        var active = order.Items.Where(i => !i.IsDeleted && i.Status != OrderItemStatus.Cancelled).ToList();
        if (active.Count == 0) return;
        order.Status = active.All(i => i.Status == OrderItemStatus.Ready)
            ? OrderStatus.Ready
            : active.Any(i => i.Status == OrderItemStatus.InPreparation)
                ? OrderStatus.InPreparation
                : OrderStatus.Confirmed;
    }
}
