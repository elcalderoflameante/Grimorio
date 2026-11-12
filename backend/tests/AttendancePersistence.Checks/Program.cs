using Grimorio.API.Services;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Attendance.Commands;
using Grimorio.Application.Features.Attendance.Queries;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Domain.Entities.Scheduling;
using Grimorio.Domain.Enums;
using Grimorio.Infrastructure.Features.Attendance;
using Grimorio.Infrastructure.Security;
using Grimorio.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var count = 0;
void Check(bool condition, string label) { if (!condition) throw new Exception(label); count++; }
async Task Reject(Func<Task> action, string label)
{
    try { await action(); } catch (InvalidOperationException) { count++; return; }
    throw new Exception("Expected rejection: " + label);
}
Check(Math.Abs(SFaceBiometricService.CosineSimilarity([1, 0], [1, 0]) - 1) < 1e-6,
    "Same embedding has similarity one");
Check(SFaceBiometricService.CosineSimilarity([1, 0], [0, 1]) == 0,
    "Orthogonal embeddings do not match");
Check(SFaceBiometricService.CosineSimilarity([1, 0], [-1, 0]) == -1,
    "Opposite embeddings do not match");
foreach (var invalid in new float[][] { [], [1], [0, 0], [float.NaN, 0], [float.PositiveInfinity, 0] })
{
    try { SFaceBiometricService.CosineSimilarity(invalid, [1, 0]); }
    catch (ArgumentException) { count++; continue; }
    throw new Exception("Invalid biometric embedding must fail closed");
}
var beforeMidnight = AttendanceWorkDatePolicy.GetWindow(
    new DateTime(2026, 11, 11, 4, 59, 0, DateTimeKind.Utc), "America/Guayaquil");
Check(beforeMidnight.Today == new DateOnly(2026, 11, 10) && beforeMidnight.CarryoverDate is null,
    "Previous work date is not carried before local midnight");
var atGraceLimit = AttendanceWorkDatePolicy.GetWindow(
    new DateTime(2026, 11, 11, 5, 30, 0, DateTimeKind.Utc), "America/Guayaquil");
Check(atGraceLimit.Today == new DateOnly(2026, 11, 11) &&
    atGraceLimit.CarryoverDate == new DateOnly(2026, 11, 10),
    "Previous open shift remains active through 00:30 local time");
var afterGraceLimit = AttendanceWorkDatePolicy.GetWindow(
    new DateTime(2026, 11, 11, 5, 30, 1, DateTimeKind.Utc), "America/Guayaquil");
Check(afterGraceLimit.Today == new DateOnly(2026, 11, 11) && afterGraceLimit.CarryoverDate is null,
    "Previous open shift becomes administrative after 00:30 local time");
await using var db = ChecksDbContextFactory.Create();
await db.Database.MigrateAsync();
var branch = new Branch { Name = "Attendance checks", Code = Guid.NewGuid().ToString("N")[..8] };
branch.BranchId = branch.Id;
var position = new Position { BranchId = branch.Id, BranchIdParent = branch.Id, Name = "Checks" };
var employee = new Employee { BranchId = branch.Id, PositionId = position.Id, FirstName = "Test", LastName = "Attendance",
    Email = Guid.NewGuid() + "@example.invalid", IdentificationNumber = Guid.NewGuid().ToString("N")[..10] };
db.AddRange(branch, position, employee); await db.SaveChangesAsync();
AttendanceHandlers Handler(Grimorio.Infrastructure.Persistence.GrimorioDbContext context) =>
    new(context, new PasswordHashingService(), new SFaceBiometricService(), new EphemeralDataProtectionProvider());
CreateManualAttendanceCommand Manual(DateTime start, DateTime? end = null) => new() {
    EmployeeId = employee.Id, BranchId = branch.Id, CreatedByUserId = Guid.NewGuid(),
    ClockInTimeUtc = start, ClockOutTimeUtc = end, Reason = "Isolated database check" };
var day = DateTime.SpecifyKind(new DateTime(2026, 10, 20, 15, 0, 0), DateTimeKind.Utc);
var scheduledEmployee = new Employee { BranchId = branch.Id, PositionId = position.Id, FirstName = "Scheduled", LastName = "Attendance",
    Email = Guid.NewGuid() + "@example.invalid", IdentificationNumber = Guid.NewGuid().ToString("N")[..10] };
