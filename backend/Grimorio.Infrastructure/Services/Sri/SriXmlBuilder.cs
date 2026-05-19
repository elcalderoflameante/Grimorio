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
        // MemoryStream garantiza encoding UTF-8 real — StringBuilder usa UTF-16 internamente
        using var ms = new MemoryStream();
        var settings = new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true };

        using (var w = XmlWriter.Create(ms, settings))
        {
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
        }

        return new UTF8Encoding(false).GetString(ms.ToArray());
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

        var taxGroups = ComputeTaxGroups(d.Items);
        decimal totalSinImpuestos = taxGroups.Sum(g => g.TaxableBase);
        decimal importeTotal = d.Order.Total;

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
        w.WriteElementString("fechaEmision", EcuadorTime.FromUtc(payment.PaidAt).ToString("dd/MM/yyyy"));
        w.WriteElementString("dirEstablecimiento", c.Direccion);
        if (!string.IsNullOrEmpty(c.ContribuyenteEspecial))
            w.WriteElementString("contribuyenteEspecial", c.ContribuyenteEspecial);
        w.WriteElementString("obligadoContabilidad", c.ObligadoContabilidad ? "SI" : "NO");
        w.WriteElementString("tipoIdentificacionComprador", tipoId);
        w.WriteElementString("razonSocialComprador", razonComprador);
        w.WriteElementString("identificacionComprador", idComprador);
        w.WriteElementString("totalSinImpuestos", Fmt(totalSinImpuestos));
        w.WriteElementString("totalDescuento", Fmt(0));

        w.WriteStartElement("totalConImpuestos");
        foreach (var g in taxGroups)
            WriteTotalImpuesto(w, "2", g.SriCode, g.TaxableBase, g.TaxAmt);
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

    private record TaxGroup(string SriCode, decimal Percentage, decimal TaxableBase, decimal TaxAmt);

    private static List<TaxGroup> ComputeTaxGroups(List<OrderItem> items)
    {
        return items
            .GroupBy(i => new
            {
                SriCode = i.MenuItem?.TaxRate?.SriCode ?? "6",
                Percentage = i.MenuItem?.TaxRate?.Percentage ?? 0m,
            })
            .Select(g =>
            {
                var pct = g.Key.Percentage;
                decimal taxableBase = 0, taxAmt = 0;
                foreach (var item in g)
                {
                    var net = item.UnitPrice * item.Quantity
                              - Math.Round(item.UnitPrice * item.Quantity * (item.DiscountPct / 100m), 2);
                    if (pct > 0)
                    {
                        var b = Math.Round(net / (1m + pct / 100m), 2);
                        taxableBase += b;
                        taxAmt += Math.Round(net - b, 2);
                    }
                    else { taxableBase += net; }
                }
                return new TaxGroup(g.Key.SriCode, pct, taxableBase, taxAmt);
            })
            .OrderBy(g => g.Percentage)
            .ToList();
    }

    private static void WriteDetalles(XmlWriter w, SriInvoiceData d)
    {
        w.WriteStartElement("detalles");
        foreach (var item in d.Items)
        {
            var menuItem = item.MenuItem;
            var taxRate = menuItem?.TaxRate;
            var pct = taxRate?.Percentage ?? 0m;
            var sriCode = taxRate?.SriCode ?? "6";

            var gross = item.UnitPrice * item.Quantity;
            var discount = Math.Round(gross * (item.DiscountPct / 100m), 2);
            var net = gross - discount;
            decimal taxableBase, taxAmt;
            if (pct > 0)
            {
                taxableBase = Math.Round(net / (1m + pct / 100m), 2);
                taxAmt = Math.Round(net - taxableBase, 2);
            }
            else { taxableBase = net; taxAmt = 0m; }

            w.WriteStartElement("detalle");
            w.WriteElementString("codigoPrincipal", menuItem?.InternalCode ?? item.MenuItemId.ToString()[..8]);
            w.WriteElementString("descripcion", menuItem?.Name ?? item.MenuItemId.ToString()[..8]);
            w.WriteElementString("cantidad", item.Quantity.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
            w.WriteElementString("precioUnitario", (net / (pct > 0 ? 1m + pct / 100m : 1m)).ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
            w.WriteElementString("descuento", Fmt(discount));
            w.WriteElementString("precioTotalSinImpuesto", Fmt(taxableBase));

            w.WriteStartElement("impuestos");
            w.WriteStartElement("impuesto");
            w.WriteElementString("codigo", "2");
            w.WriteElementString("codigoPorcentaje", sriCode);
            w.WriteElementString("tarifa", pct > 0 ? pct.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) : "0.00");
            w.WriteElementString("baseImponible", Fmt(taxableBase));
            w.WriteElementString("valor", Fmt(taxAmt));
            w.WriteEndElement(); // impuesto
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
