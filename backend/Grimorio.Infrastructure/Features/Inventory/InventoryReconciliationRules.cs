namespace Grimorio.Infrastructure.Features.Inventory;

public static class InventoryReconciliationRules
{
    // Match the persisted numeric(18,4) quantities, not floating-point tolerances.
    public static decimal Normalize(decimal quantity) => Math.Round(quantity, 4, MidpointRounding.AwayFromZero);
    public static bool Differs(decimal expected, decimal actual) => Normalize(expected) != Normalize(actual);

    public static bool HasInvalidReservation(bool orderMissingOrDeleted, bool orderCancelled,
        bool orderPaid, bool itemMissingOrDeleted, bool itemCancelled, decimal ordered, decimal paid) =>
        orderMissingOrDeleted || orderCancelled || orderPaid || itemMissingOrDeleted || itemCancelled || paid >= ordered;
}
