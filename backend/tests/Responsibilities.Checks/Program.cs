using System.Text.Json;
using Grimorio.Application.Abstractions;
using Grimorio.Application.DTOs;
using Grimorio.Domain.Entities.Auth;
using Grimorio.Domain.Entities.Operations;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Domain.Entities.Scheduling;
using Grimorio.Infrastructure.Features.Operations;
using Grimorio.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var count = 0;
void Check(bool value, string message) { if (!value) throw new Exception(message); count++; }
async Task Rejected(Func<Task> action, string label) { try { await action(); } catch (InvalidOperationException) { count++; return; } throw new Exception($"Expected rejection: {label}"); }
var anchor = new DateOnly(2026, 10, 19); // Monday
var rule = new ResponsibilityTask { AnchorDate = anchor, Frequency = ResponsibilityFrequency.Weekly, DaysOfWeekMask = 1 };
Check(ResponsibilitySchedule.IsDue(rule, anchor) && !ResponsibilitySchedule.IsDue(rule, anchor.AddDays(1)) && ResponsibilitySchedule.IsDue(rule, anchor.AddDays(7)), "Weekly days");
rule.Frequency = ResponsibilityFrequency.Fortnightly; rule.DaysOfWeekMask = 127;
Check(!ResponsibilitySchedule.IsDue(rule, anchor.AddDays(7)) && ResponsibilitySchedule.IsDue(rule, anchor.AddDays(14)), "Fortnightly anchor");
Check(!ResponsibilitySchedule.IsDue(rule, anchor.AddDays(-14)), "No obligations before the anchor");
rule.Frequency = ResponsibilityFrequency.Monthly; rule.AnchorDate = new(2026, 1, 31);
Check(ResponsibilitySchedule.IsDue(rule, new(2026, 2, 28)) && ResponsibilitySchedule.IsDue(rule, new(2026, 3, 31)) && !ResponsibilitySchedule.IsDue(rule, new(2026, 3, 28)), "End of month without drifting");
rule.Frequency = ResponsibilityFrequency.EveryNDays; rule.EveryNDays = 3; rule.AnchorDate = anchor;
Check(ResponsibilitySchedule.IsDue(rule, anchor.AddDays(3)) && !ResponsibilitySchedule.IsDue(rule, anchor.AddDays(4)), "Custom interval");
rule.Stage = ResponsibilityStage.DuringShift; rule.IntervalMinutes = 60;
var start = new DateTime(2026, 10, 21, 22, 0, 0); var end = start.AddHours(4);
Check(ResponsibilitySchedule.Times(rule, start, end).Count() == 4 && ResponsibilitySchedule.Times(rule, start, end).Last().Day == 22, "Overnight recurring checks");
rule.Stage = ResponsibilityStage.Closing; rule.WindowMinutes = 30;
Check(ResponsibilitySchedule.Times(rule, start, end).Single() == end.AddMinutes(-30), "Closing window");
Check(BranchTimeZone.ToUtc(new DateTime(2026, 10, 21, 10, 0, 0), "America/Guayaquil").Hour == 15, "Branch timezone");

