namespace Grimorio.Domain.Entities.Menu;

public static class MenuItemNames
{
    public static string Format(string? name, string? generalName)
    {
        var thematic = name?.Trim() ?? string.Empty;
        var general = generalName?.Trim() ?? string.Empty;
        if (thematic.Length == 0) return general;
        return general.Length == 0 || string.Equals(thematic, general, StringComparison.OrdinalIgnoreCase)
            ? thematic
            : $"{thematic} - {general}";
    }
}
