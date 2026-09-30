namespace Grimorio.Infrastructure.Features.Purchases;

public static class PurchaseCostCalculator
{
    public static (decimal BaseQuantity, decimal NetCost, decimal UnitCost) Calculate(
        decimal billedQuantity, decimal inventoryBaseQuantity, decimal billedUnitPrice, decimal discountAmount,
        decimal allocatedCost = 0m)
    {
        var netCost = billedUnitPrice * billedQuantity - discountAmount + allocatedCost;
        var unitCost = inventoryBaseQuantity > 0 ? netCost / inventoryBaseQuantity : 0m;
        return (inventoryBaseQuantity, netCost, unitCost);
    }

    public static (decimal BaseQuantity, decimal UnitCost, decimal TotalCost) CalculateMovement(
        decimal billedQuantity, decimal inventoryBaseQuantity, decimal billedUnitPrice, decimal discountAmount,
        decimal allocatedCost = 0m)
    {
        var quantity = Math.Round(inventoryBaseQuantity, 4, MidpointRounding.AwayFromZero);
        if (quantity <= 0)
            throw new InvalidOperationException("La cantidad de ingreso en unidad base debe ser mayor a cero.");

        var cost = Calculate(billedQuantity, quantity, billedUnitPrice, discountAmount, allocatedCost);
        if (cost.NetCost < 0)
            throw new InvalidOperationException("El costo de la linea de compra no puede ser negativo.");

        var total = Math.Round(cost.NetCost, 4, MidpointRounding.AwayFromZero);
        return (quantity, Math.Round(total / quantity, 4, MidpointRounding.AwayFromZero), total);
    }

    public static IReadOnlyList<decimal> AllocateAdditionalCost(
        IReadOnlyList<decimal> inventoryNetCosts, decimal additionalCost)
    {
        if (additionalCost < 0)
            throw new InvalidOperationException("El costo adicional no puede ser negativo.");
        if (inventoryNetCosts.Count == 0)
        {
            if (additionalCost > 0)
                throw new InvalidOperationException("No hay articulos de inventario para distribuir el costo adicional.");
            return [];
        }

        var weights = inventoryNetCosts.Select(x => Math.Max(x, 0m)).ToList();
        var totalWeight = weights.Sum();
        if (totalWeight <= 0)
            weights = Enumerable.Repeat(1m, inventoryNetCosts.Count).ToList();
        totalWeight = weights.Sum();

        var allocations = new decimal[inventoryNetCosts.Count];
        var allocated = 0m;
        for (var i = 0; i < allocations.Length - 1; i++)
        {
            allocations[i] = Math.Round(additionalCost * weights[i] / totalWeight, 4, MidpointRounding.AwayFromZero);
            allocated += allocations[i];
        }
        allocations[^1] = Math.Round(additionalCost - allocated, 4, MidpointRounding.AwayFromZero);
        return allocations;
    }
}
