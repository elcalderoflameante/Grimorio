using System.Globalization;
using System.Xml.Linq;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.Menu;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Services.Sri;
using Grimorio.Infrastructure.Features.Billing;

var count = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); count++; }
decimal Number(XElement parent, string name) => decimal.Parse(parent.Element(name)!.Value, CultureInfo.InvariantCulture);
var menu = new MenuItem { Name = "Combo dragon", GeneralName = "Costilla y limonada", InternalCode = "M-ABC123", Price = 9 };
var item = new OrderItem { MenuItem = menu, MenuItemId = menu.Id, Quantity = 1, UnitPrice = 9, TotalPrice = 6.99m };
var paidItem = new OrderPaymentItem { OrderItem = item, OrderItemId = item.Id, ItemCode = menu.InternalCode,
    ItemName = "Combo dragon - Costilla y limonada", Quantity = 1, UnitPrice = 6.99m, Total = 6.99m,
    TaxRatePercentage = 15, TaxRateSriCode = "4", OriginalTotal = 9, PromotionName = "Martes magico" };
var payment = new OrderPayment { OrderAmount = 6.99m, Items = [paidItem] };
var order = new Order { Number = 7, Total = 6.99m, Items = [item] };
var config = new BranchTaxConfig { Ruc = "1790012345001", RazonSocial = "Restaurante de pruebas", NombreComercial = "Restaurante de pruebas",
    Direccion = "Direccion de pruebas", CodigoEstablecimiento = "001", PuntoEmision = "003", Ambiente = "1" };
var lines = SriInvoiceLineFactory.Build(payment, order);
var amounts = SriInvoiceLineFactory.Amounts(lines.Single());
Check(amounts.Savings == 2.01m && amounts.Discount == 1.75m && amounts.FinalBase == 6.08m, "Gross savings and fiscal discount differ correctly");
var totals = SriInvoiceLineFactory.CalculateTotals(lines, 6.99m);
Check(totals.TotalTax == .91m && totals.TotalWithoutTaxes == 6.08m && totals.TotalDiscount == 1.75m, "Unchanged final total and IVA");
XDocument Xml(List<SriInvoiceLine> detail, decimal total) => XDocument.Parse(SriXmlBuilder.Build(new SriInvoiceData(config,
    new OrderPayment { OrderAmount = total }, order, detail, new string('1', 49), 1, null)));
var xml = Xml(lines, 6.99m);
var row = xml.Root!.Element("detalles")!.Element("detalle")!;
Check(Number(row, "descuento") == 1.75m && Number(row, "precioTotalSinImpuesto") == 6.08m, "XML line discount");
Check(Number(xml.Root.Element("infoFactura")!, "totalDescuento") == 1.75m, "XML total discount");
Check(row.Element("descripcion")!.Value.Contains("Combo dragon") && !row.Element("descripcion")!.Value.Contains("Martes magico"), "Combo identity retained");
menu.Price = 50; menu.Name = "Edited"; item.UnitPrice = 50; item.PromotionName = "Edited promotion";
Check(SriInvoiceLineFactory.Amounts(SriInvoiceLineFactory.Build(payment, order).Single()).OriginalTotal == 9, "Saved original price remains immutable");
paidItem.OriginalTotal = null;
Check(SriInvoiceLineFactory.Amounts(SriInvoiceLineFactory.Build(payment, order).Single()).Savings == 0, "No invented historical savings");
paidItem.OriginalTotal = 9;
foreach (var tax in new[] { 0m, 5m, 15m })
foreach (var quantity in new[] { .0001m, .25m, .5m, 1m, 2m, 3m, 12m, 100m, 10000m })
{
    var final = Math.Max(.01m, Math.Round(6.99m * quantity, 2));
    var original = Math.Max(final, Math.Round(9m * quantity, 2));
    var line = new SriInvoiceLine("M-ABC123", "Combo", quantity, final / quantity, tax > 0 ? "4" : "0", tax, original, final);
    var a = SriInvoiceLineFactory.Amounts(line);
    var document = Xml([line], final);
    var detail = document.Root!.Element("detalles")!.Element("detalle")!;
    var reconstructed = Math.Round(Number(detail, "precioUnitario") * Number(detail, "cantidad"), 2) - Number(detail, "descuento");
    Check(Math.Abs(reconstructed - Number(detail, "precioTotalSinImpuesto")) <= .01m, "XML unit price and discount reconcile");
    var result = SriInvoiceLineFactory.CalculateTotals([line], final);
    Check(result.TotalWithoutTaxes + result.TotalTax == final && result.TotalDiscount >= 0, "Quantity/tax rounding");
}
var mixed = new List<SriInvoiceLine> { lines[0], new("M-ZERO", "Producto sin IVA", 1, 1, "0", 0, 2, 1) };
Check(SriInvoiceLineFactory.CalculateTotals(mixed, 7.99m).TotalDiscount == 2.75m, "Mixed tax discount totals");
for (var part = 0; part < 3; part++)
{
    var normal = OrderPaymentPricing.AllocateTotal(27, 3, part, 1);
    var final = OrderPaymentPricing.AllocateTotal(20, 3, part, 1);
    Check(SriInvoiceLineFactory.Amounts(new("M-ABC123", "3x2", 1, final, "4", 15, normal, final)).Savings == normal - final, "3x2 partial allocation");
}
var noDiscount = new SriInvoiceLine("M-ABC123", "Sin descuento", 1, 9.99m, "4", 15, 9.99m, 9.99m);
Check(SriInvoiceLineFactory.Amounts(noDiscount).Discount == 0, "Alternative price without savings");
try { SriInvoiceLineFactory.Amounts(noDiscount with { OriginalTotal = 9 }); throw new Exception("Expected invalid original price"); }
catch (InvalidOperationException) { count++; }
var doc = new ElectronicDocument { NumeroFactura = "001-003-000000001", ClaveAcceso = new string('1', 49), NumeroAutorizacion = new string('1', 49),
    EmissionDate = DateTime.UtcNow, FechaAutorizacion = DateTime.UtcNow, TotalSinImpuestos = totals.TotalWithoutTaxes, TotalIva = totals.TotalTax,
    TotalDescuento = totals.TotalDiscount, ImporteTotal = totals.Total, Environment = "1" };
