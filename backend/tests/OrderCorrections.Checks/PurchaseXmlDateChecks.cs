using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using Grimorio.API.Controllers;
using Grimorio.Application.DTOs;

internal static class PurchaseXmlDateChecks
{
    internal static void Run() {
        var parse = typeof(PurchasesController).Assembly.GetType("Grimorio.API.Controllers.PurchaseInvoiceXmlParser")!
            .GetMethod("Parse", BindingFlags.Public | BindingFlags.Static)!;
        PurchaseInvoiceImportDto Parse(string xml) {
            try { return (PurchaseInvoiceImportDto)parse.Invoke(null, [xml])!; }
            catch (TargetInvocationException e) when (e.InnerException is InvalidOperationException error) { throw error; }
        }
        static string Xml(string? value) => new XElement("factura", new XElement("infoTributaria",
            new XElement("estab", "001"), new XElement("ptoEmi", "001"), new XElement("secuencial", "000000123")),
            new XElement("infoFactura", value is null ? null : new XElement("fechaEmision", value))).ToString();
        var count = 0;
        void Check(bool condition) { if (!condition) throw new Exception("Purchase XML date regression"); count++; }
        var previous = CultureInfo.CurrentCulture;
        try {
            foreach (var culture in new[] { "en-US", "es-EC", "tr-TR" }) {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                foreach (var (input, expected) in new[] {
                    ("24/10/2026", "2026-10-24"), ("2026-10-24", "2026-10-24"),
                    ("01/01/2027", "2027-01-01"), ("01/03/2026", "2026-03-01"), ("29/02/2024", "2024-02-29"),
                }) {
                    var invoice = Parse(Xml(input));
                    Check(invoice.DocumentDate!.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) == expected);
                    var json = JsonSerializer.Serialize(invoice, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                    using var payload = JsonDocument.Parse(json);
                    Check(payload.RootElement.GetProperty("documentDate").GetString() == expected);
                    var wrapped = new XElement("autorizaciones", new XElement("autorizacion",
                        new XElement("fechaAutorizacion", "25/10/2026 00:10:00"), new XElement("comprobante", new XCData(Xml(input))))).ToString();
                    var authorized = Parse(wrapped);
                    Check(authorized.DocumentDate == invoice.DocumentDate && authorized.AuthorizationDate.HasValue);
                }
            }
            foreach (var invalid in new string?[] { null, "", "not-a-date", "31/02/2026", "29/02/2025", "10/24/2026" }) {
                try { Parse(Xml(invalid)); throw new Exception("Invalid XML emission date accepted"); }
                catch (InvalidOperationException e) { Check(e.Message.Contains("fechaEmision")); }
            }
        } finally { CultureInfo.CurrentCulture = previous; }
        Console.WriteLine($"{count} purchase XML calendar-date checks passed; no database or production services.");
    }
}
