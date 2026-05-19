using System.Globalization;
using Grimorio.Application.DTOs;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.Menu;
using Grimorio.Domain.Entities.POS;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Grimorio.Infrastructure.Services.Sri;

public static class RideGenerator
{
    static RideGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Generate(
        BranchTaxConfig config,
        ElectronicDocument doc,
        OrderPayment payment,
        Order order,
        List<OrderItem> items,
        Customer? customer,
        InvoiceTemplateDto? template = null)
    {
        var logoBytes = ParseLogo(template?.LogoBase64);
        var fechaEc = EcuadorTime.FromUtc(payment.PaidAt);
        var ambiente = config.Ambiente == "2" ? "PRODUCCION" : "PRUEBAS";

        return Build(container =>
            RenderRide(container, config, doc, payment, order, items, customer, logoBytes, fechaEc, ambiente, template));
    }

    public static byte[] GeneratePreview(BranchTaxConfig config, InvoiceTemplateDto template)
    {
        var logoBytes = ParseLogo(template.LogoBase64);
        var fechaEc = EcuadorTime.FromUtc(DateTime.UtcNow);
        var ambiente = config.Ambiente == "2" ? "PRODUCCION" : "PRUEBAS";

        var fakeDoc = new ElectronicDocument
        {
            NumeroFactura = $"{config.CodigoEstablecimiento.PadLeft(3, '0')}-{config.PuntoEmision.PadLeft(3, '0')}-000000001",
            ClaveAcceso = "1305202601179181248400120100050001078807846333316",
            NumeroAutorizacion = "1305202601179181248400120100050001078807846333316",
            FechaAutorizacion = DateTime.UtcNow,
            Status = ElectronicDocumentStatus.Authorized,
            TotalSinImpuestos = 16.37m,
            TotalDescuento = 0m,
            TotalIva = 2.46m,
            ImporteTotal = 18.83m,
        };

        var fakeCustomer = new Customer
        {
            Name = "PROANO PORTILLA KARINA ALEXANDRA",
            TaxId = "1721141792001",
            TaxIdType = TaxIdType.Ruc,
            Address = "E LT-47 y S24E ANCONCITO",
            Email = "cliente@ejemplo.com",
            Phone = "0996692039",
        };

        var fakePayment = new OrderPayment { PaidAt = DateTime.UtcNow, Customer = fakeCustomer, Lines = [] };
        var fakeOrder = new Order
        {
            Number = 1,
            TaxableBase15 = 16.37m,
            TaxableBase0 = 0m,
            TaxableBaseExempt = 0m,
            Iva15 = 2.46m,
            Ice = 0m,
            Total = 18.83m,
        };

        var fakeTaxRate = new TaxRate { Percentage = 15m, SriCode = "4" };
        var fakeItems = new List<OrderItem>
        {
            new()
            {
                UnitPrice = 18.83m,
                Quantity = 1,
                DiscountPct = 0,
                MenuItem = new MenuItem
                {
                    Name = "LATEX DURATEX-40ANIV BASE-T 3.785LT WESCO",
                    InternalCode = "77215",
                    TaxRate = fakeTaxRate
                }
            },
        };

        return Build(container =>
            RenderRide(container, config, fakeDoc, fakePayment, fakeOrder, fakeItems, fakeCustomer, logoBytes, fechaEc, ambiente, template));
    }