if (!args.Contains("--database")) { Console.WriteLine($"{count} scheduling checks passed. Use --database only with the isolated test container."); return; }
var actor = new CheckUser { UserId = Guid.NewGuid() };
await using var db = ChecksDbContextFactory.Create(actor);
await db.Database.MigrateAsync(); // Dedicated test database only; never deletes or connects to production.
var branchId = Guid.NewGuid();
var branch = new Branch { Id = branchId, BranchId = branchId, Name = "Responsibility checks", Code = Guid.NewGuid().ToString("N")[..8], TimeZoneId = "America/Guayaquil" };
var position = new Position { BranchId = branchId, BranchIdParent = branchId, Name = "Mesero" };
var employee = new Employee { BranchId = branchId, PositionId = position.Id, FirstName = "Ana", LastName = "Checks", Email = $"{Guid.NewGuid()}@example.invalid", IdentificationNumber = Guid.NewGuid().ToString("N")[..10] };
var second = new Employee { BranchId = branchId, PositionId = position.Id, FirstName = "Maria", LastName = "Checks", Email = $"{Guid.NewGuid()}@example.invalid", IdentificationNumber = Guid.NewGuid().ToString("N")[..10] };
var firstUser = new User { Id = actor.UserId, BranchId = branchId, EmployeeId = employee.Id, FirstName = "Ana", LastName = "Checks", Email = employee.Email, PasswordHash = "test-only" };
var secondUser = new User { BranchId = branchId, EmployeeId = second.Id, FirstName = "Maria", LastName = "Checks", Email = second.Email, PasswordHash = "test-only" };
var workArea = new WorkArea { BranchId = branchId, Name = "Mesas" };
var role = new WorkRole { BranchId = branchId, WorkAreaId = workArea.Id, Name = "Mesera" };
var localDate = BranchTimeZone.DateFromUtc(DateTime.UtcNow, branch.TimeZoneId);
var shift = new ShiftAssignment { BranchId = branchId, EmployeeId = employee.Id, Date = localDate.ToDateTime(TimeOnly.MinValue), StartTime = TimeSpan.Zero, EndTime = new TimeSpan(23, 59, 0), WorkAreaId = workArea.Id, WorkRoleId = role.Id };
var secondShift = new ShiftAssignment { BranchId = branchId, EmployeeId = second.Id, Date = shift.Date, StartTime = shift.StartTime, EndTime = shift.EndTime, WorkAreaId = workArea.Id, WorkRoleId = role.Id };
db.AddRange(branch, position, employee, second, firstUser, secondUser, workArea, role, shift, secondShift); await db.SaveChangesAsync();
var service = new ResponsibilityService(db, actor);
var area = await service.SaveNamed(branchId, "areas", new() { Name = "Bano" }, default);
var slot = await service.SaveNamed(branchId, "positions", new() { Name = "Mesera 1" }, default);
var assignment = await service.SaveAssignment(branchId, new() { PositionId = slot.Id, EmployeeId = employee.Id, FromDate = localDate, ToDate = localDate.AddDays(6), ShiftStartTime = TimeSpan.Zero }, default);
await Rejected(() => service.SaveAssignment(branchId, new() { PositionId = slot.Id, EmployeeId = second.Id, FromDate = localDate, ToDate = localDate.AddDays(6), ShiftStartTime = TimeSpan.Zero }, default), "Two owners for one position");
await Rejected(() => service.SaveTask(Guid.NewGuid(), new() { AreaId = area.Id, PositionId = slot.Id, Name = "Cross branch", AnchorDate = localDate }, default), "Cross branch references");
var definition = await service.SaveTask(branchId, new() { AreaId = area.Id, PositionId = slot.Id, Name = "Revisar bano", AnchorDate = localDate, PhotoMode = "BeforeAndAfter", Checklist = [new() { Key = "soap", Label = "Jabon", Options = ["Suficiente", "Agotado"], AttentionOptions = ["Agotado"] }] }, default);
await service.Generate(branchId, default); await service.Generate(branchId, default);
Check(await db.ResponsibilityActivities.CountAsync(x => x.BranchId == branchId) == 1, "Repeated generation must not duplicate work");
var activity = await db.ResponsibilityActivities.SingleAsync(x => x.BranchId == branchId);
definition.Name = "Nombre nuevo"; await service.SaveTask(branchId, definition, default);
Check(activity.TaskName == "Revisar bano", "Template edits preserve the activity snapshot");
actor.UserId = secondUser.Id;
await Rejected(() => service.Complete(branchId, activity.Id, new() { IdempotencyKey = Guid.NewGuid() }, default), "Other employee cannot complete");
actor.UserId = firstUser.Id;
await Rejected(() => service.Capture(branchId, activity.Id, "After", default), "Before photo required first");
var cancelled = await service.Capture(branchId, activity.Id, "Before", default);
await service.CancelCapture(branchId, cancelled.Id, default);
await Rejected(() => service.StoreEvidence(branchId, cancelled.Id, cancelled.IssuedAtUtc, "test.webp", "hash", 1, default), "Cancelled camera ticket");
var before = await service.Capture(branchId, activity.Id, "Before", default);
await service.StoreEvidence(branchId, before.Id, before.IssuedAtUtc, "before.webp", "test-hash", 1, default);
await Rejected(() => service.StoreEvidence(branchId, before.Id, before.IssuedAtUtc, "again.webp", "test-hash", 1, default), "Single-use ticket");
var after = await service.Capture(branchId, activity.Id, "After", default);
await service.StoreEvidence(branchId, after.Id, after.IssuedAtUtc, "after.webp", "test-hash", 1, default);
Check(await service.EvidencePath(branchId, after.Id, false, default) == "after.webp", "Owner can view evidence");
actor.UserId = secondUser.Id;
Check(await service.EvidencePath(branchId, after.Id, false, default) == null, "Other employee cannot view private evidence");
actor.UserId = firstUser.Id;
await Rejected(() => service.Complete(branchId, activity.Id, new() { IdempotencyKey = Guid.NewGuid(), Answers = [new("soap", "Agotado")] }, default), "Missing issue status");
var completion = new CompleteResponsibilityDto { IdempotencyKey = Guid.NewGuid(), HasIssue = true, Notes = "No hay reposicion disponible", Answers = [new("soap", "Agotado")] };
var completed = await service.Complete(branchId, activity.Id, completion, default);
var replay = await service.Complete(branchId, activity.Id, completion, default);
Check(completed.Status == "CompletedWithIssue" && replay.CompletedAtUtc == completed.CompletedAtUtc, "Completion retries preserve the original record");
await Rejected(() => service.Transfer(branchId, activity.Id, new(second.Id, "Ausencia"), default), "Completed history cannot be reassigned");
definition.Id = Guid.Empty; definition.Name = "Revisar salon"; definition.PhotoMode = "None"; definition.Checklist = [];
await service.SaveTask(branchId, definition, default); await service.Generate(branchId, default);
var pending = await db.ResponsibilityActivities.SingleAsync(x => x.BranchId == branchId && x.Status == ResponsibilityStatus.Pending);
assignment.EmployeeId = second.Id; await service.SaveAssignment(branchId, assignment, default); await service.Generate(branchId, default);
Check(await db.ResponsibilityActivities.CountAsync(x => x.BranchId == branchId) == 2 && pending.EmployeeId == employee.Id, "Rotation edits do not create a second owner or rewrite prior work");
await service.Transfer(branchId, pending.Id, new(second.Id, "Reemplazo por ausencia"), default);
Check(pending.EmployeeId == second.Id && await db.ResponsibilityTransfers.CountAsync(x => x.ActivityId == pending.Id) == 1, "Explicit transfer keeps one owner and audit history");
Console.WriteLine($"{count} scheduling and isolated PostgreSQL checks passed.");

sealed class CheckUser : ICurrentUserContext { public Guid UserId { get; set; } }
