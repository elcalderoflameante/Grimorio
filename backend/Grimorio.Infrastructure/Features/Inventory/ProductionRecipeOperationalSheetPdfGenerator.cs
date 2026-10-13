using System.Globalization;
using Grimorio.Domain.Entities.Inventory;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Grimorio.Infrastructure.Features.Inventory;

internal static class ProductionRecipeOperationalSheetPdfGenerator
{
    static ProductionRecipeOperationalSheetPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Generate(
        ProductionRecipe recipe,
        byte[]? logoBytes,
        string restaurantName,
        DateTime generatedAtLocal)
    {
        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(28);
                page.MarginVertical(24);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial").FontColor("#1F2937"));
                page.Header().Element(c => Header(c, recipe, logoBytes, restaurantName));
                page.Content().Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Element(c => InternalNotice(c, restaurantName, recipe.AccessLevel));
                    column.Item().Element(c => Summary(c, recipe));
                    column.Item().Element(c => Ingredients(c, recipe));
                    column.Item().Element(c => Preparation(c, recipe));
                    if (!string.IsNullOrWhiteSpace(recipe.Notes))
                        column.Item().Element(c => NoteBox(c, "Observaciones", recipe.Notes!));
                });
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Ficha de producción generada el ");
                    text.Span(generatedAtLocal.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)).SemiBold();
                    text.Span($" - Documento interno de {restaurantName}");
                });
            });
        }).GeneratePdf();
    }

    private static void Header(IContainer container, ProductionRecipe recipe, byte[]? logoBytes, string restaurantName)
    {
        container.BorderBottom(1).BorderColor("#D0D5DD").PaddingBottom(8).Row(row =>
        {
            row.ConstantItem(76).Height(54).Element(c => LogoBox(c, logoBytes, restaurantName));
            row.ConstantItem(12);
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(restaurantName).FontSize(11).SemiBold().FontColor("#667085");
                column.Item().Text("Ficha operativa de producción").FontSize(18).Bold();
                column.Item().Text(recipe.OutputArticle?.Name ?? "Producto elaborado").FontSize(14).SemiBold().FontColor("#344054");
            });
            row.ConstantItem(150).AlignRight().AlignMiddle()
                .Text(recipe.AccessLevel == ProductionRecipeAccessLevel.Restricted ? "Acceso restringido" : "Uso general")
                .FontSize(9)
                .SemiBold()
                .FontColor(recipe.AccessLevel == ProductionRecipeAccessLevel.Restricted ? "#B42318" : "#027A48");
        });
    }

    private static void LogoBox(IContainer container, byte[]? logoBytes, string restaurantName)
    {
        var box = container.Border(1).BorderColor("#EAECF0").Padding(3);
        if (logoBytes is null || logoBytes.Length == 0)
        {
            box.AlignMiddle().AlignCenter().Text(restaurantName).FontSize(8).SemiBold().FontColor("#667085");
            return;
        }

        box.Image(logoBytes).FitArea();
    }

    private static void InternalNotice(IContainer container, string restaurantName, ProductionRecipeAccessLevel accessLevel)
    {
        var restricted = accessLevel == ProductionRecipeAccessLevel.Restricted;
        container
            .Border(1)
            .BorderColor(restricted ? "#FDA29B" : "#FEDF89")
            .Background(restricted ? "#FFFBFA" : "#FFFCF5")
            .Padding(8)
            .Text(restricted
                ? $"Documento confidencial de {restaurantName}. Acceso limitado a personal autorizado. No distribuir, reproducir ni modificar sin autorización."
                : $"Documento operativo interno de {restaurantName}. Uso exclusivo para capacitación y estandarización del personal.")
            .FontColor(restricted ? "#B42318" : "#93370D")
            .FontSize(8);
    }

    private static void Summary(IContainer container, ProductionRecipe recipe)
    {
        container.Column(column =>
        {
            column.Spacing(5);
            column.Item().Element(c => SectionTitle(c, "Datos generales"));
            column.Item().Row(row =>
            {
                row.RelativeItem().Element(c => KeyValue(c, "Producto", recipe.OutputArticle?.Name ?? "No disponible"));
                row.RelativeItem().Element(c => KeyValue(c, "Rendimiento", $"{FormatQty(recipe.OutputQuantity)} {recipe.OutputUnit?.Symbol ?? recipe.OutputUnit?.Name}"));
            });
        });
    }

    private static void Ingredients(IContainer container, ProductionRecipe recipe)
    {
        var ingredients = recipe.Ingredients.Where(x => !x.IsDeleted).OrderBy(x => x.Article?.Name).ToList();
        container.Column(column =>
        {
            column.Spacing(5);
            column.Item().Element(c => SectionTitle(c, "Ingredientes"));
            if (ingredients.Count == 0)
            {
                column.Item().Element(c => Empty(c, "No se han registrado ingredientes."));
                return;
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2.5f);
                    columns.RelativeColumn(0.8f);
                    columns.RelativeColumn(0.8f);
                    columns.RelativeColumn(2.0f);
                });
                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Ingrediente");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Cant.");
                    header.Cell().Element(HeaderCell).Text("Unidad");
                    header.Cell().Element(HeaderCell).Text("Observación");
                });
                foreach (var ingredient in ingredients)
                {
                    table.Cell().Element(Cell).Text(ingredient.Article?.Name ?? "Artículo no disponible");
                    table.Cell().Element(Cell).AlignRight().Text(FormatQty(ingredient.Quantity));
                    table.Cell().Element(Cell).Text(ingredient.Unit?.Symbol ?? ingredient.Unit?.Name ?? string.Empty);
                    table.Cell().Element(Cell).Text(ingredient.Notes ?? string.Empty);
                }
            });
        });
    }

    private static void Preparation(IContainer container, ProductionRecipe recipe)
    {
        var steps = recipe.PreparationSteps.Where(x => !x.IsDeleted).OrderBy(x => x.StepNumber).ToList();
        container.Column(column =>
        {
            column.Spacing(5);
            column.Item().Element(c => SectionTitle(c, "Preparación"));
            if (steps.Count == 0)
            {
                column.Item().Element(c => Empty(c, "No se han registrado pasos de preparación."));
                return;
            }

            foreach (var step in steps)
            {
                column.Item().Border(1).BorderColor("#EAECF0").Padding(7).Column(stepColumn =>
                {
                    stepColumn.Spacing(3);
                    stepColumn.Item().Text(text =>
                    {
                        text.Span($"Paso {step.StepNumber}").SemiBold();
                        if (step.EstimatedMinutes.HasValue) text.Span($"  {step.EstimatedMinutes} min").FontColor("#667085");
                        if (!string.IsNullOrWhiteSpace(step.Temperature)) text.Span($"  {step.Temperature}").FontColor("#667085");
                    });
                    stepColumn.Item().Text(step.Instructions);
                });
            }
        });
    }

    private static void SectionTitle(IContainer container, string title) =>
        container.Background("#F2F4F7").PaddingVertical(5).PaddingHorizontal(7).Text(title).SemiBold().FontColor("#344054");

    private static void KeyValue(IContainer container, string key, string value) =>
        container.Row(row =>
        {
            row.ConstantItem(88).Text(key).FontColor("#667085");
            row.RelativeItem().Text(value).SemiBold();
        });

    private static void Empty(IContainer container, string text) =>
        container.Border(1).BorderColor("#EAECF0").Padding(8).Text(text).FontColor("#98A2B3");

    private static void NoteBox(IContainer container, string title, string text) =>
        container.Border(1).BorderColor("#EAECF0").Padding(8).Column(column =>
        {
            column.Spacing(3);
            column.Item().Text(title).SemiBold();
            column.Item().Text(text);
        });

    private static IContainer HeaderCell(IContainer container) =>
        container.BorderBottom(1).BorderColor("#D0D5DD").Background("#F9FAFB").Padding(5).DefaultTextStyle(x => x.SemiBold());

    private static IContainer Cell(IContainer container) =>
        container.BorderBottom(1).BorderColor("#EAECF0").Padding(5);

    private static string FormatQty(decimal value) => value.ToString("0.####", CultureInfo.InvariantCulture);
}