var pdf = RideGenerator.Generate(config, doc, payment, order, lines, null);
Check(pdf.Length > 100 && System.Text.Encoding.ASCII.GetString(pdf, 0, 4) == "%PDF", "RIDE generated");
var output = Environment.GetEnvironmentVariable("INVOICE_SAVINGS_PDF");
if (output is not null) await File.WriteAllBytesAsync(output, pdf);
SriInvoiceLineFactory.CaptureAmounts(paidItem);
Check(paidItem.OriginalSubtotal == 7.83m && paidItem.Discount == 1.75m
    && paidItem.Subtotal == 6.08m && paidItem.Tax == .91m, "Explicit line snapshot captured");
lines = SriInvoiceLineFactory.Build(payment, order);
totals = SriInvoiceLineFactory.CalculateTotals(lines, payment.OrderAmount);
doc.SubtotalBeforeDiscount = totals.SubtotalBeforeDiscount;
Check(doc.SubtotalBeforeDiscount == 7.83m && doc.SubtotalBeforeDiscount - doc.TotalDescuento == doc.TotalSinImpuestos
    && doc.TotalSinImpuestos + doc.TotalIva == doc.ImporteTotal, "Header reconciles with line snapshots");
var savedXml = XDocument.Parse(SriXmlBuilder.Build(new SriInvoiceData(config, payment, order, lines,
    new string('1', 49), 1, null, Document: doc)));
Check(Number(savedXml.Root!.Element("infoFactura")!, "totalDescuento") == paidItem.Discount, "XML uses saved header and detail discount");
void InvalidSnapshot(Action action, string message)
{
    try { action(); } catch (InvalidOperationException) { count++; return; }
    throw new Exception(message);
}
InvalidSnapshot(() => SriInvoiceLineFactory.Amounts(lines[0] with { Discount = 0 }), "Inconsistent discount must fail");
InvalidSnapshot(() => SriInvoiceLineFactory.Amounts(lines[0] with { Tax = null }), "Incomplete snapshot must fail");
InvalidSnapshot(() => SriInvoiceLineFactory.CalculateTotals(lines, payment.OrderAmount + .01m),
    "New persisted detail must match payment exactly");
doc.TotalDescuento = 0;
InvalidSnapshot(() => SriXmlBuilder.Build(new SriInvoiceData(config, payment, order, lines,
    new string('1', 49), 1, null, Document: doc)), "Inconsistent header must fail");
doc.TotalDescuento = totals.TotalDiscount;
Check(RideGenerator.Generate(config, doc, payment, order, lines, null).Length > 100,
    "PDF uses saved header and line snapshots");
