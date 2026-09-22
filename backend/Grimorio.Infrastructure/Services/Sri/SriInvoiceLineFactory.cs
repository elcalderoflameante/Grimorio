using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.POS;

namespace Grimorio.Infrastructure.Services.Sri;

public static class SriInvoiceLineFactory
{
    public static List<SriInvoiceLine> Build(OrderPayment payment, Order order)
    {
        var paymentItems = payment.Items.Where(i => !i.IsDeleted).ToList();
        if (paymentItems.Count > 0)
        {
            return paymentItems.Select(i =>
            {
                var orderItem = i.OrderItem
                    ?? throw new InvalidOperationException("No se pudo recuperar el item cobrado para generar la factura.");
                return Map(
                    orderItem,
                    i.Quantity,
                    i.UnitPrice,
                    i.ItemCode,
                    i.ItemName,
                    i.TaxRateSriCode,
                    i.TaxRatePercentage);
            }).ToList();
        }

        // Compatibilidad con cobros anteriores a la persistencia obligatoria del detalle.
        // Solo es seguro reconstruirlos cuando el pago corresponde a la orden completa.
        if (Math.Abs(payment.OrderAmount - order.Total) > 0.01m)
            throw new InvalidOperationException(
                "El cobro parcial no tiene detalle de items y no puede facturarse automaticamente.");

        return order.Items
            .Where(i => !i.IsDeleted && i.Status != OrderItemStatus.Cancelled)
            .Select(i => Map(
                i,
                i.Quantity,
                i.Quantity > 0 ? Math.Round(i.TotalPrice / i.Quantity, 4) : 0m))
            .ToList();
    }

    public static SriInvoiceTotals CalculateTotals(IReadOnlyCollection<SriInvoiceLine> lines, decimal paymentAmount)
    {
        decimal subtotal = 0m;
        decimal tax = 0m;

        foreach (var line in lines)
        {
            var total = Math.Round(line.UnitPrice * line.Quantity, 2);
            var taxableBase = line.TaxRatePercentage > 0
                ? Math.Round(total / (1m + line.TaxRatePercentage / 100m), 2)
                : total;
            subtotal += taxableBase;
            tax += Math.Round(total - taxableBase, 2);
        }

        if (Math.Abs(subtotal + tax - paymentAmount) > 0.01m)
            throw new InvalidOperationException(
                "El detalle del cobro no coincide con el total que se debe facturar.");

        return new SriInvoiceTotals(subtotal, tax, paymentAmount);
    }

    private static SriInvoiceLine Map(
        OrderItem item,
        decimal quantity,
        decimal unitPrice,
        string? itemCode = null,
        string? itemName = null,
        string? taxRateSriCode = null,
        decimal? taxRatePercentage = null)
    {
        var taxRate = item.TaxRate ?? item.MenuItem?.TaxRate;
        var fallbackCode = item.MenuItemId.ToString()[..8];

        return new SriInvoiceLine(
            itemCode ?? item.MenuItem?.InternalCode ?? fallbackCode,
            itemName ?? item.MenuItem?.Name ?? fallbackCode,
            quantity,
            unitPrice,
            taxRateSriCode ?? taxRate?.SriCode ?? "6",
            taxRatePercentage ?? taxRate?.Percentage ?? 0m);
    }
}

public record SriInvoiceTotals(decimal TotalWithoutTaxes, decimal TotalTax, decimal Total);
