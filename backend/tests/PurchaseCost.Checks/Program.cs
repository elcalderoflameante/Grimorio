using Grimorio.Infrastructure.Features.Purchases;

void Check(decimal billedQuantity, decimal receivedBaseQuantity, decimal billedUnitPrice,
    decimal discountAmount, decimal expectedTotal, decimal expectedUnitCost)
{
    var result = PurchaseCostCalculator.Calculate(billedQuantity, receivedBaseQuantity, billedUnitPrice, discountAmount);
    if (result.BaseQuantity != receivedBaseQuantity || result.NetCost != expectedTotal || result.UnitCost != expectedUnitCost)
        throw new InvalidOperationException($"Expected {receivedBaseQuantity} units at {expectedUnitCost}, got {result}.");
}

Check(1m, 12m, 24m, 0m, 24m, 2m);       // One billed case, twelve inventory bottles.
Check(2m, 24m, 24m, 0m, 48m, 2m);      // Two cases.
Check(1m, 12m, 24m, 6m, 18m, 1.5m);   // Line discount applies before unit cost.
Check(12m, 12m, 2m, 0m, 24m, 2m);      // Already billed by bottle.
Check(1m, 0m, 24m, 0m, 24m, 0m);       // Invalid conversion cannot divide by zero.

void CheckMovement(decimal billedQuantity, decimal receivedBaseQuantity, decimal billedUnitPrice,
    decimal discountAmount, decimal expectedQuantity, decimal expectedTotal, decimal expectedUnitCost)
{
    var result = PurchaseCostCalculator.CalculateMovement(billedQuantity, receivedBaseQuantity, billedUnitPrice, discountAmount);
    if (result.BaseQuantity != expectedQuantity || result.TotalCost != expectedTotal || result.UnitCost != expectedUnitCost)
        throw new InvalidOperationException($"Incorrect persisted purchase cost: {result}.");
}

CheckMovement(1m, 12m, 24m, 0m, 12m, 24m, 2m);
CheckMovement(2m, 24m, 24m, 0m, 24m, 48m, 2m);
CheckMovement(1m, 12m, 24m, 6m, 12m, 18m, 1.5m);
CheckMovement(1m, 1000m, 24m, 0m, 1000m, 24m, 0.024m); // Received kg expressed as base grams.
CheckMovement(1m, 3m, 10m, 0m, 3m, 10m, 3.3333m);      // Preserve total despite unit-cost rounding.
CheckMovement(1m, 12m, 24m, 24m, 12m, 0m, 0m);        // Fully discounted line.
CheckMovement(1m, 1.12345m, 10m, 0m, 1.1235m, 10m, 8.9008m);

void CheckInvalidMovement(decimal receivedBaseQuantity, decimal discountAmount)
{
    try
    {
        PurchaseCostCalculator.CalculateMovement(1m, receivedBaseQuantity, 24m, discountAmount);
    }
    catch (InvalidOperationException) { return; }
    throw new InvalidOperationException("Invalid movement was accepted.");
}
CheckInvalidMovement(0m, 0m);
CheckInvalidMovement(0.00001m, 0m);
CheckInvalidMovement(12m, 25m);
Console.WriteLine("15 purchase cost checks passed.");