var workArea = new WorkArea { BranchId = branch.Id, Name = "Attendance area" };
var workRole = new WorkRole { BranchId = branch.Id, WorkAreaId = workArea.Id, Name = "Attendance role" };
var scheduledWorkDate = new DateOnly(2026, 10, 20);
var scheduledShift = new ShiftAssignment { BranchId = branch.Id, EmployeeId = scheduledEmployee.Id,
    Date = scheduledWorkDate.ToDateTime(TimeOnly.MinValue), StartTime = new TimeSpan(14, 0, 0),
    EndTime = new TimeSpan(22, 30, 0), WorkAreaId = workArea.Id, WorkRoleId = workRole.Id };
db.AddRange(scheduledEmployee, workArea, workRole, scheduledShift); await db.SaveChangesAsync();
var scheduledResult = await Handler(db).Handle(new CreateManualAttendanceCommand {
    EmployeeId = scheduledEmployee.Id, BranchId = branch.Id, CreatedByUserId = Guid.NewGuid(),
    ClockInTimeUtc = BranchTimeZone.ToUtc(scheduledWorkDate.ToDateTime(new TimeOnly(13, 55)), branch.TimeZoneId),
    BreakStartedAtUtc = BranchTimeZone.ToUtc(scheduledWorkDate.ToDateTime(new TimeOnly(17, 0)), branch.TimeZoneId),
    BreakEndedAtUtc = BranchTimeZone.ToUtc(scheduledWorkDate.ToDateTime(new TimeOnly(17, 30)), branch.TimeZoneId),
    ClockOutTimeUtc = BranchTimeZone.ToUtc(scheduledWorkDate.ToDateTime(new TimeOnly(22, 30)), branch.TimeZoneId),
    Reason = "Early arrival calculation check"
}, default);
Check(scheduledResult.EarlyArrivalMinutes == 5 && scheduledResult.WorkedMinutes == 480 && scheduledResult.OvertimeMinutes == 0,
    "Early arrival is recorded but does not count as worked time or overtime");
var scheduledStored = await db.EmployeeClockings.SingleAsync(x => x.Id == scheduledResult.Id);
scheduledStored.WorkedMinutes = 485; scheduledStored.OvertimeMinutes = 5; await db.SaveChangesAsync();
var normalizedHistorical = await Handler(db).Handle(new GetAttendanceAdminRowsQuery {
    BranchId = branch.Id, FromDate = scheduledWorkDate, ToDate = scheduledWorkDate
}, default);
var normalizedScheduled = normalizedHistorical.Single(x => x.Id == scheduledResult.Id);
Check(normalizedScheduled.WorkedMinutes == 480 && normalizedScheduled.OvertimeMinutes == 0,
    "Historical attendance rows are normalized when queried");
var first = await Handler(db).Handle(Manual(day), default);
await Reject(() => Handler(db).Handle(Manual(day.AddDays(1)), default), "Two open shifts");
await Reject(() => Handler(db).Handle(Manual(day.AddHours(-2), day.AddHours(2)), default), "Duplicate day");
await Handler(db).Handle(new CorrectAttendanceCommand { EmployeeClockingId = first.Id, BranchId = branch.Id,
    CorrectedByUserId = Guid.NewGuid(), ClockInTimeUtc = day, ClockOutTimeUtc = day.AddHours(8), Reason = "Close for checks" }, default);
var next = await Handler(db).Handle(Manual(day.AddDays(1)), default);
await Reject(() => Handler(db).Handle(new CorrectAttendanceCommand { EmployeeClockingId = first.Id, BranchId = branch.Id,
    CorrectedByUserId = Guid.NewGuid(), ClockInTimeUtc = day, Reason = "Attempt to reopen" }, default), "Reopening while another shift is open");
Check(next.Status == AttendanceStatus.Working, "Next shift can open after close");
// A historical closed interval cannot cross into the currently open shift.
await Reject(() => Handler(db).Handle(Manual(day.AddDays(-1), day.AddDays(2)), default), "Cross-date overlap");

