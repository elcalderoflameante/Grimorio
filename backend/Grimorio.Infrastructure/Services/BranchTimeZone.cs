namespace Grimorio.Infrastructure.Services;

public static class BranchTimeZone
{
    public const string DefaultTimeZoneId = "America/Guayaquil";

    public static DateTime FromUtc(DateTime utcDateTime, string? timeZoneId)
    {
        var zone = Resolve(timeZoneId);
        return DateTime.SpecifyKind(
            TimeZoneInfo.ConvertTimeFromUtc(utcDateTime.ToUniversalTime(), zone),
            DateTimeKind.Unspecified);
    }

    public static DateOnly DateFromUtc(DateTime utcDateTime, string? timeZoneId) =>
        DateOnly.FromDateTime(FromUtc(utcDateTime, timeZoneId));

    public static bool IsValid(string? timeZoneId)
    {
        try
        {
            _ = Resolve(timeZoneId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static TimeZoneInfo Resolve(string? timeZoneId)
    {
        var id = string.IsNullOrWhiteSpace(timeZoneId) ? DefaultTimeZoneId : timeZoneId.Trim();

        if (TryFind(id, out var zone)) return zone;

        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out var windowsId) && TryFind(windowsId, out zone))
            return zone;

        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(id, out var ianaId) && TryFind(ianaId, out zone))
            return zone;

        throw new TimeZoneNotFoundException($"Zona horaria no valida: {id}");
    }

    private static bool TryFind(string id, out TimeZoneInfo zone)
    {
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch
        {
            zone = null!;
            return false;
        }
    }
}