    private static byte[] Build(Action<IContainer> content)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(26);
                page.MarginVertical(22);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));
                page.Content().Element(content);
            });
        }).GeneratePdf();
    }

    private static void RenderRide(
        IContainer root,
        BranchTaxConfig config,
        ElectronicDocument doc,
        OrderPayment payment,
        Order order,
        List<OrderItem> items,
        Customer? customer,
        byte[]? logoBytes,
        DateTime fechaEc,
        string ambiente,
        InvoiceTemplateDto? template)
    {
        root.Column(col =>
        {
            col.Spacing(8);

            col.Item().Row(row =>
            {
                row.RelativeItem(0.82f).Column(left =>
                {
                    left.Item().Height(112).AlignCenter().Element(c => DrawLogo(c, logoBytes, config));
                    left.Item().Height(10);
                    left.Item().Element(c => IssuerBox(c, config));
                });

                row.ConstantItem(8);

                row.RelativeItem(1.05f).Element(c =>
                    AuthorizationBox(c, config, doc, ambiente));
            });

            col.Item().Element(c => CustomerBox(c, customer, fechaEc));
            col.Item().Element(c => ItemsTable(c, items));

            col.Item().Row(row =>
            {
                row.RelativeItem(1.35f).Element(c => AdditionalInfoBox(c, order, customer, template));
                row.ConstantItem(10);
                row.RelativeItem(1.0f).Element(c => TotalsTable(c, order, items));
            });

            col.Item().Width(310).Element(c => PaymentTable(c, payment, order));
        });
    }

    private static void DrawLogo(IContainer container, byte[]? logoBytes, BranchTaxConfig config)
    {
        if (logoBytes == null)
        {
            container.AlignMiddle().AlignCenter().Text(config.NombreComercial ?? config.RazonSocial)
                .Bold().FontSize(14);
            return;
        }

        container.Image(logoBytes).FitArea();
    }

    private static void IssuerBox(IContainer container, BranchTaxConfig config)
    {
        container.Border(1).Padding(8).Column(col =>
        {
            col.Spacing(7);
            col.Item().Text(config.RazonSocial).Bold().FontSize(12);
            col.Item().Text(t =>
            {
                t.Span("DIRECCION MATRIZ:").Bold();
                t.Span(config.Direccion);
            });
            col.Item().Text(t =>
            {
                t.Span("DIRECCION SUCURSAL:").Bold();
                t.Span(config.Direccion);
            });
            col.Item().Text(t =>
            {
                t.Span("Contribuyente Especial Nro:  ").Bold();
                t.Span(string.IsNullOrWhiteSpace(config.ContribuyenteEspecial) ? "NO ESPECIAL" : config.ContribuyenteEspecial);
            });
            col.Item().Text(config.ObligadoContabilidad
                ? "OBLIGADO A LLEVAR CONTABILIDAD:SI"
                : "OBLIGADO A LLEVAR CONTABILIDAD:NO");
        });
    }

    private static void AuthorizationBox(IContainer container, BranchTaxConfig config, ElectronicDocument doc, string ambiente)
    {
        container.Border(1).Padding(8).MinHeight(250).Column(col =>
        {
            col.Spacing(8);
            col.Item().Text($"RUC: {config.Ruc}").Bold();
            col.Item().Text("FACTURA").Bold();
            col.Item().Text($"NO: {doc.NumeroFactura}");
            col.Item().Text("NUMERO DE AUTORIZACION");
            col.Item().Text(doc.NumeroAutorizacion ?? doc.ClaveAcceso).FontSize(8);
            col.Item().Text("FECHA Y HORA DE AUTORIZACION:");
            col.Item().Text(doc.FechaAutorizacion.HasValue
                ? EcuadorTime.FromUtc(doc.FechaAutorizacion.Value).ToString("yyyy-MM-ddTHH:mm:ss-05:00")
                : "");
            col.Item().Text($"AMBIENTE: {ambiente}");
            col.Item().Text("EMISION: NORMAL");
            col.Item().Text("CLAVE DE ACCESO");
            col.Item().Height(36).Element(c => PseudoBarcode(c, doc.ClaveAcceso));
            col.Item().Text(doc.ClaveAcceso).FontSize(8);
        });
    }

    private static void CustomerBox(IContainer container, Customer? customer, DateTime fechaEc)
    {
        var razon = customer?.Name ?? "CONSUMIDOR FINAL";
        var id = customer?.TaxId ?? "9999999999999";
        var address = string.IsNullOrWhiteSpace(customer?.Address) ? "S/N" : customer.Address;

        container.Border(1).Padding(7).Column(col =>
        {
            col.Spacing(5);
            col.Item().Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("Razon Social / Nombres y Apellidos: ");
                    t.Span(razon);
                });
                row.ConstantItem(230).Text($"Identificacion: {id}");
            });
            col.Item().Text($"Fecha Emision: {fechaEc:dd/MM/yyyy}");
            col.Item().Text($"Direccion: {address}");
        });
    }

    private static void ItemsTable(IContainer container, List<OrderItem> items)
    {
        var showDiscount = items.Any(i => i.DiscountPct > 0);

        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(112);
                cols.ConstantColumn(62);
                cols.RelativeColumn();
                cols.ConstantColumn(84);
                if (showDiscount) cols.ConstantColumn(62);
                cols.ConstantColumn(78);
            });

            table.Header(h =>
            {
                HeaderCell(h.Cell(), "Cod. Principal", center: true);
                HeaderCell(h.Cell(), "Cant", center: true);
                HeaderCell(h.Cell(), "Descripcion", center: true);
                HeaderCell(h.Cell(), "Precio Unitario", center: true);
                if (showDiscount) HeaderCell(h.Cell(), "Descuento", center: true);
                HeaderCell(h.Cell(), "Precio Total", center: true);
            });

            foreach (var item in items)
            {
                var taxPct = item.MenuItem?.TaxRate?.Percentage ?? 0m;
                var gross = item.UnitPrice * item.Quantity;
                var discount = Math.Round(gross * (item.DiscountPct / 100m), 2);
                var netInclusive = gross - discount;
                var taxableBase = taxPct > 0
                    ? Math.Round(netInclusive / (1m + taxPct / 100m), 3)
                    : netInclusive;
                var unitSinIva = taxPct > 0
                    ? item.UnitPrice / (1m + taxPct / 100m)
                    : item.UnitPrice;

                BodyCell(table.Cell(), item.MenuItem?.InternalCode ?? "-", fontSize: 8);
                BodyCell(table.Cell(), Fmt(item.Quantity, 2), right: true, fontSize: 8);
                BodyCell(table.Cell(), item.MenuItem?.Name ?? "-", fontSize: 8);
                BodyCell(table.Cell(), Fmt(unitSinIva, 6), right: true, fontSize: 8);
                if (showDiscount) BodyCell(table.Cell(), Fmt(discount, 2), right: true, fontSize: 8);
                BodyCell(table.Cell(), Fmt(taxableBase, 3), right: true, fontSize: 8);
            }
        });
    }

    private static void AdditionalInfoBox(
        IContainer container,
        Order order,
        Customer? customer,
        InvoiceTemplateDto? template)
    {
        var customText = template?.PdfBlocks?.FirstOrDefault(b => b.Type == "footer")?.CustomText;
        var address = string.IsNullOrWhiteSpace(customer?.Address) ? null : customer.Address;

        container.Border(1).MinHeight(110).Padding(6).Column(col =>
        {
            col.Item().Text("Informacion Adicional").Bold();
            if (!string.IsNullOrWhiteSpace(address))
                col.Item().Text($"DIRECCION: {address}");
            col.Item().Text($"ORDEN: {order.Number}");
            if (!string.IsNullOrWhiteSpace(customer?.Email))
                col.Item().Text($"EMAIL: {customer.Email}");
            if (!string.IsNullOrWhiteSpace(customer?.Phone))
                col.Item().Text($"TELEFONOS: {customer.Phone}");

            if (!string.IsNullOrWhiteSpace(customText))
            {
                col.Item().Height(5);
                foreach (var line in customText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    col.Item().Text(line.Trim());
            }
        });
    }

    private static void TotalsTable(IContainer container, Order order, List<OrderItem> items)
    {
        var discount = items.Sum(i => Math.Round(i.UnitPrice * i.Quantity * (i.DiscountPct / 100m), 2));
        var subtotal = order.TaxableBase15 + order.TaxableBase0 + order.TaxableBaseExempt;

        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn();
                cols.ConstantColumn(78);
            });

            TotalRow(table, "SUBTOTAL IVA 15%", order.TaxableBase15);
            TotalRow(table, "SUBTOTAL 0%", order.TaxableBase0);
            TotalRow(table, "SUBTOTAL SIN IMPUESTOS", subtotal);
            if (discount > 0) TotalRow(table, "DESCUENTO", discount);
            TotalRow(table, "ICE", order.Ice);
            TotalRow(table, "IVA 15%", order.Iva15);
            TotalRow(table, "VALOR TOTAL", order.Total);
        });
    }

    private static void PaymentTable(IContainer container, OrderPayment payment, Order order)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn();
                cols.ConstantColumn(70);
            });

            HeaderCell(table.Cell(), "Forma de pago", center: true);
            HeaderCell(table.Cell(), "Valor", center: true);
            BodyCell(table.Cell(), GetPaymentMethodLabel(payment), fontSize: 8);
            BodyCell(table.Cell(), Fmt(order.Total, 2), right: true, fontSize: 8);
        });
    }

    private static void HeaderCell(IContainer cell, string text, bool center = false)
    {
        cell.Border(1).PaddingHorizontal(5).PaddingVertical(2)
            .Text(t =>
            {
                if (center) t.AlignCenter();
                t.Span(text).FontSize(8);
            });
    }

    private static void BodyCell(IContainer cell, string text, bool right = false, float fontSize = 9)
    {
        cell.Border(1).PaddingHorizontal(5).PaddingVertical(3)
            .Text(t =>
            {
                if (right) t.AlignRight();
                t.Span(text).FontSize(fontSize);
            });
    }

    private static void TotalRow(TableDescriptor table, string label, decimal value)
    {
        BodyCell(table.Cell(), label, fontSize: 8);
        BodyCell(table.Cell(), Fmt(value, 2), right: true, fontSize: 8);
    }

    private static string Fmt(decimal value, int decimals) =>
        value.ToString($"F{decimals}", CultureInfo.InvariantCulture);

    private static void PseudoBarcode(IContainer container, string value)
    {
        var bars = BuildBarcodeBars(value);
        container.Row(row =>
        {
            foreach (var bar in bars)
            {
                var segment = row.RelativeItem(bar.Width);
                if (bar.Black)
                    segment.Background(Colors.Black);
            }
        });
    }

    private static List<(bool Black, int Width)> BuildBarcodeBars(string value)
    {
        var bars = new List<(bool Black, int Width)>();
        foreach (var ch in value.Where(char.IsDigit))
        {
            var digit = ch - '0';
            bars.Add((true, digit % 3 + 1));
            bars.Add((false, 1));
            bars.Add((true, digit % 2 + 1));
            bars.Add((false, 1));
        }
        return bars.Count == 0 ? [(true, 1), (false, 1), (true, 1)] : bars;
    }

    private static byte[]? ParseLogo(string? logoBase64)
    {
        if (string.IsNullOrWhiteSpace(logoBase64)) return null;
        try
        {
            var data = logoBase64.Contains(',') ? logoBase64.Split(',')[1] : logoBase64;
            return Convert.FromBase64String(data);
        }
        catch
        {
            return null;
        }
    }

    private static string GetPaymentMethodLabel(OrderPayment payment)
    {
        var lines = payment.Lines?.ToList();
        if (lines == null || lines.Count == 0) return "SIN UTILIZACION DEL SISTEMA FINANCIERO";

        var first = lines[0];
        if (first.Config?.IsCash == true) return "SIN UTILIZACION DEL SISTEMA FINANCIERO";
        if (first.Config?.Name?.Contains("debito", StringComparison.OrdinalIgnoreCase) == true) return "TARJETA DE DEBITO";
        if (first.Config?.Name?.Contains("credito", StringComparison.OrdinalIgnoreCase) == true) return "TARJETA DE CREDITO";
        if (first.Config?.Name?.Contains("transfer", StringComparison.OrdinalIgnoreCase) == true) return "TRANSFERENCIA";
        return first.Config?.Name?.ToUpperInvariant() ?? "OTROS CON UTILIZACION DEL SISTEMA FINANCIERO";
    }
}
