using Grimorio.Infrastructure.Services;

namespace Grimorio.Infrastructure.Features.Attendance;

public readonly record struct AttendanceWorkDateWindow(DateOnly Today, DateOnly? CarryoverDate);

public static class AttendanceWorkDatePolicy
{
    public const int PreviousDayGraceMinutes = 30;

    public static AttendanceWorkDateWindow GetWindow(DateTime nowUtc, string? timeZoneId)
    {
        var localNow = BranchTimeZone.FromUtc(nowUtc, timeZoneId);
        var today = DateOnly.FromDateTime(localNow);
        var carryoverDate = localNow.TimeOfDay <= TimeSpan.FromMinutes(PreviousDayGraceMinutes)
            ? today.AddDays(-1)
            : (DateOnly?)null;
        return new AttendanceWorkDateWindow(today, carryoverDate);
    }
}
