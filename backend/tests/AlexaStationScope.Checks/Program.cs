using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Features.POS;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

var grill = new OrderItem { Station = new WorkStation { Name = "Parrilla" }, Status = OrderItemStatus.Pending };
var fried = new OrderItem { Station = new WorkStation { Name = "Fritos" }, Status = OrderItemStatus.Pending };
var bar = new OrderItem { Station = new WorkStation { Name = "Bar" }, Status = OrderItemStatus.Pending };
var order = new Order { Items = [grill, fried, bar], Status = OrderStatus.Confirmed };
string[] kitchen = ["parrilla", "fritos"];

var kitchenItems = order.Items.Where(i => AlexaStationScope.Includes(i, kitchen)).ToList();
Check(kitchenItems.Count == 2 && !kitchenItems.Contains(bar), "Kitchen must only read grill and fried items.");
foreach (var item in kitchenItems) item.Status = OrderItemStatus.Ready;
KitchenOrderState.Recalculate(order);
Check(bar.Status == OrderItemStatus.Pending, "Whole kitchen order must leave bar pending.");
Check(order.Status != OrderStatus.Ready, "The full order must wait for bar.");

var barItems = order.Items.Where(i => AlexaStationScope.Includes(i, ["bar"])).ToList();
Check(barItems.Count == 1 && barItems[0] == bar, "Bar must only read bar items.");
barItems[0].Status = OrderItemStatus.Ready;
KitchenOrderState.Recalculate(order);
Check(order.Status == OrderStatus.Ready, "The order is ready when both areas finish.");
Check(!AlexaStationScope.Includes(bar, []), "Empty scope must not permit anything.");
Check(!AlexaStationScope.Includes(bar, ["unknown"]), "Unknown stations must not fall back to the full order.");
Check(!AlexaStationScope.Includes(bar, ["ba"]), "Scope must not use partial station matching.");
Check(!AlexaStationScope.Includes(new OrderItem(), kitchen), "Unassigned items must not enter a scoped skill.");
Check(AlexaStationScope.Includes(bar, null), "Legacy requests must remain compatible.");
Check(AlexaStationScope.Includes(grill, [" PARRILLA "]), "Station names must ignore case and surrounding whitespace.");
grill.Status = OrderItemStatus.Pending;
Check(AlexaStationScope.IsRepeatable(grill, kitchen), "Pending kitchen items must be repeated.");
grill.Status = OrderItemStatus.InPreparation;
Check(AlexaStationScope.IsRepeatable(grill, kitchen), "Items in preparation must be repeated.");
grill.Status = OrderItemStatus.Ready;
Check(!AlexaStationScope.IsRepeatable(grill, kitchen), "Ready items must never be repeated.");
Check(!AlexaStationScope.IsRepeatable(bar, kitchen), "Items outside the skill scope must never be repeated.");
Console.WriteLine("Alexa station scope checks passed.");
