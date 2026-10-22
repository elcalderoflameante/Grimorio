using Grimorio.API.Services;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Attendance.Commands;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Domain.Enums;
using Grimorio.Infrastructure.Features.Attendance;
using Grimorio.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var count = 0;
void Check(bool condition, string label) { if (!condition) throw new Exception(label); count++; }
async Task Reject(Func<Task> action, string label)
{
    try { await action(); } catch (InvalidOperationException) { count++; return; }
    throw new Exception("Expected rejection: " + label);
}
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
async Task<AttendanceStatusDto> Mark(KioskAttendanceCommand command) {
    await using var context = ChecksDbContextFactory.Create();
    command.EmployeeId = journeyEmployee.Id; command.KioskDeviceId = device.Id;
    var ticket = tickets.Issue(device.Id, journeyEmployee.Id);
    return await new AttendanceOperationExecutor(context, tickets).ExecuteAsync(Guid.NewGuid(), branch.Id,
        device.Id, journeyEmployee.Id, command.GetType().Name, ticket, null, ct => command switch {
            ClockInCommand c => Handler(context).Handle(c, ct),
            StartBreakCommand c => Handler(context).Handle(c, ct),
            EndBreakCommand c => Handler(context).Handle(c, ct),
            ClockOutCommand c => Handler(context).Handle(c, ct),
            _ => throw new Exception("Unsupported check command") }, default);
}
Check((await Mark(new ClockInCommand())).Status == AttendanceStatus.Working, "Biometric entry with durable receipt");
Check((await Mark(new StartBreakCommand())).Status == AttendanceStatus.OnBreak, "Break starts with durable receipt");
await Reject(() => Mark(new StartBreakCommand()), "Second break is rejected");
var closed = await Mark(new ClockOutCommand());
Check(closed.Status == AttendanceStatus.Completed && closed.BreakEndedAtUtc == closed.ClockOutTimeUtc,
    "Exit closes active break in same transaction");
await Reject(() => Mark(new ClockInCommand()), "One shift per day also applies to biometric entry");
Console.WriteLine($"{count} isolated PostgreSQL attendance checks passed.");
