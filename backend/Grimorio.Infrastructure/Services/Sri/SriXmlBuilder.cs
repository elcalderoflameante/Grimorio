using System.Text;
using System.Xml;
using Grimorio.Application.DTOs;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.POS;

namespace Grimorio.Infrastructure.Services.Sri;

public record SriInvoiceData(
    BranchTaxConfig Config,
    OrderPayment Payment,
    Order Order,
    List<OrderItem> Items,
    string ClaveAcceso,
    long Secuencial,
    Customer? Customer);

// Construye el XML de Factura v2.1.0 sin firma
public static class SriXmlBuilder
{
    public static string Build(SriInvoiceData d)
    {
        var sb = new StringBuilder();
        var settings = new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true };

        using var w = XmlWriter.Create(sb, settings);

        w.WriteStartDocument();
        w.WriteStartElement("factura");
        w.WriteAttributeString("id", "comprobante");
        w.WriteAttributeString("version", "2.1.0");

        WriteInfoTributaria(w, d);
        WriteInfoFactura(w, d);
        WriteDetalles(w, d);
        WriteInfoAdicional(w, d);

        w.WriteEndElement(); // factura
        w.WriteEndDocument();

        return sb.ToString();
    }

    private static void WriteInfoTributaria(XmlWriter w, SriInvoiceData d)
    {
        var c = d.Config;
        w.WriteStartElement("infoTributaria");
        w.WriteElementString("ambiente", c.Ambiente);
        w.WriteElementString("tipoEmision", "1");
        w.WriteElementString("razonSocial", c.RazonSocial);
        w.WriteElementString("nombreComercial", c.NombreComercial ?? c.RazonSocial);
        w.WriteElementString("ruc", c.Ruc);
        w.WriteElementString("claveAcceso", d.ClaveAcceso);
        w.WriteElementString("codDoc", "01");
        w.WriteElementString("estab", c.CodigoEstablecimiento.PadLeft(3, '0'));
        w.WriteElementString("ptoEmi", c.PuntoEmision.PadLeft(3, '0'));
        w.WriteElementString("secuencial", d.Secuencial.ToString().PadLeft(9, '0'));
        w.WriteElementString("dirMatriz", c.Direccion);
        if (!string.IsNullOrEmpty(c.ContribuyenteEspecial))
            w.WriteElementString("contribuyenteEspecial", c.ContribuyenteEspecial);
        w.WriteEndElement();
    }

    private static void WriteInfoFactura(XmlWriter w, SriInvoiceData d)
    {
        var c = d.Config;
        var customer = d.Customer;
        var payment = d.Payment;

        // Calcular totales desde los ítems (ya calculados por el backend del POS)
        var order = d.Order;
        decimal totalSinImpuestos = order.TaxableBase15 + order.TaxableBase0 + order.TaxableBaseExempt;
        decimal totalDescuento = 0m; // descuentos ya incorporados en base imponible
        decimal totalIva = order.Iva15;
        decimal importeTotal = order.Total;

        // Identificación del comprador
        string tipoId, idComprador, razonComprador;
        if (customer == null || customer.TaxIdType == Domain.Entities.Billing.TaxIdType.FinalConsumer)
        {
            tipoId = "07"; idComprador = "9999999999999"; razonComprador = "CONSUMIDOR FINAL";
        }
        else
        {
            tipoId = customer.TaxIdType switch
            {
                Domain.Entities.Billing.TaxIdType.Ruc => "04",
                Domain.Entities.Billing.TaxIdType.Cedula => "05",
                Domain.Entities.Billing.TaxIdType.Passport => "06",
                _ => "07",
            };
            idComprador = customer.TaxId ?? "9999999999999";
            razonComprador = customer.Name;
        }

        w.WriteStartElement("infoFactura");
        w.WriteElementString("fechaEmision", payment.PaidAt.ToString("dd/MM/yyyy"));
        w.WriteElementString("dirEstablecimiento", c.Direccion);
        if (!string.IsNullOrEmpty(c.ContribuyenteEspecial))
            w.WriteElementString("contribuyenteEspecial", c.ContribuyenteEspecial);
        w.WriteElementString("obligadoContabilidad", c.ObligadoContabilidad ? "SI" : "NO");
        w.WriteElementString("tipoIdentificacionComprador", tipoId);
        w.WriteElementString("razonSocialComprador", razonComprador);
        w.WriteElementString("identificacionComprador", idComprador);
        w.WriteElementString("totalSinImpuestos", Fmt(totalSinImpuestos));
        w.WriteElementString("totalDescuento", Fmt(totalDescuento));

        w.WriteStartElement("totalConImpuestos");
        WriteTotalImpuesto(w, "2", "10", order.TaxableBase15, order.Iva15);   // IVA 15%
        if (order.TaxableBase0 > 0)
            WriteTotalImpuesto(w, "2", "0", order.TaxableBase0, 0m);           // IVA 0%
        if (order.TaxableBaseExempt > 0)
            WriteTotalImpuesto(w, "2", "6", order.TaxableBaseExempt, 0m);      // Exento
        if (order.Ice > 0)
            WriteTotalImpuesto(w, "3", "3051", order.Ice, order.Ice);          // ICE
        w.WriteEndElement(); // totalConImpuestos

        w.WriteElementString("propina", Fmt(0));
        w.WriteElementString("importeTotal", Fmt(importeTotal));
        w.WriteElementString("moneda", "DÓLARES");

        // Forma de pago (SRI requiere al menos una)
        w.WriteStartElement("pagos");
        w.WriteStartElement("pago");
        w.WriteElementString("formaPago", MapPaymentMethod(payment));
        w.WriteElementString("total", Fmt(importeTotal));
        w.WriteElementString("plazo", "0");
        w.WriteElementString("unidadTiempo", "dias");
        w.WriteEndElement(); // pago
        w.WriteEndElement(); // pagos

        w.WriteEndElement(); // infoFactura
    }

    private static void WriteTotalImpuesto(XmlWriter w, string codigo, string codigoPct, decimal base_, decimal valor)
    {
        if (base_ <= 0 && valor <= 0) return;
        w.WriteStartElement("totalImpuesto");
        w.WriteElementString("codigo", codigo);
        w.WriteElementString("codigoPorcentaje", codigoPct);
        w.WriteElementString("baseImponible", Fmt(base_));
        w.WriteElementString("valor", Fmt(valor));
        w.WriteEndElement();
    }

    private static void WriteDetalles(XmlWriter w, SriInvoiceData d)
    {
        w.WriteStartElement("detalles");
        foreach (var item in d.Items)
        {
            var menuItem = item.MenuItem;
            var taxPct = menuItem?.TaxRate?.Percentage;
            var sriCode = menuItem?.TaxRate?.SriCode;

            // Calcular base imponible por línea (precio ya incluye IVA)
            var gross = item.UnitPrice * item.Quantity;
            var discount = Math.Round(gross * (item.DiscountPct / 100m), 2);
            var netInclusive = gross - discount;
            decimal taxableBase, taxAmt;
            if (taxPct.HasValue && taxPct.Value > 0)
            {
                taxableBase = Math.Round(netInclusive / (1m + taxPct.Value / 100m), 2);
                taxAmt = Math.Round(netInclusive - taxableBase, 2);
            }
            else { taxableBase = netInclusive; taxAmt = 0m; }

            w.WriteStartElement("detalle");
            w.WriteElementString("codigoPrincipal", menuItem?.InternalCode ?? item.MenuItemId.ToString()[..8]);
            w.WriteElementString("descripcion", menuItem?.Name ?? item.MenuItemId.ToString()[..8]);
            w.WriteElementString("cantidad", item.Quantity.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
            w.WriteElementString("precioUnitario", (item.UnitPrice / (1m + (taxPct ?? 0m) / 100m)).ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
            w.WriteElementString("descuento", Fmt(discount));
            w.WriteElementString("precioTotalSinImpuesto", Fmt(taxableBase));

            w.WriteStartElement("impuestos");
            if (taxAmt > 0 || taxableBase > 0)
            {
                var codigoPct = sriCode == "10" ? "10" : sriCode == "0" || sriCode == "8" ? "0" : "6";
                var tarifa = taxPct.HasValue && taxPct.Value > 0 ? taxPct.Value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) : "0.00";
                w.WriteStartElement("impuesto");
                w.WriteElementString("codigo", "2");
                w.WriteElementString("codigoPorcentaje", codigoPct);
                w.WriteElementString("tarifa", tarifa);
                w.WriteElementString("baseImponible", Fmt(taxableBase));
                w.WriteElementString("valor", Fmt(taxAmt));
                w.WriteEndElement();
            }
            w.WriteEndElement(); // impuestos

            w.WriteEndElement(); // detalle
        }
        w.WriteEndElement(); // detalles
    }

    private static void WriteInfoAdicional(XmlWriter w, SriInvoiceData d)
    {
        w.WriteStartElement("infoAdicional");
        if (d.Customer?.Email != null)
        {
            w.WriteStartElement("campoAdicional");
            w.WriteAttributeString("nombre", "Email");
            w.WriteString(d.Customer.Email);
            w.WriteEndElement();
        }
        if (d.Customer?.Phone != null)
        {
            w.WriteStartElement("campoAdicional");
            w.WriteAttributeString("nombre", "Teléfono");
            w.WriteString(d.Customer.Phone);
            w.WriteEndElement();
        }
        w.WriteStartElement("campoAdicional");
        w.WriteAttributeString("nombre", "Orden");
        w.WriteString(d.Order.Number.ToString());
        w.WriteEndElement();
        w.WriteEndElement(); // infoAdicional
    }

    private static string MapPaymentMethod(OrderPayment payment)
    {
        // Código de forma de pago SRI
        // 01 = efectivo, 08 = tarjeta débito, 19 = tarjeta crédito, 20 = transferencia
        var lines = payment.Lines?.ToList();
        if (lines == null || lines.Count == 0) return "01";
        var first = lines[0];
        if (first.Config?.IsCash == true) return "01";
        if (first.Config?.Name?.Contains("débito", StringComparison.OrdinalIgnoreCase) == true) return "08";
        if (first.Config?.Name?.Contains("crédit", StringComparison.OrdinalIgnoreCase) == true) return "19";
        if (first.Config?.Name?.Contains("transfer", StringComparison.OrdinalIgnoreCase) == true) return "20";
        return "01";
    }

    private static string Fmt(decimal v) =>
        v.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
}
