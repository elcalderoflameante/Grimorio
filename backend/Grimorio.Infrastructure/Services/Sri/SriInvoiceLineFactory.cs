using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.Menu;
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
                    i.TaxRatePercentage,
                    i.OriginalTotal,
                    i.Total,
                    i.PromotionName) with
                {
                    OriginalSubtotal = i.OriginalSubtotal,
                    Discount = i.Discount,
                    Subtotal = i.Subtotal,
                    Tax = i.Tax,
                };
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
                i.Quantity > 0 ? Math.Round(i.TotalPrice / i.Quantity, 4) : 0m,
                lineTotal: i.TotalPrice))
            .ToList();
    }

    public static SriInvoiceTotals CalculateTotals(IReadOnlyCollection<SriInvoiceLine> lines, decimal paymentAmount)
    {
        decimal subtotal = 0m;
        decimal tax = 0m;

        foreach (var line in lines)
        {
            var amounts = Amounts(line);
            subtotal += amounts.FinalBase;
            tax += amounts.Tax;
        }

        if (Math.Abs(subtotal + tax - paymentAmount) > 0.01m
            || (lines.Any(line => line.Subtotal.HasValue) && subtotal + tax != paymentAmount))
            throw new InvalidOperationException(
                "El detalle del cobro no coincide con el total que se debe facturar.");

        return new SriInvoiceTotals(subtotal, tax, paymentAmount, lines.Sum(line => Amounts(line).Discount),
            lines.Sum(line => Amounts(line).OriginalBase));
    }

    public static void CaptureAmounts(OrderPaymentItem item)
    {
        var amounts = Amounts(new SriInvoiceLine("", "", item.Quantity, item.UnitPrice,
            item.TaxRateSriCode ?? "6", item.TaxRatePercentage ?? 0m, item.OriginalTotal, item.Total));
        item.OriginalSubtotal = amounts.OriginalBase;
        item.Discount = amounts.Discount;
        item.Subtotal = amounts.FinalBase;
        item.Tax = amounts.Tax;
    }

    public static void ValidateDocumentTotals(ElectronicDocument document, SriInvoiceTotals totals)
    {
        if (document.SubtotalBeforeDiscount.HasValue
            && (document.SubtotalBeforeDiscount != totals.SubtotalBeforeDiscount
                || document.TotalDescuento != totals.TotalDiscount
                || document.TotalSinImpuestos != totals.TotalWithoutTaxes
                || document.TotalIva != totals.TotalTax || document.ImporteTotal != totals.Total))
            throw new InvalidOperationException("Los importes guardados de la cabecera no coinciden con el detalle de factura.");
    }

    public static SriInvoiceLineAmounts Amounts(SriInvoiceLine line)
    {
        if (line.Quantity <= 0 || line.TaxRatePercentage < 0)
            throw new InvalidOperationException("Cantidad o tarifa de IVA no valida en el detalle de factura.");
        var finalTotal = Math.Round(line.LineTotal ?? line.UnitPrice * line.Quantity, 2);
        var originalTotal = Math.Round(line.OriginalTotal ?? finalTotal, 2);
        if (finalTotal < 0 || originalTotal < finalTotal)
            throw new InvalidOperationException("El precio original no puede ser menor al importe cobrado.");
        var factor = 1m + line.TaxRatePercentage / 100m;
        var finalBase = Math.Round(finalTotal / factor, 2);
        var originalBase = Math.Round(originalTotal / factor, 2);
        if (line.OriginalSubtotal.HasValue || line.Discount.HasValue || line.Subtotal.HasValue || line.Tax.HasValue)
        {
            if (line.OriginalSubtotal != originalBase || line.Subtotal != finalBase
                || line.Discount != originalBase - finalBase || line.Tax != finalTotal - finalBase)
                throw new InvalidOperationException("Los importes guardados del detalle de factura no cuadran con el total y el IVA.");
            return new SriInvoiceLineAmounts(originalTotal, finalTotal, line.OriginalSubtotal.Value,
                line.Subtotal!.Value, line.Discount!.Value, originalTotal - finalTotal,
                line.OriginalSubtotal.Value / line.Quantity, line.Tax!.Value);
        }
        return new SriInvoiceLineAmounts(originalTotal, finalTotal, originalBase, finalBase,
            originalBase - finalBase, originalTotal - finalTotal, originalBase / line.Quantity, finalTotal - finalBase);
    }

    private static SriInvoiceLine Map(
        OrderItem item,
        decimal quantity,
        decimal unitPrice,
        string? itemCode = null,
        string? itemName = null,
        string? taxRateSriCode = null,
        decimal? taxRatePercentage = null,
        decimal? originalTotal = null,
        decimal? lineTotal = null,
        string? promotionName = null)
    {
        var taxRate = item.TaxRate ?? item.MenuItem?.TaxRate;
        var fallbackCode = item.MenuItemId.ToString()[..8];

        return new SriInvoiceLine(
            ResolveCode(item.MenuItem?.Name ?? itemName ?? item.MenuItemId.ToString(), itemCode, item.MenuItem?.InternalCode),
            itemName ?? BuildDescription(item.MenuItem) ?? fallbackCode,
            quantity,
            unitPrice,
            taxRateSriCode ?? taxRate?.SriCode ?? "6",
            taxRatePercentage ?? taxRate?.Percentage ?? 0m,
            originalTotal, lineTotal, promotionName);
    }

    public static string ResolveCode(string itemName, string? savedCode, string? currentCode = null)
    {
        // Preserve valid payment snapshots; recover legacy codes rejected by the XML schema.
        foreach (var candidate in new[] { savedCode, currentCode })
        {
            var code = candidate?.Trim();
            if (!string.IsNullOrEmpty(code) && code.Length <= 25)
                return code;
        }

        throw new InvalidOperationException(
            $"El item '{itemName}' no tiene un codigo valido para facturar. Corrige su codigo interno en Items y recetas: debe tener entre 1 y 25 caracteres. Luego reintenta la factura.");
    }

    public static string? BuildDescription(MenuItem? item)
    {
        if (item is null) return null;

        var thematicName = item.Name.Trim();
        var generalName = item.GeneralName?.Trim();
        if (string.IsNullOrWhiteSpace(generalName)
            || string.Equals(thematicName, generalName, StringComparison.OrdinalIgnoreCase))
        {
            return thematicName;
        }

        var description = $"{thematicName} - {generalName}";
        return description.Length <= 300 ? description : description[..300];
    }
}

public record SriInvoiceTotals(decimal TotalWithoutTaxes, decimal TotalTax, decimal Total, decimal TotalDiscount = 0m,
    decimal SubtotalBeforeDiscount = 0m);
public record SriInvoiceLineAmounts(decimal OriginalTotal, decimal FinalTotal, decimal OriginalBase,
    decimal FinalBase, decimal Discount, decimal Savings, decimal OriginalUnitPriceWithoutTax, decimal Tax);
