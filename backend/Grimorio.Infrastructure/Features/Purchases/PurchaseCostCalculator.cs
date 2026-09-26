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

    public static (decimal BaseQuantity, decimal UnitCost, decimal TotalCost) CalculateMovement(
        decimal billedQuantity, decimal inventoryBaseQuantity, decimal billedUnitPrice, decimal discountAmount)
    {
        var quantity = Math.Round(inventoryBaseQuantity, 4, MidpointRounding.AwayFromZero);
        if (quantity <= 0)
            throw new InvalidOperationException("La cantidad de ingreso en unidad base debe ser mayor a cero.");

        var cost = Calculate(billedQuantity, quantity, billedUnitPrice, discountAmount);
        if (cost.NetCost < 0)
            throw new InvalidOperationException("El costo de la linea de compra no puede ser negativo.");

        var total = Math.Round(cost.NetCost, 4, MidpointRounding.AwayFromZero);
        return (quantity, Math.Round(total / quantity, 4, MidpointRounding.AwayFromZero), total);
    }
}