var tickets = new AttendanceRecognitionTickets();
var kiosk = Guid.NewGuid();
var token = tickets.Issue(kiosk, employee.Id);
var id = Guid.NewGuid();
var executed = 0;
async Task<AttendanceStatusDto> Execute(bool fail = false, Guid? operationId = null, string? suppliedToken = null, string action = "clock-out")
{
    await using var context = ChecksDbContextFactory.Create();
    var executor = new AttendanceOperationExecutor(context, tickets);
    return await executor.ExecuteAsync(operationId ?? id, branch.Id, kiosk, employee.Id, action, suppliedToken ?? token, null, async ct => {
        Interlocked.Increment(ref executed);
        // Persist a state change before the simulated error to verify transaction rollback.
        var shift = await context.EmployeeClockings.SingleAsync(x => x.Id == next.Id, ct);
        shift.AdministrativeNotes = fail ? "must roll back" : "committed exactly once";
        await context.SaveChangesAsync(ct);
        if (fail) throw new InvalidOperationException("simulated failure after save");
        return new AttendanceStatusDto { EmployeeId = employee.Id, EmployeeName = "Test Attendance", WorkDate = next.WorkDate, Status = AttendanceStatus.Completed };
    }, default);
}
var concurrent = await Task.WhenAll(Execute(), Execute());
Check(executed == 1 && concurrent.All(x => x.Status == AttendanceStatus.Completed), "Concurrent retries execute once");
// Mimic a lost HTTP response and API restart: no original recognition ticket remains in memory.
await using (var fresh = ChecksDbContextFactory.Create()) {
    var recovered = await new AttendanceOperationExecutor(fresh, new AttendanceRecognitionTickets()).ExecuteAsync(id, branch.Id,
        kiosk, employee.Id, "clock-out", token, null, _ => throw new Exception("must not run"), default);
    Check(recovered.EmployeeId == employee.Id && recovered.Status == AttendanceStatus.Completed, "Receipt survives restart and lost response");
}
await Reject(() => Execute(action: "break/start"), "Operation cannot change action");
var failedId = Guid.NewGuid(); var failedToken = tickets.Issue(kiosk, employee.Id);
await Reject(() => Execute(true, failedId, failedToken), "Rollback failed operation");
await using (var fresh = ChecksDbContextFactory.Create()) {
    Check(!await fresh.AttendanceKioskOperations.AnyAsync(x => x.Id == failedId), "No success receipt on failure");
    Check((await fresh.EmployeeClockings.SingleAsync(x => x.Id == next.Id)).AdministrativeNotes == "committed exactly once", "Clocking change rolled back with receipt");
}
var raceEmployee = new Employee { BranchId = branch.Id, PositionId = position.Id, FirstName = "Race", LastName = "Checks",
    Email = Guid.NewGuid() + "@example.invalid", IdentificationNumber = Guid.NewGuid().ToString("N")[..10] };
db.Employees.Add(raceEmployee); await db.SaveChangesAsync();
async Task<bool> OpenConcurrent(int offset) {
    await using var context = ChecksDbContextFactory.Create();
    var command = Manual(day.AddDays(offset)); command.EmployeeId = raceEmployee.Id;
    try { await Handler(context).Handle(command, default); return true; }
    catch (InvalidOperationException) { return false; }
}
var races = await Task.WhenAll(OpenConcurrent(0), OpenConcurrent(1));
Check(races.Count(x => x) == 1, "Concurrent manual shifts allow only one open shift");
var journeyEmployee = new Employee { BranchId = branch.Id, PositionId = position.Id, FirstName = "Journey", LastName = "Checks",
    Email = Guid.NewGuid() + "@example.invalid", IdentificationNumber = Guid.NewGuid().ToString("N")[..10] };
var device = new AttendanceKioskDevice { BranchId = branch.Id, Name = "Test kiosk", DeviceIdentifier = Guid.NewGuid().ToString(),
    Status = KioskDeviceStatus.Active, ApiKeyHash = "test-only" };
