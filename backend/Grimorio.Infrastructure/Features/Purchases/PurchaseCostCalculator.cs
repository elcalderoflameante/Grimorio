namespace Grimorio.Infrastructure.Features.Purchases;

public static class PurchaseCostCalculator
{
    public static (decimal BaseQuantity, decimal NetCost, decimal UnitCost) Calculate(
        decimal billedQuantity, decimal inventoryBaseQuantity, decimal billedUnitPrice, decimal discountAmount)
    {
        var netCost = billedUnitPrice * billedQuantity - discountAmount;
        var unitCost = inventoryBaseQuantity > 0 ? netCost / inventoryBaseQuantity : 0m;
        return (inventoryBaseQuantity, netCost, unitCost);
    }
}
