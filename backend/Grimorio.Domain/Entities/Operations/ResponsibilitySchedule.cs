namespace Grimorio.Domain.Entities.Operations;

public static class ResponsibilitySchedule
{
    public static bool IsDue(ResponsibilityTask task, DateOnly date)
    {
        if (!task.IsActive || date < task.AnchorDate) return false;
        var day = ((int)date.DayOfWeek + 6) % 7;
        if ((task.DaysOfWeekMask & (1 << day)) == 0) return false;
        var elapsed = date.DayNumber - task.AnchorDate.DayNumber;
        return task.Frequency switch
        {
            ResponsibilityFrequency.Daily => true,
            ResponsibilityFrequency.Weekly => true, // DaysOfWeekMask selects the weekly days.
            ResponsibilityFrequency.Fortnightly => elapsed % 14 == 0,
            ResponsibilityFrequency.Monthly => date.Day == Math.Min(task.AnchorDate.Day, DateTime.DaysInMonth(date.Year, date.Month)),
            ResponsibilityFrequency.EveryNDays => task.EveryNDays > 0 && elapsed % task.EveryNDays == 0,
            _ => false,
        };
    }

    public static IEnumerable<DateTime> Times(ResponsibilityTask task, DateTime start, DateTime end)
    {
        if (end <= start) throw new InvalidOperationException("El turno debe tener una duracion positiva.");
        if (task.Stage == ResponsibilityStage.Closing) { yield return end.AddMinutes(-task.WindowMinutes) < start ? start : end.AddMinutes(-task.WindowMinutes); yield break; }
        if (task.Stage == ResponsibilityStage.Opening || !task.IntervalMinutes.HasValue) { yield return start; yield break; }
        if (task.IntervalMinutes < 15) throw new InvalidOperationException("El intervalo minimo es 15 minutos.");
        for (var time = start; time < end; time = time.AddMinutes(task.IntervalMinutes.Value)) yield return time;
    }
}