db.AddRange(journeyEmployee, device); await db.SaveChangesAsync();
async Task<AttendanceStatusDto> Mark(KioskAttendanceCommand command, Guid? employeeId = null) {
    await using var context = ChecksDbContextFactory.Create();
    command.EmployeeId = employeeId ?? journeyEmployee.Id; command.KioskDeviceId = device.Id;
    var ticket = tickets.Issue(device.Id, command.EmployeeId);
    return await new AttendanceOperationExecutor(context, tickets).ExecuteAsync(Guid.NewGuid(), branch.Id,
        device.Id, command.EmployeeId, command.GetType().Name, ticket, null, ct => command switch {
            ClockInCommand c => Handler(context).Handle(c, ct),
            StartBreakCommand c => Handler(context).Handle(c, ct),
            EndBreakCommand c => Handler(context).Handle(c, ct),
            ClockOutCommand c => Handler(context).Handle(c, ct),
            _ => throw new Exception("Unsupported check command") }, default);
}
var staleEmployee = new Employee { BranchId = branch.Id, PositionId = position.Id, FirstName = "Stale", LastName = "Checks",
    Email = Guid.NewGuid() + "@example.invalid", IdentificationNumber = Guid.NewGuid().ToString("N")[..10] };
var currentWindow = AttendanceWorkDatePolicy.GetWindow(DateTime.UtcNow, branch.TimeZoneId);
var staleDate = currentWindow.Today.AddDays(-2);
var staleClocking = new EmployeeClocking { BranchId = branch.Id, EmployeeId = staleEmployee.Id, WorkDate = staleDate,
    ClockInTimeUtc = BranchTimeZone.ToUtc(staleDate.ToDateTime(new TimeOnly(14, 0)), branch.TimeZoneId),
    Status = AttendanceStatus.Working, ClockInMethod = AttendanceMethod.Face, ClockInKioskDeviceId = device.Id };
db.AddRange(staleEmployee, staleClocking); await db.SaveChangesAsync();
var staleToday = await Handler(db).Handle(new GetTodayAttendanceStatusQuery {
    EmployeeId = staleEmployee.Id, BranchId = branch.Id }, default);
Check(staleToday.Status is null && staleToday.WorkDate == currentWindow.Today,
    "Stale open shift is not returned as today's state");
var entryAfterStale = await Mark(new ClockInCommand(), staleEmployee.Id);
Check(entryAfterStale.WorkDate == currentWindow.Today && entryAfterStale.Status == AttendanceStatus.Working &&
    staleClocking.ClockOutTimeUtc is null,
    "Stale open shift remains for correction and does not block today's entry");
Check((await Mark(new ClockInCommand())).Status == AttendanceStatus.Working, "Biometric entry with durable receipt");
Check((await Mark(new StartBreakCommand())).Status == AttendanceStatus.OnBreak, "Break starts with durable receipt");
await Reject(() => Mark(new StartBreakCommand()), "Second break is rejected");
var closed = await Mark(new ClockOutCommand());
Check(closed.Status == AttendanceStatus.Completed && closed.BreakEndedAtUtc == closed.ClockOutTimeUtc,
    "Exit closes active break in same transaction");
await Reject(() => Mark(new ClockInCommand()), "One shift per day also applies to biometric entry");
var returningEmployee = new Employee { BranchId = branch.Id, PositionId = position.Id, FirstName = "Return", LastName = "Checks",
    Email = Guid.NewGuid() + "@example.invalid", IdentificationNumber = Guid.NewGuid().ToString("N")[..10] };
db.Add(returningEmployee); await db.SaveChangesAsync();
await Mark(new ClockInCommand(), returningEmployee.Id);
await Mark(new StartBreakCommand(), returningEmployee.Id);
var returned = await Mark(new EndBreakCommand(), returningEmployee.Id);
Check(returned.Status == AttendanceStatus.Working && returned.BreakEndedAtUtc is not null,
    "Return from break restores working status");
await Reject(() => Mark(new EndBreakCommand(), returningEmployee.Id), "Duplicate return is rejected");
await Reject(() => Mark(new StartBreakCommand(), returningEmployee.Id), "Used break cannot be restarted after return");
Check((await Mark(new ClockOutCommand(), returningEmployee.Id)).Status == AttendanceStatus.Completed,
    "Exit after returning from break completes the shift");
Console.WriteLine($"{count} isolated PostgreSQL attendance checks passed.");
