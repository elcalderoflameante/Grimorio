using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Grimorio.Domain.Entities.Billing;

namespace Grimorio.Infrastructure.Services.Sri;

public record SriInvoicePayment(string Code, decimal Amount)
{
    public string Label => Code switch {
        "01" => "SIN UTILIZACION DEL SISTEMA FINANCIERO",
        "15" => "COMPENSACION DE DEUDAS",
        "16" => "TARJETA DE DEBITO",
        "17" => "DINERO ELECTRONICO",
        "18" => "TARJETA PREPAGO",
        "19" => "TARJETA DE CREDITO",
        "20" => "OTROS CON UTILIZACION DEL SISTEMA FINANCIERO",
        "21" => "ENDOSO DE TITULOS",
        _ => $"FORMA DE PAGO SRI {Code}",
    };
}

public static class SriInvoicePaymentSnapshot
{
    public static List<SriInvoicePayment>? Read(ElectronicDocument? document, OrderPayment payment)
    {
        if (payment.OriginalPaymentLinesJson == null) return null;
        var xml = document?.XmlAuthorized ?? document?.XmlSigned;
        if (string.IsNullOrWhiteSpace(xml)) return null;
        try
        {
            using var reader = XmlReader.Create(new StringReader(xml.Trim()), new XmlReaderSettings {
                DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 8 * 1024 * 1024 });
            var invoice = XElement.Load(reader);
            var payments = invoice.Name == "factura" ? invoice.Element("infoFactura")?.Element("pagos") : null;
            var result = new List<SriInvoicePayment>();
            foreach (var entry in payments?.Elements("pago") ?? [])
            {
                var code = entry.Element("formaPago")?.Value.Trim();
                if (string.IsNullOrWhiteSpace(code) || !decimal.TryParse(entry.Element("total")?.Value,
                    NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
                    CultureInfo.InvariantCulture, out var amount) || amount <= 0)
                    throw new InvalidOperationException("Los medios de pago del XML original no son validos.");
                result.Add(new(code, amount));
            }
            if (result.Count == 0 || result.Sum(p => p.Amount) != payment.OrderAmount)
                throw new InvalidOperationException("Los medios de pago del XML original no coinciden con el total del cobro.");
            return result;
        }
        catch (XmlException ex)
        {
            throw new InvalidOperationException("No se pudo conservar los medios de pago del XML original.", ex);
        }
    }
}
