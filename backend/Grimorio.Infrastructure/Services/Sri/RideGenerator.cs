using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.POS;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Grimorio.Infrastructure.Services.Sri;

// Genera el PDF del RIDE (Representación Impresa del Documento Electrónico)
// según el estándar del SRI Ecuador para facturas electrónicas
public static class RideGenerator
{
    static RideGenerator()
    {
        // Licencia comunitaria de QuestPDF (gratis para proyectos no comerciales)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Generate(
        BranchTaxConfig config,
        ElectronicDocument doc,
        OrderPayment payment,
        Order order,
        List<OrderItem> items,
        Customer? customer)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20, Unit.Point);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    // ── Encabezado ────────────────────────────────────────────
                    col.Item().Row(row =>
                    {
                        // Datos del emisor
                        row.RelativeItem(2).Border(1).Padding(6).Column(ec =>
                        {
                            ec.Item().Text(config.RazonSocial).Bold().FontSize(11);
                            if (!string.IsNullOrEmpty(config.NombreComercial))
                                ec.Item().Text(config.NombreComercial).FontSize(10);
                            ec.Item().Text($"Dir: {config.Direccion}").FontSize(8);
                            ec.Item().Text($"RUC: {config.Ruc}").FontSize(8);
                            ec.Item().Text(config.ObligadoContabilidad
                                ? "OBLIGADO A LLEVAR CONTABILIDAD: SI"
                                : "OBLIGADO A LLEVAR CONTABILIDAD: NO").FontSize(8);
                        });

                        // Datos del comprobante
                        row.RelativeItem().Border(1).Padding(6).Column(dc =>
                        {
                            dc.Item().AlignCenter().Text("FACTURA").Bold().FontSize(12);
                            dc.Item().AlignCenter().Text($"No. {doc.NumeroFactura}").Bold().FontSize(10);
                            dc.Item().PaddingTop(4).Text($"NÚMERO DE AUTORIZACIÓN:").Bold().FontSize(8);
                            dc.Item().Text(doc.NumeroAutorizacion ?? "PENDIENTE").FontSize(7);
                            dc.Item().PaddingTop(4).Text($"FECHA Y HORA DE AUTORIZACIÓN:").Bold().FontSize(8);
                            dc.Item().Text(doc.FechaAutorizacion.HasValue
                                ? doc.FechaAutorizacion.Value.ToString("dd/MM/yyyy HH:mm:ss")
                                : "—").FontSize(8);
                            dc.Item().PaddingTop(4).Text($"AMBIENTE: {(config.Ambiente == "2" ? "PRODUCCIÓN" : "PRUEBAS")}").Bold().FontSize(8);
                            dc.Item().Text("EMISIÓN: NORMAL").FontSize(8);
                            dc.Item().PaddingTop(4).Text("CLAVE DE ACCESO:").Bold().FontSize(7);
                            dc.Item().Text(doc.ClaveAcceso).FontSize(6);
                        });
                    });

                    col.Item().PaddingTop(8);

                    // ── Datos del comprador ───────────────────────────────────
                    col.Item().Border(1).Padding(6).Column(bc =>
                    {
                        var razon = customer?.Name ?? "CONSUMIDOR FINAL";
                        var id = customer?.TaxId ?? "9999999999999";
                        var idType = customer?.TaxIdType.ToString() ?? "Consumidor Final";
                        bc.Item().Row(r =>
                        {
                            r.RelativeItem().Text($"Razón Social / Nombres: {razon}").FontSize(8);
                            r.ConstantItem(120).Text($"Identificación: {id}").FontSize(8);
                        });
                        bc.Item().Row(r =>
                        {
                            r.RelativeItem().Text($"Tipo de ID: {idType}").FontSize(8);
                            r.ConstantItem(120).Text($"Fecha: {payment.PaidAt:dd/MM/yyyy}").FontSize(8);
                        });
                        if (customer?.Address != null)
                            bc.Item().Text($"Dirección: {customer.Address}").FontSize(8);
                    });

                    col.Item().PaddingTop(8);

                    // ── Detalle de productos ──────────────────────────────────
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(60);   // código
                            cols.RelativeColumn(3);    // descripción
                            cols.ConstantColumn(45);   // cantidad
                            cols.ConstantColumn(60);   // precio unit
                            cols.ConstantColumn(50);   // descuento
                            cols.ConstantColumn(65);   // subtotal
                        });

                        // Encabezado de la tabla
                        static IContainer HeaderCell(IContainer c) => c.Border(1).Background(Colors.Grey.Lighten2).Padding(3);
                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("Cód. Principal").Bold().FontSize(8);
                            h.Cell().Element(HeaderCell).Text("Descripción").Bold().FontSize(8);
                            h.Cell().Element(HeaderCell).AlignRight().Text("Cant.").Bold().FontSize(8);
                            h.Cell().Element(HeaderCell).AlignRight().Text("P. Unit. (sin IVA)").Bold().FontSize(8);
                            h.Cell().Element(HeaderCell).AlignRight().Text("Desc.").Bold().FontSize(8);
                            h.Cell().Element(HeaderCell).AlignRight().Text("Subtotal").Bold().FontSize(8);
                        });

                        // Filas de ítems
                        static IContainer DataCell(IContainer c) => c.Border(1).Padding(3);
                        foreach (var item in items)
                        {
                            var taxPct = item.MenuItem?.TaxRate?.Percentage;
                            var gross = item.UnitPrice * item.Quantity;
                            var discount = Math.Round(gross * (item.DiscountPct / 100m), 2);
                            var netInc = gross - discount;
                            decimal taxableBase = taxPct.HasValue && taxPct.Value > 0
                                ? Math.Round(netInc / (1m + taxPct.Value / 100m), 2)
                                : netInc;
                            var unitPriceSinIva = taxPct.HasValue && taxPct.Value > 0
                                ? item.UnitPrice / (1m + taxPct.Value / 100m)
                                : item.UnitPrice;

                            var code = item.MenuItem?.InternalCode ?? "—";
                            table.Cell().Element(DataCell).Text(code).FontSize(8);
                            table.Cell().Element(DataCell).Text(item.MenuItem?.Name ?? "—").FontSize(8);
                            table.Cell().Element(DataCell).AlignRight().Text($"{item.Quantity:F2}").FontSize(8);
                            table.Cell().Element(DataCell).AlignRight().Text($"${unitPriceSinIva:F4}").FontSize(8);
                            table.Cell().Element(DataCell).AlignRight().Text($"${discount:F2}").FontSize(8);
                            table.Cell().Element(DataCell).AlignRight().Text($"${taxableBase:F2}").FontSize(8);
                        }
                    });

                    col.Item().PaddingTop(8);

                    // ── Totales ───────────────────────────────────────────────
                    col.Item().AlignRight().Width(220).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn();
                            cols.ConstantColumn(80);
                        });

                        void TotRow(string label, string value, bool bold = false)
                        {
                            var lbl = table.Cell().Border(1).Padding(3).Text(label).FontSize(8);
                            var val = table.Cell().Border(1).Padding(3).AlignRight().Text(value).FontSize(8);
                            if (bold) { lbl.Bold(); val.Bold(); }
                        }

                        if (order.TaxableBase15 > 0)
                            TotRow("SUBTOTAL IVA 15%", $"${order.TaxableBase15:F2}");
                        if (order.TaxableBase0 > 0)
                            TotRow("SUBTOTAL IVA 0%", $"${order.TaxableBase0:F2}");
                        if (order.TaxableBaseExempt > 0)
                            TotRow("SUBTOTAL NO OBJETO IVA", $"${order.TaxableBaseExempt:F2}");
                        TotRow("DESCUENTO", "$0.00");
                        if (order.Iva15 > 0)
                            TotRow("IVA 15%", $"${order.Iva15:F2}");
                        if (order.Ice > 0)
                            TotRow("ICE", $"${order.Ice:F2}");
                        TotRow("PROPINA", "$0.00");
                        TotRow("VALOR TOTAL", $"${order.Total:F2}", bold: true);
                    });

                    col.Item().PaddingTop(12);

                    // ── Información adicional ─────────────────────────────────
                    col.Item().Border(1).Padding(6).Column(ia =>
                    {
                        ia.Item().Text("INFORMACIÓN ADICIONAL").Bold().FontSize(8);
                        ia.Item().Text($"Orden #: {order.Number}").FontSize(8);
                        if (customer?.Email != null)
                            ia.Item().Text($"Email: {customer.Email}").FontSize(8);
                        if (customer?.Phone != null)
                            ia.Item().Text($"Teléfono: {customer.Phone}").FontSize(8);
                    });
                });
            });
        }).GeneratePdf();
    }
}
