using System.Text;
using System.Xml;
using Grimorio.Application.DTOs;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Features.Billing;

namespace Grimorio.Infrastructure.Services.Sri;

public record SriInvoiceData(
    BranchTaxConfig Config,
    OrderPayment Payment,
    Order Order,
    List<SriInvoiceLine> Items,
    string ClaveAcceso,
    long Secuencial,
    Customer? Customer,
    DateTime? EmissionDate = null,
    ElectronicDocument? Document = null);

public record SriInvoiceLine(
    string Code,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    string TaxRateSriCode,
    decimal TaxRatePercentage,
    decimal? OriginalTotal = null,
    decimal? LineTotal = null,
    string? PromotionName = null,
    decimal? OriginalSubtotal = null,
    decimal? Discount = null,
    decimal? Subtotal = null,
    decimal? Tax = null);

// Construye el XML de Factura v2.1.0 sin firma
public static class SriXmlBuilder
{
    public static string Build(SriInvoiceData d)
    {
        if (d.Document is not null)
            SriInvoiceLineFactory.ValidateDocumentTotals(d.Document,
                SriInvoiceLineFactory.CalculateTotals(d.Items, d.Payment.OrderAmount));
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
        decimal totalSinImpuestos = d.Document?.TotalSinImpuestos ?? taxGroups.Sum(g => g.TaxableBase);
        decimal importeTotal = d.Document?.ImporteTotal ?? d.Payment.OrderAmount;

        // Identificación del comprador
        string tipoId, idComprador, razonComprador;
        if (customer == null || customer.TaxIdType == Domain.Entities.Billing.TaxIdType.FinalConsumer)
        {
            tipoId = "07"; idComprador = "9999999999999"; razonComprador = "CONSUMIDOR FINAL";
        }
        else
        {
            var identity = CustomerIdentity.Validate(customer.TaxId, customer.TaxIdType.ToString());
            tipoId = identity.Type switch
            {
                Domain.Entities.Billing.TaxIdType.Ruc => "04",
                Domain.Entities.Billing.TaxIdType.Cedula => "05",
                Domain.Entities.Billing.TaxIdType.Passport => "06",
                _ => "07",
            };
            idComprador = identity.Number ?? "9999999999999";
            razonComprador = identity.Type == TaxIdType.FinalConsumer ? "CONSUMIDOR FINAL" : customer.Name;
        }

        w.WriteStartElement("infoFactura");
        var emissionDate = d.EmissionDate ?? payment.PaidAt;
        w.WriteElementString("fechaEmision", EcuadorTime.FromUtc(emissionDate).ToString("dd/MM/yyyy"));
        w.WriteElementString("dirEstablecimiento", c.Direccion);
        if (!string.IsNullOrEmpty(c.ContribuyenteEspecial))
            w.WriteElementString("contribuyenteEspecial", c.ContribuyenteEspecial);
        w.WriteElementString("obligadoContabilidad", c.ObligadoContabilidad ? "SI" : "NO");
        w.WriteElementString("tipoIdentificacionComprador", tipoId);
        w.WriteElementString("razonSocialComprador", razonComprador);
        w.WriteElementString("identificacionComprador", idComprador);
        w.WriteElementString("totalSinImpuestos", Fmt(totalSinImpuestos));
        w.WriteElementString("totalDescuento", Fmt(d.Document?.TotalDescuento
            ?? d.Items.Sum(i => SriInvoiceLineFactory.Amounts(i).Discount)));

        w.WriteStartElement("totalConImpuestos");
        foreach (var g in taxGroups)
            WriteTotalImpuesto(w, "2", g.SriCode, g.TaxableBase, g.TaxAmt);
        w.WriteEndElement(); // totalConImpuestos

        w.WriteElementString("propina", Fmt(0));
        w.WriteElementString("importeTotal", Fmt(importeTotal));
        w.WriteElementString("moneda", "DÓLARES");

        // Forma de pago (SRI requiere al menos una)
        w.WriteStartElement("pagos");
        var paymentGroups = payment.Lines
            .Where(l => !l.IsDeleted)
            .GroupBy(MapPaymentMethod)
            .Select(g => new
            {
                SriCode = g.Key,
                Total = g.Sum(l => l.AmountTendered - l.Change),
            })
            .Where(g => g.Total > 0)
            .ToList();

        if (paymentGroups.Count == 0)
            paymentGroups.Add(new { SriCode = "01", Total = importeTotal });

        foreach (var paymentGroup in paymentGroups)
        {
            w.WriteStartElement("pago");
            w.WriteElementString("formaPago", paymentGroup.SriCode);
            w.WriteElementString("total", Fmt(paymentGroup.Total));
            w.WriteElementString("plazo", "0");
            w.WriteElementString("unidadTiempo", "dias");
            w.WriteEndElement(); // pago
        }
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

    private static List<TaxGroup> ComputeTaxGroups(List<SriInvoiceLine> items)
    {
        return items
            .GroupBy(i => new
            {
                SriCode = i.TaxRateSriCode,
                Percentage = i.TaxRatePercentage,
            })
            .Select(g =>
            {
                var pct = g.Key.Percentage;
                decimal taxableBase = 0, taxAmt = 0;
                foreach (var item in g)
                {
                    var amounts = SriInvoiceLineFactory.Amounts(item);
                    taxableBase += amounts.FinalBase;
                    taxAmt += amounts.Tax;
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
            var pct = item.TaxRatePercentage;
            var sriCode = item.TaxRateSriCode;

            var amounts = SriInvoiceLineFactory.Amounts(item);
            var discount = amounts.Discount;
            var taxableBase = amounts.FinalBase;
            var taxAmt = amounts.Tax;

            w.WriteStartElement("detalle");
            w.WriteElementString("codigoPrincipal", item.Code);
            w.WriteElementString("descripcion", item.Description);
            w.WriteElementString("cantidad", item.Quantity.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
            w.WriteElementString("precioUnitario", amounts.OriginalUnitPriceWithoutTax.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
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

    private static string MapPaymentMethod(PaymentLine line)
    {
        // Código de forma de pago SRI
        // 01 = efectivo, 16 = tarjeta débito, 19 = tarjeta crédito, 20 = transferencia
        if (line.Config?.IsCash == true) return "01";
        if (line.CardPaymentType == CardPaymentType.Debit) return "16";
        if (line.CardPaymentType == CardPaymentType.Credit) return "19";
        if (line.Config?.Name?.Contains("transfer", StringComparison.OrdinalIgnoreCase) == true) return "20";
        return "01";
    }

    private static string Fmt(decimal v) =>
        v.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
}
