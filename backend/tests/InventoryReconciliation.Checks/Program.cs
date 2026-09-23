using Grimorio.Infrastructure.Features.Inventory;

var checks = 0;
void Check(bool condition, string description)
{
    if (!condition) throw new InvalidOperationException(description);
    checks++;
}

Check(!InventoryReconciliationRules.Differs(10m, 10m), "Equal balances");
Check(InventoryReconciliationRules.Differs(10m, 9m), "Stale projection");
Check(InventoryReconciliationRules.Differs(0m, 0.0001m), "Small persisted difference");
Check(!InventoryReconciliationRules.Differs(0.12344m, 0.1234m), "Numeric precision");
Check(!InventoryReconciliationRules.Differs(0.12345m, 0.1235m), "Positive midpoint");
Check(!InventoryReconciliationRules.Differs(-0.12345m, -0.1235m), "Negative midpoint");
Check(!InventoryReconciliationRules.HasInvalidReservation(false, false, false, false, false, 4, 2), "Partial payment preserves remaining reservations");
Check(!InventoryReconciliationRules.HasInvalidReservation(false, false, false, false, false, 4, 0), "Open order");
Check(InventoryReconciliationRules.HasInvalidReservation(false, false, false, false, false, 4, 4), "Fully paid item");
Check(InventoryReconciliationRules.HasInvalidReservation(false, false, false, false, false, 4, 5), "Overpaid quantity");
for (var flag = 0; flag < 5; flag++)
    Check(InventoryReconciliationRules.HasInvalidReservation(flag == 0, flag == 1, flag == 2, flag == 3, flag == 4, 4, 0), $"Invalid reservation flag {flag}");
Console.WriteLine($"{checks} inventory reconciliation checks passed.");
