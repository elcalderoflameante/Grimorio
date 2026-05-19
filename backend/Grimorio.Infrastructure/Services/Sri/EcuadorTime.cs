namespace Grimorio.Infrastructure.Services.Sri;

// Ecuador es UTC-5 permanente, sin horario de verano (DST)
public static class EcuadorTime
{
    private static readonly TimeSpan Offset = TimeSpan.FromHours(-5);

    public static DateTime FromUtc(DateTime utcDateTime) =>
        DateTime.SpecifyKind(utcDateTime.ToUniversalTime() + Offset, DateTimeKind.Unspecified);
}
