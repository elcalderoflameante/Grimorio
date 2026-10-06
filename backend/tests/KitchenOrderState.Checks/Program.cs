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

var grill = new OrderItem { Station = new WorkStation { Name = "Parrilla" }, Status = OrderItemStatus.Pending };
var fried = new OrderItem { Station = new WorkStation { Name = "Fritos" }, Status = OrderItemStatus.Pending };
var bar = new OrderItem { Station = new WorkStation { Name = "Bar" }, Status = OrderItemStatus.Pending };
var mixedOrder = new Order { Items = [grill, fried, bar], Status = OrderStatus.Confirmed };
string[] kitchenStations = ["parrilla", "fritos"];

var kitchenItems = mixedOrder.Items.Where(i => AlexaStationScope.Includes(i, kitchenStations)).ToList();
Check(kitchenItems.Count == 2 && !kitchenItems.Contains(bar), "Kitchen scope only includes grill and fried items.");
foreach (var item in kitchenItems) item.Status = OrderItemStatus.Ready;
KitchenOrderState.Recalculate(mixedOrder);
Check(bar.Status == OrderItemStatus.Pending, "Whole kitchen command leaves bar pending.");
Check(mixedOrder.Status != OrderStatus.Ready, "Mixed order waits for bar.");

var barItems = mixedOrder.Items.Where(i => AlexaStationScope.Includes(i, ["bar"])).ToList();
Check(barItems.Count == 1 && barItems[0] == bar, "Bar scope only includes bar items.");
barItems[0].Status = OrderItemStatus.Ready;
KitchenOrderState.Recalculate(mixedOrder);
Check(mixedOrder.Status == OrderStatus.Ready, "Mixed order is ready after both areas finish.");
Check(!AlexaStationScope.Includes(bar, []), "Empty Alexa scope permits nothing.");
Check(!AlexaStationScope.Includes(bar, ["unknown"]), "Unknown Alexa scope does not fall back to all items.");
Check(!AlexaStationScope.Includes(bar, ["ba"]), "Alexa scope does not use partial station matching.");
Check(!AlexaStationScope.Includes(new OrderItem(), kitchenStations), "Unassigned items are excluded from scoped skills.");
Check(AlexaStationScope.Includes(bar, null), "Legacy Alexa requests keep full scope.");
Check(AlexaStationScope.Includes(grill, [" PARRILLA "]), "Alexa station names ignore case and whitespace.");
grill.Status = OrderItemStatus.Pending;
Check(AlexaStationScope.IsRepeatable(grill, kitchenStations), "Alexa repeats pending kitchen items.");
grill.Status = OrderItemStatus.InPreparation;
Check(AlexaStationScope.IsRepeatable(grill, kitchenStations), "Alexa repeats items in preparation.");
grill.Status = OrderItemStatus.Ready;
Check(!AlexaStationScope.IsRepeatable(grill, kitchenStations), "Alexa does not repeat ready items.");
Check(!AlexaStationScope.IsRepeatable(bar, kitchenStations), "Alexa does not repeat items outside its scope.");
Console.WriteLine($"Kitchen order state checks passed: {checks}.");