doc.TotalIva += .01m;
InvalidSnapshot(() => RideGenerator.Generate(config, doc, payment, order, lines, null),
    "PDF must reject inconsistent header");
doc.TotalIva = totals.TotalTax;
foreach (var rate in new[] { 0m, 5m, 15m })
{
    var plain = new OrderPaymentItem { Quantity = 2, UnitPrice = 3, Total = 6, OriginalTotal = 6, TaxRatePercentage = rate };
    SriInvoiceLineFactory.CaptureAmounts(plain);
    Check(plain.Discount == 0 && plain.OriginalSubtotal == plain.Subtotal && plain.Subtotal + plain.Tax == plain.Total,
        "Explicit zero discount snapshot reconciles across tax rates");
}
var debit = new PaymentMethodConfig { Name = "Tarjeta", IsCard = true };
var cash = new PaymentMethodConfig { Name = "Efectivo", IsCash = true };
var transfer = new PaymentMethodConfig { Name = "Transferencia" };
XElement Payments(params PaymentLine[] paymentLines)
{
    var total = paymentLines.Where(l => !l.IsDeleted).Sum(l => l.AmountTendered - l.Change);
    var data = new SriInvoiceData(config, new OrderPayment { OrderAmount = total, Lines = paymentLines.ToList() },
        order, [new SriInvoiceLine("CHECK", "Payment check", 1, total, "0", 0)], new string('1', 49), 56, null);
    return XDocument.Parse(SriXmlBuilder.Build(data)).Root!.Element("infoFactura")!.Element("pagos")!;
}
PaymentLine Pay(PaymentMethodConfig method, CardPaymentType? card, decimal amount, decimal change = 0) =>
    new() { Config = method, CardPaymentType = card, AmountTendered = amount, Change = change };
foreach (var (line, expected) in new[] {
    (Pay(debit, CardPaymentType.Debit, 29.30m), "16"),
    (Pay(debit, CardPaymentType.Credit, 29.30m), "19"),
    (Pay(cash, null, 30, .70m), "01"),
    (Pay(transfer, null, 29.30m), "20") })
{
    var entry = Payments(line).Element("pago")!;
    Check(entry.Element("formaPago")!.Value == expected, $"SRI payment code {expected}");
    Check(Number(entry, "total") == 29.30m, "Net payment amount preserved");
}
var removedDebit = Pay(debit, CardPaymentType.Debit, 99); removedDebit.IsDeleted = true;
var mixedPayments = Payments(Pay(debit, CardPaymentType.Debit, 10), Pay(debit, CardPaymentType.Debit, 5),
    Pay(debit, CardPaymentType.Credit, 9.30m), Pay(cash, null, 5), removedDebit);
var byCode = mixedPayments.Elements("pago").ToDictionary(p => p.Element("formaPago")!.Value, p => Number(p, "total"));
Check(byCode.Count == 3 && byCode["16"] == 15 && byCode["19"] == 9.30m && byCode["01"] == 5,
    "Mixed payments group debit separately, ignoring deleted lines");
Check(!byCode.ContainsKey("08") && byCode.Values.Sum() == 29.30m, "No obsolete debit code or changed totals");
XDocument BuyerXml(Customer? buyer) => XDocument.Parse(SriXmlBuilder.Build(new SriInvoiceData(config,
    new OrderPayment { OrderAmount = 1 }, order, [new SriInvoiceLine("CHECK", "Identity check", 1, 1, "0", 0)],
    new string('1', 49), 63, buyer, new DateTime(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc))));
foreach (var (number, storedType, expectedType, expectedNumber) in new[] {
    ("1725637795001", TaxIdType.Cedula, "04", "1725637795001"),
    ("1725637795", TaxIdType.Ruc, "05", "1725637795"),
    (" 1725637795-001 ", TaxIdType.Cedula, "04", "1725637795001"),
    ("ab-123", TaxIdType.Passport, "06", "AB123"),
    ("9999999999999", TaxIdType.Cedula, "07", "9999999999999"),
})
{
    var buyer = new Customer { Name = "Identity checks", TaxId = number, TaxIdType = storedType };
    var rebuilt = BuyerXml(buyer);
    var header = rebuilt.Root!.Element("infoFactura")!;
    Check(header.Element("tipoIdentificacionComprador")!.Value == expectedType
        && header.Element("identificacionComprador")!.Value == expectedNumber, "Legacy buyer type detected from validated identification");
    Check(buyer.TaxIdType == storedType && buyer.TaxId == number, "XML generation does not modify customer records");
    Check(rebuilt.Root.Element("infoTributaria")!.Element("secuencial")!.Value == "000000063"
        && rebuilt.Root.Element("infoTributaria")!.Element("claveAcceso")!.Value == new string('1', 49)
        && header.Element("fechaEmision")!.Value == "25/10/2026" && Number(header, "importeTotal") == 1,
        "Buyer correction preserves access key, sequence, date and amount");
}
InvalidSnapshot(() => BuyerXml(new Customer { TaxId = "1725637794", TaxIdType = TaxIdType.Cedula }),
    "Invalid buyer identification must fail before sending to SRI");
