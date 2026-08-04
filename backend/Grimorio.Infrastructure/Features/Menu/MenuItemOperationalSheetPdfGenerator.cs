using System.Globalization;
using Grimorio.Domain.Entities.Menu;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Grimorio.Infrastructure.Features.Menu;

internal static class MenuItemOperationalSheetPdfGenerator
{
    static MenuItemOperationalSheetPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Generate(MenuItem item, byte[]? imageBytes)
    {
        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(28);
                page.MarginVertical(24);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial").FontColor("#1F2937"));
                page.Header().Element(c => Header(c, item));
                page.Content().Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Element(c => Summary(c, item, imageBytes));
                    column.Item().Element(c => Recipe(c, item));
                    column.Item().Element(c => Modifiers(c, item));
                    column.Item().Element(c => Preparation(c, item));
                });
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Ficha operativa generada el ");
                    text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)).SemiBold();
                    text.Span(" - Uso interno");
                });
            });
        }).GeneratePdf();
    }

    private static void Header(IContainer container, MenuItem item)
    {
        container.BorderBottom(1).BorderColor("#D0D5DD").PaddingBottom(8).Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("Ficha operativa").FontSize(18).Bold();
                column.Item().Text(item.Name).FontSize(14).SemiBold().FontColor("#344054");
            });
            row.ConstantItem(150).AlignRight().AlignMiddle().Text(item.InternalCode is null
                    ? "Recetario interno"
                    : $"Codigo: {item.InternalCode}")
                .FontSize(9)
                .FontColor("#667085");
        });
    }

    private static void Summary(IContainer container, MenuItem item, byte[]? imageBytes)
    {
        container.Row(row =>
        {
            row.ConstantItem(150).Height(112).Element(c => ImageBox(c, imageBytes));
            row.ConstantItem(14);
            row.RelativeItem().Column(column =>
            {
                column.Spacing(6);
                column.Item().Element(c => SectionTitle(c, "Datos generales"));
                column.Item().Element(c => KeyValue(c, "Categoria", item.Category?.Name ?? "Sin categoria"));
                column.Item().Element(c => KeyValue(c, "Estacion", item.Station?.Name ?? "Sin estacion"));
                column.Item().Element(c => KeyValue(c, "Rendimiento", item.Preparation?.Yield ?? "No definido"));
                column.Item().Element(c => KeyValue(c, "Tiempo estimado", item.Preparation?.EstimatedMinutes is null ? "No definido" : $"{item.Preparation.EstimatedMinutes} min"));
                column.Item().Element(c => KeyValue(c, "Temperatura", string.IsNullOrWhiteSpace(item.Preparation?.Temperature) ? "No aplica" : item.Preparation!.Temperature!));
            });
        });
    }

    private static void ImageBox(IContainer container, byte[]? imageBytes)
    {
        var box = container.Border(1).BorderColor("#D0D5DD").Background("#F9FAFB").Padding(4);
        if (imageBytes is null || imageBytes.Length == 0)
        {
            box.AlignMiddle().AlignCenter().Text("Sin imagen").FontColor("#98A2B3");
            return;
        }

        box.Image(imageBytes).FitArea();
    }

    private static void Recipe(IContainer container, MenuItem item)
    {
        container.Column(column =>
        {
            column.Spacing(5);
            column.Item().Element(c => SectionTitle(c, "Receta"));

            var recipe = item.Recipe
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.CreatedAt)
                .ToList();

            if (recipe.Count == 0)
            {
                column.Item().Element(c => Empty(c, "No se han registrado ingredientes."));
                return;
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2.3f);
                    columns.RelativeColumn(0.8f);
                    columns.RelativeColumn(0.8f);
                    columns.RelativeColumn(2.0f);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Ingrediente");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Cant.");
                    header.Cell().Element(HeaderCell).Text("Unidad");
                    header.Cell().Element(HeaderCell).Text("Notas");
                });

                foreach (var ingredient in recipe)
                {
                    var name = ingredient.Type == RecipeIngredientType.SubRecipe
                        ? $"Subreceta: {ingredient.SubRecipe?.Name ?? "Sin nombre"}"
                        : ingredient.Article?.Name ?? "Articulo no disponible";

                    table.Cell().Element(Cell).Text(name);
                    table.Cell().Element(Cell).AlignRight().Text(FormatQty(ingredient.Quantity));
                    table.Cell().Element(Cell).Text(ingredient.Unit?.Symbol ?? ingredient.Unit?.Name ?? "");
                    table.Cell().Element(Cell).Text(ingredient.Notes ?? "");

                    if (ingredient.Type == RecipeIngredientType.SubRecipe && ingredient.SubRecipe is not null)
                    {
                        foreach (var subIngredient in ingredient.SubRecipe.Ingredients.Where(x => !x.IsDeleted).OrderBy(x => x.Article?.Name))
                        {
                            table.Cell().Element(SubCell).Text($"  - {subIngredient.Article?.Name ?? "Articulo no disponible"}");
                            table.Cell().Element(SubCell).AlignRight().Text(FormatQty(subIngredient.Quantity));
                            table.Cell().Element(SubCell).Text(subIngredient.Unit?.Symbol ?? subIngredient.Unit?.Name ?? "");
                            table.Cell().Element(SubCell).Text(subIngredient.Notes ?? $"Para {FormatQty(ingredient.SubRecipe.OutputQuantity)} {ingredient.SubRecipe.OutputUnit?.Symbol ?? ingredient.SubRecipe.OutputUnit?.Name}");
                        }
                    }
                }
            });
        });
    }

    private static void Modifiers(IContainer container, MenuItem item)
    {
        var groups = item.ModifierGroups
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToList();

        container.Column(column =>
        {
            column.Spacing(5);
            column.Item().Element(c => SectionTitle(c, "Modificadores"));

            if (groups.Count == 0)
            {
                column.Item().Element(c => Empty(c, "No tiene modificadores configurados."));
                return;
            }

            foreach (var group in groups)
            {
                var rule = group.IsRequired ? "Obligatorio" : "Opcional";
                column.Item().Border(1).BorderColor("#EAECF0").Padding(7).Column(groupColumn =>
                {
                    groupColumn.Spacing(4);
                    groupColumn.Item().Text($"{group.Name} - {rule} - Selecciones {group.MinSelections}-{group.MaxSelections}").SemiBold();

                    var options = group.Options
                        .Where(x => !x.IsDeleted && x.IsActive)
                        .OrderBy(x => x.DisplayOrder)
                        .ThenBy(x => x.Name)
                        .ToList();

                    if (options.Count == 0)
                    {
                        groupColumn.Item().Text("Sin opciones activas.").FontColor("#98A2B3");
                        return;
                    }

                    foreach (var option in options)
                    {
                        var tracked = option.Article is null || option.Quantity <= 0
                            ? "sin descuento de inventario"
                            : $"{FormatQty(option.Quantity)} {option.Unit?.Symbol ?? option.Unit?.Name} de {option.Article.Name}";
                        var price = option.PriceDelta == 0 ? "" : $" - adicional ${option.PriceDelta:0.00}";
                        groupColumn.Item().Text($"- {option.Name}: {tracked}{price}");
                    }
                });
            }
        });
    }

    private static void Preparation(IContainer container, MenuItem item)
    {
        var preparation = item.Preparation is null || item.Preparation.IsDeleted ? null : item.Preparation;
        container.Column(column =>
        {
            column.Spacing(5);
            column.Item().Element(c => SectionTitle(c, "Preparacion"));

            if (preparation is null)
            {
                column.Item().Element(c => Empty(c, "No se han registrado instrucciones de preparacion."));
                return;
            }

            var steps = preparation.Steps
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.StepNumber)
                .ToList();

            if (steps.Count == 0)
            {
                column.Item().Element(c => Empty(c, "No se han registrado pasos."));
            }
            else
            {
                foreach (var step in steps)
                {
                    column.Item().Border(1).BorderColor(step.IsCritical ? "#FDA29B" : "#EAECF0").Padding(7).Column(stepColumn =>
                    {
                        stepColumn.Spacing(3);
                        stepColumn.Item().Text(text =>
                        {
                            text.Span($"Paso {step.StepNumber}").SemiBold();
                            if (step.IsCritical) text.Span("  Critico").FontColor("#B42318").SemiBold();
                            if (step.EstimatedMinutes.HasValue) text.Span($"  {step.EstimatedMinutes} min").FontColor("#667085");
                            if (!string.IsNullOrWhiteSpace(step.Temperature)) text.Span($"  {step.Temperature}").FontColor("#667085");
                        });
                        stepColumn.Item().Text(step.Instructions);
                    });
                }
            }

            if (!string.IsNullOrWhiteSpace(preparation.Presentation))
            {
                column.Item().Element(c => NoteBox(c, "Presentacion", preparation.Presentation!));
            }

            if (!string.IsNullOrWhiteSpace(preparation.Notes))
            {
                column.Item().Element(c => NoteBox(c, "Notas internas", preparation.Notes!));
            }
        });
    }

    private static void SectionTitle(IContainer container, string title)
    {
        container.Background("#F2F4F7").PaddingVertical(5).PaddingHorizontal(7).Text(title).SemiBold().FontColor("#344054");
    }

    private static void KeyValue(IContainer container, string key, string value)
    {
        container.Row(row =>
        {
            row.ConstantItem(88).Text(key).FontColor("#667085");
            row.RelativeItem().Text(value).SemiBold();
        });
    }

    private static void Empty(IContainer container, string text)
    {
        container.Border(1).BorderColor("#EAECF0").Padding(8).Text(text).FontColor("#98A2B3");
    }

    private static void NoteBox(IContainer container, string title, string text)
    {
        container.Border(1).BorderColor("#EAECF0").Padding(8).Column(column =>
        {
            column.Spacing(3);
            column.Item().Text(title).SemiBold();
            column.Item().Text(text);
        });
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.BorderBottom(1).BorderColor("#D0D5DD").Background("#F9FAFB").Padding(5).DefaultTextStyle(x => x.SemiBold());

    private static IContainer Cell(IContainer container) =>
        container.BorderBottom(1).BorderColor("#EAECF0").Padding(5);

    private static IContainer SubCell(IContainer container) =>
        container.BorderBottom(1).BorderColor("#F2F4F7").Background("#FCFCFD").Padding(5).DefaultTextStyle(x => x.FontColor("#475467"));

    private static string FormatQty(decimal value) =>
        value.ToString("0.####", CultureInfo.InvariantCulture);
}
