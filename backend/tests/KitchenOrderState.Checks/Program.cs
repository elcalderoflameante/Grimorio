using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Features.POS;

var checks = 0;
static bool Accepts(Order order, OrderItem item, OrderItemStatus target)
{
    try { KitchenOrderState.Validate(order, item, target); return true; }
    catch (InvalidOperationException) { return false; }
}
void Check(bool condition, string scenario)
{
    if (!condition) throw new Exception(scenario);
    checks++;
}

foreach (var orderStatus in Enum.GetValues<OrderStatus>())
foreach (var current in Enum.GetValues<OrderItemStatus>())
foreach (var target in Enum.GetValues<OrderItemStatus>().Append((OrderItemStatus)99))
{
    var order = new Order { Status = orderStatus };
    var item = new OrderItem { Status = current };
    var validOrder = orderStatus is OrderStatus.Confirmed or OrderStatus.InPreparation or OrderStatus.Ready;
    var validTransition = (current, target) switch
    {
        (OrderItemStatus.Pending, OrderItemStatus.InPreparation or OrderItemStatus.Ready) => true,
        (OrderItemStatus.InPreparation, OrderItemStatus.InPreparation or OrderItemStatus.Ready) => true,
        (OrderItemStatus.Ready, OrderItemStatus.Ready) => true,
        _ => false,
    };
    Check(Accepts(order, item, target) == (validOrder && validTransition), $"{orderStatus}: {current} -> {target}");
}

var prepaid = new Order { Status = OrderStatus.Confirmed, PaidAt = DateTime.UtcNow };
var pending = new OrderItem { Status = OrderItemStatus.Pending };
Check(Accepts(prepaid, pending, OrderItemStatus.InPreparation), "Prepaid items still need preparation.");
pending.IsDeleted = true;
Check(!Accepts(prepaid, pending, OrderItemStatus.Ready), "Deleted item cannot be prepared.");
pending.IsDeleted = false;
prepaid.IsDeleted = true;
Check(!Accepts(prepaid, pending, OrderItemStatus.Ready), "Deleted order cannot be prepared.");

var active = new OrderItem { Status = OrderItemStatus.InPreparation };
var other = new OrderItem { Status = OrderItemStatus.Ready };
var cancelled = new OrderItem { Status = OrderItemStatus.Cancelled };
var aggregate = new Order { Status = OrderStatus.Confirmed, Items = [active, other, cancelled] };
KitchenOrderState.Recalculate(aggregate);
Check(aggregate.Status == OrderStatus.InPreparation, "Order is preparing while one item is preparing.");
active.Status = OrderItemStatus.Ready;
KitchenOrderState.Recalculate(aggregate);
Check(aggregate.Status == OrderStatus.Ready, "Last ready item moves the order to Ready, ignoring cancelled items.");
other.Status = OrderItemStatus.Pending;
KitchenOrderState.Recalculate(aggregate);
Check(aggregate.Status == OrderStatus.Confirmed, "Pending items prevent an all-ready order.");
Console.WriteLine($"Kitchen order state checks passed: {checks}.");