Check(BuyerXml(null).Root!.Element("infoFactura")!.Element("tipoIdentificacionComprador")!.Value == "07",
    "Anonymous final consumer unchanged");
Check(BuyerXml(new Customer { TaxIdType = TaxIdType.FinalConsumer }).Root!.Element("infoFactura")!
    .Element("tipoIdentificacionComprador")!.Value == "07", "Explicit final consumer unchanged");
var fiscalPayment = new OrderPayment { OrderAmount = 29.30m, Lines = [Pay(debit, CardPaymentType.Debit, 29.30m)] };
var fiscalLine = new SriInvoiceLine("CHECK", "Internal payment correction", 1, 29.30m, "0", 0);
var fiscalDocument = new ElectronicDocument { TotalSinImpuestos = 29.30m, ImporteTotal = 29.30m,
    NumeroFactura = "001-003-000000099", ClaveAcceso = new string('1', 49), Environment = "1" };
var fiscalData = new SriInvoiceData(config, fiscalPayment, order, [fiscalLine], fiscalDocument.ClaveAcceso, 99, null,
    Document: fiscalDocument);
fiscalDocument.XmlSigned = SriXmlBuilder.Build(fiscalData);
var originalFiscalXml = fiscalDocument.XmlSigned;
fiscalPayment.OriginalPaymentLinesJson = "[]";
fiscalPayment.Lines = [Pay(cash, null, 10), Pay(transfer, null, 19.30m)];
var frozenPayments = SriInvoicePaymentSnapshot.Read(fiscalDocument, fiscalPayment)!;
Check(frozenPayments.Count == 1 && frozenPayments[0].Code == "16" && frozenPayments[0].Amount == 29.30m,
    "Internal correction retains fiscal payment codes and amounts from the existing XML");
var regeneratedPayments = XDocument.Parse(SriXmlBuilder.Build(fiscalData)).Root!.Element("infoFactura")!.Element("pagos")!;
Check(regeneratedPayments.Elements("pago").Count() == 1 && regeneratedPayments.Element("pago")!.Element("formaPago")!.Value == "16",
    "Invoice re-signing does not propagate cash reclassification into fiscal XML");
Check(RideGenerator.Generate(config, fiscalDocument, fiscalPayment, order, [fiscalLine], null).Length > 100
    && frozenPayments[0].Label == "TARJETA DE DEBITO" && fiscalDocument.XmlSigned == originalFiscalXml,
    "RIDE generation retains fiscal payment labels without modifying stored XML");
fiscalDocument.XmlAuthorized = originalFiscalXml.Replace("<formaPago>16</formaPago>", "<formaPago>19</formaPago>");
Check(SriInvoicePaymentSnapshot.Read(fiscalDocument, fiscalPayment)!.Single().Code == "19", "Authorized XML takes precedence over signed XML");
fiscalDocument.XmlAuthorized = "<invalid>";
InvalidSnapshot(() => SriInvoicePaymentSnapshot.Read(fiscalDocument, fiscalPayment), "Invalid fiscal snapshot must not silently use internal payment methods");
fiscalDocument.XmlAuthorized = originalFiscalXml.Replace("<total>29.30</total>", "<total>1.00</total>");
InvalidSnapshot(() => SriInvoicePaymentSnapshot.Read(fiscalDocument, fiscalPayment), "Fiscal payment total mismatch must fail");
fiscalDocument.XmlAuthorized = null; fiscalDocument.XmlSigned = null;
Check(SriInvoicePaymentSnapshot.Read(fiscalDocument, fiscalPayment) == null
    && XDocument.Parse(SriXmlBuilder.Build(fiscalData)).Root!.Element("infoFactura")!.Element("pagos")!.Elements("pago").Count() == 2,
    "First invoice after an earlier note correction uses current internal methods");
Console.WriteLine($"Invoice savings, payment code, buyer identity and fiscal payment snapshot checks passed: {count}. No database, SRI or email requests.");
