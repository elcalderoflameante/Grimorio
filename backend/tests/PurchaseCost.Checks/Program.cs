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
Console.WriteLine("5 purchase cost checks passed.");
