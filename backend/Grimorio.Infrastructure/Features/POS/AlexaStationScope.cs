using System.Globalization;
using System.Text;
using Grimorio.Domain.Entities.POS;

namespace Grimorio.Infrastructure.Features.POS;

internal static class AlexaStationScope
{
    public static bool IsRepeatable(OrderItem item, IReadOnlyCollection<string>? stationNames)
    {
        return item.Status is OrderItemStatus.Pending or OrderItemStatus.InPreparation &&
            Includes(item, stationNames);
    }

    // Null preserves the original integration; an explicit empty scope permits nothing.
    public static bool Includes(OrderItem item, IReadOnlyCollection<string>? stationNames)
    {
        if (stationNames == null) return true;
        var stationName = Normalize(item.Station?.Name);
        return stationName.Length > 0 &&
            stationNames.Any(name => Normalize(name) == stationName);
    }

    private static string Normalize(string? value)
    {
        var decomposed = (value ?? string.Empty).Trim().Normalize(NormalizationForm.FormD);
        return new string(decomposed
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray()).ToLowerInvariant();
    }
}
